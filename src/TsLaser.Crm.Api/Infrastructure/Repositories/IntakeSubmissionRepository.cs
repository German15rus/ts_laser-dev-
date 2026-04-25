using Google.Cloud.Firestore;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class IntakeSubmissionRepository(FirestoreDb db, FirestoreCounterRepository counters)
{
    private const string Col = "intake_submissions";

    public async Task<List<IntakeSubmission>> GetByStatusAsync(string status, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("status", status)
            .OrderByDescending("created_at")
            .GetSnapshotAsync(ct);
        return snap.Documents.Where(HasIntegerId).Select(ToEntity).ToList();
    }

    public async Task<IntakeSubmission?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<List<IntakeSubmission>> GetApprovedNotScheduledAsync(IReadOnlySet<int> scheduledIds, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("status", "approved")
            .OrderByDescending("created_at")
            .GetSnapshotAsync(ct);

        return snap.Documents
            .Where(HasIntegerId)
            .Select(ToEntity)
            .Where(x => !scheduledIds.Contains(x.Id))
            .ToList();
    }

    public async Task<List<IntakeSubmission>> GetApprovedByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("client_id", (long)clientId)
            .WhereEqualTo("status", "approved")
            .GetSnapshotAsync(ct);
        return snap.Documents.Where(HasIntegerId).Select(ToEntity).ToList();
    }

    public async Task<HashSet<int>> GetIdsByStatusAsync(string status, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("status", status)
            .Select("__name__")
            .GetSnapshotAsync(ct);
        return snap.Documents.Where(HasIntegerId).Select(d => int.Parse(d.Id)).ToHashSet();
    }

    public async Task<List<int>> GetAllIdsAsync(CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Select("__name__").GetSnapshotAsync(ct);
        return snap.Documents.Where(HasIntegerId).Select(d => int.Parse(d.Id)).ToList();
    }

    public async Task<IntakeSubmission> CreateAsync(IntakeSubmission submission, CancellationToken ct = default)
    {
        submission.Id = await counters.NextIdAsync(Col, ct);
        submission.CreatedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(submission.Id.ToString()).SetAsync(ToDict(submission), cancellationToken: ct);
        return submission;
    }

    public async Task<IntakeSubmission> UpdateAsync(IntakeSubmission submission, CancellationToken ct = default)
    {
        submission.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(submission.Id.ToString()).SetAsync(ToDict(submission), cancellationToken: ct);
        return submission;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.Collection(Col).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    public static IntakeSubmission ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ToDictionary();
        return new IntakeSubmission
        {
            Id = int.Parse(snap.Id),
            ClientId = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("client_id")),
            TattooId = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("tattoo_id")),
            FullName = FirestoreHelper.ToString(d.GetValueOrDefault("full_name")),
            Gender = FirestoreHelper.ToNullableString(d.GetValueOrDefault("gender")),
            Phone = FirestoreHelper.ToString(d.GetValueOrDefault("phone")),
            BirthDate = FirestoreHelper.FromDateString(d.GetValueOrDefault("birth_date")),
            Address = FirestoreHelper.ToNullableString(d.GetValueOrDefault("address")),
            ReferralSource = FirestoreHelper.ToNullableString(d.GetValueOrDefault("referral_source")),
            TattooType = FirestoreHelper.ToNullableString(d.GetValueOrDefault("tattoo_type")),
            TattooAge = FirestoreHelper.ToNullableString(d.GetValueOrDefault("tattoo_age")),
            CorrectionsInfo = FirestoreHelper.ToNullableString(d.GetValueOrDefault("corrections_info")),
            PreviousRemovalInfo = FirestoreHelper.ToNullableString(d.GetValueOrDefault("previous_removal_info")),
            PreviousRemovalWhere = FirestoreHelper.ToNullableString(d.GetValueOrDefault("previous_removal_where")),
            DesiredResult = FirestoreHelper.ToNullableString(d.GetValueOrDefault("desired_result")),
            Source = FirestoreHelper.ToString(d.GetValueOrDefault("source")),
            Status = FirestoreHelper.ToString(d.GetValueOrDefault("status")),
            IsNewClient = FirestoreHelper.ToBool(d.GetValueOrDefault("is_new_client")),
            ReviewedAt = d.TryGetValue("reviewed_at", out var rat) && rat is Google.Cloud.Firestore.Timestamp rts
                ? rts.ToDateTime()
                : null,
            ReviewedBy = FirestoreHelper.ToNullableString(d.GetValueOrDefault("reviewed_by")),
            RejectionReason = FirestoreHelper.ToNullableString(d.GetValueOrDefault("rejection_reason")),
            ApprovedClientId = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("approved_client_id")),
            ApprovedTattooId = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("approved_tattoo_id")),
            RawPayload = FirestoreHelper.ToNullableString(d.GetValueOrDefault("raw_payload")),
            CreatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("created_at")),
            UpdatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("updated_at")),
        };
    }

    private static bool HasIntegerId(DocumentSnapshot snap) => int.TryParse(snap.Id, out _);

    public static Dictionary<string, object?> ToDict(IntakeSubmission s) => new()
    {
        ["client_id"] = s.ClientId.HasValue ? (object)(long)s.ClientId.Value : null,
        ["tattoo_id"] = s.TattooId.HasValue ? (object)(long)s.TattooId.Value : null,
        ["full_name"] = s.FullName,
        ["gender"] = s.Gender,
        ["phone"] = s.Phone,
        ["birth_date"] = FirestoreHelper.ToDateString(s.BirthDate),
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
        ["reviewed_at"] = s.ReviewedAt.HasValue ? (object)FirestoreHelper.ToTimestamp(s.ReviewedAt.Value) : null,
        ["reviewed_by"] = s.ReviewedBy,
        ["rejection_reason"] = s.RejectionReason,
        ["approved_client_id"] = s.ApprovedClientId.HasValue ? (object)(long)s.ApprovedClientId.Value : null,
        ["approved_tattoo_id"] = s.ApprovedTattooId.HasValue ? (object)(long)s.ApprovedTattooId.Value : null,
        ["raw_payload"] = s.RawPayload,
        ["created_at"] = FirestoreHelper.ToTimestamp(s.CreatedAt),
        ["updated_at"] = FirestoreHelper.ToTimestamp(s.UpdatedAt),
    };
}
