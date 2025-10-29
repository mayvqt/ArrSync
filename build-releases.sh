#!/bin/bash

# ArrSync Release Builder
# Builds binaries for Windows, Linux, and macOS

set -e

VERSION="1.0.0"
APP_NAME="arrsync"
BUILD_DIR="releases"
SRC_DIR="src"

echo -e "\033[36mBuilding ArrSync v${VERSION} for multiple platforms...\033[0m"

# Create releases directory
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Change to src directory
cd "$SRC_DIR"

# Windows AMD64
echo -e "\n\033[33mBuilding for Windows (amd64)...\033[0m"
GOOS=windows GOARCH=amd64 go build -ldflags "-s -w" -o "../$BUILD_DIR/${APP_NAME}-windows-amd64.exe" .

# Windows ARM64
echo -e "\033[33mBuilding for Windows (arm64)...\033[0m"
GOOS=windows GOARCH=arm64 go build -ldflags "-s -w" -o "../$BUILD_DIR/${APP_NAME}-windows-arm64.exe" .

# Linux AMD64
echo -e "\033[33mBuilding for Linux (amd64)...\033[0m"
GOOS=linux GOARCH=amd64 go build -ldflags "-s -w" -o "../$BUILD_DIR/${APP_NAME}-linux-amd64" .

# Linux ARM64
echo -e "\033[33mBuilding for Linux (arm64)...\033[0m"
GOOS=linux GOARCH=arm64 go build -ldflags "-s -w" -o "../$BUILD_DIR/${APP_NAME}-linux-arm64" .

# macOS AMD64 (Intel)
echo -e "\033[33mBuilding for macOS (amd64)...\033[0m"
GOOS=darwin GOARCH=amd64 go build -ldflags "-s -w" -o "../$BUILD_DIR/${APP_NAME}-darwin-amd64" .

# macOS ARM64 (Apple Silicon)
echo -e "\033[33mBuilding for macOS (arm64)...\033[0m"
GOOS=darwin GOARCH=arm64 go build -ldflags "-s -w" -o "../$BUILD_DIR/${APP_NAME}-darwin-arm64" .

echo -e "\n\033[32m✓ All builds completed successfully!\033[0m"

# Go back to root
cd ..

# Create checksums
echo -e "\n\033[36mGenerating checksums...\033[0m"
cd "$BUILD_DIR"

if command -v sha256sum &> /dev/null; then
    sha256sum * > checksums.txt
    cat checksums.txt
elif command -v shasum &> /dev/null; then
    shasum -a 256 * > checksums.txt
    cat checksums.txt
fi

cd ..

# Display results
echo -e "\n\033[36mBuild Summary:\033[0m"
echo "Version: $VERSION"
echo "Output Directory: $BUILD_DIR"
echo -e "\n\033[37mGenerated Files:\033[0m"
ls -lh "$BUILD_DIR" | tail -n +2 | awk '{printf "  %s - %s\n", $9, $5}'

echo -e "\n\033[32m✓ Release build complete!\033[0m"
echo -e "\033[33mUpload files from '$BUILD_DIR' to GitHub Releases\033[0m"
