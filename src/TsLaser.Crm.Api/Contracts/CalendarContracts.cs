namespace TsLaser.Crm.Api.Contracts;

public sealed class AppointmentResponse
{
    public int Id { get; init; }

    public int IntakeSubmissionId { get; init; }

    public string ClientName { get; init; } = string.Empty;

    public string? Service { get; init; }

    public string MasterName { get; init; } = string.Empty;

    public DateTime StartTime { get; init; }

    public int DurationMinutes { get; init; }

    public DateTime EndTime { get; init; }

    public string AppointmentStatus { get; init; } = "waiting";

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class AvailableClientResponse
{
    public int SubmissionId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? TattooType { get; init; }

    public string Phone { get; init; } = string.Empty;
}

public sealed class CreateAppointmentRequest
{
    public int IntakeSubmissionId { get; init; }

    public string MasterName { get; init; } = string.Empty;

    public DateTime StartTime { get; init; }

    public int DurationMinutes { get; init; }
}

public sealed class UpdateAppointmentRequest
{
    public DateTime? StartTime { get; init; }

    public int? DurationMinutes { get; init; }

    public string? MasterName { get; init; }

    public string? AppointmentStatus { get; init; }
}
