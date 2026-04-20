using Google.Cloud.Firestore;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class FirestoreCounterRepository(FirestoreDb db)
{
    private const string CountersCollection = "_counters";

    public async Task<int> NextIdAsync(string collection, CancellationToken ct = default)
    {
        var docRef = db.Collection(CountersCollection).Document(collection);

        return await db.RunTransactionAsync(async tx =>
        {
            var snap = await tx.GetSnapshotAsync(docRef);
            var current = snap.Exists ? Convert.ToInt32(snap.GetValue<long>("value")) : 0;
            var next = current + 1;
            tx.Set(docRef, new Dictionary<string, object> { ["value"] = (long)next });
            return next;
        }, cancellationToken: ct);
    }

    public async Task EnsureCounterAsync(string collection, int minValue, CancellationToken ct = default)
    {
        var docRef = db.Collection(CountersCollection).Document(collection);
        var snap = await docRef.GetSnapshotAsync(ct);
        var current = snap.Exists ? Convert.ToInt32(snap.GetValue<long>("value")) : 0;
        if (minValue > current)
        {
            await docRef.SetAsync(new Dictionary<string, object> { ["value"] = (long)minValue }, cancellationToken: ct);
        }
    }
}
