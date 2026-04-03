namespace TsLaser.Crm.Api.Contracts;

public sealed class LaserSessionCreateRequest
{
    public int? TattooId { get; init; }

    public string? TattooName { get; init; }

    public int? SessionNumber { get; init; }

    public string? SubSession { get; init; }

    public string? Wavelength { get; init; }

    public string? Diameter { get; init; }

    public string? Density { get; init; }

    public string? Hertz { get; init; }

    public int? FlashesCount { get; init; }

    public DateOnly? SessionDate { get; init; }

    public string? BreakPeriod { get; init; }

    public string? Comment { get; init; }
}

public sealed class LaserSessionUpdateRequest
{
    public int? TattooId { get; init; }

    public string? TattooName { get; init; }

    public int? SessionNumber { get; init; }

    public string? SubSession { get; init; }

    public string? Wavelength { get; init; }

    public string? Diameter { get; init; }

    public string? Density { get; init; }

    public string? Hertz { get; init; }

    public int? FlashesCount { get; init; }

    public DateOnly? SessionDate { get; init; }

    public string? BreakPeriod { get; init; }

    public string? Comment { get; init; }
}

public sealed class LaserSessionResponse
{
    public int Id { get; init; }

    public int ClientId { get; init; }

    public int? TattooId { get; init; }

    public string? TattooName { get; init; }

    public int? SessionNumber { get; init; }

    public string? SubSession { get; init; }

    public string? Wavelength { get; init; }

    public string? Diameter { get; init; }

    public string? Density { get; init; }

    public string? Hertz { get; init; }

    public int? FlashesCount { get; init; }

    public DateOnly? SessionDate { get; init; }

    public string? BreakPeriod { get; init; }

    public string? Comment { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class LaserSessionsListResponse
{
    public required List<LaserSessionResponse> Sessions { get; init; }

    public required int TotalFlashes { get; init; }

    public required string ClientName { get; init; }

    public required int ClientId { get; init; }
}
