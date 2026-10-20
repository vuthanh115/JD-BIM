# 🏗️ BỘ CÂY THƯ MỤC DỰ ÁN TIÊU CHUẨN (BIM & SHOP DRAWING)
*Trích xuất và chuẩn hóa tự động từ Mindmap BSV / KAKA*

---

## 📂 1. Phân Hệ Kiến Trúc (`01_Kien_Truc/`)
Quy trình BIM kiến trúc 4 bước hoàn chỉnh:
- **`00_Khoi_Tao_Thu_Muc_Server/`**: Cây thư mục máy chủ (Architech > Cad template [Mặt bằng, Mặt đứng, Mặt cắt, Thang bộ], Rvt file [Central/Local/Templates], BoQ Architech).
- **`01_Step1_Tiep_Can_Ban_Ve/`**: Tiếp cận bản vẽ, đọc tổng quan, chỉ mục chung, Schedules Finishing, chi tiết lanh tô bổ trụ giằng tường.
- **`02_Step2_Xu_Ly_Ban_Ve/`**: Tách bản vẽ CAD bằng lệnh BURST, link vào Central file, tạo Grid & Level, xử lý thống kê cửa.
- **`03_Step3_Trien_Khai_Dung_Model_Central/`**: Dựng tường, Set Room toàn dự án, dựng sàn/trần, cửa, lanh tô bổ trụ, hoàn thiện theo Room, chi tiết kết cấu.
- **`04_Step4_Thong_Ke_KL_Nhap_BoQ/`**: Thiết lập khung bảng thống kê, gán parameter, kiểm tra chéo Model & Schedule, xuất BoQ.

---

## 📐 2. Phân Hệ Bóc Tách Khối Lượng Kết Cấu (`02_Ket_Cau/`)
Quy chuẩn bóc tách khối lượng và bảng tra cho 17 cấu kiện:
- **`00_Quy_Chuan_BoQ_Va_Bang_Tra/`**: Thứ tự ưu tiên tính toán & Join (`Sàn > Móng > Cột > Dầm > Vách > Cầu Thang`), thông số Project Parameters, quy trình 7 bước tính khối lượng.
- **14 Thư mục cấu kiện chuẩn:**
  1. `01.Coc_Pile/` (Cọc ly tâm D300, 350, 400, 500, Tool BSV_AutoPile)
  2. `02.Mong_Foundation/` (Móng đơn, móng băng, móng cọc, Tool BSV_AutoFoundation)
  3. `03.DaKieng_TieBeam/` (Đà kiềng rộng x cao, ván khuôn)
  4. `04.Nen_GroundSlab/` (Nền chiều dày H sàn, BTL 50_BTL)
  5. `05.Cot_Column/` (Cột ngắn x dài, Tool BSV_Str_TaoCot_Ver4)
  6. `06.Vach_ShearWall/` (Vách chiều dày, trám lỗ ti)
  7. `07.Dam_Beam/` (Dầm B25, ván khuôn, thép)
  8. `08.San_Slab/` (Sàn hoàn thiện, ván khuôn đáy & thành)
  9. `09.CauThang_Stair/` (Cầu thang, dầm thang, chiếu nghỉ)
  10. `10.TamCap_Steps/` (Tam cấp, đà tam cấp)
  11. `11.Ramp/` (Ramp dốc, vách ramp)
  12. `12.Pit_ThangMay/` (Hố pit thang máy)
  13. `13.Dock_Hang/` (Dock xuất nhập hàng)
  14. `14.CauKienPhu_Secondary/` (Mương, bệ máy, trám lỗ ti)

---

## 🔩 3. Phân Hệ Shop Drawing Cốt Thép (`03_Shop_Drawing_Rebar/`)
Quy chuẩn triển khai bản vẽ Shop Drawing chi tiết cho từng cấu kiện:
- **`01_Join_Cau_Kien_Theo_Quy_Tac/`**: Quy chuẩn liên kết giao cắt cốt thép giữa các cấu kiện.
- **13 Thư mục cấu kiện Shop Rebar**, mỗi cấu kiện bao gồm đầy đủ 5 đề mục tiêu chuẩn:
  - `Quy_cach_ve/`: Sử dụng kaka Rebar / Structure Rebar, quy chuẩn neo, bẻ móc, đoạn nối chồng.
  - `Quy_cach_dat_ten_thep/`: Cấu trúc Partition, BSV_PhanLoai, BSV_Comments.
  - `Xuat_ban_ve/`: View template, mặt bằng, mặt cắt, bố trí Viewport trên Sheet.
  - `Bang_thong_ke/`: Template Schedule thép, bộ lọc Filter, Sorting theo đường kính/hình dạng.
  - `Key_Plan/`: Mặt bằng định vị cấu kiện và đường gióng phạm vi nối cắt thép.
