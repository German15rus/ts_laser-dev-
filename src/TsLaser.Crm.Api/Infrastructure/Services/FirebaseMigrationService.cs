using Google.Cloud.Firestore;
using Microsoft.Data.Sqlite;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class FirebaseMigrationService(
    FirestoreCounterRepository counters,
    FirestoreDb db,
    ILogger<FirebaseMigrationService> logger)
{
    private const int BatchSize = 400;

    public async Task MigrateAsync(string sqlitePath, CancellationToken ct = default)
    {
        if (!File.Exists(sqlitePath))
        {
            throw new FileNotFoundException("SQLite database file not found", sqlitePath);
        }

        logger.LogInformation("Starting migration from {Path} to Firestore", sqlitePath);

        await using var conn = new SqliteConnection($"Data Source={sqlitePath}");
        await conn.OpenAsync(ct);

        var partnerIds = new HashSet<int>();
        var clientIds = new HashSet<int>();
        var tattooIds = new HashSet<int>();

        if (await TableExistsAsync(conn, "partners", ct))
        {
            partnerIds = await MigratePartnersAsync(conn, ct);
            logger.LogInformation("Migrated {Count} partners", partnerIds.Count);
        }

        if (await TableExistsAsync(conn, "clients", ct))
        {
            clientIds = await MigrateClientsAsync(conn, ct);
            logger.LogInformation("Migrated {Count} clients", clientIds.Count);
        }

        if (await TableExistsAsync(conn, "tattoos", ct))
        {
            tattooIds = await MigrateTattoosAsync(conn, clientIds, ct);
            logger.LogInformation("Migrated {Count} tattoos", tattooIds.Count);
        }

        if (await TableExistsAsync(conn, "laser_sessions", ct))
        {
            var count = await MigrateSessionsAsync(conn, clientIds, tattooIds, ct);
            logger.LogInformation("Migrated {Count} laser sessions", count);
        }

        if (await TableExistsAsync(conn, "intake_submissions", ct))
        {
            var count = await MigrateSubmissionsAsync(conn, clientIds, tattooIds, ct);
            logger.LogInformation("Migrated {Count} intake submissions", count);
        }

        if (await TableExistsAsync(conn, "appointments", ct))
        {
            var count = await MigrateAppointmentsAsync(conn, ct);
            logger.LogInformation("Migrated {Count} appointments", count);
        }

        logger.LogInformation("Migration completed successfully");
    }

    private async Task<HashSet<int>> MigratePartnersAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM partners";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var ids = new HashSet<int>();
        var batch = new List<Partner>();

        while (await reader.ReadAsync(ct))
        {
            var id = GetInt(reader, "id") ?? 0;
            if (id <= 0) continue;
            ids.Add(id);
            batch.Add(new Partner
            {
                Id = id,
                Name = GetString(reader, "name") ?? string.Empty,
                Contacts = GetString(reader, "contacts"),
                Type = GetString(reader, "type"),
                Terms = GetString(reader, "terms"),
                Comment = GetString(reader, "comment"),
                CreatedAt = GetDateTime(reader, "created_at"),
                UpdatedAt = GetDateTime(reader, "updated_at"),
            });

            if (batch.Count >= BatchSize)
            {
                await WriteEntitiesAsync("partners", batch, ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteEntitiesAsync("partners", batch, ct);
        }

        if (ids.Count > 0)
        {
            await counters.EnsureCounterAsync("partners", ids.Max(), ct);
        }

        return ids;
    }

    private async Task<HashSet<int>> MigrateClientsAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM clients";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var ids = new HashSet<int>();
        var batch = new List<Client>();

        while (await reader.ReadAsync(ct))
        {
            var id = GetInt(reader, "id") ?? 0;
            if (id <= 0) continue;
            ids.Add(id);
            batch.Add(new Client
            {
                Id = id,
                Name = GetString(reader, "name") ?? string.Empty,
                Phone = GetString(reader, "phone"),
                BirthDate = GetDateOnly(reader, "birth_date"),
                Age = GetInt(reader, "age"),
                Gender = GetString(reader, "gender"),
                Address = GetString(reader, "address"),
                ReferralPartnerId = GetInt(reader, "referral_partner_id"),
                ReferralCustom = GetString(reader, "referral_custom"),
                Status = (GetString(reader, "status") ?? "active").Trim().ToLowerInvariant(),
                StoppedReason = GetString(reader, "stopped_reason"),
                CreatedAt = GetDateTime(reader, "created_at"),
                UpdatedAt = GetDateTime(reader, "updated_at"),
            });

            if (batch.Count >= BatchSize)
            {
                await WriteEntitiesAsync("clients", batch, ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteEntitiesAsync("clients", batch, ct);
        }

        if (ids.Count > 0)
        {
            await counters.EnsureCounterAsync("clients", ids.Max(), ct);
        }

        return ids;
    }

    private async Task<HashSet<int>> MigrateTattoosAsync(SqliteConnection conn, HashSet<int> clientIds, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM tattoos";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var ids = new HashSet<int>();
        var batch = new List<Tattoo>();
        var archiveClientIds = new HashSet<int>(clientIds);

        while (await reader.ReadAsync(ct))
        {
            var id = GetInt(reader, "id") ?? 0;
            var clientId = GetInt(reader, "client_id") ?? 0;
            if (id <= 0 || clientId <= 0) continue;

            await EnsureArchiveClientInFirestoreAsync(clientId, archiveClientIds, ct);
            ids.Add(id);
            batch.Add(new Tattoo
            {
                Id = id,
                ClientId = clientId,
                Name = GetString(reader, "name") ?? string.Empty,
                RemovalZone = GetString(reader, "removal_zone"),
                CorrectionsCount = GetString(reader, "corrections_count"),
                LastPigmentDate = GetDateOnly(reader, "last_pigment_date"),
                LastLaserDate = GetDateOnly(reader, "last_laser_date"),
                NoLaserBefore = GetBool(reader, "no_laser_before"),
                PreviousRemovalPlace = GetString(reader, "previous_removal_place"),
                DesiredResult = GetString(reader, "desired_result"),
                CreatedAt = GetDateTime(reader, "created_at"),
                UpdatedAt = GetDateTime(reader, "updated_at"),
            });

            if (batch.Count >= BatchSize)
            {
                await WriteEntitiesAsync("tattoos", batch, ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteEntitiesAsync("tattoos", batch, ct);
        }

        if (ids.Count > 0)
        {
            await counters.EnsureCounterAsync("tattoos", ids.Max(), ct);
        }

        return ids;
    }

    private async Task<int> MigrateSessionsAsync(SqliteConnection conn, HashSet<int> clientIds, HashSet<int> tattooIds, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM laser_sessions";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var ids = new HashSet<int>();
        var batch = new List<LaserSession>();
        var archiveClientIds = new HashSet<int>(clientIds);

        while (await reader.ReadAsync(ct))
        {
            var id = GetInt(reader, "id") ?? 0;
            var clientId = GetInt(reader, "client_id") ?? 0;
            if (id <= 0 || clientId <= 0) continue;

            await EnsureArchiveClientInFirestoreAsync(clientId, archiveClientIds, ct);

            var tattooId = GetInt(reader, "tattoo_id");
            if (tattooId.HasValue && !tattooIds.Contains(tattooId.Value)) tattooId = null;

            ids.Add(id);
            batch.Add(new LaserSession
            {
                Id = id,
                ClientId = clientId,
                TattooId = tattooId,
                TattooName = GetString(reader, "tattoo_name"),
                SessionNumber = GetInt(reader, "session_number"),
                SubSession = GetString(reader, "sub_session"),
                Wavelength = GetString(reader, "wavelength"),
                Diameter = GetString(reader, "diameter"),
                Density = GetString(reader, "density"),
                Hertz = GetString(reader, "hertz"),
                FlashesCount = GetInt(reader, "flashes_count"),
                SessionDate = GetDateOnly(reader, "session_date"),
                BreakPeriod = GetString(reader, "break_period"),
                Comment = GetString(reader, "comment"),
                CreatedAt = GetDateTime(reader, "created_at"),
                UpdatedAt = GetDateTime(reader, "updated_at"),
            });

            if (batch.Count >= BatchSize)
            {
                await WriteEntitiesAsync("laser_sessions", batch, ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteEntitiesAsync("laser_sessions", batch, ct);
        }

        if (ids.Count > 0)
        {
            await counters.EnsureCounterAsync("laser_sessions", ids.Max(), ct);
        }

        return ids.Count;
    }

    private async Task<int> MigrateSubmissionsAsync(SqliteConnection conn, HashSet<int> clientIds, HashSet<int> tattooIds, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM intake_submissions";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var ids = new HashSet<int>();
        var batch = new List<IntakeSubmission>();

        while (await reader.ReadAsync(ct))
        {
            var id = GetInt(reader, "id") ?? 0;
            if (id <= 0) continue;

            var clientId = GetInt(reader, "client_id");
            var tattooId = GetInt(reader, "tattoo_id");
            var approvedClientId = GetInt(reader, "approved_client_id");
            var approvedTattooId = GetInt(reader, "approved_tattoo_id");

            if (clientId.HasValue && !clientIds.Contains(clientId.Value)) clientId = null;
            if (tattooId.HasValue && !tattooIds.Contains(tattooId.Value)) tattooId = null;
            if (approvedClientId.HasValue && !clientIds.Contains(approvedClientId.Value)) approvedClientId = null;
            if (approvedTattooId.HasValue && !tattooIds.Contains(approvedTattooId.Value)) approvedTattooId = null;

            ids.Add(id);
            batch.Add(new IntakeSubmission
            {
                Id = id,
                ClientId = clientId,
                TattooId = tattooId,
                FullName = GetString(reader, "full_name") ?? string.Empty,
                Gender = GetString(reader, "gender"),
                Phone = GetString(reader, "phone") ?? string.Empty,
                BirthDate = GetDateOnly(reader, "birth_date"),
                Address = GetString(reader, "address"),
                ReferralSource = GetString(reader, "referral_source"),
                TattooType = GetString(reader, "tattoo_type"),
                TattooAge = GetString(reader, "tattoo_age"),
                CorrectionsInfo = GetString(reader, "corrections_info"),
                PreviousRemovalInfo = GetString(reader, "previous_removal_info"),
                PreviousRemovalWhere = GetString(reader, "previous_removal_where"),
                DesiredResult = GetString(reader, "desired_result"),
                Source = GetString(reader, "source") ?? "landing",
                Status = GetString(reader, "status") ?? "pending",
                IsNewClient = GetBool(reader, "is_new_client"),
                ReviewedAt = GetNullableDateTime(reader, "reviewed_at"),
                ReviewedBy = GetString(reader, "reviewed_by"),
                RejectionReason = GetString(reader, "rejection_reason"),
                ApprovedClientId = approvedClientId,
                ApprovedTattooId = approvedTattooId,
                RawPayload = GetString(reader, "raw_payload"),
                CreatedAt = GetDateTime(reader, "created_at"),
                UpdatedAt = GetDateTime(reader, "updated_at"),
            });

            if (batch.Count >= BatchSize)
            {
                await WriteEntitiesAsync("intake_submissions", batch, ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteEntitiesAsync("intake_submissions", batch, ct);
        }

        if (ids.Count > 0)
        {
            await counters.EnsureCounterAsync("intake_submissions", ids.Max(), ct);
        }

        return ids.Count;
    }

    private async Task<int> MigrateAppointmentsAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM appointments";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var ids = new HashSet<int>();
        var batch = new List<Appointment>();

        while (await reader.ReadAsync(ct))
        {
            var id = GetInt(reader, "id") ?? 0;
            var submissionId = GetInt(reader, "intake_submission_id") ?? 0;
            if (id <= 0 || submissionId <= 0) continue;

            ids.Add(id);
            batch.Add(new Appointment
            {
                Id = id,
                IntakeSubmissionId = submissionId,
                MasterName = GetString(reader, "master_name") ?? string.Empty,
                StartTime = GetDateTime(reader, "start_time"),
                DurationMinutes = GetInt(reader, "duration_minutes") ?? 0,
                AppointmentStatus = GetString(reader, "appointment_status") ?? "waiting",
                CreatedAt = GetDateTime(reader, "created_at"),
                UpdatedAt = GetDateTime(reader, "updated_at"),
                IntakeSubmission = null!,
            });

            if (batch.Count >= BatchSize)
            {
                await WriteEntitiesAsync("appointments", batch, ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteEntitiesAsync("appointments", batch, ct);
        }

        if (ids.Count > 0)
        {
            await counters.EnsureCounterAsync("appointments", ids.Max(), ct);
        }

        return ids.Count;
    }

    private async Task WriteEntitiesAsync<T>(string collection, List<T> entities, CancellationToken ct) where T : class
    {
        var col = db.Collection(collection);
        var batches = entities
            .Select((e, i) => (Entity: e, Index: i))
            .GroupBy(x => x.Index / 400)
            .Select(g => g.Select(x => x.Entity).ToList());

        foreach (var chunk in batches)
        {
            var batch = db.StartBatch();
            foreach (var entity in chunk)
            {
                var (id, dict) = GetIdAndDict(entity);
                batch.Set(col.Document(id.ToString()), dict);
            }
            await batch.CommitAsync(ct);
        }
    }

    private static (int Id, Dictionary<string, object?> Dict) GetIdAndDict<T>(T entity) where T : class
    {
        return entity switch
        {
            Partner p => (p.Id, new Dictionary<string, object?>
            {
                ["name"] = p.Name,
                ["contacts"] = p.Contacts,
                ["type"] = p.Type,
                ["terms"] = p.Terms,
                ["comment"] = p.Comment,
                ["created_at"] = FirestoreHelper.ToTimestamp(p.CreatedAt),
                ["updated_at"] = FirestoreHelper.ToTimestamp(p.UpdatedAt),
            }),
            Client c => (c.Id, new Dictionary<string, object?>
            {
                ["name"] = c.Name,
                ["phone"] = c.Phone,
                ["birth_date"] = FirestoreHelper.ToDateString(c.BirthDate),
                ["age"] = c.Age.HasValue ? (object)(long)c.Age.Value : null,
                ["gender"] = c.Gender,
                ["address"] = c.Address,
                ["referral_partner_id"] = c.ReferralPartnerId.HasValue ? (object)(long)c.ReferralPartnerId.Value : null,
                ["referral_custom"] = c.ReferralCustom,
                ["status"] = c.Status,
                ["stopped_reason"] = c.StoppedReason,
                ["created_at"] = FirestoreHelper.ToTimestamp(c.CreatedAt),
                ["updated_at"] = FirestoreHelper.ToTimestamp(c.UpdatedAt),
            }),
            Tattoo t => (t.Id, new Dictionary<string, object?>
            {
                ["client_id"] = (long)t.ClientId,
                ["name"] = t.Name,
                ["removal_zone"] = t.RemovalZone,
                ["corrections_count"] = t.CorrectionsCount,
                ["last_pigment_date"] = FirestoreHelper.ToDateString(t.LastPigmentDate),
                ["last_laser_date"] = FirestoreHelper.ToDateString(t.LastLaserDate),
                ["no_laser_before"] = t.NoLaserBefore,
                ["previous_removal_place"] = t.PreviousRemovalPlace,
                ["desired_result"] = t.DesiredResult,
                ["created_at"] = FirestoreHelper.ToTimestamp(t.CreatedAt),
                ["updated_at"] = FirestoreHelper.ToTimestamp(t.UpdatedAt),
            }),
            LaserSession s => (s.Id, new Dictionary<string, object?>
            {
                ["client_id"] = (long)s.ClientId,
                ["tattoo_id"] = s.TattooId.HasValue ? (object)(long)s.TattooId.Value : null,
                ["tattoo_name"] = s.TattooName,
                ["session_number"] = s.SessionNumber.HasValue ? (object)(long)s.SessionNumber.Value : null,
                ["sub_session"] = s.SubSession,
                ["wavelength"] = s.Wavelength,
                ["diameter"] = s.Diameter,
                ["density"] = s.Density,
                ["hertz"] = s.Hertz,
                ["flashes_count"] = s.FlashesCount.HasValue ? (object)(long)s.FlashesCount.Value : null,
                ["session_date"] = FirestoreHelper.ToDateString(s.SessionDate),
                ["break_period"] = s.BreakPeriod,
                ["comment"] = s.Comment,
                ["created_at"] = FirestoreHelper.ToTimestamp(s.CreatedAt),
                ["updated_at"] = FirestoreHelper.ToTimestamp(s.UpdatedAt),
            }),
            IntakeSubmission sub => (sub.Id, IntakeSubmissionRepository.ToDict(sub)),
            Appointment a => (a.Id, new Dictionary<string, object?>
            {
                ["intake_submission_id"] = (long)a.IntakeSubmissionId,
                ["master_name"] = a.MasterName,
                ["start_time"] = FirestoreHelper.ToTimestamp(a.StartTime),
                ["duration_minutes"] = (long)a.DurationMinutes,
                ["appointment_status"] = a.AppointmentStatus,
                ["created_at"] = FirestoreHelper.ToTimestamp(a.CreatedAt),
                ["updated_at"] = FirestoreHelper.ToTimestamp(a.UpdatedAt),
            }),
            _ => throw new InvalidOperationException($"Unknown entity type: {typeof(T).Name}"),
        };
    }

    private async Task EnsureArchiveClientInFirestoreAsync(int clientId, HashSet<int> knownIds, CancellationToken ct)
    {
        if (clientId <= 0 || knownIds.Contains(clientId)) return;

        var archiveClient = new Client
        {
            Id = clientId,
            Name = $"ARCHIVE_CLIENT_{clientId}",
            Status = "lost",
            ReferralCustom = "Imported orphan record",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await db.Collection("clients").Document(clientId.ToString()).SetAsync(new Dictionary<string, object?>
        {
            ["name"] = archiveClient.Name,
            ["phone"] = (string?)null,
            ["birth_date"] = (string?)null,
            ["age"] = (object?)null,
            ["gender"] = (string?)null,
            ["address"] = (string?)null,
            ["referral_partner_id"] = (object?)null,
            ["referral_custom"] = archiveClient.ReferralCustom,
            ["status"] = archiveClient.Status,
            ["stopped_reason"] = (string?)null,
            ["created_at"] = FirestoreHelper.ToTimestamp(DateTime.UtcNow),
            ["updated_at"] = FirestoreHelper.ToTimestamp(DateTime.UtcNow),
        }, cancellationToken: ct);

        knownIds.Add(clientId);
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = $table";
        cmd.Parameters.AddWithValue("$table", tableName);
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    private static string? GetString(SqliteDataReader reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase)) continue;
            return reader.IsDBNull(i) ? null : reader.GetValue(i).ToString();
        }
        return null;
    }

    private static int? GetInt(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool GetBool(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        if (bool.TryParse(value, out var b)) return b;
        return value is "1";
    }

    private static DateOnly? GetDateOnly(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        return !string.IsNullOrWhiteSpace(value) && DateOnly.TryParse(value, out var d) ? d : null;
    }

    private static DateTime GetDateTime(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out var dt))
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        return DateTime.UtcNow;
    }

    private static DateTime? GetNullableDateTime(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out var dt))
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        return null;
    }
}
