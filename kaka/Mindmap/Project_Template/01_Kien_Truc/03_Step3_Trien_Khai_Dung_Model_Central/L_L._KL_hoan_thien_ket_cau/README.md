# L. KL hoàn thiện kết cấu

- **Phân loại:** `TOPIC`
- **Đường dẫn phân cấp:** `BSV_Arch_Working process / Step 3: Triển khai dựng Model trên Central file / L. KL hoàn thiện kết cấu`

## 📋 Các nội dung / Quy chuẩn kỹ thuật chi tiết

### L.1 - L.1. Chạy tool: BSV_Arch_HoanThienCotTranKetCauDungSan_V1

### L.2 - L.2. Sử dụng lớp special equipment làm lớp hoàn thiện

### Cần thực hiện đúng các bước theo trình tự để không bị rối thông tin

### B1. Mở view 3D (1) full không ẩn cấu kiện gì hết, chạy tool joint tường với cột, tường với dầm

### B2. quét chọn trong view 3D (1) đó toàn bộ categories cột, dầm, special equipment (ván khuôn LTBT) => ẩn bằng lệnh EH

### B3. tạo 1 3D view mới có tên View KC (2), tạo filter cho parameter BSV_XuongChinh : TenXuong_KC => chọn filter contain cho parameter BSV_XuongChinh với KC

### B4. vào view 3D (1) HI (isolate) file kết cấu cần convert => chạy tool Convertlinktomodel dynamo trong sever để conver file link

### B5. Convert xong quét chọn toàn bộ đối tượng đã convert trong view 3D (1) gắn biến cho parameter BSV_XuongChinh : TenXuong_KC

### B6. qua view 3D (2) quét chọn toàn bộ ván khuôn => xóa

### B7. qua view 3D (1) dùng lệnh HR để hiện thị toàn bộ tường lên => chạy ván khuôn lại cho kết cấu và lấy phần này làm hoàn thiện kết cấu

### Nguyên tắc gán giá trị cho parameter: Luôn tập trung vào code đầu tiên để gán giá trị

### Code phân biệt tính chất: 
> Ex : mặt ngoài, 
> In: mặt trong, 
> Ce: trần, 
> St: thang , tam cấp

### + 1: 
> Code này áp dụng cho công tác trát kết cấu cùng mặt với tường xây hoặc phần diện tích kết cấu nhỏ nằm trong room sẽ được cộng vào khối lượng trát tường xây
> ví dụ: 	1.In(Ex). TratTrongSonTrong(TratNgoaiSonNgoai/TratNgoaiSonTrong...) :hiểu là hoàn thiện kết cấu trát trong sơn trong sẽ được cộng vào khối lượng trát tường trong, sơn tường trong, áp dụng cho cả phần lanh tô bổ trụ

### + 2: 
> Code này áp dụng cho công tác trát/mài kết cấu, sẽ tách ra đầu mục trát/mài kết cấu riêng nếu BoQ không có. Cấu kiện thường dùng: Chân tường ngoài nhà bằng bê tông, vách parapet mái, Vách bê tông, Vách bể . . .
> ví dụ: 2.Ex(In). TratNgoaiSonNgoai(TratTrongSonTrong/MaiNgoaiSonNgoai/MaiTrongSonTrong/MaiTrong/MaiNgoai/TratTrong/TratNgoai): 
> hiểu là hoàn thiện kết cấu trát/mài ngoài(trong) sơn ngoài(trong) khối lượng này là đầu mục công tác riêng, sơn ngoài(trong) được cộng vào khối lượng sơn tường ngoài(trong)

### + 3: 
> Code này áp dụng cho 2 mặt bên hông FW BEAM SIDE của dầm trần kết cấu có thể là trát hoặc mài tùy theo thông tin bản vẽ
> ví dụ: 3.Ce. TratTranSonTran : hiểu là hoàn thiện trát/mài 2 mặt bên của dầm trần kết cấu , khối lượng này sẽ được cộng vào KL hoàn thiện trần, khối lượng sơn được cộng vào sơn trần

### + 4: 
> Code này áp dụng cho 1 mặt đáy dầm trần, và mặt đáy sàn trần kết cấu
> ví dụ: 4.Ce. TratTranSonTran: hiểu là hoàn thiện trát/mài trần kết cấu, khối lượng này cộng vào kl hoàn thiện trần, kl sơn được cộng vào sơn trần

### + 5: 
> Code này áp dụng cho mặt đáy và mặt cạnh thang bộ, tam cấp
> ví dụ: 5.St. TratThangSonThang, 6.St. MaiThangSonThang: hiểu là hoàn thiện thang kết cấu trát/mài, khối lượng này cộng vào kl hoàn thiện thang bộ/tam cấp, kl sơn được cộng vào sơn thang, nếu không có mục sơn thang trong BoQ thì cộng vào sơn trần. đối với tam cấp sơn sẽ cộng vào sơn tường ngoài

### + 6: 
> Code này áp dụng cho mặt bậc thang bộ, tam cấp
> ví dụ: 6.Fs. (vật liệu hoàn thiện thang bộ viết hoa chữ đầu: hiểu là hoàn thiện mặt bậc thang bộ/tam cấp. bao gồm luôn chiếu tới nếu có.
