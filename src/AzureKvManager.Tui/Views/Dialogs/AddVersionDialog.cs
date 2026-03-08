using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Services;

namespace AzureKvManager.Tui.Views.Dialogs;

public sealed record AddVersionResult(string Value, string? ContentType, DateTime? ExpiresAt);

public sealed class AddVersionDialog : Dialog
{
    public new AddVersionResult? Result { get; private set; }

    public AddVersionDialog(IApplication app, string secretName)
    {
        Title = $"Add New Version to '{secretName}'";
        Width = Dim.Percent(60);
        Height = Dim.Auto(minimumContentDim: 14);

        var valueLabel = new Label
        {
            Text = "Secret Value:",
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
            Text = "Content Type (optional):",
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
            Text = "Expiration Date (yyyy-MM-dd, optional):",
            X = 1,
            Y = Pos.Bottom(contentTypeField) + 1
        };

        var expirationDateField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(expirationDateLabel),
            Width = Dim.Fill(1)
        };

        var okButton = new Button
        {
            Text = "OK",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(expirationDateField) + 1
        };

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
                MessageBox.ErrorQuery(app, "Error", errorMessage ?? "Invalid input", "OK");
                return;
            }

            Result = new AddVersionResult(value!, contentType, expiresAt);
            RequestStop();
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(expirationDateField) + 1
        };

        cancelButton.Accepting += (s, e) => RequestStop();

        Add(valueLabel, valueField, contentTypeLabel, contentTypeField,
            expirationDateLabel, expirationDateField, okButton, cancelButton);
    }
}
