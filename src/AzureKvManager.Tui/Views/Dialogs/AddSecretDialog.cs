using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.Views.Dialogs;

public sealed record AddSecretResult(string Name, string Value, string? ContentType, DateTime? ExpiresAt);

public sealed class AddSecretDialog : Dialog
{
    public new AddSecretResult? Result { get; private set; }

    public AddSecretDialog(IApplication app)
    {
        Title = "Add New Secret";
        Width = Dim.Percent(60);
        Height = Dim.Auto(minimumContentDim: 16);

        var nameLabel = new Label
        {
            Text = "Secret _Name:",
            X = 1,
            Y = 1
        };

        var nameField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(nameLabel),
            Width = Dim.Fill(1)
        };

        var valueLabel = new Label
        {
            Text = "Secret _Value:",
            X = 1,
            Y = Pos.Bottom(nameField) + 1
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
            var name = nameField.Text?.ToString()?.Trim();
            var value = valueField.Text?.ToString()?.Trim();
            var contentType = contentTypeField.Text?.ToString()?.Trim();
            var expirationDateText = expirationDateField.Text?.ToString();

            if (!SecretFormValidator.TryValidateNewSecret(
                    name,
                    value,
                    expirationDateText,
                    out var expiresAt,
                    out var errorMessage))
            {
                MessageBox.ErrorQuery(app, "Error", errorMessage ?? "Invalid input", "OK");
                return;
            }

            Result = new AddSecretResult(name!, value!, contentType, expiresAt);
            RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => RequestStop();

        Add(nameLabel, nameField, valueLabel, valueField, contentTypeLabel, contentTypeField,
            expirationDateLabel, expirationDateField);

        // AddButton manages layout; last added becomes default
        AddButton(cancelButton);
        AddButton(okButton);
    }
}
