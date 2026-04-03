using System.ComponentModel.DataAnnotations;

namespace TsLaser.Crm.Api.Contracts;

public sealed class PartnerCreateRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    public string? Contacts { get; init; }

    public string? Type { get; init; }

    public string? Terms { get; init; }

    public string? Comment { get; init; }
}

public sealed class PartnerUpdateRequest
{
    [MaxLength(255)]
    public string? Name { get; init; }

    public string? Contacts { get; init; }

    public string? Type { get; init; }

    public string? Terms { get; init; }

    public string? Comment { get; init; }
}

public sealed class PartnerResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Contacts { get; init; }

    public string? Type { get; init; }

    public string? Terms { get; init; }

    public string? Comment { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
