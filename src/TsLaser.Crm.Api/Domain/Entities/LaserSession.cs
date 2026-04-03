namespace TsLaser.Crm.Api.Domain.Entities;

public sealed class LaserSession : TimestampedEntity
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public int? TattooId { get; set; }

    public string? TattooName { get; set; }

    public int? SessionNumber { get; set; }

    public string? SubSession { get; set; }

    public string? Wavelength { get; set; }

    public string? Diameter { get; set; }

    public string? Density { get; set; }

    public string? Hertz { get; set; }

    public int? FlashesCount { get; set; }

    public DateOnly? SessionDate { get; set; }

    public string? BreakPeriod { get; set; }

    public string? Comment { get; set; }

    public Client Client { get; set; } = null!;

    public Tattoo? Tattoo { get; set; }
}
