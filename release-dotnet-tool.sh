#!/bin/bash

# Release script for publishing azkv as a dotnet tool to NuGet
#
# Usage: ./release-dotnet-tool.sh <version>
# Example: ./release-dotnet-tool.sh 1.0.1
#
# Requires NUGET_API_KEY environment variable to be set

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

if [ $# -ne 1 ]; then
    echo -e "${RED}Error: Invalid arguments${NC}"
    echo "Usage: $0 <version>"
    echo "Example: $0 1.0.1"
    exit 1
fi

VERSION=$1

if [ -z "$NUGET_API_KEY" ]; then
    echo -e "${RED}Error: NUGET_API_KEY environment variable is not set${NC}"
    exit 1
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}azkv - Publish dotnet tool to NuGet${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Version: ${YELLOW}${VERSION}${NC}"
echo ""

# Pack
echo -e "${GREEN}Packing dotnet tool...${NC}"
dotnet pack src/AzureKvManager.Tui/AzureKvManager.Tui.csproj \
    -c Release \
    -p:Version=${VERSION} \
    -o ./publish/nuget

echo ""
echo -e "${GREEN}Package created:${NC}"
ls -la ./publish/nuget/*.nupkg
echo ""

# Confirm
read -p "Push to NuGet? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 1
fi

# Push
echo -e "${GREEN}Pushing to NuGet...${NC}"
dotnet nuget push ./publish/nuget/azkv.${VERSION}.nupkg \
    --api-key $NUGET_API_KEY \
    --source https://api.nuget.org/v3/index.json

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Published azkv ${VERSION} to NuGet${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Install: ${YELLOW}dotnet tool install -g azkv${NC}"
echo -e "Update:  ${YELLOW}dotnet tool update -g azkv${NC}"
echo ""
