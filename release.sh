#!/bin/bash

# Release script for Azure Key Vault Manager
# Builds binaries locally and creates GitHub release
#
# Usage: ./release.sh <version> <release-notes-file>
# Example: ./release.sh 1.0.0 releases/v1.0.0.md

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check arguments
if [ $# -ne 2 ]; then
    echo -e "${RED}Error: Invalid arguments${NC}"
    echo "Usage: $0 <version> <release-notes-file>"
    echo "Example: $0 1.0.0 releases/v1.0.0.md"
    exit 1
fi

VERSION=$1
RELEASE_NOTES_FILE=$2
TAG="v${VERSION}"

# Validate release notes file exists
if [ ! -f "$RELEASE_NOTES_FILE" ]; then
    echo -e "${RED}Error: Release notes file not found: $RELEASE_NOTES_FILE${NC}"
    exit 1
fi

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    echo -e "${RED}Error: GitHub CLI (gh) is not installed${NC}"
    echo "Install it with: brew install gh"
    exit 1
fi

# Check if logged in to GitHub
if ! gh auth status &> /dev/null; then
    echo -e "${RED}Error: Not logged in to GitHub${NC}"
    echo "Run: gh auth login"
    exit 1
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Azure Key Vault Manager - Release${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Version: ${YELLOW}${VERSION}${NC}"
echo -e "Tag: ${YELLOW}${TAG}${NC}"
echo -e "Release Notes: ${YELLOW}${RELEASE_NOTES_FILE}${NC}"
echo ""

# Confirm
read -p "Continue with release? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 1
fi

# Clean previous builds
echo ""
echo -e "${GREEN}Cleaning previous builds...${NC}"
rm -rf ./publish
rm -f azkv-${VERSION}-*.tar.gz
rm -f azkv-${VERSION}-*.zip
rm -f azkv-${VERSION}-*.sha256

# Detect current architecture
ARCH=$(uname -m)
echo ""
echo -e "${GREEN}Detected architecture: ${YELLOW}${ARCH}${NC}"

# Build for macOS ARM64 (if on ARM64 Mac)
if [ "$ARCH" = "arm64" ]; then
    echo ""
    echo -e "${GREEN}Building macOS ARM64...${NC}"
    dotnet publish src/AzureKvManager.Tui/AzureKvManager.Tui.csproj \
        -c Release \
        -r osx-arm64 \
        --self-contained \
        -o ./publish/osx-arm64 \
        -p:PublishSingleFile=true \
        -p:Version=${VERSION} \
        -p:AssemblyVersion=${VERSION} \
        -p:FileVersion=${VERSION}
    
    cd publish/osx-arm64
    mv AzureKvManager.Tui azkv
    tar -czf ../../azkv-${VERSION}-osx-arm64.tar.gz azkv
    cd ../..
    shasum -a 256 azkv-${VERSION}-osx-arm64.tar.gz > azkv-${VERSION}-osx-arm64.tar.gz.sha256
    echo -e "${GREEN}✓ macOS ARM64 build complete${NC}"
fi

# Build for macOS x64
echo ""
echo -e "${GREEN}Building macOS x64...${NC}"
dotnet publish src/AzureKvManager.Tui/AzureKvManager.Tui.csproj \
    -c Release \
    -r osx-x64 \
    --self-contained \
    -o ./publish/osx-x64 \
    -p:PublishSingleFile=true \
    -p:Version=${VERSION} \
    -p:AssemblyVersion=${VERSION} \
    -p:FileVersion=${VERSION}

cd publish/osx-x64
mv AzureKvManager.Tui azkv
tar -czf ../../azkv-${VERSION}-osx-x64.tar.gz azkv
cd ../..
shasum -a 256 azkv-${VERSION}-osx-x64.tar.gz > azkv-${VERSION}-osx-x64.tar.gz.sha256
echo -e "${GREEN}✓ macOS x64 build complete${NC}"

# Build for Linux x64
echo ""
echo -e "${GREEN}Building Linux x64...${NC}"
dotnet publish src/AzureKvManager.Tui/AzureKvManager.Tui.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o ./publish/linux-x64 \
    -p:PublishSingleFile=true \
    -p:Version=${VERSION} \
    -p:AssemblyVersion=${VERSION} \
    -p:FileVersion=${VERSION}

cd publish/linux-x64
mv AzureKvManager.Tui azkv
tar -czf ../../azkv-${VERSION}-linux-x64.tar.gz azkv
cd ../..
shasum -a 256 azkv-${VERSION}-linux-x64.tar.gz > azkv-${VERSION}-linux-x64.tar.gz.sha256
echo -e "${GREEN}✓ Linux x64 build complete${NC}"

# Build for Windows x64
echo ""
echo -e "${GREEN}Building Windows x64...${NC}"
dotnet publish src/AzureKvManager.Tui/AzureKvManager.Tui.csproj \
    -c Release \
    -r win-x64 \
    --self-contained \
    -o ./publish/win-x64 \
    -p:PublishSingleFile=true \
    -p:Version=${VERSION} \
    -p:AssemblyVersion=${VERSION} \
    -p:FileVersion=${VERSION}

cd publish/win-x64
mv AzureKvManager.Tui.exe azkv.exe
zip ../../azkv-${VERSION}-win-x64.zip azkv.exe
cd ../..
shasum -a 256 azkv-${VERSION}-win-x64.zip > azkv-${VERSION}-win-x64.zip.sha256
echo -e "${GREEN}✓ Windows x64 build complete${NC}"

# Display checksums
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}SHA256 Checksums:${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}macOS ARM64:${NC}"
cat azkv-${VERSION}-osx-arm64.tar.gz.sha256 2>/dev/null || echo "Not built (not on ARM64 Mac)"
echo ""
echo -e "${YELLOW}macOS x64:${NC}"
cat azkv-${VERSION}-osx-x64.tar.gz.sha256
echo ""
echo -e "${YELLOW}Linux x64:${NC}"
cat azkv-${VERSION}-linux-x64.tar.gz.sha256
echo ""
echo -e "${YELLOW}Windows x64:${NC}"
cat azkv-${VERSION}-win-x64.zip.sha256
echo ""

# Create git tag
echo ""
echo -e "${GREEN}Creating git tag ${TAG}...${NC}"
git tag -a ${TAG} -m "Release version ${VERSION}" 2>/dev/null || {
    echo -e "${YELLOW}Tag ${TAG} already exists, deleting and recreating...${NC}"
    git tag -d ${TAG}
    git tag -a ${TAG} -m "Release version ${VERSION}"
}

# Push tag
echo -e "${GREEN}Pushing tag to GitHub...${NC}"
git push origin ${TAG} --force

# Create GitHub release
echo ""
echo -e "${GREEN}Creating GitHub release...${NC}"

# Delete existing release if it exists
gh release delete ${TAG} --yes 2>/dev/null || true

# Create new release with all artifacts
gh release create ${TAG} \
    --title "Release ${TAG}" \
    --notes-file "${RELEASE_NOTES_FILE}" \
    azkv-${VERSION}-*.tar.gz \
    azkv-${VERSION}-*.zip \
    azkv-${VERSION}-*.sha256

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ Release ${TAG} created successfully!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Release URL: ${YELLOW}https://github.com/musukvl/azkv/releases/tag/${TAG}${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Update Homebrew formula in musukvl/homebrew-azkv"
echo "2. Update Formula/azkv.rb with new version and checksums"
echo "3. Commit and push the formula update"
echo ""
echo -e "${GREEN}Done!${NC}"
