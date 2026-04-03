namespace TsLaser.Crm.Api.Domain.Entities;

public sealed class Tattoo : TimestampedEntity
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? RemovalZone { get; set; }

    public string? CorrectionsCount { get; set; }

    public DateOnly? LastPigmentDate { get; set; }

    public DateOnly? LastLaserDate { get; set; }

    public bool NoLaserBefore { get; set; }

    public string? PreviousRemovalPlace { get; set; }

    public string? DesiredResult { get; set; }

    public Client Client { get; set; } = null!;

    public ICollection<LaserSession> Sessions { get; set; } = new List<LaserSession>();

    public ICollection<IntakeSubmission> IntakeSubmissions { get; set; } = new List<IntakeSubmission>();
}
