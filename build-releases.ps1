$ErrorActionPreference = "Stop"

$VERSION = "1.0.0"
$APP_NAME = "arrsync"
$BUILD_DIR = "releases"
$SRC_DIR = "src"

Write-Host "Building ArrSync v$VERSION for multiple platforms..." -ForegroundColor Cyan

if (Test-Path $BUILD_DIR) {
    Remove-Item -Path $BUILD_DIR -Recurse -Force
}
New-Item -ItemType Directory -Path $BUILD_DIR | Out-Null

Push-Location $SRC_DIR

try {
    Write-Host "`nBuilding for Windows (amd64)..." -ForegroundColor Yellow
    $env:GOOS = "windows"
    $env:GOARCH = "amd64"
    go build -ldflags "-s -w" -o "..\$BUILD_DIR\${APP_NAME}-windows-amd64.exe" .
    
    Write-Host "Building for Windows (arm64)..." -ForegroundColor Yellow
    $env:GOOS = "windows"
    $env:GOARCH = "arm64"
    go build -ldflags "-s -w" -o "..\$BUILD_DIR\${APP_NAME}-windows-arm64.exe" .
    
    Write-Host "Building for Linux (amd64)..." -ForegroundColor Yellow
    $env:GOOS = "linux"
    $env:GOARCH = "amd64"
    go build -ldflags "-s -w" -o "..\$BUILD_DIR\${APP_NAME}-linux-amd64" .
    
    Write-Host "Building for Linux (arm64)..." -ForegroundColor Yellow
    $env:GOOS = "linux"
    $env:GOARCH = "arm64"
    go build -ldflags "-s -w" -o "..\$BUILD_DIR\${APP_NAME}-linux-arm64" .
    
    Write-Host "Building for macOS (amd64)..." -ForegroundColor Yellow
    $env:GOOS = "darwin"
    $env:GOARCH = "amd64"
    go build -ldflags "-s -w" -o "..\$BUILD_DIR\${APP_NAME}-darwin-amd64" .
    
    Write-Host "Building for macOS (arm64)..." -ForegroundColor Yellow
    $env:GOOS = "darwin"
    $env:GOARCH = "arm64"
    go build -ldflags "-s -w" -o "..\$BUILD_DIR\${APP_NAME}-darwin-arm64" .
    
    Write-Host "`nAll builds completed successfully!" -ForegroundColor Green
} finally {
    Pop-Location
}

Write-Host "`nGenerating checksums..." -ForegroundColor Cyan
Push-Location $BUILD_DIR

$files = Get-ChildItem -File
$checksumFile = "checksums.txt"

foreach ($file in $files) {
    $hash = (Get-FileHash -Path $file.Name -Algorithm SHA256).Hash
    "$hash  $($file.Name)" | Out-File -Append -FilePath $checksumFile -Encoding UTF8
    Write-Host "  $($file.Name): $hash"
}

Pop-Location

Write-Host "`nBuild Summary:" -ForegroundColor Cyan
Write-Host "Version: $VERSION" -ForegroundColor White
Write-Host "Output Directory: $BUILD_DIR" -ForegroundColor White
Write-Host "`nGenerated Files:" -ForegroundColor White
Get-ChildItem -Path $BUILD_DIR | ForEach-Object {
    $size = if ($_.Length -gt 1MB) { "{0:N2} MB" -f ($_.Length / 1MB) } else { "{0:N2} KB" -f ($_.Length / 1KB) }
    Write-Host "  $($_.Name) - $size" -ForegroundColor Gray
}

Write-Host "`nRelease build complete!" -ForegroundColor Green
Write-Host "Upload files from '$BUILD_DIR' to GitHub Releases" -ForegroundColor Yellow
