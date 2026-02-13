# Next Steps - After GitHub Actions Completes

## ✅ What's Done
- Pushed v1.0.0 tag to trigger release build
- GitHub Actions is building binaries for all 6 platforms
- Watch progress at: https://github.com/musukvl/azkv/actions

## 🔄 Wait for Build to Complete (~5-10 minutes)

The workflow will:
1. Build binaries for macOS (arm64 & x64), Linux (x64 & arm64), Windows (x64 & arm64)
2. Create release archives with SHA256 checksums
3. Create GitHub Release at: https://github.com/musukvl/azkv/releases
4. Attach all artifacts to the release

## 📝 After Build Completes

### 1. Get SHA256 Checksums

Go to the release page: https://github.com/musukvl/azkv/releases/tag/v1.0.0

Download or view the checksums:
- `azkv-1.0.0-osx-arm64.tar.gz.sha256`
- `azkv-1.0.0-osx-x64.tar.gz.sha256`

Or check the GitHub Actions workflow output for the checksums.

### 2. Update Homebrew Formula

Clone your tap repository:
```bash
cd ~/dev  # or wherever you keep repos
git clone https://github.com/musukvl/homebrew-azkv.git
cd homebrew-azkv
```

Create the Formula directory and copy the template:
```bash
mkdir -p Formula
cp ~/dev/azure-kv-viewer/homebrew/azkv.rb Formula/
```

Edit `Formula/azkv.rb` with the actual SHA256 checksums:
```ruby
class Azkv < Formula
  desc "Terminal UI for managing Azure Key Vaults"
  homepage "https://github.com/musukvl/azkv"
  version "1.0.0"
  license "MIT"

  on_macos do
    on_arm do
      url "https://github.com/musukvl/azkv/releases/download/v1.0.0/azkv-1.0.0-osx-arm64.tar.gz"
      sha256 "PASTE_ARM64_SHA256_HERE"  # ← Update this
    end
    on_intel do
      url "https://github.com/musukvl/azkv/releases/download/v1.0.0/azkv-1.0.0-osx-x64.tar.gz"
      sha256 "PASTE_X64_SHA256_HERE"    # ← Update this
    end
  end

  depends_on "azure-cli"

  def install
    bin.install "azkv"
  end

  test do
    assert_match "Azure Key Vault Manager", shell_output("#{bin}/azkv --help")
  end
end
```

Commit and push:
```bash
git add Formula/azkv.rb
git commit -m "Add azkv formula v1.0.0"
git push origin main
```

### 3. Test the Installation

On macOS:
```bash
# Add your tap
brew tap musukvl/azkv

# Install
brew install azkv

# Test
azkv --help
```

### 4. Test Other Platforms (Optional)

Download and test binaries for other platforms:

**Linux:**
```bash
curl -L -O https://github.com/musukvl/azkv/releases/download/v1.0.0/azkv-1.0.0-linux-x64.tar.gz
tar -xzf azkv-1.0.0-linux-x64.tar.gz
./azkv --help
```

**Windows:**
```powershell
Invoke-WebRequest -Uri "https://github.com/musukvl/azkv/releases/download/v1.0.0/azkv-1.0.0-win-x64.zip" -OutFile "azkv.zip"
Expand-Archive azkv.zip
.\azkv\azkv.exe --help
```

## 🎉 You're Done!

After completing these steps:
- ✅ Release v1.0.0 is published
- ✅ Binaries available for all platforms
- ✅ Homebrew installation works: `brew install musukvl/azkv/azkv`
- ✅ Direct downloads available from releases page

## 🔮 Future Releases

For future releases, see [RELEASING.md](RELEASING.md) for the complete process.

Quick version:
1. Update version in `src/AzureKvManager.Tui/AzureKvManager.Tui.csproj`
2. Commit and push changes
3. Create and push tag: `git tag v1.0.1 && git push origin v1.0.1`
4. Update Homebrew formula with new checksums
5. Done!
