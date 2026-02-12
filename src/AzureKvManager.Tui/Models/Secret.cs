namespace AzureKvManager.Tui.Models;

public class Secret
{
    public string Name { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public bool Enabled { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Updated { get; set; }
    public string Id { get; set; } = string.Empty;
}
