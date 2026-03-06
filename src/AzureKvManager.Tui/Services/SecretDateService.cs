using System.Globalization;

namespace AzureKvManager.Tui.Services;

public static class SecretDateService
{
    public static bool TryParseExpirationDate(string? input, out DateTime? expiresAt)
    {
        expiresAt = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (!DateTime.TryParseExact(
                input.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return false;
        }

        expiresAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        return true;
    }

    public static string BuildExpirationDetailsText(DateTime? expiresAt)
    {
        if (!expiresAt.HasValue)
        {
            return "Expiration: (not set)";
        }

        var formatted = expiresAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return IsExpired(expiresAt.Value)
            ? $"Expiration: [EXPIRED] ({formatted})"
            : $"Expiration: {formatted}";
    }

    private static bool IsExpired(DateTime expiresAt)
    {
        var expiresUtc = expiresAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)
            : expiresAt.ToUniversalTime();

        return expiresUtc < DateTime.UtcNow;
    }
}
