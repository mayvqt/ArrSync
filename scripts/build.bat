@echo off
setlocal

set "PROJECT_PATH=..\src\ArrSync.App\ArrSync.App.csproj"
set "OUTPUT_DIR=..\artifacts\publish"

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Building for Windows x64...
dotnet publish "%PROJECT_PATH%" --runtime win-x64 --self-contained true --configuration Release --output "%OUTPUT_DIR%\win-x64"
powershell Compress-Archive -Path "%OUTPUT_DIR%\win-x64" -DestinationPath "%OUTPUT_DIR%\win-x64.zip"
rmdir /s /q "%OUTPUT_DIR%\win-x64"

echo Building for Windows ARM64...
dotnet publish "%PROJECT_PATH%" --runtime win-arm64 --self-contained true --configuration Release --output "%OUTPUT_DIR%\win-arm64"
powershell Compress-Archive -Path "%OUTPUT_DIR%\win-arm64" -DestinationPath "%OUTPUT_DIR%\win-arm64.zip"
rmdir /s /q "%OUTPUT_DIR%\win-arm64"

echo Building for Linux x64...
dotnet publish "%PROJECT_PATH%" --runtime linux-x64 --self-contained true --configuration Release --output "%OUTPUT_DIR%\linux-x64"
powershell Compress-Archive -Path "%OUTPUT_DIR%\linux-x64" -DestinationPath "%OUTPUT_DIR%\linux-x64.zip"
rmdir /s /q "%OUTPUT_DIR%\linux-x64"

echo Building for Linux ARM64...
dotnet publish "%PROJECT_PATH%" --runtime linux-arm64 --self-contained true --configuration Release --output "%OUTPUT_DIR%\linux-arm64"
powershell Compress-Archive -Path "%OUTPUT_DIR%\linux-arm64" -DestinationPath "%OUTPUT_DIR%\linux-arm64.zip"
rmdir /s /q "%OUTPUT_DIR%\linux-arm64"

echo Building for macOS x64...
dotnet publish "%PROJECT_PATH%" --runtime osx-x64 --self-contained true --configuration Release --output "%OUTPUT_DIR%\osx-x64"
powershell Compress-Archive -Path "%OUTPUT_DIR%\osx-x64" -DestinationPath "%OUTPUT_DIR%\osx-x64.zip"
rmdir /s /q "%OUTPUT_DIR%\osx-x64"

echo Building for macOS ARM64...
dotnet publish "%PROJECT_PATH%" --runtime osx-arm64 --self-contained true --configuration Release --output "%OUTPUT_DIR%\osx-arm64"
powershell Compress-Archive -Path "%OUTPUT_DIR%\osx-arm64" -DestinationPath "%OUTPUT_DIR%\osx-arm64.zip"
rmdir /s /q "%OUTPUT_DIR%\osx-arm64"

echo Build complete. Zipped artifacts are in %OUTPUT_DIR%
endlocal