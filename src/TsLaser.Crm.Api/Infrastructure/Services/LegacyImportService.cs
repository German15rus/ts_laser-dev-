using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Persistence;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class LegacyImportService(AppDbContext dbContext, ILogger<LegacyImportService> logger)
{
    public async Task ImportAsync(string legacyDbPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(legacyDbPath) || !File.Exists(legacyDbPath))
        {
            throw new FileNotFoundException("Legacy database file not found", legacyDbPath);
        }

        logger.LogInformation("Starting legacy import from {Path}", legacyDbPath);

        await ResetDatabaseAsync(cancellationToken);

        await using var conn = new SqliteConnection($"Data Source={legacyDbPath}");
        await conn.OpenAsync(cancellationToken);

        if (await TableExistsAsync(conn, "partners", cancellationToken))
        {
            await ImportPartnersAsync(conn, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await TableExistsAsync(conn, "clients", cancellationToken))
        {
            await ImportClientsAsync(conn, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var clientIds = await dbContext.Clients.AsNoTracking().Select(x => x.Id).ToHashSetAsync(cancellationToken);

        if (await TableExistsAsync(conn, "tattoos", cancellationToken))
        {
            await ImportTattoosAsync(conn, clientIds, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var tattooIds = await dbContext.Tattoos.AsNoTracking().Select(x => x.Id).ToHashSetAsync(cancellationToken);

        if (await TableExistsAsync(conn, "laser_sessions", cancellationToken))
        {
            await ImportLaserSessionsAsync(conn, clientIds, tattooIds, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await TableExistsAsync(conn, "intake_submissions", cancellationToken))
        {
            await ImportIntakeSubmissionsAsync(conn, clientIds, tattooIds, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Legacy import completed");
    }

    private async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        dbContext.IntakeSubmissions.RemoveRange(dbContext.IntakeSubmissions);
        dbContext.LaserSessions.RemoveRange(dbContext.LaserSessions);
        dbContext.Tattoos.RemoveRange(dbContext.Tattoos);
        dbContext.Clients.RemoveRange(dbContext.Clients);
        dbContext.Partners.RemoveRange(dbContext.Partners);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportPartnersAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM partners";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            dbContext.Partners.Add(new Partner
            {
                Id = GetInt(reader, "id") ?? 0,
                Name = GetString(reader, "name") ?? string.Empty,
                Contacts = GetString(reader, "contacts"),
                Type = GetString(reader, "type"),
                Terms = GetString(reader, "terms"),
                Comment = GetString(reader, "comment"),
                CreatedAt = GetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTime(reader, "updated_at") ?? DateTime.UtcNow,
            });
        }
    }

    private async Task ImportClientsAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM clients";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            dbContext.Clients.Add(new Client
            {
                Id = GetInt(reader, "id") ?? 0,
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
                CreatedAt = GetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTime(reader, "updated_at") ?? DateTime.UtcNow,
            });
        }
    }

    private async Task ImportTattoosAsync(
        SqliteConnection conn,
        HashSet<int> clientIds,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM tattoos";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var clientId = GetInt(reader, "client_id") ?? 0;
            if (clientId == 0)
            {
                continue;
            }

            EnsureArchiveClient(clientId, clientIds);

            dbContext.Tattoos.Add(new Tattoo
            {
                Id = GetInt(reader, "id") ?? 0,
                ClientId = clientId,
                Name = GetString(reader, "name") ?? string.Empty,
                RemovalZone = GetString(reader, "removal_zone"),
                CorrectionsCount = GetString(reader, "corrections_count"),
                LastPigmentDate = GetDateOnly(reader, "last_pigment_date"),
                LastLaserDate = GetDateOnly(reader, "last_laser_date"),
                NoLaserBefore = GetBool(reader, "no_laser_before") ?? false,
                PreviousRemovalPlace = GetString(reader, "previous_removal_place"),
                DesiredResult = GetString(reader, "desired_result"),
                CreatedAt = GetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTime(reader, "updated_at") ?? DateTime.UtcNow,
            });
        }
    }

    private async Task ImportLaserSessionsAsync(
        SqliteConnection conn,
        HashSet<int> clientIds,
        HashSet<int> tattooIds,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM laser_sessions";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var clientId = GetInt(reader, "client_id") ?? 0;
            EnsureArchiveClient(clientId, clientIds);
            if (!clientIds.Contains(clientId))
            {
                continue;
            }

            var tattooId = GetInt(reader, "tattoo_id");
            if (tattooId.HasValue && !tattooIds.Contains(tattooId.Value))
            {
                tattooId = null;
            }

            dbContext.LaserSessions.Add(new LaserSession
            {
                Id = GetInt(reader, "id") ?? 0,
                ClientId = clientId,
                TattooId = tattooId,
                TattooName = GetString(reader, "tattoo_name"),
                SessionNumber = GetInt(reader, "session_number"),
                SubSession = GetString(reader, "sub_session"),
                Wavelength = GetStringOrNumber(reader, "wavelength"),
                Diameter = GetStringOrNumber(reader, "diameter"),
                Density = GetStringOrNumber(reader, "density"),
                Hertz = GetStringOrNumber(reader, "hertz"),
                FlashesCount = GetInt(reader, "flashes_count"),
                SessionDate = GetDateOnly(reader, "session_date"),
                BreakPeriod = GetString(reader, "break_period"),
                Comment = GetString(reader, "comment"),
                CreatedAt = GetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTime(reader, "updated_at") ?? DateTime.UtcNow,
            });
        }
    }

    private async Task ImportIntakeSubmissionsAsync(
        SqliteConnection conn,
        HashSet<int> clientIds,
        HashSet<int> tattooIds,
        CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM intake_submissions";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var clientId = GetInt(reader, "client_id") ?? 0;
            EnsureArchiveClient(clientId, clientIds);
            if (!clientIds.Contains(clientId))
            {
                continue;
            }

            var tattooId = GetInt(reader, "tattoo_id");
            if (tattooId.HasValue && !tattooIds.Contains(tattooId.Value))
            {
                tattooId = null;
            }

            dbContext.IntakeSubmissions.Add(new IntakeSubmission
            {
                Id = GetInt(reader, "id") ?? 0,
                ClientId = clientId,
                TattooId = tattooId,
                FullName = GetString(reader, "full_name") ?? string.Empty,
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
                IsNewClient = GetBool(reader, "is_new_client") ?? false,
                RawPayload = GetString(reader, "raw_payload"),
                CreatedAt = GetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTime(reader, "updated_at") ?? DateTime.UtcNow,
            });
        }
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = $table";
        cmd.Parameters.AddWithValue("$table", tableName);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private static string? GetString(SqliteDataReader reader, string column)
    {
        var ordinal = GetOrdinal(reader, column);
        if (ordinal is null || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        return reader.GetValue(ordinal.Value).ToString();
    }

    private static string? GetStringOrNumber(SqliteDataReader reader, string column)
    {
        var ordinal = GetOrdinal(reader, column);
        if (ordinal is null || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        return reader.GetValue(ordinal.Value).ToString();
    }

    private static int? GetInt(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool? GetBool(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => null,
        };
    }

    private static DateOnly? GetDateOnly(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var date) ? date : null;
    }

    private static DateTime? GetDateTime(SqliteDataReader reader, string column)
    {
        var value = GetString(reader, column);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var dateTime)
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : null;
    }

    private static int? GetOrdinal(IDataRecord reader, string column)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), column, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    private void EnsureArchiveClient(int clientId, HashSet<int> clientIds)
    {
        if (clientId <= 0 || clientIds.Contains(clientId))
        {
            return;
        }

        dbContext.Clients.Add(new Client
        {
            Id = clientId,
            Name = $"ARCHIVE_CLIENT_{clientId}",
            Status = "lost",
            ReferralCustom = "Imported orphan record",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        clientIds.Add(clientId);
    }
}
