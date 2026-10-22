#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using kaka.Structure.Structural.App.Models.PrimaveraP6;

namespace kaka.Structure.Structural.App.Services.PrimaveraP6;

/// <summary>
/// Dịch vụ cấu hình và sinh quy tắc Auto-Attach TimeLiner cho Autodesk Navisworks Manage.
/// Giúp tự động gán (Auto-Attach Using Rules) 100% tiến độ Primavera P6 vào các Search Sets 3D
/// mà không cần gán thủ công từng cấu kiện, hỗ trợ Appearance Profiles phân màu theo công tác.
/// </summary>
public static class NavisworksTimeLinerService
{
  /// <summary>
  /// Xuất file CSV định dạng chuẩn Navisworks TimeLiner Data Source.
  /// Khi import vào TimeLiner -> Task Name khớp với Search Sets -> Chạy Rule là tự động link 100%.
  /// </summary>
  public static void ExportTimeLinerCsv(string filePath, IEnumerable<Bim4DTaskItem> tasks)
  {
    var sb = new StringBuilder();
    // Headers chuẩn tương thích tuyệt đối với Navisworks TimeLiner Field Mapping
    sb.AppendLine("Task Name,Display ID,Status,Planned Start,Planned End,Actual Start,Actual End,Task Type,Attached,Cost,Notes");

    foreach (var t in tasks)
    {
      var plannedStart = t.StartDate.ToString("yyyy-MM-dd 08:00:00");
      var plannedEnd = t.EndDate.ToString("yyyy-MM-dd 17:00:00");
      var notes = $"WBS: {t.WbsCode}; Qty: {t.Quantity:N1} {t.Unit}; Prod: {t.ProductivityRate}/day; Pred: {t.PredecessorsDisplay}; Critical: {t.IsCritical}";

      sb.AppendLine(
        $"\"{t.TaskName}\"," +
        $"\"{t.TaskCode}\"," +
        $"\"Planned\"," +
        $"\"{plannedStart}\"," +
        $"\"{plannedEnd}\"," +
        $"\"\"," +
        $"\"\"," +
        $"\"{t.TaskType}\"," +
        $"\"\"," +
        $"\"0\"," +
        $"\"{notes}\"");
    }

    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
  }

  /// <summary>
  /// Xuất file quy tắc TimeLiner Rules XML.
  /// </summary>
  public static void ExportTimeLinerRulesXml(string filePath)
  {
    var settings = new XmlWriterSettings
    {
      Indent = true,
      IndentChars = "  ",
      Encoding = new UTF8Encoding(false)
    };

    using var writer = XmlWriter.Create(filePath, settings);
    writer.WriteStartDocument();

    writer.WriteStartElement("exchange", "http://download.autodesk.com/us/navisworks/schemas/nw-timeliner-rules-12.0.xsd");
    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

    writer.WriteStartElement("rules");

    // Quy tắc 1: Map theo Task Name khớp với tên Search Set
    writer.WriteStartElement("rule");
    writer.WriteAttributeString("name", "KAKA 4D: Map TimeLiner Tasks to Search Sets (Exact Name)");
    writer.WriteAttributeString("enabled", "true");
    writer.WriteElementString("description", "Tự động gán 100% hình học từ Search Sets vào Task TimeLiner có cùng tên gọi (Không phân biệt hoa thường)");
    writer.WriteStartElement("mapping");
    writer.WriteAttributeString("task_column", "Task Name");
    writer.WriteAttributeString("set_property", "Name");
    writer.WriteAttributeString("match_mode", "exact_case_insensitive");
    writer.WriteEndElement(); // mapping
    writer.WriteEndElement(); // rule 1

    // Quy tắc 2: Map theo Activity Code chứa mã cấu kiện
    writer.WriteStartElement("rule");
    writer.WriteAttributeString("name", "KAKA 4D: Map Tasks by Component Code Prefix");
    writer.WriteAttributeString("enabled", "true");
    writer.WriteElementString("description", "Gán công tác dựa trên mã Display ID tương ứng với phân mục Search Set");
    writer.WriteStartElement("mapping");
    writer.WriteAttributeString("task_column", "Display ID");
    writer.WriteAttributeString("set_property", "Name");
    writer.WriteAttributeString("match_mode", "contains");
    writer.WriteEndElement(); // mapping
    writer.WriteEndElement(); // rule 2

    writer.WriteEndElement(); // rules
    writer.WriteEndElement(); // exchange
    writer.WriteEndDocument();
  }

  /// <summary>
  /// Xuất tài liệu SOP Hướng dẫn quy trình 5 bước Rule-based 4D BIM (Navisworks + P6).
  /// </summary>
  public static void ExportWorkflowGuideMd(string filePath, string projectName)
  {
    var content = $@"# QUY TRÌNH THIẾT LẬP RULE-BASED 4D BIM (NAVISWORKS + PRIMAVERA P6)
**Dự án:** {projectName}  
**Giải pháp:** Hệ sinh thái KAKA BIM Suite (Thay thế toàn diện Bexel Manager)  
**Tác giả:** KAKA BIM Ecosystem  

---

## 🎯 TỔNG QUAN GIẢI PHÁP
Quy trình giúp nhóm dự án đạt được **luồng làm việc trực quan từ mô hình 3D sang mô phỏng tiến độ 4D** như Bexel Manager nhưng vẫn giữ nguyên hệ sinh thái tiêu chuẩn quốc tế **Navisworks Manage + Primavera P6**, loại bỏ hoàn toàn chi phí mua thêm license Bexel đắt đỏ.

```mermaid
flowchart LR
    A[Mô Hình 3D Revit] -->|Add-in KAKA| B[Quantification & Duration]
    B -->|Xuất Search Sets XML| C[Navisworks Sets]
    B -->|Xuất XER có CPM Predecessor| D[Primavera P6]
    D -->|Chạy F9 CPM Đường Găng| E[Đồng bộ Tiến độ]
    C -->|Auto-Attach Using Rules| F[TimeLiner 4D Simulation]
    E -->|TimeLiner Tasks CSV| F
    F -->|Appearance Profiles| G[Mô Phỏng 4D Đa Màu Sắc]
```

---

## 🚀 QUY TRÌNH THỰC HIỆN 5 BƯỚC

### BƯỚC 1: Xuất Bộ Dữ Liệu 4D từ Add-in KAKA
1. Mở mô hình Revit kết cấu / kiến trúc trong Autodesk Revit.
2. Trên Ribbon **KAKA**, chọn panel **Primavera P6** -> bấm **Rule-Based 4D BIM**.
3. Bấm nút **""Xuất Trọn Bộ 4D BIM (Navisworks + P6)""**.
4. Hệ thống sẽ tự động sinh 4 file đồng bộ:
   - `{projectName}_Navisworks_SearchSets.xml`: Cây thư mục Search Sets theo phân khu & bộ môn.
   - `{projectName}_P6_4D_CPM_Schedule.xer`: Tiến độ P6 có sẵn WBS, Khối lượng, Định mức và Quan hệ CPM.
   - `{projectName}_TimeLiner_Schedule.csv`: Bảng công tác sẵn sàng link vào Navisworks TimeLiner.
   - `{projectName}_TimeLiner_AutoAttach_Rules.xml`: Quy tắc tự động link hình học.

---

### BƯỚC 2: Nhập Search Sets vào Navisworks Manage
1. Mở file NWC / NWD của dự án trong **Autodesk Navisworks Manage**.
2. Mở cửa sổ **Sets** (Home -> Sets).
3. Chuột phải vào cửa sổ Sets -> chọn **Import Search Sets**.
4. Chọn file `{projectName}_Navisworks_SearchSets.xml`.
5. 👉 **Kết quả:** Navisworks tự động tạo toàn bộ Search Sets cho Cọc, Móng, Cột, Dầm, Sàn, Vách, Tường Xây, MEP... và tự động chọn chính xác các phần tử 3D tương ứng.

---

### BƯỚC 3: Chạy Đường Găng CPM trên Primavera P6
1. Mở **Oracle Primavera P6 Professional**.
2. Chọn **File -> Import -> Primavera PM (XER)**.
3. Chọn file `{projectName}_P6_4D_CPM_Schedule.xer`.
4. Mở dự án vừa import, bấm phím tắt **F9 (Schedule)** để Primavera P6 tính toán đường găng Critical Path Method.
5. Kiểm tra các công tác màu đỏ (Critical Path) và thời gian dự trữ (Total Float).

---

### BƯỚC 4: Đồng bộ vào Navisworks TimeLiner & Chạy Auto-Attach
1. Trong Navisworks Manage, mở cửa sổ **TimeLiner** (Home -> Tools -> TimeLiner).
2. Chuyển sang tab **Data Sources** -> chọn **Add -> CSV Import**.
3. Trỏ tới file `{projectName}_TimeLiner_Schedule.csv` -> Map các trường:
   - `Task Name` -> Task Name
   - `Display ID` -> Synchronization ID
   - `Planned Start` -> Planned Start Date
   - `Planned End` -> Planned End Date
   - `Task Type` -> Task Type
4. Chuột phải vào Data Source vừa tạo -> chọn **Rebuild Task Hierarchy**. Toàn bộ Tasks xuất hiện trong tab Tasks.
5. Bấm nút **Rules** trong TimeLiner:
   - Tích chọn: **""Map TimeLiner Tasks from Column 'Task Name' to Selection Sets with the same name, Matching Case: No""**.
   - Bấm **Apply Rules**.
6. 👉 **Kết quả:** 100% các công tác trong TimeLiner đã được link tự động với các đối tượng 3D tương ứng từ Search Sets mà không tốn 1 giây kéo thả tay!

---

### BƯỚC 5: Thiết Lập Appearance Profiles (Phân Màu Mô Phỏng)
Trong tab **TimeLiner -> Configure**:
- **Construct (Thi công mới):**
  - Start Appearance: `Green (80% Transparent)` - Thể hiện cấu kiện đang lắp dựng/đổ bê tông.
  - End Appearance: `Model Appearance` - Trở về màu vật liệu thực tế sau khi hoàn thành.
- **Demolish (Phá dỡ):**
  - Start Appearance: `Red (50% Transparent)`.
  - End Appearance: `Hide` - Ẩn khỏi mô hình.
- **Temporary (Tạm thời - Ván khuôn, giàn giáo):**
  - Start Appearance: `Yellow (60% Transparent)`.
  - End Appearance: `Hide`.

Chuyển sang tab **Simulate** -> Bấm **Play** để xem toàn bộ tòa nhà được xây dựng tự động từ móng đến mái theo đúng đường găng CPM của Primavera P6!

---
*Tài liệu được sinh tự động bởi Hệ Sinh Thái KAKA BIM Suite - Rule-based 4D Engine.*
";

    File.WriteAllText(filePath, content, Encoding.UTF8);
  }
}
