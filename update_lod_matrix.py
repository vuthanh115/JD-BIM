path = r'c:\Users\vophi\Downloads\Github\CV\JD-BIM\BIM_Manager_Executive_Dossier_4D_Navisworks.html'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

old_lod_box = '''        <div class="box box-gray">
          <div class="box-title">
            <span>📐 Ma Trận Phân Cấp Mức Độ Chi Tiết (LOD Matrix)</span>
            <span class="badge badge-teal">Standards</span>
          </div>
          <table class="table-report" style="margin-bottom: 0;">
            <thead>
              <tr><th>Mức LOD</th><th>Giai Đoạn</th><th>Mục Tiêu Quản Trị & Hồ Sơ Đầu Ra</th></tr>
            </thead>
            <tbody>
              <tr><td><strong>LOD 200</strong></td><td>Thiết kế cơ sở</td><td>Khối tích hình học, mặt bằng sơ bộ, khái toán tổng mức đầu tư.</td></tr>
              <tr><td><strong>LOD 300</strong></td><td>Hồ sơ TKKT</td><td>Kích thước chuẩn xác, chủng loại vật liệu, va chạm Clash bước đầu.</td></tr>
              <tr><td><strong>LOD 400</strong></td><td>Shop Drawing</td><td>Chi tiết cốt thép uốn (BBS), phụ kiện MEP, sẵn sàng gia công tại xưởng.</td></tr>
              <tr><td><strong>LOD 500</strong></td><td>As-Built & O&M</td><td>Mô hình hoàn công số hóa, tích hợp CO/CQ phục vụ bảo trì 7D FM.</td></tr>
            </tbody>
          </table>
        </div>'''

new_lod_box = '''        <div class="box box-gray">
          <div class="box-title">
            <span>📐 Ma Trận Tương Quan: Chiều BIM (3D-4D-5D) ⟷ Mức Độ Phát Triển (LOD)</span>
            <span class="badge badge-teal">Dimensions vs LOD</span>
          </div>
          <table class="table-report" style="margin-bottom: 0; font-size: 6.8pt;">
            <thead>
              <tr>
                <th style="width: 14%;">Mức LOD</th>
                <th style="width: 28%;">3D (Không Gian / LOG)</th>
                <th style="width: 29%;">4D (Tiến Độ P6/Navis)</th>
                <th style="width: 29%;">5D (Chi Phí &amp; BOQ)</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td><strong>LOD 200</strong><br><span style="color:#64748b;">(Thiết kế cơ sở)</span></td>
                <td>Khối tích sơ bộ, định vị lưới trục, dầm 300x600 chưa chốt mác.</td>
                <td>Tiến độ tổng thể cấp 1-2 theo khối công trình (Ngầm, Thân, Mái).</td>
                <td>Khái toán tổng mức đầu tư theo diện tích sàn ($/m²).</td>
              </tr>
              <tr>
                <td><strong>LOD 300</strong><br><span style="color:#64748b;">(Thiết kế KT/Tender)</span></td>
                <td>Tiết diện chuẩn tính toán (dầm 300x700, B30), kiểm tra Major Clash.</td>
                <td>Tiến độ đấu thầu theo Tầng/Khu vực (WBS Floor-by-Floor).</td>
                <td>BOQ bê tông, ván khuôn chuẩn; cốt thép tính theo hàm lượng (kg/m³).</td>
              </tr>
              <tr>
                <td><strong>LOD 350</strong><br><span style="color:#64748b;">(Phối hợp CSD/SOD)</span></td>
                <td>Triệt tiêu va chạm đa hệ, chốt lỗ mở xuyên dầm (Sleeve), bản mã.</td>
                <td>Mô phỏng 4D theo phân đoạn đổ bê tông (Pouring Zone) &amp; MEP.</td>
                <td>Kiểm soát chi phí phát sinh VE, tránh đội ngân sách trước thi công.</td>
              </tr>
              <tr>
                <td><strong>LOD 400</strong><br><span style="color:#64748b;">(Shop &amp; Chế tạo)</span></td>
                <td>Mô hình cốt thép chi tiết (BBS), bu lông, phụ kiện ống sẵn sàng xưởng.</td>
                <td>Tiến độ thi công chi tiết Look-ahead hàng tuần, cẩu tháp, mặt bằng.</td>
                <td>Bóc tách chính xác 100% từng thanh thép, đặt hàng vật tư &amp; thanh toán thầu phụ.</td>
              </tr>
              <tr>
                <td><strong>LOD 500</strong><br><span style="color:#64748b;">(Hoàn công &amp; O&amp;M)</span></td>
                <td>Mô hình hoàn công số hóa 100% khớp thực tế hiện trường.</td>
                <td>Đối chiếu tiến độ thực tế với kế hoạch (As-built vs As-planned).</td>
                <td>Quyết toán giá trị hợp đồng, tích hợp dữ liệu CO/CQ vận hành 7D FM.</td>
              </tr>
            </tbody>
          </table>
        </div>'''

assert old_lod_box in text, 'old_lod_box not found'
text = text.replace(old_lod_box, new_lod_box)

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print('SUCCESSFULLY_UPDATED_PAGE2_LOD_MATRIX')
