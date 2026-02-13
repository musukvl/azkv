# Release Process

This document describes how to create a new release of Azure Key Vault Manager (azkv).

## Overview

Releases are automated via GitHub Actions. When you push a git tag matching `v*.*.*`, the workflow automatically:

1. Builds binaries for all 6 platforms (macOS arm64/x64, Linux x64/arm64, Windows x64/arm64)
2. Creates release archives (.tar.gz for Unix, .zip for Windows)
3. Generates SHA256 checksums for all archives
4. Creates a GitHub Release with all artifacts attached
5. Provides instructions for updating the Homebrew formula

## Step-by-Step Release Process

### 1. Update Version Number

Edit [src/AzureKvManager.Tui/AzureKvManager.Tui.csproj](src/AzureKvManager.Tui/AzureKvManager.Tui.csproj):

```xml
<PropertyGroup>
  <Version>1.0.1</Version>  <!-- Update this -->
  <AssemblyVersion>1.0.1</AssemblyVersion>  <!-- Update this -->
  <FileVersion>1.0.1</FileVersion>  <!-- Update this -->
  <InformationalVersion>1.0.1</InformationalVersion>  <!-- Update this -->
</PropertyGroup>
```

### 2. Update CHANGELOG (if you have one)

Document what's changed in this release:
- New features
- Bug fixes
- Breaking changes
- Deprecations

### 3. Commit Changes

```bash
git add src/AzureKvManager.Tui/AzureKvManager.Tui.csproj
git commit -m "Bump version to 1.0.1"
git push origin main
```

### 4. Create and Push Git Tag

```bash
# Create a tag with the version number (must start with 'v')
git tag v1.0.1

# Push the tag to GitHub
git push origin v1.0.1
```

**Note**: You can also create tags with annotations:
```bash
git tag -a v1.0.1 -m "Release version 1.0.1"
git push origin v1.0.1
```

### 5. Monitor GitHub Actions

1. Go to: https://github.com/musukvl/azkv/actions
2. Watch the "Release" workflow run
3. The workflow builds all platforms (takes ~5-10 minutes)
4. Verify all jobs complete successfully

### 6. Verify the Release

1. Go to: https://github.com/musukvl/azkv/releases
2. Check that the new release appears with all artifacts:
   - `azkv-1.0.1-osx-arm64.tar.gz` + `.sha256`
   - `azkv-1.0.1-osx-x64.tar.gz` + `.sha256`
   - `azkv-1.0.1-linux-x64.tar.gz` + `.sha256`
   - `azkv-1.0.1-linux-arm64.tar.gz` + `.sha256`
   - `azkv-1.0.1-win-x64.zip` + `.sha256`
   - `azkv-1.0.1-win-arm64.zip` + `.sha256`

### 7. Update Homebrew Formula

The release notes will include the SHA256 checksums you need. Update the Homebrew formula:

1. Go to your tap repository: https://github.com/musukvl/homebrew-azkv
2. Edit `Formula/azkv.rb`
3. Update:
   - Version number
   - URLs for both arm64 and x64
   - SHA256 checksums for both architectures

Example:
```ruby
class Azkv < Formula
  desc "Terminal UI for managing Azure Key Vaults"
  homepage "https://github.com/musukvl/azkv"
  version "1.0.1"  # Update this
  license "MIT"

  on_macos do
    on_arm do
      url "https://github.com/musukvl/azkv/releases/download/v1.0.1/azkv-1.0.1-osx-arm64.tar.gz"  # Update this
      sha256 "abc123..."  # Update this from release notes
    end
    on_intel do
      url "https://github.com/musukvl/azkv/releases/download/v1.0.1/azkv-1.0.1-osx-x64.tar.gz"  # Update this
      sha256 "def456..."  # Update this from release notes
    end
  end
  
  # ... rest of formula
end
```

4. Commit and push the changes:
```bash
git add Formula/azkv.rb
git commit -m "Update azkv to v1.0.1"
git push origin main
```

### 8. Test the Release

#### Test Binary Downloads

Download and test binaries for at least one platform:

```bash
# macOS
curl -L -O https://github.com/musukvl/azkv/releases/download/v1.0.1/azkv-1.0.1-osx-arm64.tar.gz
tar -xzf azkv-1.0.1-osx-arm64.tar.gz
./azkv --help
```

#### Test Homebrew Installation

On a macOS machine:

```bash
# Update tap
brew update

# Upgrade to new version
brew upgrade azkv

# Verify version
azkv --help

# Or fresh install
brew uninstall azkv
brew install azkv
```

### 9. Announce the Release

- Update project README badges (if they don't auto-update)
- Post on social media / team channels
- Send email to users (if applicable)

## Manual Release (Emergency)

If you need to create a release without pushing a tag:

1. Go to: https://github.com/musukvl/azkv/actions
2. Click "Release" workflow
3. Click "Run workflow"
4. Enter the version number (without 'v' prefix)
5. Click "Run workflow"

This will create a release using the current main branch code.

## Versioning Strategy

We use [Semantic Versioning](https://semver.org/):

- **MAJOR** (1.0.0 → 2.0.0): Breaking changes
- **MINOR** (1.0.0 → 1.1.0): New features, backwards compatible
- **PATCH** (1.0.0 → 1.0.1): Bug fixes, backwards compatible

## Rollback a Release

If you need to rollback a bad release:

1. Delete the git tag:
   ```bash
   git tag -d v1.0.1
   git push origin :refs/tags/v1.0.1
   ```

2. Delete the GitHub release (on the releases page)

3. Revert the Homebrew formula to the previous version

4. Fix the issue and create a new patch release

## Troubleshooting

### Build fails on one platform

- Check the GitHub Actions logs for that specific job
- The build runs `dotnet publish` for each platform separately
- Common issues:
  - .NET SDK version mismatch
  - Platform-specific dependencies
  - File path issues

### Release artifacts missing

- Ensure all build jobs completed successfully
- Check the "Upload artifacts" step in each job
- Verify the artifact names match the pattern in the release step

### Homebrew installation fails

- Verify SHA256 checksums match the actual file checksums
- Check that URLs point to the correct release version
- Test the formula locally: `brew install --build-from-source /path/to/azkv.rb`

### Can't push tags

- Ensure you have push permissions to the repository
- Check if tag already exists: `git tag -l`
- Use `git push --tags` to push all local tags

## Maintenance Notes

- GitHub Actions workflow: [.github/workflows/release.yml](.github/workflows/release.yml)
- Homebrew formula template: [homebrew/azkv.rb](homebrew/azkv.rb)
- Always test on at least one platform before announcing
- Keep the Homebrew tap updated within 24 hours of each release
