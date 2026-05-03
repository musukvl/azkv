using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.Views.Dialogs;

public sealed record AddVersionResult(string Value, string? ContentType, DateTime? ExpiresAt);

public sealed class AddVersionDialog : Dialog<AddVersionResult>
{
    public AddVersionDialog(string secretName)
    {
        Title = $"Add New Version to '{secretName}'";
        Width = Dim.Percent(60);
        Height = Dim.Auto(minimumContentDim: 14);

        var valueLabel = new Label
        {
            Text = "Secret _Value:",
            X = 1,
            Y = 1
        };

        var valueField = new TextView
        {
            X = 1,
            Y = Pos.Bottom(valueLabel),
            Width = Dim.Fill(1),
            Height = 3
        };

        var contentTypeLabel = new Label
        {
            Text = "_Content Type (optional):",
            X = 1,
            Y = Pos.Bottom(valueField) + 1
        };

        var contentTypeField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(contentTypeLabel),
            Width = Dim.Fill(1)
        };

        var expirationDateLabel = new Label
        {
            Text = "_Expiration Date (yyyy-MM-dd, optional):",
            X = 1,
            Y = Pos.Bottom(contentTypeField) + 1
        };

        var expirationDateField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(expirationDateLabel),
            Width = Dim.Fill(1)
        };

        var okButton = new Button { Text = "OK" };
        okButton.Accepting += (s, e) =>
        {
            var value = valueField.Text?.ToString()?.Trim();
            var contentType = contentTypeField.Text?.ToString()?.Trim();
            var expirationDateText = expirationDateField.Text?.ToString();

            if (!SecretFormValidator.TryValidateNewVersion(
                    value,
                    expirationDateText,
                    out var expiresAt,
                    out var errorMessage))
            {
                e.Handled = true;
                MessageBox.ErrorQuery(App!, "Error", errorMessage ?? "Invalid input", "OK");
                return;
            }

            Result = new AddVersionResult(value!, contentType, expiresAt);
            e.Handled = true;
            RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };

        Add(valueLabel, valueField, contentTypeLabel, contentTypeField,
            expirationDateLabel, expirationDateField);

        // AddButton manages layout; last added becomes default
        AddButton(cancelButton);
        AddButton(okButton);
    }
}
