namespace TsLaser.Crm.Api.Domain.Entities;

public sealed class Client : TimestampedEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public int? Age { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public int? ReferralPartnerId { get; set; }

    public string? ReferralCustom { get; set; }

    public string Status { get; set; } = "active";

    public string? StoppedReason { get; set; }

    public Partner? ReferralPartner { get; set; }

    public ICollection<Tattoo> Tattoos { get; set; } = new List<Tattoo>();

    public ICollection<LaserSession> Sessions { get; set; } = new List<LaserSession>();

    public ICollection<IntakeSubmission> IntakeSubmissions { get; set; } = new List<IntakeSubmission>();
}
