# ReadOnly TextView mouse selection still cannot include final character in v2.0.1

## Description

In Terminal.Gui `v2.0.1`, a `TextView` with `ReadOnly = true` still cannot select the final character of a line using the mouse.

This looks closely related to the closed issue:

- #4061: Last character of line not mouse-selectable in TextView

It may also be related to:

- #4440 / #4441: ReadOnly TextView MoveRight selection did not reach end of line

However, the problem appears to still be reproducible in `v2.0.1` for the mouse-selection path.

## Expected Behavior

When selecting text in a read-only `TextView`, dragging the mouse to the end of a line should allow the full line to be selected, including the final character.

## Actual Behavior

The selection stops one character early. The last character cannot be included unless selection continues onto another line.

## Suspected Cause

In `TextView.Mouse.cs`, the read-only mouse path still appears to clamp the current column to `r.Count - 1`:

```cs
CurrentColumn = Math.Max(r.Count - (ReadOnly ? 1 : 0), 0);
```

Because selection rendering treats the selection endpoint as exclusive, the caret/selection endpoint needs to be able to reach `r.Count` to include the final character.

## Environment

- Terminal.Gui version: `2.0.1`
- Control: `TextView`
- Configuration:

```cs
new TextView
{
    ReadOnly = true,
    WordWrap = true
};
```

## Notes

For editable `TextView`, this behavior appears different because the caret can move to the after-last-character position. The issue seems specific to `ReadOnly = true` plus mouse selection.
