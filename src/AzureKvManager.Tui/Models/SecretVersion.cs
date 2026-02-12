namespace AzureKvManager.Tui.Models;

public class SecretVersion
{
    public string Version { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Updated { get; set; }
    public string? ContentType { get; set; }
    public string? Value { get; set; }
}
