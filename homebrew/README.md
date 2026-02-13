# Homebrew Tap Setup

This directory contains the Homebrew formula template for the Azure Key Vault Manager (azkv).

## Creating a Homebrew Tap Repository

To publish your application via Homebrew, you need to create a separate repository following Homebrew's naming convention.

### Step 1: Create the Tap Repository

1. Create a new repository on GitHub named: **`homebrew-azkv`**
   - Full repository name: `musukvl/homebrew-azkv`
   - Make it public (required for Homebrew)

2. Clone the repository:
   ```bash
   git clone https://github.com/musukvl/homebrew-azkv.git
   cd homebrew-azkv
   ```

3. Create the Formula directory structure:
   ```bash
   mkdir -p Formula
   ```

4. Copy the formula template to the repository:
   ```bash
   cp homebrew/azkv.rb Formula/
   ```

5. Update the formula with the first release details (see below)

6. Commit and push:
   ```bash
   git add Formula/azkv.rb
   git commit -m "Add azkv formula v1.0.0"
   git push origin main
   ```

### Step 2: Update Formula for Each Release

After creating a new release with GitHub Actions, update the formula:

1. Download the SHA256 checksums from the GitHub release page
2. Update `Formula/azkv.rb` with:
   - New version number
   - New URLs for the release artifacts
   - New SHA256 checksums for both arm64 and x64

3. Commit and push the changes:
   ```bash
   git add Formula/azkv.rb
   git commit -m "Update azkv to v1.0.1"
   git push origin main
   ```

### Step 3: Users Install via Homebrew

Once the tap repository is set up, users can install with:

```bash
# Add the tap
brew tap musukvl/azkv

# Install the application
brew install azkv

# Run the application
azkv --help
```

Or in one command:
```bash
brew install musukvl/azkv/azkv
```

## Formula Template Structure

The `azkv.rb` formula file includes:

- **Version**: The current version number
- **URLs**: Download URLs for both arm64 and x64 binaries
- **SHA256**: Checksums for verification
- **Dependencies**: Azure CLI requirement
- **Installation**: Binary installation to Homebrew's bin directory
- **Test**: Verification that the binary runs

## Automation (Optional)

You can automate formula updates by:

1. Creating a GitHub Action in the main repository that triggers on releases
2. Using the GitHub API to update the formula file in the tap repository
3. Automatically committing and pushing the changes

This requires:
- A personal access token with `repo` scope
- The token stored as a GitHub secret in the main repository
- A workflow that runs after successful releases

Example workflow step:
```yaml
- name: Update Homebrew formula
  run: |
    # Clone tap repository
    # Update version and checksums in Formula/azkv.rb
    # Commit and push changes
```

## Testing the Formula

Test the formula locally before publishing:

```bash
# Tap your local formula directory
brew tap musukvl/azkv /path/to/local/homebrew-azkv

# Install and test
brew install azkv
azkv --help

# Clean up
brew uninstall azkv
brew untap musukvl/azkv
```

## Resources

- [Homebrew Formula Cookbook](https://docs.brew.sh/Formula-Cookbook)
- [Homebrew Acceptable Formulae](https://docs.brew.sh/Acceptable-Formulae)
- [How to Create and Maintain a Tap](https://docs.brew.sh/How-to-Create-and-Maintain-a-Tap)
