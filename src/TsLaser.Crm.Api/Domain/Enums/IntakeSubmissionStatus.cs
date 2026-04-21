namespace TsLaser.Crm.Api.Domain.Enums;

public static class IntakeSubmissionStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Completed = "completed";

    private static readonly HashSet<string> Allowed =
    [
        Pending,
        Approved,
        Rejected,
        Completed,
    ];

    public static bool IsAllowed(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return Allowed.Contains(status.Trim().ToLowerInvariant());
    }

    public static string Normalize(string? status)
    {
        if (!IsAllowed(status))
        {
            return Pending;
        }

        return status!.Trim().ToLowerInvariant();
    }
}
