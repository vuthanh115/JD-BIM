import os
import weasyprint
import pypdf

html_content = """<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="UTF-8">
<title>Hướng Dẫn Kết Nối KAKA BIM Sang Power BI</title>
<style>
  @page {
    size: A4 portrait;
    margin: 0;
  }
  * {
    box-sizing: border-box;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
  body {
    margin: 0;
    padding: 0;
    font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif;
    color: #1e293b;
    background-color: #ffffff;
    font-size: 8.3pt;
    line-height: 1.35;
  }
  .page {
    width: 210mm;
    height: 297mm;
    padding: 9mm 11mm 9mm 11mm;
    page-break-after: always;
    position: relative;
    background: #ffffff;
    overflow: hidden;
  }
  .page:last-child {
    page-break-after: avoid;
  }

  /* Header Card */
  .header-card {
    background: linear-gradient(135deg, #1e1e1e 0%, #0f2b48 55%, #1e3a8a 100%);
    border-radius: 6px;
    padding: 8px 14px;
    color: #ffffff;
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 7px;
    border-left: 5px solid #f2c811;
  }
  .header-title-box h1 {
    font-size: 13pt;
    font-weight: 800;
    margin: 0 0 2px 0;
    letter-spacing: 0.3px;
    color: #ffffff;
    text-transform: uppercase;
  }
  .header-title-box p {
    font-size: 8.2pt;
    margin: 0;
    color: #fde047;
    font-weight: 500;
  }
  .header-badge-box {
    text-align: right;
  }
  .badge-tag {
    display: inline-block;
    background: rgba(242, 200, 17, 0.2);
    border: 1px solid rgba(242, 200, 17, 0.6);
    padding: 2.5px 7px;
    border-radius: 4px;
    font-size: 7.2pt;
    font-weight: 800;
    color: #fef08a;
    letter-spacing: 0.4px;
  }
  .badge-sub {
    font-size: 6.8pt;
    color: #cbd5e1;
    margin-top: 2px;
  }

  /* Section Styling */
  .section { margin-bottom: 7px; }
  .section-header {
    display: flex;
    align-items: center;
    gap: 6px;
    background: #f1f5f9;
    padding: 3.5px 8px;
    border-radius: 4px;
    border-left: 3.5px solid #0284c7;
    margin-bottom: 5px;
  }
  .section-title {
    font-size: 8.8pt;
    font-weight: 800;
    color: #0f172a;
    text-transform: uppercase;
    letter-spacing: 0.3px;
    margin: 0;
  }
  .section-subtitle {
    font-size: 7.4pt;
    color: #64748b;
    margin-left: auto;
    font-weight: 500;
  }

  /* Tables */
  table.custom-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 7.6pt;
    margin-bottom: 4px;
  }
  table.custom-table th {
    background: #0f2b48;
    color: #ffffff;
    padding: 4.5px 7px;
    font-weight: 700;
    text-align: left;
    border: 1px solid #0f2b48;
  }
  table.custom-table td {
    padding: 4.5px 7px;
    border: 1px solid #cbd5e1;
    vertical-align: top;
    line-height: 1.3;
  }
  table.custom-table tr:nth-child(even) td { background-color: #f8fafc; }

  /* Grids */
  .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 7px; }
  .grid-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px; }

  /* Cards */
  .card {
    border: 1px solid #e2e8f0;
    border-radius: 5px;
    padding: 6px 9px;
    background: #ffffff;
  }
  .card-blue { background: #f0f9ff; border-color: #7dd3fc; }
  .card-emerald { background: #f0fdf4; border-color: #86efac; }
  .card-amber { background: #fffbeb; border-color: #fde68a; }
  .card-purple { background: #faf5ff; border-color: #c084fc; }

  .badge-pill {
    display: inline-block;
    padding: 1.5px 5px;
    border-radius: 3px;
    font-size: 6.8pt;
    font-weight: 700;
    text-transform: uppercase;
  }
  .pill-pbi { background: #fef08a; color: #854d0e; border: 1px solid #fde047; }
  .pill-green { background: #dcfce7; color: #166534; border: 1px solid #bbf7d0; }
  .pill-blue { background: #dbeafe; color: #1e40af; border: 1px solid #bfdbfe; }

  code {
    font-family: Consolas, 'Courier New', monospace;
    font-size: 7.2pt;
    background: #f1f5f9;
    padding: 1px 4px;
    border-radius: 3px;
    color: #0f172a;
    border: 1px solid #e2e8f0;
  }

  .quote-box {
    background: #f8fafc;
    border-left: 4px solid #0284c7;
    border-right: 1px solid #e2e8f0;
    border-top: 1px solid #e2e8f0;
    border-bottom: 1px solid #e2e8f0;
    padding: 6px 9px;
    border-radius: 0 5px 5px 0;
    margin: 4px 0 5px 0;
  }

  /* Footer */
  .page-footer {
    position: absolute;
    bottom: 5mm;
    left: 11mm;
    right: 11mm;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-top: 1px solid #e2e8f0;
    padding-top: 3px;
    font-size: 6.8pt;
    color: #94a3b8;
  }
</style>
</head>
<body>

<!-- PAGE 1 -->
<div class="page">
  <div class="header-card">
    <div class="header-title-box">
      <h1>HƯỚNG DẪN KẾT NỐI DỮ LIỆU KAKA BIM SANG POWER BI</h1>
      <p>Tự động hóa luồng dữ liệu 4D Schedule, Dự toán 5D & Kiểm soát CDE lên Dashboard Lãnh đạo</p>
    </div>
    <div class="header-badge-box">
      <span class="badge-tag">POWER BI • STAR SCHEMA</span>
      <div class="badge-sub">REVIT + P6 + CDE + SQLITE</div>
    </div>
  </div>

  <!-- SECTION 1: ARCHITECTURE OVERVIEW -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">1. Tổng Quan Kiến Trúc Dữ Liệu KAKA ➔ Power BI</h2>
      <span class="section-subtitle">Mô hình Star Schema phân tách Dimension và Fact Tables</span>
    </div>
    <div class="grid-3">
      <div class="card card-blue">
        <b style="color: #0369a1; font-size: 7.9pt;">1. Nguồn Dữ Liệu KAKA</b>
        <p style="margin: 2px 0 0 0; font-size: 7.3pt; color: #334155;">
          • <b>Revit BOQ:</b> 1,056 cấu kiện thực tế.<br>
          • <b>P6 Schedule:</b> 86 tasks, CPM Float.<br>
          • <b>Mindmap & CDE:</b> ISO 19650 Quality.
        </p>
      </div>
      <div class="card card-emerald">
        <b style="color: #047857; font-size: 7.9pt;">2. Bộ Trích Xuất Tự Động</b>
        <p style="margin: 2px 0 0 0; font-size: 7.3pt; color: #334155;">
          Script Python biên dịch sang <b>6 bảng Star Schema</b> sạch, tự động tính khối lượng thép, thể tích bê tông, chi phí dự toán.
        </p>
      </div>
      <div class="card card-amber">
        <b style="color: #92400e; font-size: 7.9pt;">3. Power BI Dashboard</b>
        <p style="margin: 2px 0 0 0; font-size: 7.3pt; color: #334155;">
          Trực quan hóa đường cong S-Curve, cơ cấu chi phí 5D, ma trận lỗi BCF và báo cáo điều hành cho Ban Tổng Giám Đốc.
        </p>
      </div>
    </div>
  </div>

  <!-- SECTION 2: 3 WAYS TO CONNECT -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">2. Ba Cách Kết Nối Dữ Liệu Vào Power BI Desktop (Linh Hoạt & Tiện Dụng)</h2>
      <span class="section-subtitle">Khuyến nghị dùng Cách 1 cho tốc độ nhanh nhất hoặc Cách 3 để tự động hóa 100%</span>
    </div>
    <table class="custom-table">
      <thead>
        <tr>
          <th style="width: 20%;">Phương Pháp</th>
          <th style="width: 32%;">Các Bước Thao Tác Cụ Thể</th>
          <th style="width: 28%;">Ưu Điểm Nổi Bật</th>
          <th style="width: 20%;">File Sử Dụng</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><b>Cách 1: File Excel Master Đa Bảng (Khuyến nghị)</b></td>
          <td>
            1. Mở Power BI Desktop ➔ <b>Get Data</b> ➔ Chọn <b>Excel workbook</b>.<br>
            2. Trỏ đến file <code>KAKA_BIM_PowerBI_Dataset.xlsx</code>.<br>
            3. Tích chọn đủ 6 bảng (Sheets) ➔ Bấm <b>Load</b>.
          </td>
          <td>• Nhanh nhất, không cần cài driver ODBC.<br>• Giữ nguyên định dạng kiểu dữ liệu (Date, Currency, Number).</td>
          <td><span class="badge-pill pill-pbi">KAKA_BIM_PowerBI_Dataset.xlsx</span></td>
        </tr>
        <tr>
          <td><b>Cách 2: Thư Mục CSV Chuẩn Star Schema</b></td>
          <td>
            1. Trong Power BI ➔ <b>Get Data</b> ➔ Chọn <b>Folder</b>.<br>
            2. Trỏ đến thư mục <code>PowerBI_Data\</code>.<br>
            3. Power BI tự động map 6 file CSV độc lập theo chuẩn UTF-8.
          </td>
          <td>• Phù hợp với luồng CI/CD dữ liệu lớn.<br>• Dễ dàng cập nhật từng bảng riêng lẻ mà không khóa file.</td>
          <td><span class="badge-pill pill-blue">Folder PowerBI_Data\</span></td>
        </tr>
        <tr>
          <td><b>Cách 3: Python Script Connector Trực Tiếp</b></td>
          <td>
            1. Power BI ➔ <b>Get Data</b> ➔ Chọn <b>Python script</b>.<br>
            2. Dán đoạn mã trong <code>kaka_powerbi_python_connector.py</code>.<br>
            3. Bấm OK ➔ Dữ liệu tự động kéo từ SQLite và P6.
          </td>
          <td>• Tự động làm mới (Auto Refresh) khi chạy tool KAKA.<br>• Phân tích dữ liệu nâng cao bằng thư viện Python.</td>
          <td><span class="badge-pill pill-green">kaka_powerbi_python_connector.py</span></td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- SECTION 3: DATA MODEL & RELATIONSHIPS -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">3. Đặc Tả Mô Hình Quan Hệ Dữ Liệu (Star Schema Model View)</h2>
      <span class="section-subtitle">Thiết lập mối quan hệ 1-Nhiều (1-to-Many) trong tab Model View của Power BI</span>
    </div>
    <table class="custom-table">
      <thead>
        <tr>
          <th style="width: 22%;">Bảng Dữ Liệu (Table)</th>
          <th style="width: 14%;">Loại Bảng</th>
          <th style="width: 22%;">Khóa Chính / Khóa Ngoại</th>
          <th style="width: 42%;">Ý Nghĩa Nghiệp Vụ Trong Quản Trị BIM</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><b>Dim_Project</b></td>
          <td>Dimension (1)</td>
          <td><code>Project_ID</code></td>
          <td>Chứa thông tin tổng quan dự án, ngày bắt đầu/kết thúc kế hoạch, chuẩn ISO 19650.</td>
        </tr>
        <tr>
          <td><b>Dim_WBS</b></td>
          <td>Dimension (1)</td>
          <td><code>WBS_Code</code></td>
          <td>Cây phân cấp công việc WBS đa cấp độ (Bộ môn Kết cấu KC, Kiến trúc KT, Cấu kiện).</td>
        </tr>
        <tr>
          <td><b>Dim_Components</b></td>
          <td>Dimension (1)</td>
          <td><code>Category_Code</code></td>
          <td>14 danh mục cấu kiện KAKA (Cọc, Móng, Cột, Dầm, Sàn, Vách...) và thời gian chuẩn.</td>
        </tr>
        <tr>
          <td><b>Dim_BIM_Elements</b></td>
          <td>Fact/Dim (N)</td>
          <td><code>Element_ID</code>, <code>Category_Code</code></td>
          <td>1,056 cấu kiện Revit chi tiết: Level, Thể tích m³, Khối lượng thép kg, Dự toán thành tiền.</td>
        </tr>
        <tr>
          <td><b>Fact_Schedule_4D</b></td>
          <td>Fact (N)</td>
          <td><code>Activity_ID</code>, <code>Category_Code</code></td>
          <td>86 công tác tiến độ Primavera P6: Start/End date, Duration, Đường găng CPM, % Hoàn thành.</td>
        </tr>
        <tr>
          <td><b>Fact_CDE_Quality</b></td>
          <td>Fact (N)</td>
          <td><code>Category_Code</code></td>
          <td>Theo dõi chất lượng hồ sơ CDE: Tỷ lệ bản vẽ Published, Số lỗi BCF Issues theo thầu phụ.</td>
        </tr>
      </tbody>
    </table>
  </div>

  <div class="page-footer">
    <span>KAKA BIM DATA INTEROPERABILITY • POWER BI INTEGRATION GUIDE</span>
    <span>Trang 1 / 2</span>
  </div>
</div>

<!-- PAGE 2 -->
<div class="page">
  <div class="header-card" style="margin-bottom: 7px;">
    <div class="header-title-box">
      <h1>CÔNG THỨC DAX CHUYÊN SÂU & CHIẾN LƯỢC TRÌNH BÀY LÃNH ĐẠO</h1>
      <p>Hệ thống chỉ số KPI đo lường hiệu suất tiến độ, dự toán và kịch bản phỏng vấn</p>
    </div>
    <div class="header-badge-box">
      <span class="badge-tag">DAX MEASURES & KPIS</span>
      <div class="badge-sub">EARNED VALUE • SPI • 5D BOQ</div>
    </div>
  </div>

  <!-- SECTION 4: DAX FORMULAS -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">4. Bộ Công Thức DAX Cốt Lõi Cho Dashboard BIM 4D / 5D / CDE</h2>
      <span class="section-subtitle">Copy trực tiếp vào mục "New Measure" trong Power BI</span>
    </div>
    <div class="grid-3">
      <div class="card card-blue">
        <b style="color: #0369a1; font-size: 7.8pt;">📊 Nhóm Tiến Độ 4D (Schedule)</b>
        <p style="margin: 3px 0 2px 0; font-size: 7.1pt; color: #334155; line-height: 1.25;">
          <b>Đường găng CPM:</b><br>
          <code>CALCULATE(COUNTROWS('Fact_Schedule_4D'), 'Fact_Schedule_4D'[Is_Critical_Path]="Có")</code><br><br>
          <b>Chỉ số SPI:</b><br>
          <code>DIVIDE(AVERAGE('Fact_Schedule_4D'[Percent_Complete]), 100, 1.0)</code>
        </p>
      </div>

      <div class="card card-emerald">
        <b style="color: #047857; font-size: 7.8pt;">💰 Nhóm Dự Toán 5D (BOQ Cost)</b>
        <p style="margin: 3px 0 2px 0; font-size: 7.1pt; color: #334155; line-height: 1.25;">
          <b>Tổng Thép (Tấn):</b><br>
          <code>DIVIDE(SUM('Dim_BIM_Elements'[Rebar_Weight_kg]), 1000, 0)</code><br><br>
          <b>Hàm lượng thép bình quân (kg/m³):</b><br>
          <code>DIVIDE(SUM('Dim_BIM_Elements'[Rebar_Weight_kg]), SUM('Dim_BIM_Elements'[Volume_m3]), 0)</code>
        </p>
      </div>

      <div class="card card-amber">
        <b style="color: #92400e; font-size: 7.8pt;">🏛️ Nhóm Quản Trị CDE (ISO 19650)</b>
        <p style="margin: 3px 0 2px 0; font-size: 7.1pt; color: #334155; line-height: 1.25;">
          <b>Tỷ lệ bản vẽ Published (%):</b><br>
          <code>DIVIDE(SUM('Fact_CDE_Quality'[Drawings_Approved_Published]), SUM('Fact_CDE_Quality'[Total_Shop_Drawings]), 0)*100</code><br><br>
          <b>Tỷ lệ đóng va chạm BCF:</b><br>
          <code>AVERAGE('Fact_CDE_Quality'[Issue_Closure_Rate_Pct])</code>
        </p>
      </div>
    </div>
  </div>

  <!-- SECTION 5: DASHBOARD WIREFRAME -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">5. Bố Cục Trình Bày Dashboard Mẫu Chuẩn Cho Ban Tổng Giám Đốc</h2>
      <span class="section-subtitle">Đã đóng gói thành file tương tác trực tiếp `KAKA_PowerBI_Executive_Dashboard.html`</span>
    </div>
    <div class="grid-2">
      <div class="card" style="background: #f8fafc; border: 1.5px solid #cbd5e1;">
        <b style="color: #0f2b48; font-size: 7.8pt;">Khu Vực 1: Hàng KPI Thẻ Động (Cards)</b>
        <ul style="margin: 2px 0 0 0; padding-left: 14px; font-size: 7.2pt; color: #475569;">
          <li><b>Tổng cấu kiện:</b> 1,056 phần tử kết cấu gắn mã code.</li>
          <li><b>Khối lượng Bê tông:</b> 1,482 m³ (Thép: 163 Tấn).</li>
          <li><b>Tiến độ công tác:</b> 86 Tasks (28 công tác thuộc CPM).</li>
          <li><b>Tỷ lệ bản vẽ duyệt:</b> 78.5% sẵn sàng ra công trường.</li>
        </ul>
      </div>

      <div class="card" style="background: #f8fafc; border: 1.5px solid #cbd5e1;">
        <b style="color: #0f2b48; font-size: 7.8pt;">Khu Vực 2: Biểu Đồ Trọng Tâm (Visuals)</b>
        <ul style="margin: 2px 0 0 0; padding-left: 14px; font-size: 7.2pt; color: #475569;">
          <li><b>Đường cong S-Curve:</b> So sánh đường lũy kế Kế hoạch P6 vs Thực tế công trường để cảnh báo chậm tiến độ.</li>
          <li><b>Biểu đồ cột BOQ:</b> Phân bổ thể tích bê tông theo cấu kiện (Móng, Cột, Dầm, Sàn, Vách).</li>
          <li><b>Biểu đồ Donut CDE:</b> Trọng số hồ sơ theo trạng thái ISO 19650 (WIP vs Shared vs Published).</li>
        </ul>
      </div>
    </div>
  </div>

  <!-- SECTION 6: INTERVIEW PITCH -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">6. Kịch Bản Phỏng Vấn: "Tại Sao Nên Chuyển Dữ Liệu KAKA Lên Power BI?"</h2>
      <span class="section-subtitle">Khẳng định tư duy quản trị số cấp Trưởng phòng / Giám đốc BIM</span>
    </div>
    <div class="card" style="border-left: 4px solid #10b981; background: #f0fdf4; padding: 6px 9px;">
      <b style="color: #047857; font-size: 8pt;">✅ CÂU TRẢ LỜI ĐỈNH CAO THUYẾT PHỤC BAN TỔNG GIÁM ĐỐC:</b>
      <div class="quote-box" style="margin: 3px 0 0 0; background: #ffffff; border-left-color: #059669;">
        <p style="font-style: normal; color: #0f172a; font-size: 7.4pt; line-height: 1.35; margin: 0;">
          "Nếu chỉ để dữ liệu trong file Revit hay Primavera P6, thông tin sẽ bị 'giam lỏng' trong các phần mềm kỹ thuật chuyên dụng mà Ban Tổng Giám Đốc không thể mở xem hàng ngày. Bằng cách thiết lập bộ công cụ <b>KAKA Data Pipeline</b>, tôi tự động trích xuất toàn bộ dữ liệu <b>Hình học & BOQ (5D)</b> từ Revit và dữ liệu <b>Tiến độ đường găng CPM (4D)</b> từ P6, chuẩn hóa về mô hình <b>Star Schema</b> và đẩy lên <b>Power BI Dashboard</b>.<br><br>
          Điều này giúp Ban TGĐ chỉ cần mở điện thoại hoặc trình duyệt web là nắm trọn: Hôm nay đổ bao nhiêu m³ bê tông? Đường găng thi công có bị trễ không? Thầu phụ nào chậm nộp bản vẽ Shop Drawing? — Đây chính là <b>bản chất thực sự của Chuyển đổi số trong xây dựng</b>."
        </p>
      </div>
    </div>
  </div>

  <div class="page-footer">
    <span>KAKA BIM DATA INTEROPERABILITY • POWER BI INTEGRATION GUIDE</span>
    <span>Trang 2 / 2</span>
  </div>
</div>

</body>
</html>
"""

html_path = r"c:\Users\vophi\Downloads\Github\CV\JD-BIM\Huong_Dan_Ket_Noi_KAKA_PowerBI.html"
pdf_path = r"c:\Users\vophi\Downloads\Github\CV\JD-BIM\Huong_Dan_Ket_Noi_KAKA_PowerBI.pdf"
downloads_pdf = r"C:\Users\vophi\Downloads\Huong_Dan_Ket_Noi_KAKA_PowerBI.pdf"

with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_content)
print(f"HTML created: {html_path}")

print("Rendering PDF with WeasyPrint...")
html_obj = weasyprint.HTML(string=html_content)
html_obj.write_pdf(pdf_path)
print(f"PDF generated: {pdf_path}")
print(f"Size: {os.path.getsize(pdf_path)} bytes")

reader = pypdf.PdfReader(pdf_path)
print(f"Total Page Count: {len(reader.pages)}")

import shutil
shutil.copyfile(pdf_path, downloads_pdf)
print(f"Copied to: {downloads_pdf}")
