# Requirements

## Overview
App for azure key vault management. 
 

## General requirements

App should be cross platform.
App should be very similar to Azure Portal key vault UI, but with better search and filter capabilities.

## Azure Credentials
Application should be GUI and use AZ CLI under the hood to get secrets from the keyvaults.
  
## All key vaults page
Application should have a page with all keyvaults in a table: 
    - Name
    - Subscription
    - Resource group
On the top of the table should be search field to filter keyvaults by name, subscription or resource group.
Table should be orderable by all columns.

## Key Vault page
- Should list all secrets of key vault in the table.
- On the top of the table should be search field to filter secrets by name or description.
- Table should be orderable by all columns.
- Should have a way to create a new secret with a form dialog.
  
## Secret versions page
- Should list all versions of the secret in the table.
- On the list should be button to show the value of the secret version.
- On the top should be add new version button to open add new version form.
- Should support copying secret values to clipboard and from clipboard.

## New secret form
- Should have fields for secret name, secret value, and optional description.
- Should have save button to save the secret.
- Should validate that name and value are not empty.
- Should automatically refresh the secrets list after creation.

## New version form
- Should have fields for secret value and description.
- Should have save button to save new version of the secret.
- Should validate that value is not empty.
- Should automatically refresh the versions list after creation.

## Terminal UI (TUI) Implementation

The TUI application provides a terminal-based interface with the following features:

### Layout
- **Left Panel**: Key Vaults list with filter by name or resource group
- **Center Panel**: Secrets list with filter by name and "New Secret" button
- **Top-Right Panel**: Secret versions list with "New Version" and "Copy Value" buttons
- **Bottom-Right Panel**: Secret value viewer with read-only multi-line display
- **Status Bar**: Real-time operation feedback and error messages

### Features
- Real-time filtering for Key Vaults and Secrets
- Create new secrets with name, value, and optional description
- Create new secret versions with value and optional description
- Copy secret values to clipboard
- Automatic list refresh after operations
- Clear error handling with user feedback
- Keyboard navigation (Tab, Arrow keys, Enter)
- Menu bar with File and Help options

### Navigation
- Tab: Switch between panels and between text fields
- Enter/Space: Select items or activate buttons
- Arrow keys: Navigate through lists
- Alt+F: File menu
- Alt+H: Help menu