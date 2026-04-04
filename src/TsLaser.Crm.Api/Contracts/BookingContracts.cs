namespace TsLaser.Crm.Api.Contracts;

public sealed class BookingListItemResponse
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? TattooType { get; init; }

    public string? ReferralSource { get; init; }

    public string Status { get; init; } = "pending";

    public DateTime CreatedAt { get; init; }

    public DateTime? ReviewedAt { get; init; }
}

public sealed class BookingDetailsResponse
{
    public int Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    public string? Address { get; init; }

    public string? ReferralSource { get; init; }

    public string? TattooType { get; init; }

    public string? TattooAge { get; init; }

    public string? CorrectionsInfo { get; init; }

    public string? PreviousRemovalInfo { get; init; }

    public string? PreviousRemovalWhere { get; init; }

    public string? DesiredResult { get; init; }

    public string Status { get; init; } = "pending";

    public string? Source { get; init; }

    public bool IsNewClient { get; init; }

    public string? RejectionReason { get; init; }

    public string? ReviewedBy { get; init; }

    public DateTime? ReviewedAt { get; init; }

    public int? ApprovedClientId { get; init; }

    public int? ApprovedTattooId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

public sealed class BookingRejectRequest
{
    public string? RejectionReason { get; init; }
}

public sealed record BookingModerationResponse(
    bool Success,
    string Message,
    int SubmissionId,
    string Status,
    int? ApprovedClientId,
    int? ApprovedTattooId);
