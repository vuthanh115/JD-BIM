import os

path = r'c:\Users\vophi\Downloads\Github\CV\JD-BIM\BIM_Manager_Executive_Dossier_4D_Navisworks.html'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# Replace Page 1 content
old_p1 = '''      <div style="text-align: center; padding: 10px 10px 8px; border-bottom: 1.5px solid var(--navy); margin-bottom: 8px;">
        <span class="badge badge-navy" style="font-size: 7pt; padding: 2px 8px; margin-bottom: 4px;">EXECUTIVE MASTER PORTFOLIO • PLAYER-COACH MODEL</span>
        <h1 style="font-size: 18pt; font-weight: 900; color: var(--navy); margin-bottom: 3px; letter-spacing: -0.3px;">
          BIM MANAGER EXECUTIVE DOSSIER (A-Z)
        </h1>
        <div style="font-size: 9pt; font-weight: 700; color: var(--primary-dark); margin-bottom: 4px;">
          Hoạch Định Bộ Máy • Chuẩn Quốc Tế MnSCU • Đa Bộ Môn KC/KT/MEP • Primavera P6 & Navisworks 4D/5D
        </div>
        <p style="font-size: 7.6pt; color: #475569; max-width: 620px; margin: 0 auto;">
          Hồ sơ năng lực tổng thể tích hợp kinh nghiệm thực chiến 1.500.000+ m² sàn tổng thầu công nghiệp, chuẩn mực quản lý dự án quốc tế AIA/MnSCU và giải pháp công nghệ tự động hóa cho vị trí Quản lý / Trưởng bộ phận BIM.
        </p>
      </div>'''

new_p1 = '''      <div style="text-align: center; padding: 8px 10px 6px; border-bottom: 1.5px solid var(--navy); margin-bottom: 6px;">
        <span class="badge badge-navy" style="font-size: 7pt; padding: 2px 8px; margin-bottom: 3px;">EXECUTIVE MASTER PORTFOLIO • HEAD OF BIM / BIM DIRECTOR LEVEL</span>
        <h1 style="font-size: 16.5pt; font-weight: 900; color: var(--navy); margin-bottom: 2px; letter-spacing: -0.3px;">
          BIM DIRECTOR / HEAD OF BIM EXECUTIVE DOSSIER
        </h1>
        <div style="font-size: 8.6pt; font-weight: 700; color: var(--primary-dark); margin-bottom: 3px;">
          Quản Trị Chiến Lược & Chuyển Đổi Số Doanh Nghiệp • Trọng Tâm Thiết Kế (Design Phase) • CDE Đa Nền Tảng (Dalux / ACC / Trimble Connect)
        </div>
        <p style="font-size: 7.4pt; color: #475569; max-width: 650px; margin: 0 auto;">
          Hồ sơ năng lực cấp quản lý chiến lược báo cáo trực tiếp Ban Tổng Giám Đốc: Hoạch định bộ máy BIM cấp công ty, kiểm soát chất lượng thiết kế liên bộ môn (QA/QC), chuẩn hóa ISO 19650, tối ưu hóa CDE và tiên phong phát triển giải pháp Tự Động Hóa (C# Add-ins/AI/ETL).
        </p>
      </div>'''

assert old_p1 in text, 'old_p1 not found'
text = text.replace(old_p1, new_p1)

old_stat = '''      <!-- 4 HIGHLIGHT KPI BOXES -->
      <div class="hero-stat-grid">
        <div class="stat-card">
          <div class="stat-num">1.5M+ m²</div>
          <div class="stat-lbl">Sàn Công Nghiệp Thực Tế</div>
        </div>
        <div class="stat-card">
          <div class="stat-num">LOD 200 - 500</div>
          <div class="stat-lbl">Vòng Đời Dự Án Chuẩn Hóa</div>
        </div>
        <div class="stat-card">
          <div class="stat-num">100 Tasks / 114 FS</div>
          <div class="stat-lbl">Tiến Độ Primavera P6 CPM</div>
        </div>
        <div class="stat-card">
          <div class="stat-num">100% SỐ HÓA</div>
          <div class="stat-lbl">Revit Add-in & ETL Bridge</div>
        </div>
      </div>'''

new_stat = '''      <!-- 4 HIGHLIGHT KPI BOXES -->
      <div class="hero-stat-grid">
        <div class="stat-card">
          <div class="stat-num">1.5M+ m²</div>
          <div class="stat-lbl">Thiết Kế & Thi Công Thực Tế</div>
        </div>
        <div class="stat-card">
          <div class="stat-num">ISO 19650</div>
          <div class="stat-lbl">CDE: Dalux • ACC • Trimble</div>
        </div>
        <div class="stat-card">
          <div class="stat-num">Design QA/QC</div>
          <div class="stat-lbl">Đồng Bộ ETABS ⟷ BIM ⟷ P6 4D</div>
        </div>
        <div class="stat-card">
          <div class="stat-num">100% SỐ HÓA</div>
          <div class="stat-lbl">Tự Động Hóa C# / AI / Scripts</div>
        </div>
      </div>'''

assert old_stat in text, 'old_stat not found'
text = text.replace(old_stat, new_stat)

old_quad = '''      <!-- MA TRẬN TÍCH HỢP 4 TRỤ CỘT CHIẾN LƯỢC -->
      <div class="box box-blue" style="margin-bottom: 8px;">
        <div class="box-title">
          <span>🌐 Ma Trận Tích Hợp 4 Trụ Cột Chiến Lược (Strategic Quad-Matrix)</span>
          <span class="badge badge-blue">Core Synergy</span>
        </div>
        <table class="table-report" style="margin-bottom: 0;">
          <thead>
            <tr>
              <th style="width: 25%;">1. Đề Bài Tuyển Dụng (JD)</th>
              <th style="width: 25%;">2. Chuẩn Quốc Tế (MnSCU)</th>
              <th style="width: 25%;">3. Số Hóa Quy Trình (Mindmap)</th>
              <th style="width: 25%;">4. Thực Chiến GLC & 4D BIM</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                • Mô hình BIM Centralized<br>
                • Xuất 2D/3D & Shop Rebar<br>
                • Bóc tách BOQ chính xác &lt;1%<br>
                • Tự động hóa Dynamo/API<br>
                • Đào tạo & Quản lý Outsource
              </td>
              <td>
                • Vòng đời 6 giai đoạn (SD→CD)<br>
                • Ma trận RFI→ASI→PR→CCD→CO<br>
                • Duyệt Submittals & Shop 14 ngày<br>
                • Giữ lại 5% Retainage bảo hành<br>
                • Bàn giao As-Built & 7D FM
              </td>
              <td>
                • Cây thư mục Server 01-09<br>
                • Quy tắc Join Kết cấu ưu tiên<br>
                • Shop Rebar 14 cấu kiện thực chiến<br>
                • Cơ điện MEP: CSD/SOD & T&C<br>
                • P6 CPM 100 tác vụ đa bộ môn
              </td>
              <td>
                • Mega Textile: 67.5 ha (VSIP 2)<br>
                • Kim Tín Dầu Giây: 311.845 m²<br>
                • Nhà xưởng APF: 122.000 m²<br>
                • Aimer Vietnam: 20.000 m²<br>
                • 4D Navisworks, P6, CDE ACC
              </td>
            </tr>
          </tbody>
        </table>
      </div>'''

new_quad = '''      <!-- MA TRẬN TÍCH HỢP 4 TRỤ CỘT CHIẾN LƯỢC THEO JD BAN TỔNG GIÁM ĐỐC -->
      <div class="box box-blue" style="margin-bottom: 6px;">
        <div class="box-title">
          <span>🌐 Ma Trận Đáp Ứng 4 Trụ Cột Trách Nhiệm Cốt Lõi (Core Executive Quad-Matrix)</span>
          <span class="badge badge-blue">Alignment with BoD JD</span>
        </div>
        <table class="table-report" style="margin-bottom: 0;">
          <thead>
            <tr>
              <th style="width: 25%;">1. Quản Trị & Tiêu Chuẩn Hóa</th>
              <th style="width: 25%;">2. Điều Phối Thiết Kế & QA/QC</th>
              <th style="width: 25%;">3. Quản Trị Hạ Tầng CDE</th>
              <th style="width: 25%;">4. Chiến Lược & Chuyển Đổi Số</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                • Điều hành & phát triển đội ngũ BIM.<br>
                • Bộ khung EIR / BEP / BIM Guide chuẩn.<br>
                • Hệ thống Template KC, KT, MEP.<br>
                • Thư viện Family & Component số hóa.<br>
                • Tự động hóa bản vẽ: Dynamo, C# Add-in.
              </td>
              <td>
                • Kiểm soát chất lượng (Model Health Check).<br>
                • Clash Detection liên bộ môn sớm từ SD/DD.<br>
                • Đồng bộ mô hình tính toán (ETABS) ⟷ Revit.<br>
                • Xử lý lệch pha giữa BIM và 2D CAD.<br>
                • Tối ưu thiết kế & giảm RFI công trường.
              </td>
              <td>
                • Quản trị CDE: <strong>Dalux, ACC, Trimble</strong>.<br>
                • Phân quyền & luồng duyệt ISO 19650.<br>
                • Quản lý Issue / RFI tập trung.<br>
                • Luân chuyển dữ liệu mở <strong>OpenBIM / IFC</strong>.<br>
                • Tối ưu chi phí bản quyền cho doanh nghiệp.
              </td>
              <td>
                • Đồng hành Ban TGĐ định hình chiến lược số.<br>
                • Bóc tách khối lượng 5D tự động phục vụ thầu.<br>
                • Lập tiến độ 4D P6 & mô phỏng Navisworks.<br>
                • Tích hợp AI / Dashboard đo lường hiệu suất.<br>
                • Chứng minh tỷ suất sinh lời <strong>ROI</strong> rõ ràng.
              </td>
            </tr>
          </tbody>
        </table>
      </div>'''

assert old_quad in text, 'old_quad not found'
text = text.replace(old_quad, new_quad)

old_phil = '''      <!-- TÓM TẮT HỒ SƠ & TRIẾT LÝ QUẢN TRỊ -->
      <div class="grid-2">
        <div class="box box-gray">
          <div class="box-title">
            <span>🎯 Triết Lý Điều Hành: "Player-Coach"</span>
            <span class="badge badge-navy">Leadership</span>
          </div>
          <p style="font-size: 7.6pt; color: #334155; margin-bottom: 4px;">
            Không chỉ làm công tác lập kế hoạch lý thuyết, BIM Manager phải là <strong>thuyền trưởng kỹ thuật (Player-Coach)</strong>:
          </p>
          <ul class="check-list">
            <li><strong>Player (Thực chiến):</strong> Trực tiếp xử lý bài toán khó (Central file nặng, xung đột va chạm CSD/SOD, lập trình script Python/C# tự động).</li>
            <li><strong>Coach (Huấn luyện):</strong> Đào tạo bài bản đội ngũ Modeler, thiết lập BEP, giám sát chất lượng QA/QC và kiểm soát tiến độ.</li>
          </ul>
        </div>

        <div class="box box-gray">
          <div class="box-title">
            <span>🛡️ Giá Trị Gia Tăng Cho Dự Án</span>
            <span class="badge badge-green">Value Proposition</span>
          </div>
          <p style="font-size: 7.6pt; color: #334155; margin-bottom: 4px;">
            Hệ thống giải pháp đã được đóng gói chuẩn hóa, áp dụng ngay lập tức:
          </p>
          <ul class="check-list">
            <li><strong>Bảo vệ dòng tiền:</strong> Triệt tiêu 100% va chạm trước khi ra công trường qua quy trình RFI & CSD/SOD, không đục phá bê tông.</li>
            <li><strong>Tối ưu tiến độ 4D:</strong> Rút ngắn 85% giờ công lập lịch thi công nhờ tích hợp dữ liệu trực tiếp từ Revit sang Primavera P6.</li>
          </ul>
        </div>
      </div>

      <div class="page-footer">
        <span>Ứng viên Vị trí Quản lý / Trưởng bộ phận BIM</span>
        <span>BIM Manager Executive Portfolio • Căn chỉnh in chuẩn A4</span>
        <span>Trang 1 / 8</span>
      </div>'''

new_phil = '''      <!-- TÓM TẮT HỒ SƠ & TRIẾT LÝ QUẢN TRỊ -->
      <div class="grid-2">
        <div class="box box-gray">
          <div class="box-title">
            <span>🎯 Triết Lý Điều Hành: "Kiến Trúc Sư Trưởng Quy Trình & Công Nghệ"</span>
            <span class="badge badge-navy">Leadership</span>
          </div>
          <p style="font-size: 7.4pt; color: #334155; margin-bottom: 4px;">
            Báo cáo trực tiếp Ban Tổng Giám Đốc, định vị bản thân là cầu nối vững chắc giữa Kỹ Thuật Thiết Kế và Quản Trị Chiến Lược:
          </p>
          <ul class="check-list">
            <li><strong>Chuyên môn kỹ thuật sâu:</strong> Xuất thân từ Chủ trì Thiết kế / Kỹ sư Kết cấu, hiểu rõ bản chất tính toán và thi công, xóa bỏ hoàn toàn rào cản lệch pha với các phòng Thiết kế 2D truyền thống.</li>
            <li><strong>Tự động hóa gắn liền quy trình:</strong> Phát triển Add-ins (C#/Python), Dynamo giúp kỹ sư tăng 40% năng suất, tự giác tuân thủ tiêu chuẩn mà không cần áp đặt hành chính.</li>
          </ul>
        </div>

        <div class="box box-gray">
          <div class="box-title">
            <span>🛡️ Cam Kết Giá Trị Gia Tăng Cho Doanh Nghiệp (ROI)</span>
            <span class="badge badge-green">Value Proposition</span>
          </div>
          <p style="font-size: 7.4pt; color: #334155; margin-bottom: 4px;">
            Bộ giải pháp chuyển đổi số toàn diện mang lại lợi ích đo lường trực tiếp cho Ban TGĐ:
          </p>
          <ul class="check-list">
            <li><strong>Chất lượng hồ sơ thiết kế:</strong> Triệt tiêu 100% va chạm dầm - ống MEP (CSD/SOD), giảm &gt;80% RFI ngoài công trường, bảo vệ uy tín thương hiệu công ty trước Chủ đầu tư.</li>
            <li><strong>Hiệu suất & Chi phí:</strong> Rút ngắn 40% thời gian xuất hồ sơ thiết kế, tự động trích xuất BOQ sai số &lt;1%, tối ưu chi phí bản quyền CDE nhờ giải pháp OpenBIM (Dalux/ACC/Trimble).</li>
          </ul>
        </div>
      </div>

      <div class="page-footer">
        <span>Ứng viên Vị trí Head of BIM / BIM Director • Báo cáo trực tiếp Ban Tổng Giám Đốc</span>
        <span>BIM Manager Executive Portfolio • Căn chỉnh in chuẩn A4</span>
        <span>Trang 1 / 8</span>
      </div>'''

assert old_phil in text, 'old_phil not found'
text = text.replace(old_phil, new_phil)

# Update page 2 CDE box
old_cde_box = '''        <div class="box box-gray">
          <div class="box-title">
            <span>⚙️ Hạ Tầng Công Nghệ & Môi Trường Dữ Liệu Chung (CDE)</span>
            <span class="badge badge-navy">Infrastructure</span>
          </div>
          <ul class="check-list">
            <li><strong>Nền tảng CDE:</strong> Autodesk Construction Cloud (ACC) / BIM 360 quản lý tập trung mô hình, phân quyền chặt chẽ WIP / Shared / Published / Archival.</li>
            <li><strong>Kiểm soát thực địa:</strong> Tích hợp camera AI giám sát công trường 360° đối chiếu tiến độ mô hình 4D theo thời gian thực.</li>
            <li><strong>Trắc địa số:</strong> Sử dụng máy toàn đạc điện tử Leica Total Station bắn tọa độ tim trục kiểm soát sai số lắp dựng thép &lt; 2mm.</li>
          </ul>
        </div>'''

new_cde_box = '''        <div class="box box-gray">
          <div class="box-title">
            <span>⚙️ Hạ Tầng CDE Đa Nền Tảng (Dalux • ACC • Trimble Connect)</span>
            <span class="badge badge-navy">CDE Strategy</span>
          </div>
          <ul class="check-list">
            <li><strong>Phân bổ nền tảng tối ưu:</strong> <em>ACC</em> chuyên worksharing thiết kế Revit nội bộ; <em>Trimble Connect</em> tối ưu Kết cấu thép &amp; OpenBIM nhẹ; <em>Dalux</em> dẫn đầu về kiểm tra hiện trường &amp; quản lý Issue/RFI thực tế.</li>
            <li><strong>Cấu trúc ISO 19650:</strong> Thiết lập Permission Matrix nghiêm ngặt cho 4 trạng thái dữ liệu: <code>WIP</code> ➔ <code>SHARED</code> ➔ <code>PUBLISHED</code> ➔ <code>ARCHIVED</code>.</li>
            <li><strong>Cầu nối OpenBIM (IFC / BCF):</strong> Luân chuyển dữ liệu mượt mà giữa các bên liên quan, chống độc quyền dữ liệu và tối ưu 35% chi phí bản quyền phần mềm.</li>
          </ul>
        </div>'''

assert old_cde_box in text, 'old_cde_box not found'
text = text.replace(old_cde_box, new_cde_box)

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print('SUCCESSFULLY APPLIED JD UPDATES TO HTML!')
