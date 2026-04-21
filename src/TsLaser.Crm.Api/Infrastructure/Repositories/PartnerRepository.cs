using Google.Cloud.Firestore;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class PartnerRepository(FirestoreDb db, FirestoreCounterRepository counters)
{
    private const string Col = "partners";

    public async Task<List<Partner>> GetAllAsync(string? typeFilter, string sortBy, string sortOrder, CancellationToken ct = default)
    {
        Query query = db.Collection(Col);

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.WhereEqualTo("type", typeFilter);
        }

        query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
        {
            ("created_at", "desc") => query.OrderByDescending("created_at"),
            ("created_at", _) => query.OrderBy("created_at"),
            (_, "desc") => query.OrderByDescending("name"),
            _ => query.OrderBy("name"),
        };

        var snap = await query.GetSnapshotAsync(ct);
        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<Partner?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<Dictionary<int, string>> GetNameMapAsync(CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Select("name").GetSnapshotAsync(ct);
        return snap.Documents.ToDictionary(
            d => int.Parse(d.Id),
            d => d.GetValue<string>("name") ?? string.Empty);
    }

    public async Task<Partner> CreateAsync(Partner partner, CancellationToken ct = default)
    {
        partner.Id = await counters.NextIdAsync(Col, ct);
        partner.CreatedAt = DateTime.UtcNow;
        partner.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(partner.Id.ToString()).SetAsync(ToDict(partner), cancellationToken: ct);
        return partner;
    }

    public async Task<Partner> UpdateAsync(Partner partner, CancellationToken ct = default)
    {
        partner.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(partner.Id.ToString()).SetAsync(ToDict(partner), cancellationToken: ct);
        return partner;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.Collection(Col).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    private static Partner ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ToDictionary();
        return new Partner
        {
            Id = int.Parse(snap.Id),
            Name = FirestoreHelper.ToString(d.GetValueOrDefault("name")),
            Contacts = FirestoreHelper.ToNullableString(d.GetValueOrDefault("contacts")),
            Type = FirestoreHelper.ToNullableString(d.GetValueOrDefault("type")),
            Terms = FirestoreHelper.ToNullableString(d.GetValueOrDefault("terms")),
            Comment = FirestoreHelper.ToNullableString(d.GetValueOrDefault("comment")),
            CreatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("created_at")),
            UpdatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("updated_at")),
        };
    }

    private static Dictionary<string, object?> ToDict(Partner p) => new()
    {
        ["name"] = p.Name,
        ["contacts"] = p.Contacts,
        ["type"] = p.Type,
        ["terms"] = p.Terms,
        ["comment"] = p.Comment,
        ["created_at"] = FirestoreHelper.ToTimestamp(p.CreatedAt),
        ["updated_at"] = FirestoreHelper.ToTimestamp(p.UpdatedAt),
    };
}
