@echo off
echo ========================================
echo BẮT ĐẦU BUILD CHO TẤT CẢ PHIÊN BẢN REVIT
echo ========================================

if exist "%USERPROFILE%\.dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
    set "PATH=%USERPROFILE%\.dotnet;%PATH%"
)

echo.
echo [1/4] Building for Revit 2024...
dotnet build -c "Debug 2024"

echo.
echo [2/4] Building for Revit 2025...
dotnet build -c "Debug 2025"

echo.
echo [3/4] Building for Revit 2026...
dotnet build -c "Debug 2026"

echo.
echo [4/4] Building for Revit 2027...
dotnet build -c "Debug 2027"

echo.
echo ========================================
echo HOÀN THÀNH! TAT CA ADD-IN DA DUOC DEPLOY.
echo ========================================
pause