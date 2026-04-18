namespace TsLaser.Crm.Api.Domain.Entities;

public sealed class IntakeSubmission : TimestampedEntity
{
    public int Id { get; set; }

    public int? ClientId { get; set; }

    public int? TattooId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Gender { get; set; }

    public string Phone { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string? Address { get; set; }

    public string? ReferralSource { get; set; }

    public string? TattooType { get; set; }

    public string? TattooAge { get; set; }

    public string? CorrectionsInfo { get; set; }

    public string? PreviousRemovalInfo { get; set; }

    public string? PreviousRemovalWhere { get; set; }

    public string? DesiredResult { get; set; }

    public string Source { get; set; } = "landing";

    public string Status { get; set; } = "pending";

    public bool IsNewClient { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public string? RejectionReason { get; set; }

    public int? ApprovedClientId { get; set; }

    public int? ApprovedTattooId { get; set; }

    public string? RawPayload { get; set; }

    public Client? Client { get; set; }

    public Tattoo? Tattoo { get; set; }
}
