namespace TsLaser.Crm.Api.Common;

public static class InputCleaner
{
    public static string NormalizePhone(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length == 11 && (digits[0] == '7' || digits[0] == '8'))
        {
            digits = digits[1..];
        }

        if (digits.Length != 10)
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, "Phone must contain 10 digits without +7/8 prefix");
        }

        return digits;
    }

    public static string CleanRequired(string? value, int maxLength = 500)
    {
        var cleaned = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, "One of required fields is missing");
        }

        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }

    public static bool IsNotFilled(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsNegativeAnswer(string? value)
    {
        var lowered = (value ?? string.Empty).Trim().ToLowerInvariant();
        return lowered is "нет" or "no" or "none" or "-";
    }

    public static int? CalculateAge(DateOnly? birthDate)
    {
        if (birthDate is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value.AddYears(age) > today)
            age--;
        return age;
    }
}
