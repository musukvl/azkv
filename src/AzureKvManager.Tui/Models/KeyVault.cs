namespace AzureKvManager.Tui.Models;

public class KeyVault
{
    public string Name { get; set; } = string.Empty;
    public string Subscription { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}
