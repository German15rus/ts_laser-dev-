using System.ComponentModel.DataAnnotations;

namespace TsLaser.Crm.Api.Contracts;

public sealed class PublicBookingCreateRequest
{
    [Required, MinLength(2), MaxLength(255)]
    public string FullName { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(50)]
    public string Gender { get; init; } = string.Empty;

    [Required, MinLength(5), MaxLength(32)]
    public string Phone { get; init; } = string.Empty;

    [Required]
    public DateOnly BirthDate { get; init; }

    [Required, MinLength(2), MaxLength(500)]
    public string Address { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(255)]
    public string ReferralSource { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(255)]
    public string TattooType { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(255)]
    public string TattooAge { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(500)]
    public string CorrectionsInfo { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(500)]
    public string PreviousRemovalInfo { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(500)]
    public string PreviousRemovalWhere { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(500)]
    public string DesiredResult { get; init; } = string.Empty;

    [Required]
    public bool ConsentPersonalData { get; init; }

    public long? FormStartedAt { get; init; }

    public string? Website { get; init; }
}

public sealed record PublicBookingResponse(bool Success, string Message, int SubmissionId);
