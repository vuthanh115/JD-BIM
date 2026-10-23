import sys
import os
import weasyprint

html_content = """<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="UTF-8">
<title>Cẩm Nang Quản Trị CDE Cho Vị Trí Trưởng Phòng BIM</title>
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
    font-size: 8.4pt;
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

  /* Header Bar */
  .header-card {
    background: linear-gradient(135deg, #0b1f3a 0%, #1e3a8a 65%, #1e40af 100%);
    border-radius: 6px;
    padding: 8px 14px;
    color: #ffffff;
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 7px;
    border-left: 5px solid #38bdf8;
  }
  .header-title-box h1 {
    font-size: 13.5pt;
    font-weight: 800;
    margin: 0 0 2px 0;
    letter-spacing: 0.3px;
    color: #ffffff;
    text-transform: uppercase;
  }
  .header-title-box p {
    font-size: 8.2pt;
    margin: 0;
    color: #93c5fd;
    font-weight: 500;
  }
  .header-badge-box {
    text-align: right;
  }
  .badge-tag {
    display: inline-block;
    background: rgba(255, 255, 255, 0.15);
    border: 1px solid rgba(255, 255, 255, 0.35);
    padding: 2.5px 7px;
    border-radius: 4px;
    font-size: 7.2pt;
    font-weight: 700;
    color: #e0f2fe;
    letter-spacing: 0.4px;
  }
  .badge-sub {
    font-size: 6.8pt;
    color: #cbd5e1;
    margin-top: 2px;
  }

  /* Section Styling */
  .section {
    margin-bottom: 7px;
  }
  .section-header {
    display: flex;
    align-items: center;
    gap: 6px;
    background: #f1f5f9;
    padding: 3.5px 8px;
    border-radius: 4px;
    border-left: 3.5px solid #1e3a8a;
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

  /* Table */
  table.custom-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 7.7pt;
    margin-bottom: 3px;
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
  table.custom-table tr:nth-child(even) td {
    background-color: #f8fafc;
  }
  .col-pillar { width: 18%; font-weight: 700; color: #1e3a8a; background: #f0fdf4 !important; }
  .col-user { width: 22%; color: #64748b; }
  .col-manager { width: 33%; font-weight: 600; color: #0f172a; }
  .col-keywords { width: 27%; color: #047857; background: #f0fdf4; }

  /* Cards Grid */
  .grid-2 {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 7px;
  }
  .grid-3 {
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 6px;
  }
  .card {
    border: 1px solid #e2e8f0;
    border-radius: 5px;
    padding: 6px 9px;
    background: #ffffff;
  }
  .card-highlight {
    background: #faf5ff;
    border-color: #c084fc;
  }
  .card-blue {
    background: #f0f9ff;
    border-color: #7dd3fc;
  }
  .card-emerald {
    background: #f0fdf4;
    border-color: #86efac;
  }
  .card-amber {
    background: #fffbeb;
    border-color: #fde68a;
  }
  .card-title {
    font-size: 8.3pt;
    font-weight: 800;
    margin: 0 0 3px 0;
    display: flex;
    align-items: center;
    gap: 5px;
  }

  /* List */
  ul.compact-list {
    margin: 2px 0 0 0;
    padding-left: 14px;
  }
  ul.compact-list li {
    margin-bottom: 2.5px;
  }

  /* Quote / Box */
  .quote-box {
    background: #f8fafc;
    border-left: 4px solid #0284c7;
    border-right: 1px solid #e2e8f0;
    border-top: 1px solid #e2e8f0;
    border-bottom: 1px solid #e2e8f0;
    padding: 7px 10px;
    border-radius: 0 5px 5px 0;
    margin: 4px 0 6px 0;
  }
  .quote-box p {
    margin: 0;
    font-style: italic;
    color: #1e293b;
    font-size: 7.9pt;
    line-height: 1.35;
  }

  .badge-pill {
    display: inline-block;
    padding: 1px 5px;
    border-radius: 3px;
    font-size: 6.8pt;
    font-weight: 700;
    margin-right: 3px;
    text-transform: uppercase;
  }
  .pill-blue { background: #dbeafe; color: #1e40af; border: 1px solid #bfdbfe; }
  .pill-green { background: #dcfce7; color: #166534; border: 1px solid #bbf7d0; }
  .pill-amber { background: #fef3c7; color: #92400e; border: 1px solid #fde68a; }
  .pill-purple { background: #f3e8ff; color: #6b21a8; border: 1px solid #e9d5ff; }

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
      <h1>CẨM NANG QUẢN TRỊ CDE • BIM MANAGER & DIRECTOR</h1>
      <p>Chuyển dịch năng lực từ "Người dùng mở file (30%)" sang "Nhà thiết lập quy tắc vận hành CDE (100%)"</p>
    </div>
    <div class="header-badge-box">
      <span class="badge-tag">ISO 19650 GOVERNANCE</span>
      <div class="badge-sub">ACC • TRIMBLE CONNECT • DALUX</div>
    </div>
  </div>

  <!-- NHẬN ĐỊNH BẢN CHẤT -->
  <div class="section">
    <div class="card card-amber" style="padding: 6px 10px; margin-bottom: 6px;">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 2px;">
        <span style="font-weight: 800; color: #92400e; font-size: 8.3pt;">⚠️ ĐÁNH GIÁ THỰC TẾ YÊU CẦU JD CẤP TRƯỞNG PHÒNG / GIÁM ĐỐC BIM</span>
        <span class="badge-pill pill-amber">TRỌNG TÂM CHIẾN LƯỢC</span>
      </div>
      <p style="margin: 0; font-size: 7.7pt; color: #78350f; line-height: 1.35;">
        Với cấp <b>Kỹ sư hay BIM Coordinator</b>, việc mở xem model và combine 2D/3D là đạt yêu cầu. Tuy nhiên ở cấp <b>Trưởng phòng / Giám đốc BIM</b> có yêu cầu <i>"Xây dựng, duy trì các nền tảng CDE"</i>, thao tác này mới đạt khoảng <b>30%</b>. Hội đồng phỏng vấn sẽ <b>không hỏi</b> <i>"Bấm nút combine thế nào?"</i>, mà sẽ hỏi <b>"Thiết lập quy tắc quản trị (Governance) trên CDE ra sao?"</b>. <b>70% còn lại là tư duy quy trình (Workflow)</b> hoàn toàn làm chủ được dựa trên kinh nghiệm quản lý thiết kế & kiểm soát Shop Drawing.
      </p>
    </div>
  </div>

  <!-- 4 MẢNH GHÉP QUẢN TRỊ -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">1. Bốn Mảnh Ghép Quản Trị CDE Cốt Lõi (Từ "Người Dùng" ➔ "Người Thiết Lập")</h2>
      <span class="section-subtitle">Tiêu chuẩn hóa theo khung ISO 19650 & Nền tảng Cloud</span>
    </div>
    <table class="custom-table">
      <thead>
        <tr>
          <th style="width: 17%;">Tính Năng CDE</th>
          <th style="width: 22%;">Mức Độ Đã Làm (30% - User)</th>
          <th style="width: 35%;">Mức Trưởng Phòng BIM Cần Nắm (70% - Admin)</th>
          <th style="width: 26%;">Từ Khóa "Ăn Tiền" (ACC & Trimble & Dalux)</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="col-pillar">1. Cấu Trúc Thư Mục & Trạng Thái Dữ Liệu</td>
          <td class="col-user">Tải file lên thư mục có sẵn của dự án để xem, tải về và gộp (combine) mô hình.</td>
          <td class="col-manager">
            <b>Quy hoạch 4 phân khu dữ liệu chuẩn ISO 19650:</b><br>
            • <b>WIP:</b> Đang thực hiện nội bộ từng bộ môn.<br>
            • <b>Shared:</b> Chia sẻ phối hợp liên bộ môn.<br>
            • <b>Published:</b> Đã duyệt phát hành cho TVGS, Nhà thầu.<br>
            • <b>Archive:</b> Đóng băng lưu trữ lịch sử pháp lý.
          </td>
          <td class="col-keywords">
            • <b>ACC:</b> <i>Naming Standard</i> (ép chuẩn tên file, sai chuẩn rơi vào <i>Holding Area</i>).<br>
            • <b>Trimble:</b> <i>Folder Template & Releases</i>.<br>
            • <b>Dalux:</b> <i>Box Pro Published Folders</i>.
          </td>
        </tr>
        <tr>
          <td class="col-pillar">2. Ma Trận Phân Quyền (Permission Matrix)</td>
          <td class="col-user">Đăng nhập bằng tài khoản được cấp quyền sẵn để truy cập file dự án.</td>
          <td class="col-manager">
            <b>Phân quyền ma trận theo Vai trò (Role) và Đơn vị (Company):</b><br>
            • Thầu phụ MEP: Chỉ thấy thư mục riêng (View/Upload).<br>
            • Chủ trì Kết cấu: Quyền Edit/Manage trong WIP.<br>
            • Hiện trường/QS: View Only tại thư mục Published.
          </td>
          <td class="col-keywords">
            • <b>ACC:</b> <i>5 Permission Levels</i> (View ➔ Download ➔ Markup ➔ Upload ➔ Edit/Manage).<br>
            • <b>Trimble:</b> <i>User Groups & Access Control</i>.<br>
            • <b>Dalux:</b> <i>User Roles & User Groups</i>.
          </td>
        </tr>
        <tr>
          <td class="col-pillar">3. Luồng Phê Duyệt Hồ Sơ (Approval Workflows)</td>
          <td class="col-user">Xem bản vẽ 2D/3D trên viewer để kiểm tra kỹ thuật và phản hồi thủ công.</td>
          <td class="col-manager">
            <b>Thiết lập luồng trình ký tự động trên mây:</b><br>
            Modeler nộp file (WIP) ➔ Chủ trì bộ môn kiểm tra (Review) ➔ BIM Manager phê duyệt (Approve) ➔ <i>Hệ thống tự động copy file sang Published kèm chữ ký số/con dấu.</i>
          </td>
          <td class="col-keywords">
            • <b>ACC:</b> <i>Reviews Module</i> (Setup 1, 2, 3-Step Review; Auto-copy to Published).<br>
            • <b>Trimble:</b> <i>Releases & ToDos Tracking</i>.<br>
            • <b>Dalux:</b> <i>Comments, Tasks & Release Flow</i>.
          </td>
        </tr>
        <tr>
          <td class="col-pillar">4. Quản Lý Lỗi & Phối Hợp (Issue / BCF)</td>
          <td class="col-user">Bật tắt các model bộ môn để mắt thường soi va chạm (Visual Clash).</td>
          <td class="col-manager">
            <b>Quản lý vòng đời phối hợp (Issue Lifecycle):</b><br>
            Khoanh vùng (Markup), ghim Issue gắn tọa độ camera 3D, chỉ định đích danh cá nhân phụ trách, ấn định hạn xử lý (Due date) và <i>đồng bộ 2 chiều về Revit/Navisworks bằng chuẩn BCF</i>.
          </td>
          <td class="col-keywords">
            • <b>ACC:</b> <i>Issues & Model Coordination</i>.<br>
            • <b>Trimble:</b> <i>BCF Topics Sync</i> (mở thẳng file view Revit/Tekla).<br>
            • <b>Dalux:</b> <i>BIM Issues & Capture Tasks</i>.
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- SỰ THẬT VỀ VAI TRÒ BIM MANAGER VỚI CDE -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">2. Sự Thật Về Vai Trò Của BIM Manager Với CDE Trong Vận Hành Thực Tế</h2>
      <span class="section-subtitle">Giải phóng tâm lý lo lắng về kỹ năng "Bấm nút cấu hình phần mềm"</span>
    </div>
    <div class="grid-3">
      <div class="card card-blue">
        <div class="card-title" style="color: #0369a1;">
          <span>⚙️ Khởi Tạo Một Lần (Kick-off)</span>
        </div>
        <p style="margin: 0; font-size: 7.4pt; color: #334155;">
          Việc thiết lập Admin CDE chỉ thực hiện <b>1 lần duy nhất</b> khi khởi tạo dự án dựa trên bộ <b>Project Template chuẩn</b>. Sau đó hệ thống tự động kế thừa cấu trúc thư mục, nhóm quyền và luồng duyệt cho toàn bộ dự án.
        </p>
      </div>
      <div class="card card-emerald">
        <div class="card-title" style="color: #047857;">
          <span>🤝 Hãng/Đại Lý Luôn Hỗ Trợ</span>
        </div>
        <p style="margin: 0; font-size: 7.4pt; color: #334155;">
          Khi công ty đầu tư license ACC, Trimble hay Dalux, đội ngũ kỹ thuật của hãng (Autodesk, Trimble, Dalux) hoặc đại lý phân phối luôn có gói <b>Onboarding & Training Admin</b> tận nơi để chuyển giao kỹ thuật.
        </p>
      </div>
      <div class="card card-highlight">
        <div class="card-title" style="color: #6b21a8;">
          <span>🎯 Giá Trị Cốt Lõi: Ra Đề Bài</span>
        </div>
        <p style="margin: 0; font-size: 7.4pt; color: #334155;">
          Nhiệm vụ của BIM Manager không phải là người gõ cấu hình, mà là <b>kiến trúc sư trưởng ra đề bài quy trình</b>: Ma trận phân quyền là gì? Bản vẽ kiểm duyệt mấy bước? Đặt tên ra sao để máy tính tự động lọc?
        </p>
      </div>
    </div>
  </div>

  <div class="page-footer">
    <span>KAKA BIM MANAGEMENT FRAMEWORK • CDE GOVERNANCE BLUEPRINT</span>
    <span>Trang 1 / 2</span>
  </div>
</div>

<!-- PAGE 2 -->
<div class="page">
  <div class="header-card" style="margin-bottom: 7px;">
    <div class="header-title-box">
      <h1>CHIẾN LƯỢC PHỎNG VẤN & LỘ TRÌNH LẤP LỖ HỔNG THẦN TỐC</h1>
      <p>Kịch bản trả lời biến điểm yếu thành thế mạnh • Lộ trình 45 phút làm chủ giao diện Admin</p>
    </div>
    <div class="header-badge-box">
      <span class="badge-tag">EXECUTIVE INTERVIEW STRATEGY</span>
      <div class="badge-sub">THỰC CHIẾN + TƯ DUY HỆ THỐNG</div>
    </div>
  </div>

  <!-- CHIẾN LƯỢC TRẢ LỜI PHỎNG VẤN -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">3. Chiến Lược Trả Lời Khéo Léo Trong Phỏng Vấn (Biến Điểm Yếu Thành Điểm Mạnh)</h2>
      <span class="section-subtitle">Phương pháp định vị: Kỹ thuật thực chiến + Tư duy kiến trúc hệ thống</span>
    </div>
    <div class="card" style="border-left: 4px solid #ef4444; background: #fff5f5; margin-bottom: 5px; padding: 5px 9px;">
      <b style="color: #b91c1c; font-size: 7.7pt;">❌ CÂU TRẢ LỜI CẦN TUYỆT ĐỐI TRÁNH KHI BỊ HỎI TRỰC DIỆN VỀ SETUP ADMIN:</b>
      <p style="margin: 1px 0 0 0; font-size: 7.4pt; color: #7f1d1d; font-style: italic;">
        "Dạ trước đây công ty cũ có người khác làm Admin, em chỉ vào xem bản vẽ, tải file và combine kiểm tra va chạm thôi ạ..." ➔ <i>(Khiến bạn bị đánh tụt xuống cấp độ Họa viên / Coordinator).</i>
      </p>
    </div>

    <div class="card" style="border-left: 4px solid #10b981; background: #f0fdf4; padding: 7px 10px;">
      <b style="color: #047857; font-size: 8.1pt;">✅ KỊCH BẢN TRẢ LỜI CHUẨN MỰC CẤP TRƯỞNG PHÒNG / GIÁM ĐỐC BIM:</b>
      <div class="quote-box" style="margin: 4px 0 0 0; background: #ffffff; border-left-color: #059669;">
        <p style="font-style: normal; color: #0f172a; font-size: 7.6pt; line-height: 1.38;">
          "Trong các dự án trước đây, tôi trực tiếp khai thác <b>ACC, Trimble Connect và Dalux</b> tập trung vào bài toán cốt lõi nhất của kỹ thuật: <b>Federated Model (gộp mô hình đa bộ môn)</b>, đối chiếu đa chiều 2D/3D để kiểm duyệt hồ sơ thiết kế, bóc tách khối lượng BOQ và kiểm soát thầu phụ về chất lượng Shop Drawing.<br><br>
          Về mặt <b>quản trị hệ thống (CDE Governance)</b>, tôi nắm vững nguyên lý thiết lập theo chuẩn <b>ISO 19650</b>: từ việc quy hoạch 4 phân vùng trạng thái dữ liệu (<b>WIP ➔ Shared ➔ Published ➔ Archive</b>), thiết lập <b>ma trận phân quyền theo vai trò (Role-based Matrix)</b>, đến các <b>luồng phê duyệt tự động (Approval Workflows)</b>. Đặc biệt, thay vì chỉ lưu trữ thụ động, định hướng của tôi là <b>kết nối CSDL SQL nội bộ với CDE thông qua Open API & chuẩn BCF</b> để tự động hóa kiểm soát tiến độ, chất lượng và phục vụ Dashboard quản trị cho Ban Lãnh đạo."
        </p>
      </div>
    </div>
  </div>

  <!-- LỘ TRÌNH 45 PHÚT -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">4. Lộ Trình 45 Phút "Lấp Lỗ Hổng" Thần Tốc Trước Giờ Phỏng Vấn</h2>
      <span class="section-subtitle">Chỉ cần 3 điểm chạm thao tác nhanh để hoàn toàn tự tin về giao diện Admin</span>
    </div>
    <div class="grid-3">
      <!-- TRIMBLE CONNECT -->
      <div class="card card-blue">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 3px;">
          <b style="color: #0369a1; font-size: 8.2pt;">Trimble Connect</b>
          <span class="badge-pill pill-blue">15 PHÚT</span>
        </div>
        <p style="margin: 0 0 3px 0; font-size: 7.3pt; color: #475569;"><b>Thao tác trực tiếp Web miễn phí:</b></p>
        <ul class="compact-list" style="font-size: 7.2pt; color: #334155; padding-left: 12px;">
          <li>Vào <code>web.connect.trimble.com</code> ➔ Tạo 1 Project cá nhân.</li>
          <li>Bấm vào <b>BCF Topics</b>: Tạo thử 1 Issue gắn ghim vào mô hình 3D.</li>
          <li>Xem mục <b>Releases</b> (quản lý phát hành hồ sơ đóng gói).</li>
          <li>Vào <b>Settings ➔ Permissions</b>: Xem cách phân quyền User Groups.</li>
        </ul>
      </div>

      <!-- ACC -->
      <div class="card card-emerald">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 3px;">
          <b style="color: #047857; font-size: 8.2pt;">Autodesk ACC</b>
          <span class="badge-pill pill-green">15 PHÚT</span>
        </div>
        <p style="margin: 0 0 3px 0; font-size: 7.3pt; color: #475569;"><b>Xem nhanh Case Study chuẩn ISO:</b></p>
        <ul class="compact-list" style="font-size: 7.2pt; color: #334155; padding-left: 12px;">
          <li>Lên YouTube gõ từ khóa: <i>"ACC Docs ISO 19650 Naming Standard"</i> (clip 6 phút).</li>
          <li>Xem cách ACC tự giữ file sai tên vào <b>Holding Area</b>.</li>
          <li>Gõ tiếp: <i>"ACC Reviews Approval Workflow"</i>: Xem quy trình tạo luồng duyệt 1 bước, 2 bước tự đóng dấu <b>Approved</b>.</li>
        </ul>
      </div>

      <!-- DALUX -->
      <div class="card card-highlight">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 3px;">
          <b style="color: #6b21a8; font-size: 8.2pt;">Dalux Build / Box</b>
          <span class="badge-pill pill-purple">15 PHÚT</span>
        </div>
        <p style="margin: 0 0 3px 0; font-size: 7.3pt; color: #475569;"><b>Xem quy trình hiện trường & 2D/3D:</b></p>
        <ul class="compact-list" style="font-size: 7.2pt; color: #334155; padding-left: 12px;">
          <li>Gõ YouTube: <i>"Dalux Box Pro workflow and BIM Viewer"</i> (clip 5-7 phút).</li>
          <li>Xem tính năng độc quyền: <b>Chồng lớp bản vẽ 2D cắt ngang không gian 3D</b>.</li>
          <li>Xem giao diện tạo phiếu kiểm tra hiện trường: <b>Tasks / Defect Checklists</b> gắn mã QR.</li>
        </ul>
      </div>
    </div>
  </div>

  <!-- BỘ 3 ĐÒN BẨY CHIẾN LƯỢC -->
  <div class="section">
    <div class="section-header">
      <h2 class="section-title">5. Bộ 3 Đòn Bẩy Chiến Lược Đưa Bạn Vượt Xa 90% Ứng Viên Khác</h2>
      <span class="section-subtitle">Chân dung Trưởng phòng BIM toàn diện: Kỹ thuật + Quản trị + Công nghệ số</span>
    </div>
    <div class="grid-3">
      <div class="card" style="border-top: 3px solid #0284c7; background: #f8fafc;">
        <b style="color: #0369a1; font-size: 7.9pt;">1. Năng Lực Combine & Shop Thực Tế</b>
        <p style="margin: 3px 0 0 0; font-size: 7.2pt; color: #475569; line-height: 1.3;">
          Kinh nghiệm thực chiến về phát hiện va chạm, bóc tách BOQ chính xác và siết chặt kỷ luật Shop Drawing của nhà thầu phụ — điều mà các ứng viên lý thuyết suông không thể có.
        </p>
      </div>
      <div class="card" style="border-top: 3px solid #10b981; background: #f8fafc;">
        <b style="color: #047857; font-size: 7.9pt;">2. Tư Duy Quản Trị ISO 19650 Chuẩn</b>
        <p style="margin: 3px 0 0 0; font-size: 7.2pt; color: #475569; line-height: 1.3;">
          Nắm trọn vẹn quy trình vận hành CDE: 4 trạng thái dữ liệu (WIP ➔ Archive), ma trận phân quyền theo Role, luồng ký duyệt tự động và quản lý lỗi BCF xuyên suốt.
        </p>
      </div>
      <div class="card" style="border-top: 3px solid #7c3aed; background: #f8fafc;">
        <b style="color: #6b21a8; font-size: 7.9pt;">3. Vũ Khí KAKA Open API Middleware</b>
        <p style="margin: 3px 0 0 0; font-size: 7.2pt; color: #475569; line-height: 1.3;">
          Khả năng tự động hóa kết nối CSDL SQL nội bộ với Cloud CDE (ACC, Trimble, Dalux) — giúp công ty làm chủ dữ liệu, tiết kiệm chi phí license và phục vụ Dashboard Ban Lãnh đạo.
        </p>
      </div>
    </div>
  </div>

  <div class="page-footer">
    <span>KAKA BIM MANAGEMENT FRAMEWORK • CDE GOVERNANCE BLUEPRINT</span>
    <span>Trang 2 / 2</span>
  </div>
</div>

</body>
</html>
"""

html_path = r"c:\Users\vophi\Downloads\Github\CV\JD-BIM\KAKA_BIM_Manager_CDE_Governance_Guide.html"
pdf_path = r"c:\Users\vophi\Downloads\Github\CV\JD-BIM\KAKA_BIM_Manager_CDE_Governance_Guide.pdf"
downloads_pdf_path = r"C:\Users\vophi\Downloads\KAKA_BIM_Manager_CDE_Governance_Guide.pdf"

with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_content)
print(f"HTML created: {html_path}")

print("Rendering PDF with WeasyPrint...")
html_obj = weasyprint.HTML(string=html_content)
html_obj.write_pdf(pdf_path)
print(f"PDF generated: {pdf_path}")
print(f"Size: {os.path.getsize(pdf_path)} bytes")

# Copy to Downloads
import shutil
shutil.copyfile(pdf_path, downloads_pdf_path)
print(f"Copied to: {downloads_pdf_path}")
