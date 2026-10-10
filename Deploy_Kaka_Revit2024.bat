@echo off
chcp 65001 >nul
title Deploy kaka Structure to Revit 2024
echo ===============================================================================
echo     DEPLOY KAKA STRUCTURE ADD-IN TO REVIT 2024
echo ===============================================================================
echo.

set "SOURCE_BIN=%~dp0kaka\kaka.Structure.Structural.App\bin\Debug 2024"
set "TARGET_ADDINS=%APPDATA%\Autodesk\Revit\Addins\2024"
set "TARGET_KAKA=%TARGET_ADDINS%\kaka.Structure"

:: Kiểm tra xem Revit có đang chạy không
tasklist /FI "IMAGENAME eq Revit.exe" 2>NUL | find /I /N "Revit.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [CANH BAO] Phat hien Autodesk Revit 2024 dang chay!
    echo File DLL dang bi Revit khoa (lock) nen khong the ghi de truc tiep.
    echo.
    echo Vui long:
    echo  1. Luu file dang mo trong Revit (Ctrl + S)
    echo  2. Dong Revit 2024
    echo  3. Chay lai file nay de hoan tat deploy trong 1 giay!
    echo.
    pause
    exit /b 1
)

echo [1/3] Dang tao thu muc addin...
if not exist "%TARGET_KAKA%" mkdir "%TARGET_KAKA%"

echo [2/3] Dang sao chep cac file DLL, dependencies va ribbon.json...
xcopy /Y /Q /E "%SOURCE_BIN%\*" "%TARGET_KAKA%\" >nul

echo [3/3] Dang sao chep file kaka.Structure.addin...
copy /Y "%~dp0kaka\kaka.Structure.Structural.App\kaka.Structure.addin" "%TARGET_ADDINS%\" >nul

echo.
echo ===============================================================================
echo [THANH CONG] Da load toan bo kaka Structure vao Revit 2024!
echo Thu muc deploy: %TARGET_KAKA%
echo Hay khoi dong lai Revit 2024 de trai nghiem giao dien Ribbon day du!
echo ===============================================================================
echo.
pause
