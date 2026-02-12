# Azure Key Vault Manager - Terminal UI

A terminal-based user interface application for managing Azure Key Vaults, built with [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).

## Overview

This application provides a cross-platform terminal UI for comprehensive Azure Key Vault secret management, offering better search and filter capabilities than the standard Azure Portal.

![Screenshot](doc/screen.png)

## Features

- 📋 List all Azure Key Vaults in your subscription
- 🔐 Browse secrets within each Key Vault
- 📜 View secret versions with metadata
- 👁️ Display secret values
- 📋 Copy secret values to clipboard
- 🔍 Filter Key Vaults and Secrets by name
- ➕ Create new secrets with descriptions
- ➕ Create new secret versions
- ⌨️ Full keyboard navigation
- 🖥️ Cross-platform support (Windows, macOS, Linux)

## Prerequisites

- .NET 10.0 SDK or later
- Azure CLI (`az`) installed and configured
- Active Azure subscription with Key Vaults

## Building

From the project root:

```bash
dotnet build
```

```bash
dotnet build
```

## Running

From the project root:

```bash
dotnet run --project src/AzureKvManager.Tui
```

## Usage

### Layout

The application is divided into four main panels:

1. **Key Vaults** (left) - Lists all available Key Vaults with filter
2. **Secrets** (center) - Shows secrets in the selected Key Vault with filter and "New Secret" button
3. **Secret Details** (top-right) - Displays versions of the selected secret with "New Version" and "Copy Value" buttons
4. **Secret Value** (bottom-right) - Shows the actual secret value

### Navigation

- **Tab** - Switch between panels
- **Arrow Keys** - Navigate through lists
- **Enter** - Select items or activate buttons
- **Alt+F** - File menu
- **Alt+H** - Help menu

### Creating Secrets

1. Select a Key Vault from the left panel
2. Click "New Secret" in the Secrets panel
3. Enter secret name, value, and optional description
4. Click "OK" to create

### Creating Secret Versions

1. Select a Key Vault and Secret
2. Click "New Version" in the Secret Details panel
3. Enter the secret value and optional description
4. Click "OK" to create

## Project Structure

```
.
├── src/
│   ├── tgui/
│   │   └── AzureKvManager.Tui/        # Terminal UI application
│   └── avalonia/
│       └── AzureKvManager.Core/       # Shared core library
├── AzureKvManager.sln                 # Solution file
└── requirements.md                    # Project requirements
```

## Architecture

### Core Components

- **AzureKvManager.Core** - Shared library providing:
  - Data models (KeyVault, Secret, SecretVersion)
  - Azure CLI service integration
  - Business logic for all UI implementations

- **AzureKvManager.Tui** - Terminal UI application:
  - Terminal.Gui based interface
  - Real-time filtering and search
  - Dialog-based create/edit operations
  - Clipboard integration

## Dependencies

- **Terminal.Gui** 2.0+ - Cross-platform Terminal UI toolkit
- **.NET 10.0** - Runtime and sdk

## Development

The project follows .NET 10 best practices with:
- Async/await for non-blocking operations
- Proper error handling and user feedback
- Clean separation of concerns
- Terminal.Gui best practices for responsive UI

## License

See the project for licensing information.

- Azure CLI installed and configured
- Active Azure session: `az login`
- Access to Azure Key Vaults

## Architecture

```
azure-kv-viewer/
├── src/
│   ├── AzureKvManager.Core/          # Shared library
│   │   ├── Models/                   # Domain models
│   │   └── Services/                 # Azure CLI integration
│   ├── Avalonia/                     # GUI solution folder
│   │   ├── AzureKvManager.Avalonia/
│   │   ├── AzureKvManager.Avalonia.Tests/
│   │   └── AzureKvManager.Avalonia.sln
│   └── Spectre/                      # CLI solution folder
│       ├── AzureKvManager.Spectre/
│       └── AzureKvManager.Spectre.sln
└── README.md
```

## Development

### Building All Projects

```bash
# Build Core library
cd src/AzureKvManager.Core
dotnet build

# Build Avalonia solution
cd ../Avalonia
dotnet build

# Build Spectre solution
cd ../Spectre
dotnet build
```

### Running Tests

```bash
cd src/Avalonia
dotnet test
```

## Authentication

Both applications use the current Azure CLI session for authentication. Make sure you're logged in:

```bash
az login
az account show  # Verify current subscription
```

## Usage

### GUI Application
1. Launch the application
2. The app will automatically load all key vaults you have access to via Azure CLI
3. Double-click a key vault to view its secrets
4. Double-click a secret to view its versions
5. Click "Show Value" to display a specific version's value
6. Click "Add New Version" to create a new version of a secret

### CLI Application
1. Launch the CLI
2. Select from the interactive menu:
   - List all Key Vaults
   - Browse secrets in a Key Vault
   - Add/Update a secret
   - Search for a secret across all vaults
3. Follow the prompts to navigate and manage secrets

## Requirements

See [requirements.md](requirements.md) for the full feature specification.
