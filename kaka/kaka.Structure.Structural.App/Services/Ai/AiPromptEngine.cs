#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.Services.Database;

namespace kaka.Structure.Structural.App.Services.Ai;

public sealed class PromptExecutionResult
{
  public bool Success { get; set; } = true;
  public string Title { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public List<long> AffectedElementIds { get; set; } = new();
  public string? ActionType { get; set; }
}

public static class AiPromptEngine
{
  public static PromptExecutionResult Execute(UIApplication uiapp, string rawPrompt)
  {
    if (string.IsNullOrWhiteSpace(rawPrompt))
    {
      return new PromptExecutionResult
      {
        Success = false,
        Title = "Prompt trống",
        Message = "Vui lòng nhập câu lệnh hoặc yêu cầu bạn muốn AI thực hiện."
      };
    }

    var uiDoc = uiapp.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      return new PromptExecutionResult
      {
        Success = false,
        Title = "Chưa mở dự án",
        Message = "Vui lòng mở một mô hình Revit trước khi gửi yêu cầu cho AI."
      };
    }

    var doc = uiDoc.Document;
    var normalized = RemoveAccents(rawPrompt.ToLowerInvariant().Trim());

    // 1. Ý định Bóc tách BoQ / Khối lượng
    if (normalized.Contains("boc") || normalized.Contains("khoi luong") ||
        normalized.Contains("boq") || normalized.Contains("takeoff") ||
        normalized.Contains("the tich"))
    {
      return ExecuteBoQTakeoff(doc);
    }

    // 2. Ý định Thông tin Dự án / Levels
    if (normalized.Contains("thong tin") || normalized.Contains("du an") ||
        normalized.Contains("mo hinh") || normalized.Contains("tang") && normalized.Contains("danh sach") ||
        normalized.Contains("levels") || normalized.Contains("project info"))
    {
      return ExecuteModelInfo(doc);
    }

    // 3. Ý định Gán tham số (Set Parameter)
    if (normalized.Contains("gan") || normalized.Contains("doi ten") ||
        normalized.Contains("set parameter") || normalized.Contains("comments") && (normalized.Contains("la") || normalized.Contains("thanh")))
    {
      return ExecuteSetParameter(doc, uiDoc, rawPrompt, normalized);
    }

    // 4. Ý định Bố trí cốt thép cột (Column Rebar)
    if ((normalized.Contains("thep") || normalized.Contains("rebar") || normalized.Contains("cot thep")) &&
        (normalized.Contains("cot") || normalized.Contains("column") || normalized.Contains("bo tri") || normalized.Contains("dat thep")))
    {
      return ExecuteColumnRebar(uiDoc, doc, rawPrompt, normalized);
    }

    // 5. Ý định Tìm kiếm / Lọc / Chọn / Zoom cấu kiện
    return ExecuteQueryOrSelect(uiDoc, doc, rawPrompt, normalized);
  }

  private static PromptExecutionResult ExecuteBoQTakeoff(Document doc)
  {
    try
    {
      var items = RevitBoQExtractor.ExtractBoQ(doc);
      var summaries = RevitBoQExtractor.Summarize(items);

      var sb = new StringBuilder();
      sb.AppendLine($"📊 **Tổng hợp khối lượng BoQ từ mô hình:**");
      sb.AppendLine($"• Dự án: **{doc.ProjectInformation?.Name ?? doc.Title}**");
      sb.AppendLine($"• Tổng số cấu kiện kết cấu: **{items.Count:N0}**");
      sb.AppendLine();
      sb.AppendLine("| Đề mục | Tên cấu kiện | Số lượng | Bê tông (m³) | Ván khuôn (m²) |");
      sb.AppendLine("| :--- | :--- | :---: | :---: | :---: |");

      foreach (var s in summaries)
      {
        sb.AppendLine($"| `{s.CategoryCode}` | {s.CategoryName} | {s.Count:N0} | {s.TotalVolumeM3:N2} | {s.TotalAreaM2:N2} |");
      }

      return new PromptExecutionResult
      {
        Success = true,
        Title = "Bóc tách khối lượng BoQ thành công",
        Message = sb.ToString(),
        ActionType = "BOQ"
      };
    }
    catch (Exception ex)
    {
      return new PromptExecutionResult
      {
        Success = false,
        Title = "Lỗi khi trích xuất BoQ",
        Message = ex.Message
      };
    }
  }

  private static PromptExecutionResult ExecuteModelInfo(Document doc)
  {
    var levels = new FilteredElementCollector(doc)
      .OfClass(typeof(Level))
      .Cast<Level>()
      .OrderBy(l => l.Elevation)
      .ToList();

    var sb = new StringBuilder();
    sb.AppendLine($"🏢 **Thông tin mô hình Revit hiện tại:**");
    sb.AppendLine($"• Tên dự án: **{doc.ProjectInformation?.Name ?? "Chưa đặt tên"}**");
    sb.AppendLine($"• Tập tin: `{doc.Title}.rvt`");
    sb.AppendLine($"• View đang mở: **{doc.ActiveView?.Name}** ({doc.ActiveView?.ViewType})");
    sb.AppendLine();
    sb.AppendLine($"**Danh sách các tầng ({levels.Count} tầng):**");
    foreach (var lvl in levels)
    {
      var elevM = lvl.Elevation * 0.3048;
      sb.AppendLine($"• **{lvl.Name}**: cao độ `{elevM:+0.000;-0.000;0.000} m` (ID: `{lvl.Id.Value}`)");
    }

    return new PromptExecutionResult
    {
      Success = true,
      Title = "Thông tin dự án",
      Message = sb.ToString(),
      ActionType = "INFO"
    };
  }

  private static PromptExecutionResult ExecuteQueryOrSelect(
    UIDocument uiDoc,
    Document doc,
    string rawPrompt,
    string normalized)
  {
    var isSelectAction = normalized.Contains("chon") || normalized.Contains("select") ||
                         normalized.Contains("zoom") || normalized.Contains("highlight") ||
                         normalized.Contains("boi xanh");

    var bic = DetectCategory(normalized);
    var targetLevel = DetectLevel(doc, normalized, rawPrompt);
    var searchKeyword = DetectSearchKeyword(rawPrompt, normalized);

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (bic.HasValue)
    {
      collector = collector.OfCategory(bic.Value);
    }
    else
    {
      // Nếu không nói rõ category, lọc các categories chính
      var filter = new ElementMulticategoryFilter(new[]
      {
        BuiltInCategory.OST_Walls,
        BuiltInCategory.OST_StructuralColumns,
        BuiltInCategory.OST_StructuralFraming,
        BuiltInCategory.OST_Floors,
        BuiltInCategory.OST_StructuralFoundation,
        BuiltInCategory.OST_Doors,
        BuiltInCategory.OST_Windows,
        BuiltInCategory.OST_Stairs
      });
      collector = collector.WherePasses(filter);
    }

    var matches = new List<Element>();
    foreach (var elem in collector)
    {
      if (elem.Category == null) continue;

      // Kiểm tra Level
      if (targetLevel != null)
      {
        var levelId = elem.LevelId;
        var elemLevel = levelId != ElementId.InvalidElementId ? doc.GetElement(levelId)?.Name : null;
        if (string.IsNullOrWhiteSpace(elemLevel))
        {
          var p = elem.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM)
                  ?? elem.get_Parameter(BuiltInParameter.LEVEL_PARAM);
          elemLevel = p?.AsValueString() ?? p?.AsString();
        }

        if (string.IsNullOrWhiteSpace(elemLevel) ||
            elemLevel.IndexOf(targetLevel, StringComparison.OrdinalIgnoreCase) < 0)
        {
          continue;
        }
      }

      // Kiểm tra từ khóa
      if (!string.IsNullOrWhiteSpace(searchKeyword))
      {
        var typeId = elem.GetTypeId();
        var elemType = typeId != ElementId.InvalidElementId ? doc.GetElement(typeId) as ElementType : null;
        var typeName = elemType?.Name ?? elem.Name ?? "";
        var familyName = elemType?.FamilyName ?? "";
        var comments = elem.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
        var mark = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";

        var textPool = $"{typeName} {familyName} {comments} {mark}";
        if (textPool.IndexOf(searchKeyword, StringComparison.OrdinalIgnoreCase) < 0 &&
            RemoveAccents(textPool).IndexOf(RemoveAccents(searchKeyword), StringComparison.OrdinalIgnoreCase) < 0)
        {
          continue;
        }
      }

      matches.Add(elem);
      if (matches.Count >= 300) break; // giới hạn
    }

    if (matches.Count == 0)
    {
      return new PromptExecutionResult
      {
        Success = false,
        Title = "Không tìm thấy cấu kiện phù hợp",
        Message = $"Không tìm thấy cấu kiện nào thỏa mãn điều kiện:\n" +
                  (bic.HasValue ? $"• Category: `{bic.Value}`\n" : "") +
                  (!string.IsNullOrEmpty(targetLevel) ? $"• Tầng: `{targetLevel}`\n" : "") +
                  (!string.IsNullOrEmpty(searchKeyword) ? $"• Từ khóa: `{searchKeyword}`\n" : "") +
                  "Vui lòng kiểm tra lại tên tầng hoặc tên loại cấu kiện."
      };
    }

    var ids = matches.Select(e => e.Id).ToList();
    var idNumbers = matches.Select(e => e.Id.Value).ToList();

    if (isSelectAction)
    {
      uiDoc.Selection.SetElementIds(ids);
      uiDoc.ShowElements(ids);
    }

    var sb = new StringBuilder();
    if (isSelectAction)
    {
      sb.AppendLine($"🎯 **Đã chọn và Zoom đến {matches.Count} cấu kiện trên Revit!**\n");
    }
    else
    {
      sb.AppendLine($"🔍 **Tìm thấy {matches.Count} cấu kiện phù hợp:**\n");
    }

    var groupedByType = matches
      .GroupBy(e =>
      {
        var tId = e.GetTypeId();
        var t = tId != ElementId.InvalidElementId ? doc.GetElement(tId) as ElementType : null;
        return t?.Name ?? e.Name ?? "Unknown";
      })
      .OrderByDescending(g => g.Count());

    foreach (var g in groupedByType.Take(10))
    {
      sb.AppendLine($"• **{g.Key}**: {g.Count()} đối tượng");
    }

    if (groupedByType.Count() > 10)
    {
      sb.AppendLine($"• ... và {groupedByType.Count() - 10} loại khác");
    }

    sb.AppendLine();
    sb.AppendLine($"*Các ID tiêu biểu:* `{string.Join(", ", idNumbers.Take(8))}`" +
                  (idNumbers.Count > 8 ? $" *(+{idNumbers.Count - 8} đối tượng khác)*" : ""));

    return new PromptExecutionResult
    {
      Success = true,
      Title = isSelectAction ? $"Đã chọn {matches.Count} cấu kiện" : $"Tìm thấy {matches.Count} cấu kiện",
      Message = sb.ToString(),
      AffectedElementIds = idNumbers,
      ActionType = isSelectAction ? "SELECT" : "QUERY"
    };
  }

  private static PromptExecutionResult ExecuteSetParameter(
    Document doc,
    UIDocument uiDoc,
    string rawPrompt,
    string normalized)
  {
    var selectionIds = uiDoc.Selection.GetElementIds();
    if (selectionIds.Count == 0)
    {
      return new PromptExecutionResult
      {
        Success = false,
        Title = "Chưa chọn đối tượng",
        Message = "Vui lòng chọn ít nhất một cấu kiện trên màn hình Revit trước khi yêu cầu AI gán tham số."
      };
    }

    // Trích xuất giá trị cần gán
    string paramName = "Comments";
    if (normalized.Contains("mark")) paramName = "Mark";
    else if (normalized.Contains("bsv_phanloai")) paramName = "BSV_PhanLoai";

    // Tìm giá trị trong nháy kép hoặc sau chữ "thanh" / "la"
    string val = "";
    var matchQuote = Regex.Match(rawPrompt, "[\"']([^\"']+)[\"']");
    if (matchQuote.Success)
    {
      val = matchQuote.Groups[1].Value;
    }
    else
    {
      var idx = normalized.IndexOf("thanh");
      if (idx < 0) idx = normalized.IndexOf("la");
      if (idx >= 0)
      {
        val = rawPrompt.Substring(idx + 5).Trim(' ', ':', '"', '\'');
      }
    }

    if (string.IsNullOrWhiteSpace(val))
    {
      val = "AI_Checked";
    }

    using var tx = new Transaction(doc, $"KAKA AI: Set {paramName}");
    tx.Start();
    int updatedCount = 0;

    foreach (var id in selectionIds)
    {
      var elem = doc.GetElement(id);
      var param = elem?.LookupParameter(paramName) ?? elem?.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
      if (param != null && !param.IsReadOnly)
      {
        param.Set(val);
        updatedCount++;
      }
    }

    tx.Commit();

    return new PromptExecutionResult
    {
      Success = true,
      Title = "Gán tham số thành công",
      Message = $"✅ Đã gán tham số `{paramName}` = **\"{val}\"** cho **{updatedCount} cấu kiện** đang chọn.",
      ActionType = "SET_PARAM"
    };
  }

  private static BuiltInCategory? DetectCategory(string normalized)
  {
    if (normalized.Contains("tuong") || normalized.Contains("vach") || normalized.Contains("wall"))
      return BuiltInCategory.OST_Walls;

    if (normalized.Contains("cot") || normalized.Contains("column"))
      return BuiltInCategory.OST_StructuralColumns;

    if (normalized.Contains("dam") || normalized.Contains("giang") || normalized.Contains("beam") || normalized.Contains("framing"))
      return BuiltInCategory.OST_StructuralFraming;

    if (normalized.Contains("san") || normalized.Contains("nen") || normalized.Contains("floor") || normalized.Contains("slab"))
      return BuiltInCategory.OST_Floors;

    if (normalized.Contains("mong") || normalized.Contains("coc") || normalized.Contains("foundation") || normalized.Contains("pile"))
      return BuiltInCategory.OST_StructuralFoundation;

    if (normalized.Contains("thang") || normalized.Contains("stair"))
      return BuiltInCategory.OST_Stairs;

    if (normalized.Contains("cua di") || normalized.Contains("cua") && !normalized.Contains("cua so") || normalized.Contains("door"))
      return BuiltInCategory.OST_Doors;

    if (normalized.Contains("cua so") || normalized.Contains("window"))
      return BuiltInCategory.OST_Windows;

    if (normalized.Contains("thep") || normalized.Contains("rebar"))
      return BuiltInCategory.OST_Rebar;

    return null;
  }

  private static string? DetectLevel(Document doc, string normalized, string raw)
  {
    // Kiểm tra tên tầng trực tiếp từ document
    var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
    foreach (var lvl in levels)
    {
      var lvlNorm = RemoveAccents(lvl.Name.ToLowerInvariant());
      if (normalized.Contains(lvlNorm))
      {
        return lvl.Name;
      }
    }

    // Nhận diện theo mẫu "+0.000", "+4.500", "-0.300"
    var mElev = Regex.Match(raw, @"[\+\-]\d+(\.\d+)?");
    if (mElev.Success)
    {
      return mElev.Value;
    }

    // Nhận diện theo "tang 1", "tang 2", "tang 3", "tang 0"
    var mTang = Regex.Match(normalized, @"tang\s*(\d+)");
    if (mTang.Success)
    {
      var num = mTang.Groups[1].Value;
      var found = levels.FirstOrDefault(l => l.Name.Contains(num));
      if (found != null) return found.Name;
    }

    return null;
  }

  private static string? DetectSearchKeyword(string raw, string normalized)
  {
    // Từ trong ngoặc kép
    var mQuote = Regex.Match(raw, "[\"']([^\"']+)[\"']");
    if (mQuote.Success) return mQuote.Groups[1].Value;

    // Các từ khóa đặc biệt như Tole, C1, DK...
    var words = raw.Split(new[] { ' ', ',', ':', ';' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var w in words)
    {
      var wNorm = RemoveAccents(w.ToLowerInvariant());
      if (wNorm is "tole" or "ton" or "dk" or "d300" or "c1" or "c2" or "v1" or "basic")
      {
        return w;
      }
    }

    return null;
  }

  private static string RemoveAccents(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return text;
    var normalizedString = text.Normalize(NormalizationForm.FormD);
    var stringBuilder = new StringBuilder();

    foreach (var c in normalizedString)
    {
      var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
      if (unicodeCategory != UnicodeCategory.NonSpacingMark)
      {
        stringBuilder.Append(c);
      }
    }

    return stringBuilder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
  }

  private static PromptExecutionResult ExecuteColumnRebar(
    UIDocument uiDoc,
    Document doc,
    string rawPrompt,
    string normalized)
  {
    try
    {
      // 1. Kiểm tra danh sách cột: ưu tiên các cột đang chọn trên màn hình
      var selIds = uiDoc.Selection.GetElementIds();
      var selectedCols = selIds
        .Select(id => doc.GetElement(id))
        .OfType<FamilyInstance>()
        .Where(fi => fi.Category != null && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns)
        .ToList();

      // Nếu chưa chọn cột nào, tìm theo tên/mác xuất hiện trong prompt (ví dụ: c5, c1, cot c5...)
      if (selectedCols.Count == 0)
      {
        var markMatch = Regex.Match(normalized, @"\b(c\d+)\b");
        string targetMark = markMatch.Success ? markMatch.Groups[1].Value : "";

        var collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
          .OfCategory(BuiltInCategory.OST_StructuralColumns)
          .OfClass(typeof(FamilyInstance))
          .Cast<FamilyInstance>();

        if (!string.IsNullOrEmpty(targetMark))
        {
          selectedCols = collector.Where(fi =>
          {
            var mark = fi.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
            var tenCk = fi.LookupParameter("BSV_TenCauKien")?.AsString() ?? "";
            var name = fi.Name ?? "";
            return mark.Equals(targetMark, StringComparison.OrdinalIgnoreCase) ||
                   tenCk.Equals(targetMark, StringComparison.OrdinalIgnoreCase) ||
                   name.ToLowerInvariant().Contains(targetMark);
          }).ToList();
        }
      }

      if (selectedCols.Count == 0)
      {
        return new PromptExecutionResult
        {
          Success = false,
          Title = "Chưa chọn cột",
          Message = "Không tìm thấy cột nào để bố trí thép. Vui lòng chọn các đoạn cột trên mô hình rồi gửi lại lệnh."
        };
      }

      // Sắp xếp các đoạn cột theo cao độ chân tầng (từ dưới lên trên)
      selectedCols = selectedCols.OrderBy(fi =>
      {
        var p = fi.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
        var lvl = p != null ? doc.GetElement(p.AsElementId()) as Level : null;
        return lvl?.Elevation ?? fi.get_BoundingBox(null)?.Min.Z ?? 0;
      }).ToList();

      var colIds = selectedCols.Select(c => c.Id).ToList();
      uiDoc.Selection.SetElementIds(colIds);
      uiDoc.ShowElements(colIds);

      // Đảm bảo DLL phụ thuộc được nạp
      try
      {
        string assemblyFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string utilsDllPath = System.IO.Path.Combine(assemblyFolder, "BimSpeedUtils.dll");
        if (System.IO.File.Exists(utilsDllPath))
        {
          System.Reflection.Assembly.LoadFrom(utilsDllPath);
        }
      }
      catch { }

      // Khởi tạo ViewModel & nạp các cột vào
      var vm = new ViewModels.Column.ColumnRebarViewModel(uiDoc);
      vm.SetHosts(selectedCols);

      // Phân tích các thông số từ prompt
      bool crank = normalized.Contains("crank") || normalized.Contains("be cong") ||
                   normalized.Contains("nhan thep") || normalized.Contains("uon thep") ||
                   normalized.Contains("co chai");
      if (crank)
      {
        vm.GeneralSetting.ShouldCrankRebarAtSplice = true;
        vm.GeneralSetting.CoefficientCrankLength = 6.0;
      }

      bool stagger = normalized.Contains("so le") || normalized.Contains("stagger") ||
                     normalized.Contains("2 vi tri") || normalized.Contains("hai vi tri");
      if (stagger)
      {
        vm.GeneralSetting.ShouldSpliceAtTwoPositions = true;
      }

      var lapMatch = Regex.Match(normalized, @"(\d+)\s*d");
      if (lapMatch.Success && double.TryParse(lapMatch.Groups[1].Value, out var lapCoeff))
      {
        vm.GeneralSetting.CoefficientLapLength = lapCoeff;
      }

      // Mở cửa sổ Kaka Column Rebar Window với đầy đủ các cột và tùy chọn đã được cấu hình sẵn
      var win = new Views.Column.ColumnRebarWindow(vm);
      win.ShowDialog();

      var colName = selectedCols.First().LookupParameter("BSV_TenCauKien")?.AsString()
                    ?? selectedCols.First().get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString()
                    ?? selectedCols.First().Name;

      return new PromptExecutionResult
      {
        Success = true,
        Title = "Khởi chạy Bố trí thép cột thành công",
        Message = $"✅ **Đã nạp {selectedCols.Count} phân đoạn cột ({colName}) vào công cụ Kaka:**\n\n" +
                  $"• Nhấn thép bẻ cong 1:6 (Crank): **{(crank ? "Đã bật" : "Tắt")}**\n" +
                  $"• Nối so le 50%: **{(stagger ? "Đã bật (2 vị trí)" : "Nối 1 vị trí")}**\n" +
                  $"• Hệ số nối chồng: **{vm.GeneralSetting.CoefficientLapLength}d**\n\n" +
                  "👉 Cửa sổ **Column Rebar** đã được mở với các cột được nạp sẵn để bạn kiểm tra mặt cắt và bấm bố trí thép!",
        AffectedElementIds = colIds.Select(i => i.Value).ToList(),
        ActionType = "COLUMN_REBAR"
      };
    }
    catch (Exception ex)
    {
      return new PromptExecutionResult
      {
        Success = false,
        Title = "Lỗi khi bố trí thép cột",
        Message = $"Có lỗi xảy ra: {ex.Message}"
      };
    }
  }
}
