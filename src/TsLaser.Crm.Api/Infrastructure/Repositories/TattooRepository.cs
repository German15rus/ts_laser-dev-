using Google.Cloud.Firestore;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class TattooRepository(FirestoreDb db, FirestoreCounterRepository counters)
{
    private const string Col = "tattoos";

    public async Task<List<Tattoo>> GetByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("client_id", (long)clientId)
            .OrderBy("name")
            .GetSnapshotAsync(ct);
        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<Tattoo?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<Tattoo?> GetByClientIdAndNameAsync(int clientId, string name, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("client_id", (long)clientId)
            .WhereEqualTo("name", name)
            .Limit(1)
            .GetSnapshotAsync(ct);
        return snap.Count > 0 ? ToEntity(snap.Documents[0]) : null;
    }

    public async Task<Dictionary<int, string>> GetNameMapByClientIdAsync(int clientId, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("client_id", (long)clientId)
            .Select("name")
            .GetSnapshotAsync(ct);
        return snap.Documents.ToDictionary(
            d => int.Parse(d.Id),
            d => d.GetValue<string>("name") ?? string.Empty);
    }

    public async Task<Tattoo> CreateAsync(Tattoo tattoo, CancellationToken ct = default)
    {
        tattoo.Id = await counters.NextIdAsync(Col, ct);
        tattoo.CreatedAt = DateTime.UtcNow;
        tattoo.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(tattoo.Id.ToString()).SetAsync(ToDict(tattoo), cancellationToken: ct);
        return tattoo;
    }

    public async Task<Tattoo> UpdateAsync(Tattoo tattoo, CancellationToken ct = default)
    {
        tattoo.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(tattoo.Id.ToString()).SetAsync(ToDict(tattoo), cancellationToken: ct);
        return tattoo;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.Collection(Col).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    private static Tattoo ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ToDictionary();
        return new Tattoo
        {
            Id = int.Parse(snap.Id),
            ClientId = FirestoreHelper.ToInt(d.GetValueOrDefault("client_id")),
            Name = FirestoreHelper.ToString(d.GetValueOrDefault("name")),
            RemovalZone = FirestoreHelper.ToNullableString(d.GetValueOrDefault("removal_zone")),
            CorrectionsCount = FirestoreHelper.ToNullableString(d.GetValueOrDefault("corrections_count")),
            LastPigmentDate = FirestoreHelper.FromDateString(d.GetValueOrDefault("last_pigment_date")),
            LastLaserDate = FirestoreHelper.FromDateString(d.GetValueOrDefault("last_laser_date")),
            NoLaserBefore = FirestoreHelper.ToBool(d.GetValueOrDefault("no_laser_before")),
            PreviousRemovalPlace = FirestoreHelper.ToNullableString(d.GetValueOrDefault("previous_removal_place")),
            DesiredResult = FirestoreHelper.ToNullableString(d.GetValueOrDefault("desired_result")),
            CreatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("created_at")),
            UpdatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("updated_at")),
        };
    }

    private static Dictionary<string, object?> ToDict(Tattoo t) => new()
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
    };
}
