using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Persistence;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class FirestoreService
{
    private readonly FirestoreDb? _db;
    private const string Collection = "intake_submissions";
    private readonly ILogger<FirestoreService> _logger;

    public FirestoreService(IConfiguration configuration, ILogger<FirestoreService> logger)
    {
        _logger = logger;

        var credentialsPath = configuration["Firebase:CredentialsPath"] ?? "firebase-key.json";

        if (!File.Exists(credentialsPath))
        {
            _logger.LogWarning("Firebase credentials file not found at {Path}. Firestore sync is disabled.", credentialsPath);
            return;
        }

        try
        {
            var keyJson = File.ReadAllText(credentialsPath);
            var keyDoc = JsonDocument.Parse(keyJson);
            var projectId = keyDoc.RootElement.GetProperty("project_id").GetString()
                ?? throw new InvalidOperationException("project_id not found in Firebase credentials file.");

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(keyJson));
            var credential = GoogleCredential.FromStream(stream);
            var client = new FirestoreClientBuilder { Credential = credential }.Build();
            _db = FirestoreDb.Create(projectId, client);

            _logger.LogInformation("Firestore initialized for project {ProjectId}.", projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firestore client.");
        }
    }

    public async Task SaveSubmissionAsync(IntakeSubmission submission, CancellationToken ct = default)
    {
        if (_db is null) return;

        try
        {
            var doc = _db.Collection(Collection).Document($"sub_{submission.Id}");
            await doc.SetAsync(ToFirestoreDict(submission), cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save submission {Id} to Firestore.", submission.Id);
        }
    }

    public async Task DeleteSubmissionAsync(int submissionId, CancellationToken ct = default)
    {
        if (_db is null) return;

        try
        {
            var doc = _db.Collection(Collection).Document($"sub_{submissionId}");
            await doc.DeleteAsync(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete submission {Id} from Firestore.", submissionId);
        }
    }

    public async Task RestoreMissingSubmissionsAsync(AppDbContext dbContext, CancellationToken ct = default)
    {
        if (_db is null) return;

        try
        {
            var snapshot = await _db.Collection(Collection).GetSnapshotAsync(ct);
            if (snapshot.Count == 0) return;

            var existingIds = dbContext.IntakeSubmissions.Select(s => s.Id).ToHashSet();
            var restored = 0;

            foreach (var doc in snapshot.Documents)
            {
                if (!doc.Exists) continue;

                var data = doc.ToDictionary();
                var originalId = Convert.ToInt32(data["original_id"]);

                if (existingIds.Contains(originalId)) continue;

                var createdAt = ((Timestamp)data["created_at"]).ToDateTime();
                var updatedAt = ((Timestamp)data["updated_at"]).ToDateTime();
                var birthDateStr = data.GetValueOrDefault("birth_date") as string;

                var parameters = new object?[]
                {
                    originalId,
                    data.GetValueOrDefault("full_name") as string ?? "",
                    data.GetValueOrDefault("gender") as string,
                    data.GetValueOrDefault("phone") as string ?? "",
                    birthDateStr,
                    data.GetValueOrDefault("address") as string,
                    data.GetValueOrDefault("referral_source") as string,
                    data.GetValueOrDefault("tattoo_type") as string,
                    data.GetValueOrDefault("tattoo_age") as string,
                    data.GetValueOrDefault("corrections_info") as string,
                    data.GetValueOrDefault("previous_removal_info") as string,
                    data.GetValueOrDefault("previous_removal_where") as string,
                    data.GetValueOrDefault("desired_result") as string,
                    data.GetValueOrDefault("source") as string ?? "landing",
                    data.GetValueOrDefault("status") as string ?? "pending",
                    Convert.ToBoolean(data.GetValueOrDefault("is_new_client") ?? false),
                    data.GetValueOrDefault("raw_payload") as string,
                    createdAt,
                    updatedAt,
                };

                await dbContext.Database.ExecuteSqlRawAsync(
                    @"INSERT OR IGNORE INTO intake_submissions
                        (id, full_name, gender, phone, birth_date, address, referral_source,
                         tattoo_type, tattoo_age, corrections_info, previous_removal_info,
                         previous_removal_where, desired_result, source, status, is_new_client,
                         raw_payload, created_at, updated_at)
                      VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18})",
                    parameters.Cast<object>(),
                    ct);

                restored++;
                _logger.LogInformation("Restored submission {Id} from Firestore.", originalId);
            }

            if (restored > 0)
                _logger.LogInformation("Restored {Count} submission(s) from Firestore.", restored);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore submissions from Firestore.");
        }
    }

    private static Dictionary<string, object?> ToFirestoreDict(IntakeSubmission s) => new()
    {
        ["original_id"] = s.Id,
        ["full_name"] = s.FullName,
        ["gender"] = s.Gender,
        ["phone"] = s.Phone,
        ["birth_date"] = s.BirthDate?.ToString("yyyy-MM-dd"),
        ["address"] = s.Address,
        ["referral_source"] = s.ReferralSource,
        ["tattoo_type"] = s.TattooType,
        ["tattoo_age"] = s.TattooAge,
        ["corrections_info"] = s.CorrectionsInfo,
        ["previous_removal_info"] = s.PreviousRemovalInfo,
        ["previous_removal_where"] = s.PreviousRemovalWhere,
        ["desired_result"] = s.DesiredResult,
        ["source"] = s.Source,
        ["status"] = s.Status,
        ["is_new_client"] = s.IsNewClient,
        ["raw_payload"] = s.RawPayload,
        ["created_at"] = Timestamp.FromDateTime(DateTime.SpecifyKind(s.CreatedAt, DateTimeKind.Utc)),
        ["updated_at"] = Timestamp.FromDateTime(DateTime.SpecifyKind(s.UpdatedAt, DateTimeKind.Utc)),
    };
}
