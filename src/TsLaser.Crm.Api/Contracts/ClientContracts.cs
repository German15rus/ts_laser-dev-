using System.ComponentModel.DataAnnotations;

namespace TsLaser.Crm.Api.Contracts;

public sealed class ClientCreateRequest
{
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public DateOnly? BirthDate { get; init; }

    public int? Age { get; init; }

    public string? Gender { get; init; }

    public string? Address { get; init; }

    public int? ReferralPartnerId { get; init; }

    public string? ReferralCustom { get; init; }

    public string Status { get; init; } = "active";

    public string? StoppedReason { get; init; }
}

public sealed class ClientUpdateRequest
{
    [MaxLength(255)]
    public string? Name { get; init; }

    public string? Phone { get; init; }

    public DateOnly? BirthDate { get; init; }

    public int? Age { get; init; }

    public string? Gender { get; init; }

    public string? Address { get; init; }

    public int? ReferralPartnerId { get; init; }

    public string? ReferralCustom { get; init; }

    public string? Status { get; init; }

    public string? StoppedReason { get; init; }
}

public sealed class ClientResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public DateOnly? BirthDate { get; init; }

    public int? Age { get; init; }

    public string? Gender { get; init; }

    public string? Address { get; init; }

    public int? ReferralPartnerId { get; init; }

    public string? ReferralCustom { get; init; }

    public string Status { get; init; } = "active";

    public string? StoppedReason { get; init; }

    public string? ReferralPartnerName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class ClientListResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Address { get; init; }

    public string Status { get; init; } = "active";

    public DateTime CreatedAt { get; init; }
}
