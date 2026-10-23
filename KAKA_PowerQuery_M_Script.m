// ==============================================================================
// KAKA BIM TO POWER BI - POWER QUERY (M) SCRIPT
// Hướng dẫn: Mở Power BI Desktop -> Home -> Transform Data (Power Query Editor)
// -> New Source -> Blank Query -> Advanced Editor -> Dán đoạn mã bên dưới
// ==============================================================================

let
    // ĐƯỜNG DẪN FILE EXCEL DỮ LIỆU KAKA
    // Hãy đổi đường dẫn dưới đây nếu bạn lưu file ở vị trí khác
    FilePath = "C:\Users\vophi\Downloads\KAKA_BIM_PowerBI_Dataset.xlsx",
    
    // Đọc toàn bộ Workbook
    Source = Excel.Workbook(File.Contents(FilePath), null, true),
    
    // 1. Tải bảng Fact_Schedule_4D (Tiến độ thi công)
    Fact_Schedule_Sheet = Source{[Item="Fact_Schedule_4D",Kind="Sheet"]}[Data],
    Fact_Schedule_Headers = Table.PromoteHeaders(Fact_Schedule_Sheet, [PromoteAllScalars=true]),
    Fact_Schedule_Typed = Table.TransformColumnTypes(Fact_Schedule_Headers,{
        {"Activity_ID", type text},
        {"Activity_Name", type text},
        {"WBS_Code", type text},
        {"Category_Code", type text},
        {"Start_Date", type date},
        {"End_Date", type date},
        {"Duration_Days", type number},
        {"Total_Float_Days", type number},
        {"Is_Critical_Path", type text},
        {"Status", type text},
        {"Percent_Complete", type number},
        {"Primary_Resource", type text},
        {"Cost_Account", type text}
    }),

    // 2. Tải bảng Dim_BIM_Elements (1056 Cấu kiện Revit)
    Dim_Elements_Sheet = Source{[Item="Dim_BIM_Elements",Kind="Sheet"]}[Data],
    Dim_Elements_Headers = Table.PromoteHeaders(Dim_Elements_Sheet, [PromoteAllScalars=true]),
    Dim_Elements_Typed = Table.TransformColumnTypes(Dim_Elements_Headers,{
        {"Element_ID", Int64.Type},
        {"Unique_ID", type text},
        {"Category_Code", type text},
        {"Category_Name", type text},
        {"Family_Name", type text},
        {"Type_Name", type text},
        {"Level_Name", type text},
        {"Volume_m3", type number},
        {"Area_m2", type number},
        {"Length_m", type number},
        {"Rebar_Weight_kg", type number},
        {"Material_Name", type text},
        {"Estimated_Cost_VND", type number}
    }),

    // 3. Tải bảng Fact_CDE_Quality (Kiểm soát chất lượng & Shop)
    Fact_CDE_Sheet = Source{[Item="Fact_CDE_Quality",Kind="Sheet"]}[Data],
    Fact_CDE_Headers = Table.PromoteHeaders(Fact_CDE_Sheet, [PromoteAllScalars=true]),
    Fact_CDE_Typed = Table.TransformColumnTypes(Fact_CDE_Headers,{
        {"Category_Code", type text},
        {"Component_Name", type text},
        {"Subcontractor", type text},
        {"Total_Shop_Drawings", Int64.Type},
        {"Drawings_Approved_Published", Int64.Type},
        {"Drawings_In_Review_Shared", Int64.Type},
        {"Drawings_WIP", Int64.Type},
        {"Approval_Rate_Pct", type number},
        {"Open_BCF_Issues", Int64.Type},
        {"Resolved_BCF_Issues", Int64.Type},
        {"Issue_Closure_Rate_Pct", type number}
    })
in
    Fact_Schedule_Typed
