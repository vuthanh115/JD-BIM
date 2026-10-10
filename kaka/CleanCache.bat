@echo off
title Dọn dẹp Bin và Obj - Kaka Structure
color 0C

echo =======================================================
echo     DANG DON DEP CACHE (BIN ^& OBJ) CHO TOAN BO DU AN
echo =======================================================
echo.

:: Lặp qua tất cả các thư mục con và xóa các thư mục có tên bin hoặc obj
FOR /D /R %%X IN (bin obj) DO (
    IF EXIST "%%X" (
        echo [XOA] "%%X"
        RD /S /Q "%%X"
    )
)

echo.
echo =======================================================
echo  DA DON DEP XONG! BAY GIO BAN CO THE BUILD LAI DU AN.
echo =======================================================
pause