using Google.Cloud.Firestore;

namespace TsLaser.Crm.Api.Infrastructure.Repositories;

internal static class FirestoreHelper
{
    public static Timestamp ToTimestamp(DateTime dt) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));

    public static DateTime ToDateTime(object? value) =>
        value is Timestamp ts ? ts.ToDateTime() : DateTime.UtcNow;

    public static string? ToDateString(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd");

    public static DateOnly? FromDateString(object? value) =>
        value is string s && !string.IsNullOrEmpty(s)
            ? DateOnly.ParseExact(s, "yyyy-MM-dd")
            : null;

    public static int? ToNullableInt(object? value) =>
        value is null ? null : Convert.ToInt32(value);

    public static int ToInt(object? value) =>
        value is null ? 0 : Convert.ToInt32(value);

    public static bool ToBool(object? value) =>
        value is bool b ? b : Convert.ToBoolean(value ?? false);

    public static string ToString(object? value) =>
        value as string ?? string.Empty;

    public static string? ToNullableString(object? value) =>
        value as string;
}
