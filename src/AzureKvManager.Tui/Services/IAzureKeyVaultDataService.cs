using AzureKvManager.Tui.Models;

namespace AzureKvManager.Tui.Services;

public interface IAzureKeyVaultDataService
{
    Task<List<KeyVault>> GetAllKeyVaultsAsync();
    Task<List<Secret>> GetSecretsAsync(string keyVaultName);
    Task<List<SecretVersion>> GetSecretVersionsAsync(string keyVaultName, string secretName);
    Task<string?> GetSecretValueAsync(string keyVaultName, string secretName, string? version = null);
    Task<SecretVersion?> GetSecretVersionDetailsAsync(string keyVaultName, string secretName, string version);
    Task<bool> SetSecretAsync(string keyVaultName, string secretName, string value, string? contentType = null, DateTime? expires = null);
}
