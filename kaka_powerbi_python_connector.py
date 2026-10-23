# ==============================================================================
# KAKA BIM DIRECT PYTHON CONNECTOR CHO POWER BI
# Hướng dẫn sử dụng trong Power BI:
# 1. Mở Power BI Desktop -> Get Data -> More... -> Chọn "Python script" -> Connect
# 2. Dán toàn bộ nội dung script này vào và bấm OK
# 3. Chọn các bảng kết quả: Dim_Project, Dim_WBS, Dim_Components, 
#    Dim_BIM_Elements, Fact_Schedule_4D, Fact_CDE_Quality để nạp vào mô hình.
# ==============================================================================

import pandas as pd
import sqlite3
import os

excel_path = r"C:\Users\vophi\Downloads\KAKA_BIM_PowerBI_Dataset.xlsx"

# 1. Đọc trực tiếp từ file Excel Master do KAKA sinh ra
Dim_Project = pd.read_excel(excel_path, sheet_name="Dim_Project")
Dim_WBS = pd.read_excel(excel_path, sheet_name="Dim_WBS")
Dim_Components = pd.read_excel(excel_path, sheet_name="Dim_Components")
Fact_Schedule_4D = pd.read_excel(excel_path, sheet_name="Fact_Schedule_4D")
Dim_BIM_Elements = pd.read_excel(excel_path, sheet_name="Dim_BIM_Elements")
Fact_CDE_Quality = pd.read_excel(excel_path, sheet_name="Fact_CDE_Quality")

# Chuyển đổi định dạng ngày tháng chuẩn cho Power BI
Fact_Schedule_4D['Start_Date'] = pd.to_datetime(Fact_Schedule_4D['Start_Date'])
Fact_Schedule_4D['End_Date'] = pd.to_datetime(Fact_Schedule_4D['End_Date'])

print("KAKA BIM Data successfully prepared for Power BI!")
