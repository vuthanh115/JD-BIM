# 00. Quy Chuẩn Bóc Tách Khối Lượng BoQ Kết Cấu

Quy ước tính khối lượng với QS và bảng tra tiêu chuẩn cho 17 cấu kiện kết cấu.

### Thứ tự ưu tiên tính toán & Join cấu kiện:
`Sàn > Móng > Cột > Dầm > Vách > Cầu Thang` *(Sử dụng ATool để chạy Join tự động)*

### Quy trình 7 bước tính khối lượng:
1. Gán biến bê tông theo BoQ (chia nhỏ để dễ đối chiếu).
2. Chạy Join bê tông theo quy ước (Load thứ tự trong ATool).
3. Chạy ván khuôn (Bim Speed), gán biến ván khuôn (Tool BSV_PhanLoaiVanKhuon).
4. Kiểm tra trùng lặp (Tool BSV_KiemTraTrungNhau).
5. Kiểm tra bảng thống kê với Model 3D và xuất khối lượng ra Excel.
6. Link khối lượng vào BoQ (tuyệt đối không tính toán thêm trong bảng).
7. Sum tổng khối lượng lót, ván lót, bê tông, ván khuôn khớp số.

## [A.TH] A.THỨ TỰ ƯU TIÊN TÍNH TOÁN
(Quy ước tính khối lượng với QS, xem file excel để hiểu chi tiết)
**Phân loại:** `BRANCH`

## [B] B. PROJECT PARAMETERS (REVIT)
**Phân loại:** `BRANCH`

## [C.QUY] C.QUY TRÌNH
**Phân loại:** `BRANCH`

## [00.1.2.KL] 00.1.2.KL BÊ TÔNG
**Phân loại:** `TABLE`

## [00.1.2.KL] 00.1.2.KL BÊ TÔNG
**Phân loại:** `TABLE`

## [00.2.2.KL] 00.2.2.KL VÁN KHUÔN
**Phân loại:** `TABLE`

## [00.2.2.KL] 00.2.2.KL VÁN KHUÔN
**Phân loại:** `TABLE`

## [00.3.1KL] 00.3.1KL DIỆN TÍCH
**Phân loại:** `TABLE`

## [01.1.C] 01.1.CỌC
**Phân loại:** `TABLE`

## [00.2.1.KL] 00.2.1.KL VÁN KHUÔN LÓT
**Phân loại:** `TABLE`

