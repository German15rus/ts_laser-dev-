using Google.Cloud.Firestore;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class ClientRepository(FirestoreDb db, FirestoreCounterRepository counters)
{
    private const string Col = "clients";

    public async Task<List<Client>> GetAllAsync(string? statusFilter, int? partnerFilter, string sortBy, string sortOrder, CancellationToken ct = default)
    {
        Query query = db.Collection(Col);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.WhereEqualTo("status", statusFilter);
        }

        if (partnerFilter.HasValue)
        {
            query = query.WhereEqualTo("referral_partner_id", (long)partnerFilter.Value);
        }

        query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
        {
            ("status", "desc") => query.OrderByDescending("status"),
            ("status", _) => query.OrderBy("status"),
            ("created_at", "desc") => query.OrderByDescending("created_at"),
            ("created_at", _) => query.OrderBy("created_at"),
            ("name", "desc") => query.OrderByDescending("name"),
            _ => query.OrderBy("name"),
        };

        var snap = await query.GetSnapshotAsync(ct);
        return snap.Documents.Select(ToEntity).ToList();
    }

    public async Task<Client?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        return snap.Exists ? ToEntity(snap) : null;
    }

    public async Task<Client?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).WhereEqualTo("phone", phone).Limit(1).GetSnapshotAsync(ct);
        return snap.Count > 0 ? ToEntity(snap.Documents[0]) : null;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        return snap.Exists;
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        client.Id = await counters.NextIdAsync(Col, ct);
        client.CreatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(client.Id.ToString()).SetAsync(ToDict(client), cancellationToken: ct);
        return client;
    }

    public async Task<Client> UpdateAsync(Client client, CancellationToken ct = default)
    {
        client.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(client.Id.ToString()).SetAsync(ToDict(client), cancellationToken: ct);
        return client;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.Collection(Col).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    private static Client ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ToDictionary();
        return new Client
        {
            Id = int.Parse(snap.Id),
            Name = FirestoreHelper.ToString(d.GetValueOrDefault("name")),
            Phone = FirestoreHelper.ToNullableString(d.GetValueOrDefault("phone")),
            BirthDate = FirestoreHelper.FromDateString(d.GetValueOrDefault("birth_date")),
            Age = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("age")),
            Gender = FirestoreHelper.ToNullableString(d.GetValueOrDefault("gender")),
            Address = FirestoreHelper.ToNullableString(d.GetValueOrDefault("address")),
            ReferralPartnerId = FirestoreHelper.ToNullableInt(d.GetValueOrDefault("referral_partner_id")),
            ReferralCustom = FirestoreHelper.ToNullableString(d.GetValueOrDefault("referral_custom")),
            Status = FirestoreHelper.ToString(d.GetValueOrDefault("status")),
            StoppedReason = FirestoreHelper.ToNullableString(d.GetValueOrDefault("stopped_reason")),
            CreatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("created_at")),
            UpdatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("updated_at")),
        };
    }

    private static Dictionary<string, object?> ToDict(Client c) => new()
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
    };
}
