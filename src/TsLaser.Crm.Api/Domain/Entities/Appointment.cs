namespace TsLaser.Crm.Api.Domain.Entities;

public sealed class Appointment : TimestampedEntity
{
    public int Id { get; set; }

    public int IntakeSubmissionId { get; set; }

    public IntakeSubmission IntakeSubmission { get; set; } = null!;

    public string MasterName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public int DurationMinutes { get; set; }

    public string AppointmentStatus { get; set; } = "waiting";
}
