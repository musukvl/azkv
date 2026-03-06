namespace AzureKvManager.Tui.Services;

public static class SecretFormValidator
{
    public static bool TryValidateNewSecret(
        string? name,
        string? value,
        string? expirationDateText,
        out DateTime? expiresAt,
        out string? errorMessage)
    {
        expiresAt = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "Secret name is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Secret value is required";
            return false;
        }

        if (SecretDateService.TryParseExpirationDate(expirationDateText, out expiresAt))
        {
            return true;
        }

        errorMessage = "Expiration date must be in yyyy-MM-dd format";
        return false;
    }

    public static bool TryValidateNewVersion(
        string? value,
        string? expirationDateText,
        out DateTime? expiresAt,
        out string? errorMessage)
    {
        expiresAt = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Secret value is required";
            return false;
        }

        if (SecretDateService.TryParseExpirationDate(expirationDateText, out expiresAt))
        {
            return true;
        }

        errorMessage = "Expiration date must be in yyyy-MM-dd format";
        return false;
    }
}
