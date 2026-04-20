using Google.Cloud.Firestore;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class LaserSessionRepository(FirestoreDb db, FirestoreCounterRepository counters)
{
    private const string Col = "laser_sessions";

    public async Task<List<LaserSession>> GetByClientIdAsync(int clientId, string? tattooFilter, string sortBy, string sortOrder, CancellationToken ct = default)
    {
        Query query = db.Collection(Col).WhereEqualTo("client_id", (long)clientId);

        if (!string.IsNullOrWhiteSpace(tattooFilter))
        {
            query = query.WhereEqualTo("tattoo_name", tattooFilter);
        }

        query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
        {
            ("tattoo_name", "desc") => query.OrderByDescending("tattoo_name"),
            ("tattoo_name", _) => query.OrderBy("tattoo_name"),
            ("session_number", "desc") => query.OrderByDescending("session_number"),
            ("session_number", _) => query.OrderBy("session_number"),
            (_, "desc") => query.OrderByDescending("session_date"),
            _ => query.OrderBy("session_date"),
        };

        var snap = await query.GetSnapshotAsync(ct);
        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<List<LaserSession>> GetByClientIdRawAsync(int clientId, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("client_id", (long)clientId)
            .OrderBy("session_date")
            .GetSnapshotAsync(ct);
        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<LaserSession?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<LaserSession> CreateAsync(LaserSession session, CancellationToken ct = default)
    {
        session.Id = await counters.NextIdAsync(Col, ct);
        session.CreatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(session.Id.ToString()).SetAsync(ToDict(session), cancellationToken: ct);
        return session;
    }

    public async Task<LaserSession> UpdateAsync(LaserSession session, CancellationToken ct = default)
    {
        session.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(session.Id.ToString()).SetAsync(ToDict(session), cancellationToken: ct);
        return session;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.Collection(Col).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    private static LaserSession ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ToDictionary();
        return new LaserSession
        {
            Id = int.Parse(snap.Id),
            ClientId = FirestoreHelper.ToInt(d.GetValueOrDefault("client_id")),
            TattooId = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("tattoo_id")),
            TattooName = FirestoreHelper.ToNullableString(d.GetValueOrDefault("tattoo_name")),
            SessionNumber = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("session_number")),
            SubSession = FirestoreHelper.ToNullableString(d.GetValueOrDefault("sub_session")),
            Wavelength = FirestoreHelper.ToNullableString(d.GetValueOrDefault("wavelength")),
            Diameter = FirestoreHelper.ToNullableString(d.GetValueOrDefault("diameter")),
            Density = FirestoreHelper.ToNullableString(d.GetValueOrDefault("density")),
            Hertz = FirestoreHelper.ToNullableString(d.GetValueOrDefault("hertz")),
            FlashesCount = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("flashes_count")),
            SessionDate = FirestoreHelper.FromDateString(d.GetValueOrDefault("session_date")),
            BreakPeriod = FirestoreHelper.ToNullableString(d.GetValueOrDefault("break_period")),
            Comment = FirestoreHelper.ToNullableString(d.GetValueOrDefault("comment")),
            CreatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("created_at")),
            UpdatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("updated_at")),
        };
    }

    private static Dictionary<string, object?> ToDict(LaserSession s) => new()
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
    };
}
