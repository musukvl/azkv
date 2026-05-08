# Terminal.Gui v2 Alignment Report

Date: 2026-05-02

## Scope

This review compares the current Azure Key Vault TUI architecture against the latest released Terminal.Gui version.

Latest release verified: `Terminal.Gui v2.0.1` from the GitHub releases page, checked on 2026-05-02. The release commit date is 2026-04-28T19:08:04-06:00, and the GitHub release page shows Apr 29. The release tag was cloned locally to `/tmp/Terminal.Gui-v2.0.1-codex` and inspected alongside the local project.

Local project version checked:

- `src/AzureKvManager.Tui/AzureKvManager.Tui.csproj` targets `net10.0`.
- `Terminal.Gui` package is already `2.0.1`.
- `dotnet build AzureKvManager.slnx` succeeds with 0 warnings and 0 errors.

Note: no GitHub MCP server is configured in this session, so release verification used the GitHub release page plus a local clone of the tagged repository.

## Sources

- Terminal.Gui latest releases: https://github.com/gui-cs/Terminal.Gui/releases
- Terminal.Gui v2.0.1 README: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/README.md
- v2 agent primer: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/ai-v2-primer.md
- Application architecture: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/application.md
- v1 to v2 migration guide: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/migratingfromv1.md
- Layout guide: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/layout.md
- Keyboard guide: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/keyboard.md
- Configuration guide: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/config.md
- TableView guide: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/tableview.md
- Multitasking guide: https://github.com/gui-cs/Terminal.Gui/blob/v2.0.1/docfx/docs/multitasking.md
- Local README: `README.md`
- Local requirements: `requirements.md`

## Executive Summary

The project is already on the latest released Terminal.Gui package and uses several v2-era practices correctly: instance-based `IApplication`, explicit disposal, v2 namespaces, `ConfigurationManager`/`ThemeManager`, `TableView` with `EnumerableTableSource`, `Pos`/`Dim` layout, and `app.Invoke` for cross-thread UI updates.

Architecturally, it is mostly aligned with Terminal.Gui v2, but not fully idiomatic. The main gaps are not package-version gaps; they are shape and ownership gaps:

- Dialogs return custom data by hiding `Dialog.Result` instead of deriving from `Dialog<TResult>` or `Runnable<TResult>`.
- `SecretDetailsPanel` is not a real composite view in the Terminal.Gui hierarchy; it owns controls that are added elsewhere, which weakens ownership, disposal, and command routing.
- The custom `Command.Copy` binding is attached to a detached `SecretDetailsPanel`, so `Ctrl+C`/`Alt+C` from the secret value `TextView` is unlikely to route to the intended full-secret copy handler.
- Startup loading uses `Task.Run` plus `async void`, which is fragile compared with v2's async/task and main-loop patterns.
- Button handlers use `Accepting` for many post-action cases where v2 guidance prefers `Accepted`.
- Theme handling uses `ConfigurationManager`, which is good, but then manually patches read-only `TextView` schemes with explicit `SetScheme`, which works against theme-based scheme resolution.

Overall rating: "v2-compatible and build-clean, but only partially v2-idiomatic."

## Alignment Matrix

| Area | Current project | v2 best practice | Alignment |
| --- | --- | --- | --- |
| Package version | `Terminal.Gui` `2.0.1` | Use latest release | Aligned |
| Target framework | `net10.0` | Project guideline says dotnet 10 | Aligned |
| Application lifecycle | `using IApplication app = Application.Create(); app.Init(); app.Run(mainWindow);` in `Program.cs` | Instance-based app via `Application.Create().Init()`, dispose app | Aligned, minor style improvement |
| Static API usage | No old `Application.Init()`, `Application.Run()`, `Application.Top`, or `Application.Shutdown()` found | Avoid obsolete static app model | Aligned |
| View construction | Object initializers, `Pos`/`Dim`, no v1 rect constructors | Use declarative layout | Aligned |
| Layout | Responsive split panes with `Dim.Percent`, `Dim.Fill`, sibling-relative `Pos.Right`/`Pos.Bottom` | Responsive layout language | Mostly aligned |
| Table data | `TableView` with `EnumerableTableSource<T>` for secrets and versions | `TableView` should receive an `ITableSource` | Aligned |
| Key handling | Uses `KeyBindings` and `Command.Copy` | Use command/key binding model | Partially aligned |
| Background UI updates | Many updates use `_app.Invoke(...)` | All UI work from background threads goes through `App.Invoke` or `app.Invoke` | Mostly aligned |
| Dialog results | `AddSecretDialog : Dialog` with `new AddSecretResult? Result` | Use `Dialog<TResult>` or `Runnable<TResult>` for typed results | Not aligned |
| View hierarchy | `SecretDetailsPanel` creates frames that `MainWindow` adds directly | Views should own their SubViews; `Add` controls ownership | Not aligned |
| Themes | `ConfigurationManager.Enable(ConfigLocations.All)` and `ThemeManager.Theme` | Enable config and apply themes | Mostly aligned |
| Event model | Many simple handlers use `Accepting` | Use `Accepted` for simple post-actions; use `Accepting` when cancellation is needed | Partially aligned |

## Findings

### High: `SecretDetailsPanel` is detached from the view hierarchy

Relevant code:

- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:13`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:25`
- `src/AzureKvManager.Tui/Views/MainWindow.cs:84`
- `src/AzureKvManager.Tui/Views/MainWindow.cs:119`

`SecretDetailsPanel` derives from `View`, but `MainWindow` never adds the `SecretDetailsPanel` instance. Instead, it adds `ActionsFrame`, `ContentTypeFrame`, `ExpirationFrame`, and `ValueFrame` directly.

That means `SecretDetailsPanel` is acting as a factory/controller rather than as a Terminal.Gui composite view. This conflicts with the v2 view hierarchy and ownership model, where `view.Add(subView)` establishes SubView ownership and command routing follows the SuperView chain.

Impact:

- The panel itself is not initialized or run as part of the view tree.
- Its own `App` property is not meaningful.
- Its commands do not naturally participate in routing from controls inside the frames.
- Disposal and event unsubscribe responsibilities are less obvious.

Recommendation:

Make `SecretDetailsPanel` a real composite view. Add the four frames to `SecretDetailsPanel`, lay them out inside it, and add only `_detailsPanel` to `MainWindow`. Alternatively, remove the `View` inheritance and make it an explicit non-view coordinator, but then do not attach Terminal.Gui commands to it.

### High: Copy command routing is probably wrong for `Ctrl+C` / `Alt+C`

Relevant code:

- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:102`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:104`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:105`

The code adds `Command.Copy` to `SecretDetailsPanel`, but the key bindings are on `_valueView`. Since `SecretDetailsPanel` is not a SuperView of `_valueView`, command bubbling from the `TextView` will not naturally reach `SecretDetailsPanel`.

Impact:

- The "Copy Value" button should work.
- The documented keyboard shortcut may copy selected text from `TextView` or do nothing, rather than copying the full secret value.

Recommendation:

After making `SecretDetailsPanel` a real parent view, either keep the command at the panel level with proper bubbling or attach the command directly to `_valueView`:

```csharp
_valueView.AddCommand(Command.Copy, () =>
{
    CopySecretValue();
    return true;
});
```

Then bind `Ctrl+C` and `Alt+C` to `Command.Copy` on `_valueView`.

### Medium: Typed dialog results use an old-style shadow property

Relevant code:

- `src/AzureKvManager.Tui/Views/Dialogs/AddSecretDialog.cs:11`
- `src/AzureKvManager.Tui/Views/Dialogs/AddSecretDialog.cs:13`
- `src/AzureKvManager.Tui/Views/Dialogs/AddVersionDialog.cs:11`
- `src/AzureKvManager.Tui/Views/Dialogs/AddVersionDialog.cs:13`
- `src/AzureKvManager.Tui/Views/Panels/SecretsPanel.cs:176`
- `src/AzureKvManager.Tui/Views/MainWindow.cs:194`

Both custom dialogs inherit from non-generic `Dialog` and define `public new AddSecretResult? Result` / `public new AddVersionResult? Result`.

Terminal.Gui v2 provides `Dialog<TResult>` and `Runnable<TResult>` specifically for typed result handling. The current approach works only because callers read the hidden property directly after `_app.Run(dialog)`. It does not participate in the framework's typed `IRunnable<TResult>` result model and will not work with `app.GetResult<T>()`.

Recommendation:

Change:

```csharp
public sealed class AddSecretDialog : Dialog
```

to:

```csharp
public sealed class AddSecretDialog : Dialog<AddSecretResult>
```

Do the same for `AddVersionDialog`. Then set the inherited `Result` before `RequestStop()`.

### Medium: Startup load uses `Task.Run` with `async void`

Relevant code:

- `src/AzureKvManager.Tui/Views/MainWindow.cs:147`
- `src/AzureKvManager.Tui/Views/Panels/KeyVaultsPanel.cs:68`

`MainWindow` starts the initial load with:

```csharp
Task.Run(() => _keyVaultsPanel.RefreshKeyVaults());
```

But `RefreshKeyVaults` is `async void`. This makes the startup operation hard to await, hard to test, and vulnerable to unobserved exceptions outside the view model's caught path.

Terminal.Gui v2's multitasking guidance prefers async/await and requires UI updates to be marshaled back with `app.Invoke`, which this project mostly does. The weak spot is the `async void` boundary.

Recommendation:

Change `RefreshKeyVaults` to return `Task`, start it from a clear lifecycle hook, and keep the fire-and-forget wrapper explicit:

```csharp
_ = _keyVaultsPanel.RefreshKeyVaultsAsync();
```

or trigger it from an initialized/running event so the app context is definitely established.

### Medium: Spinner is manually implemented instead of using v2 timer/view facilities

Relevant code:

- `src/AzureKvManager.Tui/Views/MainWindow.cs:16`
- `src/AzureKvManager.Tui/Views/MainWindow.cs:334`
- `src/AzureKvManager.Tui/Views/MainWindow.cs:377`

The custom spinner loop works by running an async delay loop and calling `_app.Invoke`. That is safe enough, but Terminal.Gui v2 includes `SpinnerView` and app timeouts for periodic UI updates.

Recommendation:

Prefer either:

- `SpinnerView` where a visible spinner is appropriate.
- `app.AddTimeout(...)` / `app.RemoveTimeout(...)` for periodic status updates.

This would reduce cancellation and disposal code in `MainWindow`.

### Medium: Theme handling is close, but explicit `SetScheme` fights theme resolution

Relevant code:

- `src/AzureKvManager.Tui/Themes/ThemeProvider.cs:7`
- `src/AzureKvManager.Tui/Themes/ThemeProvider.cs:17`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:68`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:176`

Good:

- `ConfigurationManager.Enable(ConfigLocations.All)` is called before `app.Init()`.
- `ThemeManager.Theme` plus `ConfigurationManager.Apply()` is the right v2 theme direction.

Concern:

`ApplyReadableTextScheme` calls `SetScheme(new Scheme(currentScheme) { ... })`. In v2, explicit schemes have priority over `SchemeName` and parent/theme lookup. That means these `TextView`s are no longer purely driven by named theme schemes. The code compensates by manually reapplying after theme switches, but that is easy to forget for other config changes.

Recommendation:

Prefer named schemes or theme configuration. For example, register a custom read-only display scheme and set `SchemeName`, or adjust the current theme's `ReadOnly` visual role through config.

### Low: `Accepting` is used for simple post-actions

Relevant code:

- `src/AzureKvManager.Tui/Views/Panels/SecretsPanel.cs:76`
- `src/AzureKvManager.Tui/Views/Panels/SecretsPanel.cs:84`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:44`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:54`
- `src/AzureKvManager.Tui/Views/Dialogs/AddSecretDialog.cs:101`
- `src/AzureKvManager.Tui/Views/Dialogs/AddVersionDialog.cs:85`

Terminal.Gui v2 uses the Cancellable Work Pattern. `Accepting` is the pre-event and can cancel. `Accepted` is the post-event and is better for simple "do this after activation" handlers.

Recommendation:

Keep `Accepting` where validation can prevent the operation, such as OK in the add dialogs. Use `Accepted` for simple button actions such as reload, copy, add-version request, and cancel.

### Low: Bare `using Terminal.Gui;` appears in view files

Relevant code:

- `src/AzureKvManager.Tui/Views/MainWindow.cs:1`
- `src/AzureKvManager.Tui/Views/Panels/SecretsPanel.cs:2`
- `src/AzureKvManager.Tui/Views/Panels/SecretDetailsPanel.cs:1`

The v2 primer advises using the split namespaces (`Terminal.Gui.App`, `Terminal.Gui.Views`, `Terminal.Gui.ViewBase`, `Terminal.Gui.Input`, `Terminal.Gui.Drawing`, `Terminal.Gui.Configuration`) instead of the old broad namespace. The project already uses the split namespaces, but still has some broad imports.

Recommendation:

Remove broad imports where they are no longer needed. Keep only the v2-specific namespaces.

## Requirements Alignment Notes

The current `requirements.md` says tables should be orderable by all columns on the Key Vault and Key Vault Secret pages.

Current implementation:

- Key Vaults use `ListView`, not `TableView`.
- Secrets use `TableView`, but headers are hidden with `ShowHeaders = false`, and no sorting interaction is implemented.
- Versions use `TableView` and are sorted by created/updated/version in the view model, but not interactively orderable.

This is not mainly a Terminal.Gui v2 migration issue, but it is a product requirement gap.

Suggested requirements update:

- If column sorting is mandatory, explicitly require `TableView` for key vaults, secrets, and versions with visible headers and sortable column commands.
- If the desired UX is filtering-first and portal-like rather than sortable tables, update the requirement to remove "orderable by all columns" or mark it as future work.

Suggested new TUI architecture requirement:

```markdown
## Terminal.Gui v2 architecture

- The TUI must target the latest stable Terminal.Gui v2 package.
- Use `Application.Create().Init()` and avoid obsolete static `Application` lifecycle APIs.
- Dialogs that return data must derive from `Dialog<TResult>` or `Runnable<TResult>`.
- Composite controls must own their SubViews through the Terminal.Gui view hierarchy.
- Background operations must update UI only through `IApplication.Invoke` or `View.App.Invoke`.
- Periodic UI updates should use Terminal.Gui timeout APIs or built-in views where practical.
- Themes should use `ConfigurationManager`, `ThemeManager`, and named schemes instead of hard-coded color overrides.
```

## Recommended Refactor Order

1. Refactor `SecretDetailsPanel` into a real composite view and fix copy command routing.
2. Convert `AddSecretDialog` and `AddVersionDialog` to `Dialog<TResult>`.
3. Replace `async void RefreshKeyVaults` and startup `Task.Run` with a `Task`-returning async load.
4. Replace or simplify the custom spinner with `SpinnerView` or app timeouts.
5. Change simple button handlers from `Accepting` to `Accepted`.
6. Move read-only text colors to named schemes/theme config.
7. Decide whether the sorting requirements are still mandatory and update `requirements.md` accordingly.

## Bottom Line

The current architecture is compatible with Terminal.Gui v2.0.1 and already follows the most important migration rules. It does not need a wholesale rewrite.

The main modernization work should focus on making custom dialogs and composite panels participate fully in the v2 runnable/view hierarchy. That will also fix the most important behavioral risk: command routing for copying secret values.
