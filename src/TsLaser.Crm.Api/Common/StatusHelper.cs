namespace TsLaser.Crm.Api.Common;

public static class StatusHelper
{
    private static readonly HashSet<string> Allowed =
    [
        "active",
        "completed",
        "stopped",
        "lost",
        "in_progress",
    ];

    public static string Normalize(string? status)
    {
        var normalized = string.IsNullOrWhiteSpace(status)
            ? "active"
            : status.Trim().ToLowerInvariant();

        return Allowed.Contains(normalized) ? normalized : "active";
    }

    public static string ToLabel(string status) => status switch
    {
        "active" => "В процессе",
        "in_progress" => "В процессе",
        "completed" => "Завершено",
        "stopped" => "Перестал ходить",
        "lost" => "Потерялся",
        _ => status,
    };
}
