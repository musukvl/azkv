class Azkv < Formula
  desc "Terminal UI for managing Azure Key Vaults"
  homepage "https://github.com/musukvl/azkv"
  version "1.0.0"
  license "MIT"

  on_macos do
    on_arm do
      url "https://github.com/musukvl/azkv/releases/download/v1.0.0/azkv-1.0.0-osx-arm64.tar.gz"
      sha256 "REPLACE_WITH_ARM64_SHA256"
    end
    on_intel do
      url "https://github.com/musukvl/azkv/releases/download/v1.0.0/azkv-1.0.0-osx-x64.tar.gz"
      sha256 "REPLACE_WITH_X64_SHA256"
    end
  end

  depends_on "azure-cli"

  def install
    bin.install "azkv"
  end

  test do
    # Test that the binary exists and runs
    assert_match "Azure Key Vault Manager", shell_output("#{bin}/azkv --help")
  end
end
