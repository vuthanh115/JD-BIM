@echo off
chcp 65001 >nul
title Sinh Báo Cáo PDF Portfolio BIM Manager
echo ===============================================================================
echo   BỘ SINH BÁO CÁO PDF PORTFOLIO TỰ ĐỘNG - BIM MANAGER EXECUTIVE (A4)
echo ===============================================================================
echo.
echo Đang kiểm tra và khởi chạy trình biên dịch PDF...
python "%~dp0generate_pdf_portfolio.py"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Đang tự động mở file PDF...
    start "" "%~dp0BIM_Manager_Executive_Portfolio.pdf"
) else (
    echo.
    echo Có lỗi xảy ra trong quá trình xuất PDF.
    pause
)
