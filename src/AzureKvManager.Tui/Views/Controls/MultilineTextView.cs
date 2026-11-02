// TextView is obsolete in Terminal.Gui 2.5.0 in favour of EditorView, which ships in the separate
// tui-cs/Editor package. This app only needs a plain multi-line box, so that extra dependency is not
// warranted. Deriving here keeps the suppression confined to this one file.
#pragma warning disable CS0618

using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace AzureKvManager.Tui.Views.Controls;

/// <summary>Plain multi-line text box used for secret values.</summary>
public class MultilineTextView : TextView
{
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (base.OnGettingAttributeForRole (in role, ref currentAttribute))
        {
            return true;
        }

        // Themes that leave Editable/ReadOnly to be derived from Normal can collapse them onto a single
        // color - the Default theme derives ReadOnly as #808080 on #808080, making the text invisible.
        if (role is VisualRole.ReadOnly or VisualRole.Editable
            && currentAttribute.Foreground == currentAttribute.Background)
        {
            currentAttribute = GetScheme ().Normal;

            return true;
        }

        return false;
    }
}

#pragma warning restore CS0618
