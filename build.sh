#!/bin/bash

# Build script for Azure Key Vault Manager TUI
# Creates a single executable for macOS

set -e

echo "Building Azure Key Vault Manager..."

# Clean previous builds
rm -rf ./publish

# Detect architecture
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    RID="osx-arm64"
else
    RID="osx-x64"
fi

echo "Building for: $RID"

# Build for macOS
dotnet publish src/AzureKvManager.Tui/AzureKvManager.Tui.csproj \
    -c Release \
    -r $RID \
    --self-contained \
    -o ./publish/$RID \
    -p:PublishSingleFile=true

echo ""
echo "✅ Build completed successfully!"
echo ""
echo "Output: ./publish/$RID/AzureKvManager.Tui"
echo ""
echo "To run the application:"
echo "  ./publish/$RID/AzureKvManager.Tui"

sudo cp ./publish/$RID/AzureKvManager.Tui /usr/local/bin/azkv
sudo chmod +x /usr/local/bin/azkv


