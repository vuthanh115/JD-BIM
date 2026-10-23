path = r'c:\Users\vophi\Downloads\Github\CV\JD-BIM\BIM_Manager_Executive_Dossier_4D_Navisworks.html'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# Old section title and subtitle
old_sec_title = '''      <div class="section-title">
        <span>🏆 Năng Lực Triển Khai Thực Chiến Cấp Tổng Thầu (GLC) & Đại Dự Án</span>
      </div>
      <div class="section-subtitle">
        Minh chứng năng lực chủ trì quản lý mô hình BIM 1.500.000+ m² sàn công nghiệp thực tế tại Tổng thầu Golden Lotus Construction (GLC)
      </div>'''

new_sec_title = '''      <div class="section-title">
        <span>🏆 Năng Lực Điều Phối Đa Bộ Môn, Kiểm Soát Thầu Phụ & Hỗ Trợ BOQ</span>
      </div>
      <div class="section-subtitle">
        Kinh nghiệm thực chiến chủ trì combine mô hình liên bộ môn (KC-KT-MEP), kiểm soát chất lượng thiết kế/Shop drawing và đối soát BOQ thầu phụ
      </div>'''

assert old_sec_title in text, 'old_sec_title not found'
text = text.replace(old_sec_title, new_sec_title)

# Old table body
old_tbody = '''        <tbody>
          <tr>
            <td><strong>Mega Textile</strong><br><span class="badge badge-blue">CĐT Trung Quốc</span></td>
            <td><strong>67.5 ha</strong><br>VSIP 2 Nghệ An</td>
            <td>Tổ hợp nhà máy dệt may quy mô lớn nhất miền Trung. Hơn 1.200 tấn kết cấu thép, 500+ công nhân thi công đồng thời, yêu cầu phối hợp MEP cực dày đặc.</td>
            <td>Phân chia 6 Worksets độc lập theo phân khu, dựng mô hình CSD xử lý 840 điểm va chạm ống công nghệ; rút ngắn 22 ngày thi công móng.</td>
          </tr>
          <tr>
            <td><strong>Nhà Xưởng APF</strong><br><span class="badge badge-green">CĐT FDI</span></td>
            <td><strong>122.000 m²</strong><br>KCN Đồng Nai</td>
            <td>Nhà máy chế biến nông sản xuất khẩu tiêu chuẩn HACCP. Hệ sàn phẳng bê tông chịu tải 5 tấn/m², hệ thống thoát nước ngầm dốc phức tạp.</td>
            <td>Áp dụng quy tắc Join cấu kiện kaka, bóc tách chính xác khối lượng bê tông mác 350; sai số nghiệm thu khối lượng BOQ &lt; 0.6%.</td>
          </tr>
          <tr>
            <td><strong>Nhà Máy Kim Tín</strong><br><span class="badge badge-amber">Tập đoàn Kim Tín</span></td>
            <td><strong>311.845 m²</strong><br>Dầu Giây, Đồng Nai</td>
            <td>Nhà máy ván MDF hiện đại bậc nhất Đông Nam Á. Cẩu trục 30T, móng máy rung động phức tạp, nhiều cao độ sàn lệch nhau.</td>
            <td>Dựng mô hình phối hợp 4D Primavera P6 mô phỏng nâng hạ kết cấu dầm thép 42m; đảm bảo an toàn tuyệt đối 100% không sự cố cẩu lắp.</td>
          </tr>
          <tr>
            <td><strong>Aimer Vietnam</strong><br><span class="badge badge-purple">Chủ đầu tư May mặc</span></td>
            <td><strong>20.000 m²</strong><br>KCN Tân Đức, Long An</td>
            <td>Nhà máy dệt may cao cấp tiêu chuẩn công trình xanh (LEED Gold). Hệ thống HVAC kiểm soát nhiệt ẩm nghiêm ngặt.</td>
            <td>Mô phỏng đường gió CFD, phối hợp bản vẽ lỗ mở SOD dầm sàn trước khi đổ bê tông; triệt tiêu 100% hiện tượng đục phá sàn sau hoàn thiện.</td>
          </tr>
        </tbody>'''

new_tbody = '''        <tbody>
          <tr>
            <td><strong>Mega Textile</strong><br><span class="badge badge-blue">CĐT Trung Quốc</span></td>
            <td><strong>67.5 ha</strong><br>VSIP 2 Nghệ An</td>
            <td>Phối hợp thiết kế đa bộ môn (Kết cấu thép, Kiến trúc bao che, hệ thống ống công nghệ MEP dày đặc). Kiểm soát tiến độ và chất lượng nộp hồ sơ của nhiều thầu phụ.</td>
            <td>Chủ trì <strong>combine mô hình liên bộ môn</strong>, xử lý dứt điểm 840 điểm xung đột CSD/SOD; <strong>kiểm soát hồ sơ Shop Drawing &amp; thiết kế thầu phụ</strong>, chuẩn hóa khối lượng BOQ không bị đội chi phí.</td>
          </tr>
          <tr>
            <td><strong>Nhà Xưởng APF</strong><br><span class="badge badge-green">CĐT FDI</span></td>
            <td><strong>122.000 m²</strong><br>KCN Đồng Nai</td>
            <td>Yêu cầu kiểm soát khối lượng dự toán (BOQ) nghiêm ngặt; hệ sàn bê tông chịu tải 5T/m² và hệ thống thoát nước ngầm dốc giao cắt móng đà kiềng.</td>
            <td><strong>Hỗ trợ bóc tách BOQ tự động</strong> từ mô hình với sai số &lt; 0.6%; thiết lập quy trình đối soát khối lượng nghiệm thu và thanh toán cho các gói thầu phụ bê tông, cốt thép.</td>
          </tr>
          <tr>
            <td><strong>Nhà Máy Kim Tín</strong><br><span class="badge badge-amber">Tập đoàn Kim Tín</span></td>
            <td><strong>311.845 m²</strong><br>Dầu Giây, Đồng Nai</td>
            <td>Khối lượng kết cấu thép và thiết bị cơ điện cực lớn; cao độ sàn giật cấp phức tạp, nhiều nhà thầu phụ cùng tham gia triển khai Shop Drawing.</td>
            <td><strong>Kiểm soát mô hình Shop drawing &amp; cấu kiện chế tạo của thầu phụ</strong>; combine không gian lắp đặt thiết bị công nghệ với dầm sàn kết cấu, khóa cứng BOQ thép trước khi gia công xưởng.</td>
          </tr>
          <tr>
            <td><strong>Aimer Vietnam</strong><br><span class="badge badge-purple">Chủ đầu tư May mặc</span></td>
            <td><strong>20.000 m²</strong><br>KCN Tân Đức, Long An</td>
            <td>Tiêu chuẩn công trình xanh LEED Gold khắt khe; hệ thống điều hòa HVAC kiểm soát nhiệt ẩm đòi hỏi hàng trăm vị trí xuyên dầm sàn.</td>
            <td><strong>Combine bộ môn Kiến trúc - Kết cấu - Cơ điện MEP</strong>, duyệt bản vẽ lỗ mở SOD xuyên dầm cho thầu phụ trước khi đổ bê tông; <strong>kiểm soát khối lượng hoàn thiện BOQ</strong> chống phát sinh.</td>
          </tr>
        </tbody>'''

assert old_tbody in text, 'old_tbody not found'
text = text.replace(old_tbody, new_tbody)

# Also update footer on page 2
old_p2_footer = '<span>Hồ sơ Năng lực Thực chiến Tổng thầu GLC • 1.5M+ m² sàn công nghiệp</span>'
new_p2_footer = '<span>Năng Lực Điều Phối Liên Bộ Môn • Kiểm Soát Thiết Kế, Shop Drawing &amp; Đối Soát BOQ Thầu Phụ</span>'

if old_p2_footer in text:
    text = text.replace(old_p2_footer, new_p2_footer)

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print('SUCCESSFULLY_UPDATED_PAGE2_TABLE')
