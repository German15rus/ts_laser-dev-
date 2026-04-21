using Google.Cloud.Firestore;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

public sealed class AppointmentRepository(FirestoreDb db, FirestoreCounterRepository counters, IntakeSubmissionRepository submissionRepo)
{
    private const string Col = "appointments";

    public async Task<List<Appointment>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var fromTs = FirestoreHelper.ToTimestamp(fromUtc);
        var toTs = FirestoreHelper.ToTimestamp(toUtc);

        var snap = await db.Collection(Col)
            .WhereGreaterThanOrEqualTo("start_time", fromTs)
            .WhereLessThanOrEqualTo("start_time", toTs)
            .OrderBy("start_time")
            .GetSnapshotAsync(ct);

        var appointments = snap.Documents.Select(ToEntity).ToList();

        var submissionIds = appointments.Select(a => a.IntakeSubmissionId).Distinct().ToList();
        var submissionsMap = new Dictionary<int, IntakeSubmission>();

        foreach (var sid in submissionIds)
        {
            var sub = await submissionRepo.GetByIdAsync(sid, ct);
            if (sub is not null)
            {
                submissionsMap[sid] = sub;
            }
        }

        foreach (var appt in appointments)
        {
            if (submissionsMap.TryGetValue(appt.IntakeSubmissionId, out var sub))
            {
                appt.IntakeSubmission = sub;
            }
        }

        return appointments;
    }

    public async Task<HashSet<int>> GetAllSubmissionIdsAsync(CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Select("intake_submission_id").GetSnapshotAsync(ct);
        return snap.Documents
            .Select(d => FirestoreHelper.ToInt(d.ToDictionary().GetValueOrDefault("intake_submission_id")))
            .ToHashSet();
    }

    public async Task<bool> ExistsBySubmissionIdAsync(int submissionId, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col)
            .WhereEqualTo("intake_submission_id", (long)submissionId)
            .Limit(1)
            .GetSnapshotAsync(ct);
        return snap.Count > 0;
    }

    public async Task<Appointment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snap = await db.Collection(Col).Document(id.ToString()).GetSnapshotAsync(ct);
        if (!snap.Exists) return null;

        var appointment = ToEntity(snap);
        appointment.IntakeSubmission = (await submissionRepo.GetByIdAsync(appointment.IntakeSubmissionId, ct))!;
        return appointment;
    }

    public async Task<Appointment> CreateAsync(Appointment appointment, CancellationToken ct = default)
    {
        appointment.Id = await counters.NextIdAsync(Col, ct);
        appointment.CreatedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(appointment.Id.ToString()).SetAsync(ToDict(appointment), cancellationToken: ct);
        return appointment;
    }

    public async Task<Appointment> UpdateAsync(Appointment appointment, CancellationToken ct = default)
    {
        appointment.UpdatedAt = DateTime.UtcNow;
        await db.Collection(Col).Document(appointment.Id.ToString()).SetAsync(ToDict(appointment), cancellationToken: ct);
        return appointment;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.Collection(Col).Document(id.ToString()).DeleteAsync(cancellationToken: ct);
    }

    private static Appointment ToEntity(DocumentSnapshot snap)
    {
        var d = snap.ToDictionary();
        return new Appointment
        {
            Id = int.Parse(snap.Id),
            IntakeSubmissionId = FirestoreHelper.ToInt(d.GetValueOrDefault("intake_submission_id")),
            MasterName = FirestoreHelper.ToString(d.GetValueOrDefault("master_name")),
            StartTime = FirestoreHelper.ToDateTime(d.GetValueOrDefault("start_time")),
            DurationMinutes = FirestoreHelper.ToInt(d.GetValueOrDefault("duration_minutes")),
            AppointmentStatus = FirestoreHelper.ToString(d.GetValueOrDefault("appointment_status")),
            CreatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("created_at")),
            UpdatedAt = FirestoreHelper.ToDateTime(d.GetValueOrDefault("updated_at")),
            IntakeSubmission = null!,
        };
    }

    private static Dictionary<string, object?> ToDict(Appointment a) => new()
    {
        ["intake_submission_id"] = (long)a.IntakeSubmissionId,
        ["master_name"] = a.MasterName,
        ["start_time"] = FirestoreHelper.ToTimestamp(a.StartTime),
        ["duration_minutes"] = (long)a.DurationMinutes,
        ["appointment_status"] = a.AppointmentStatus,
        ["created_at"] = FirestoreHelper.ToTimestamp(a.CreatedAt),
        ["updated_at"] = FirestoreHelper.ToTimestamp(a.UpdatedAt),
    };
}
