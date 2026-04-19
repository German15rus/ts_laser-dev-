namespace TsLaser.Crm.Api.Domain.Enums;

public static class AppointmentStatus
{
    public const string Waiting = "waiting";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";

    private static readonly HashSet<string> Allowed =
    [
        Waiting,
        InProgress,
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
            return Waiting;
        }

        return status!.Trim().ToLowerInvariant();
    }
}
