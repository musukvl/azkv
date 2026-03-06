using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(IAzureKeyVaultDataService dataService)
    {
        KeyVaults = new KeyVaultsViewModel(dataService);
        Secrets = new SecretsViewModel(dataService);
        Versions = new VersionsViewModel(dataService);
        SecretDetails = new SecretDetailsViewModel(dataService);
    }

    public KeyVaultsViewModel KeyVaults { get; }

    public SecretsViewModel Secrets { get; }

    public VersionsViewModel Versions { get; }

    public SecretDetailsViewModel SecretDetails { get; }

    public void ClearFromVaultSelection(string? vaultName = null)
    {
        Secrets.ClearForVaultSwitch(vaultName);
        Versions.ClearForSecretSwitch();
        SecretDetails.Clear();
    }

    public void ClearFromSecretSelection(string vaultName, string secretName)
    {
        Versions.ClearForSecretSwitch(vaultName, secretName);
        SecretDetails.Clear();
    }
}
