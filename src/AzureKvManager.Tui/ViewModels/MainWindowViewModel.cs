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

    public string? SelectedVaultName { get; private set; }

    public string? SelectedSecretName { get; private set; }

    public void SelectVault(string vaultName)
    {
        SelectedVaultName = vaultName;
        SelectedSecretName = null;
        Secrets.ClearForVaultSwitch();
        Versions.ClearForSecretSwitch();
        SecretDetails.Clear();
    }

    public void SelectSecret(string secretName)
    {
        SelectedSecretName = secretName;
        Versions.ClearForSecretSwitch();
        SecretDetails.Clear();
    }
}
