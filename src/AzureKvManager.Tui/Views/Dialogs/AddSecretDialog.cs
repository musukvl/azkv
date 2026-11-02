using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using AzureKvManager.Tui.Services;
using AzureKvManager.Tui.Views.Controls;

namespace AzureKvManager.Tui.Views.Dialogs;

public sealed record AddSecretResult(string Name, string Value, string? ContentType, DateTime? ExpiresAt);

public sealed class AddSecretDialog : Dialog<AddSecretResult>
{
    private readonly TextField _nameField;
    private readonly MultilineTextView _valueField;
    private readonly TextField _contentTypeField;
    private readonly TextField _expirationDateField;

    public AddSecretDialog()
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

        _nameField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(nameLabel),
            Width = Dim.Fill(1)
        };

        var valueLabel = new Label
        {
            Text = "Secret _Value:",
            X = 1,
            Y = Pos.Bottom(_nameField) + 1
        };

        _valueField = new MultilineTextView
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
            Y = Pos.Bottom(_valueField) + 1
        };

        _contentTypeField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(contentTypeLabel),
            Width = Dim.Fill(1)
        };

        var expirationDateLabel = new Label
        {
            Text = "_Expiration Date (yyyy-MM-dd, optional):",
            X = 1,
            Y = Pos.Bottom(_contentTypeField) + 1
        };

        _expirationDateField = new TextField
        {
            X = 1,
            Y = Pos.Bottom(expirationDateLabel),
            Width = Dim.Fill(1)
        };

        Add(nameLabel, _nameField, valueLabel, _valueField, contentTypeLabel, _contentTypeField,
            expirationDateLabel, _expirationDateField);

        // AddButton manages layout; last added becomes default
        AddButton(new Button { Text = "Cancel" });
        AddButton(new Button { Text = "OK" });
    }

    protected override bool OnAccepting(CommandEventArgs args)
    {
        View? source = null;
        args.Context?.Source?.TryGetTarget(out source);

        // Cancel closes with a null Result. Everything else (OK, or Enter inside a field,
        // which bubbles Command.Accept up to the dialog) submits the form.
        if (source is IAcceptTarget { IsDefault: false })
        {
            return base.OnAccepting(args);
        }

        var name = _nameField.Text.Trim();
        var value = _valueField.Text.Trim();
        var contentType = _contentTypeField.Text.Trim();

        if (!SecretFormValidator.TryValidateNewSecret(
                name,
                value,
                _expirationDateField.Text,
                out var expiresAt,
                out var errorMessage))
        {
            args.Handled = true;
            MessageBox.ErrorQuery(App!, "Error", errorMessage ?? "Invalid input", "OK");

            return true;
        }

        Result = new AddSecretResult(
            name,
            value,
            string.IsNullOrEmpty(contentType) ? null : contentType,
            expiresAt);

        return base.OnAccepting(args);
    }
}
