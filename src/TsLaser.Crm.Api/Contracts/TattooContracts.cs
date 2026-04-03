using System.ComponentModel.DataAnnotations;

namespace TsLaser.Crm.Api.Contracts;

public sealed class TattooCreateRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    public string? RemovalZone { get; init; }

    public string? CorrectionsCount { get; init; }

    public DateOnly? LastPigmentDate { get; init; }

    public DateOnly? LastLaserDate { get; init; }

    public bool NoLaserBefore { get; init; }

    public string? PreviousRemovalPlace { get; init; }

    public string? DesiredResult { get; init; }
}

public sealed class TattooUpdateRequest
{
    [MaxLength(255)]
    public string? Name { get; init; }

    public string? RemovalZone { get; init; }

    public string? CorrectionsCount { get; init; }

    public DateOnly? LastPigmentDate { get; init; }

    public DateOnly? LastLaserDate { get; init; }

    public bool? NoLaserBefore { get; init; }

    public string? PreviousRemovalPlace { get; init; }

    public string? DesiredResult { get; init; }
}

public sealed class TattooResponse
{
    public int Id { get; init; }

    public int ClientId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? RemovalZone { get; init; }

    public string? CorrectionsCount { get; init; }

    public DateOnly? LastPigmentDate { get; init; }

    public DateOnly? LastLaserDate { get; init; }

    public bool NoLaserBefore { get; init; }

    public string? PreviousRemovalPlace { get; init; }

    public string? DesiredResult { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class TattoosListResponse
{
    public required List<TattooResponse> Tattoos { get; init; }

    public required string ClientName { get; init; }

    public required int ClientId { get; init; }
}
