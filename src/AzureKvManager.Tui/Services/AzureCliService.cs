using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureKvManager.Tui.Models;

namespace AzureKvManager.Tui.Services;

public class AzureCliService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureCliService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<List<KeyVault>> GetAllKeyVaultsAsync()
    {
        var result = await ExecuteAzCliCommandAsync("az keyvault list");
        
        if (string.IsNullOrWhiteSpace(result))
            return [];

        try
        {
            var kvList = JsonSerializer.Deserialize<List<AzKeyVaultDto>>(result, _jsonOptions);
            return kvList?.Select(kv => new KeyVault
            {
                Name = kv.Name ?? string.Empty,
                ResourceGroup = kv.ResourceGroup ?? string.Empty,
                Id = kv.Id ?? string.Empty,
                Subscription = ExtractSubscriptionFromId(kv.Id)
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing key vaults: {ex.Message}");
            return [];
        }
    }

    public async Task<List<Secret>> GetSecretsAsync(string keyVaultName)
    {
        var result = await ExecuteAzCliCommandAsync($"az keyvault secret list --vault-name {keyVaultName}");
        
        if (string.IsNullOrWhiteSpace(result))
            return [];

        try
        {
            var secretList = JsonSerializer.Deserialize<List<AzSecretDto>>(result, _jsonOptions);
            return secretList?.Select(s => new Secret
            {
                Name = s.Name ?? string.Empty,
                ContentType = s.ContentType,
                Enabled = s.Attributes?.Enabled ?? false,
                Created = s.Attributes?.Created,
                Updated = s.Attributes?.Updated,
                Id = s.Id ?? string.Empty
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing secrets: {ex.Message}");
            return [];
        }
    }

    public async Task<List<SecretVersion>> GetSecretVersionsAsync(string keyVaultName, string secretName)
    {
        var result = await ExecuteAzCliCommandAsync($"az keyvault secret list-versions --vault-name {keyVaultName} --name {secretName}");
        
        if (string.IsNullOrWhiteSpace(result))
            return [];

        try
        {
            var versionList = JsonSerializer.Deserialize<List<AzSecretVersionDto>>(result, _jsonOptions);
            return versionList?.Select(v => new SecretVersion
            {
                Version = v.Id?.Split('/').Last() ?? string.Empty,
                Enabled = v.Attributes?.Enabled ?? false,
                Created = v.Attributes?.Created,
                Updated = v.Attributes?.Updated,
                ContentType = v.ContentType
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing secret versions: {ex.Message}");
            return [];
        }
    }

    public async Task<string?> GetSecretValueAsync(string keyVaultName, string secretName, string? version = null)
    {
        var versionParam = string.IsNullOrWhiteSpace(version) ? "" : $" --version {version}";
        var result = await ExecuteAzCliCommandAsync($"az keyvault secret show --vault-name {keyVaultName} --name {secretName}{versionParam}");
        
        if (string.IsNullOrWhiteSpace(result))
            return null;

        try
        {
            var secret = JsonSerializer.Deserialize<AzSecretWithValueDto>(result, _jsonOptions);
            return secret?.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting secret value: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SetSecretAsync(string keyVaultName, string secretName, string value, string? contentType = null)
    {
        var contentTypeParam = string.IsNullOrWhiteSpace(contentType) ? "" : $" --content-type \"{contentType}\"";
        var result = await ExecuteAzCliCommandAsync($"az keyvault secret set --vault-name {keyVaultName} --name {secretName} --value \"{value}\"{contentTypeParam}");
        
        return !string.IsNullOrWhiteSpace(result);
    }

    private async Task<string> ExecuteAzCliCommandAsync(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // For Windows, use cmd instead
            if (OperatingSystem.IsWindows())
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/c {command}";
            }

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Azure CLI error: {error}");
                return string.Empty;
            }

            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing Azure CLI command: {ex.Message}");
            return string.Empty;
        }
    }

    private string ExtractSubscriptionFromId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        var parts = id.Split('/');
        var subIndex = Array.IndexOf(parts, "subscriptions");
        
        return subIndex >= 0 && subIndex + 1 < parts.Length 
            ? parts[subIndex + 1] 
            : string.Empty;
    }

    // DTOs for Azure CLI JSON responses
    private class AzKeyVaultDto
    {
        public string? Name { get; set; }
        public string? ResourceGroup { get; set; }
        public string? Id { get; set; }
    }

    private class AzSecretDto
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public string? ContentType { get; set; }
        public AzAttributesDto? Attributes { get; set; }
    }

    private class AzSecretVersionDto
    {
        public string? Id { get; set; }
        public string? ContentType { get; set; }
        public AzAttributesDto? Attributes { get; set; }
    }

    private class AzSecretWithValueDto
    {
        public string? Value { get; set; }
        public string? ContentType { get; set; }
    }

    private class AzAttributesDto
    {
        public bool Enabled { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
