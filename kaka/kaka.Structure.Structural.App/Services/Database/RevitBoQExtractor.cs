#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using kaka.Structure.Structural.App.Models.Database;

namespace kaka.Structure.Structural.App.Services.Database;

public static class RevitBoQExtractor
{
  private const double CuFtToCuM = 0.028316846592;
  private const double SqFtToSqM = 0.09290304;
  private const double FtToM = 0.3048;

  public static List<RevitBoQItem> ExtractBoQ(Document doc, bool includeLinks = false, IEnumerable<string>? selectedLinkTitles = null)
  {
    var list = new List<RevitBoQItem>();

    var categories = new List<BuiltInCategory>
    {
      BuiltInCategory.OST_StructuralColumns,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_StructuralFoundation,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Stairs,
      BuiltInCategory.OST_Ramps,
      BuiltInCategory.OST_Rebar
    };

    if (includeLinks)
    {
      categories.AddRange(new[]
      {
        BuiltInCategory.OST_Doors,
        BuiltInCategory.OST_Windows,
        BuiltInCategory.OST_Ceilings,
        BuiltInCategory.OST_Roofs,
        BuiltInCategory.OST_StairsRailing,
        BuiltInCategory.OST_CurtainWallPanels
      });
    }

    var docs = new List<Document> { doc };
    if (includeLinks)
    {
      try
      {
        var linkInstances = new FilteredElementCollector(doc)
          .OfClass(typeof(RevitLinkInstance))
          .Cast<RevitLinkInstance>();

        var allowedSet = selectedLinkTitles != null
          ? new HashSet<string>(selectedLinkTitles, StringComparer.OrdinalIgnoreCase)
          : null;

        foreach (var link in linkInstances)
        {
          var linkDoc = link.GetLinkDocument();
          if (linkDoc != null)
          {
            if (allowedSet != null && !allowedSet.Contains(linkDoc.Title))
            {
              continue; // Bỏ qua link không được chọn
            }

            if (!docs.Any(d => string.Equals(d.Title, linkDoc.Title, StringComparison.OrdinalIgnoreCase)))
            {
              docs.Add(linkDoc);
            }
          }
        }
      }
      catch { }
    }

    var multicatFilter = new ElementMulticategoryFilter(categories);

    foreach (var targetDoc in docs)
    {
      bool isLink = !ReferenceEquals(targetDoc, doc);
      var collector = new FilteredElementCollector(targetDoc)
        .WherePasses(multicatFilter)
        .WhereElementIsNotElementType();

      foreach (var elem in collector)
      {
        if (elem.Category == null) continue;

        var bic = (BuiltInCategory)(long)elem.Category.Id.Value;
        var item = new RevitBoQItem
        {
          ElementId = elem.Id.Value,
          UniqueId = elem.UniqueId
        };

        // Tên Family & Type
        var typeId = elem.GetTypeId();
        var elemType = typeId != ElementId.InvalidElementId ? targetDoc.GetElement(typeId) as ElementType : null;
        item.TypeName = elemType?.Name ?? elem.Name ?? "";
        item.FamilyName = elemType?.FamilyName ?? "";

        // Level
        var levelId = elem.LevelId;
        if (levelId != ElementId.InvalidElementId)
        {
          item.LevelName = targetDoc.GetElement(levelId)?.Name ?? "";
        }
        else
        {
          var lvlParam = elem.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM)
                         ?? elem.get_Parameter(BuiltInParameter.LEVEL_PARAM);
          item.LevelName = lvlParam?.AsValueString() ?? lvlParam?.AsString() ?? "";
        }

        // Parameters
        item.Comments = elem.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString()
                        ?? elem.LookupParameter("Comments")?.AsString() ?? "";
        item.Mark = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString()
                    ?? elem.LookupParameter("Mark")?.AsString() ?? "";
        item.BsvPhanLoai = elem.LookupParameter("BSV_PhanLoai")?.AsString() ?? "";
        item.BsvComments = elem.LookupParameter("BSV_Comments")?.AsString() ?? "";
        item.Partition = elem.LookupParameter("Partition")?.AsString() ?? "";

        // Volume & Area
        var volParam = elem.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
        if (volParam != null && volParam.HasValue)
          item.VolumeM3 = Math.Round(volParam.AsDouble() * CuFtToCuM, 4);

        var areaParam = elem.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
        if (areaParam != null && areaParam.HasValue)
          item.AreaM2 = Math.Round(areaParam.AsDouble() * SqFtToSqM, 4);

        // Phân loại theo cấu trúc Mindmap Kết cấu & Kiến trúc
        switch (bic)
        {
          case BuiltInCategory.OST_StructuralColumns:
            item.CategoryCode = "05.Cot";
            item.CategoryName = "Cột kết cấu (Column)";
            break;

          case BuiltInCategory.OST_StructuralFraming:
            if (item.TypeName.IndexOf("DK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("ĐK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Comments.IndexOf("DaKieng", StringComparison.OrdinalIgnoreCase) >= 0)
            {
              item.CategoryCode = "03.DaKieng";
              item.CategoryName = "Đà kiềng (Tie Beam)";
            }
            else
            {
              item.CategoryCode = "07.Dam";
              item.CategoryName = "Dầm kết cấu (Beam)";
            }
            break;

          case BuiltInCategory.OST_Floors:
            if (item.TypeName.IndexOf("Nen", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("Nền", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Comments.IndexOf("Nen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
              item.CategoryCode = "04.Nen";
              item.CategoryName = "Nền (Ground Slab)";
            }
            else if (isLink || item.TypeName.IndexOf("Gach", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.TypeName.IndexOf("Gạch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.TypeName.IndexOf("Granite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.TypeName.IndexOf("Op", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     item.TypeName.IndexOf("Ốp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
              item.CategoryCode = "KT.03.OpLat";
              item.CategoryName = "Ốp lát gạch & đá (Tiling & Flooring)";
            }
            else
            {
              item.CategoryCode = "08.San";
              item.CategoryName = "Sàn kết cấu (Slab)";
            }
            break;

          case BuiltInCategory.OST_StructuralFoundation:
            if (item.TypeName.IndexOf("Coc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("Cọc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.FamilyName.IndexOf("Pile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
              item.CategoryCode = "01.Coc";
              item.CategoryName = "Cọc (Pile)";
            }
            else
            {
              item.CategoryCode = "02.Mong";
              item.CategoryName = "Móng (Foundation)";
            }
            break;

          case BuiltInCategory.OST_Walls:
            if (isLink ||
                item.TypeName.IndexOf("Gach", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("Gạch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("Xay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("Xây", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.TypeName.IndexOf("Thach cao", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (elem is Wall w && w.StructuralUsage == StructuralWallUsage.NonBearing))
            {
              item.CategoryCode = "KT.01.TuongXay";
              item.CategoryName = "Tường xây gạch (Brick Walls)";
            }
            else
            {
              item.CategoryCode = "06.Vach";
              item.CategoryName = "Vách kết cấu (Shear Wall)";
            }
            break;

          case BuiltInCategory.OST_Doors:
            item.CategoryCode = "KT.04.CuaDi";
            item.CategoryName = "Cửa đi & Phụ kiện (Doors & Hardware)";
            break;

          case BuiltInCategory.OST_Windows:
          case BuiltInCategory.OST_CurtainWallPanels:
            item.CategoryCode = "KT.05.CuaSo";
            item.CategoryName = "Cửa sổ & Vách kính (Windows & Glazing)";
            break;

          case BuiltInCategory.OST_Ceilings:
            item.CategoryCode = "KT.06.Tran";
            item.CategoryName = "Trần thạch cao (Gypsum Ceilings)";
            break;

          case BuiltInCategory.OST_Roofs:
            item.CategoryCode = "KT.10.Mai";
            item.CategoryName = "Công tác Mái (Roofing Works)";
            break;

          case BuiltInCategory.OST_StairsRailing:
            item.CategoryCode = "KT.09.LanCan";
            item.CategoryName = "Lan can & Tay vịn (Railings)";
            break;

          case BuiltInCategory.OST_Stairs:
            item.CategoryCode = "09.CauThang";
            item.CategoryName = "Cầu thang (Stair)";
            break;

          case BuiltInCategory.OST_Ramps:
            item.CategoryCode = "11.Ramp";
            item.CategoryName = "Ramp dốc";
            break;

          case BuiltInCategory.OST_Rebar:
            item.CategoryCode = "00.Rebar";
            item.CategoryName = "Cốt thép (Rebar)";
            if (elem is Rebar rebar)
            {
              var len = rebar.TotalLength * FtToM;
              item.RebarLengthM = Math.Round(len, 3);
              var diaParam = elemType?.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER);
              if (diaParam != null && diaParam.HasValue)
              {
                var diaMm = diaParam.AsDouble() * FtToM * 1000.0;
                item.RebarDiameterMm = Math.Round(diaMm, 1);
                // Khối lượng thép (kg) = L * (d^2 / 162.0)
                item.RebarWeightKg = Math.Round(len * (diaMm * diaMm / 162.0), 3);
              }
            }
            break;

          default:
            item.CategoryCode = "14.CauKienPhu";
            item.CategoryName = elem.Category.Name;
            break;
        }

        // Material
        try
        {
          var matIds = elem.GetMaterialIds(false);
          if (matIds.Count > 0)
          {
            var mat = targetDoc.GetElement(matIds.First()) as Material;
            item.MaterialName = mat?.Name ?? "";
          }
        }
        catch { }

        list.Add(item);
      }
    }

    return list;
  }

  public static List<BoQCategorySummary> Summarize(List<RevitBoQItem> items)
  {
    return items
      .GroupBy(x => new { x.CategoryCode, x.CategoryName })
      .Select(g => new BoQCategorySummary
      {
        CategoryCode = g.Key.CategoryCode,
        CategoryName = g.Key.CategoryName,
        Count = g.Count(),
        TotalVolumeM3 = Math.Round(g.Sum(x => x.VolumeM3), 3),
        TotalAreaM2 = Math.Round(g.Sum(x => x.AreaM2), 2),
        TotalWeightKg = Math.Round(g.Sum(x => x.RebarWeightKg), 2)
      })
      .OrderBy(x => x.CategoryCode)
      .ToList();
  }

  public static string GenerateSqlScript(
    string projectName,
    string filePath,
    List<RevitBoQItem> items,
    List<BoQCategorySummary> summaries)
  {
    var sb = new StringBuilder();
    sb.AppendLine("-- ============================================================================");
    sb.AppendLine("-- DỮ LIỆU BÓC TÁCH KHỐI LƯỢNG (BoQ) XUẤT TỪ REVIT MODEL THEO CHUẨN MINDMAP.DB");
    sb.AppendLine($"-- Dự án: {projectName}");
    sb.AppendLine($"-- File Revit: {filePath}");
    sb.AppendLine($"-- Thời gian xuất: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"-- Tổng số cấu kiện: {items.Count}");
    sb.AppendLine("-- ============================================================================");
    sb.AppendLine();
    sb.AppendLine("BEGIN TRANSACTION;");
    sb.AppendLine();

    // 1. Model Info
    sb.AppendLine("-- 1. THÔNG TIN DỰ ÁN");
    sb.AppendLine("CREATE TABLE IF NOT EXISTS revit_model_info (");
    sb.AppendLine("    project_name NVARCHAR(255),");
    sb.AppendLine("    file_path NVARCHAR(500),");
    sb.AppendLine("    export_time NVARCHAR(50),");
    sb.AppendLine("    revit_version VARCHAR(20),");
    sb.AppendLine("    total_elements INT");
    sb.AppendLine(");");
    sb.AppendLine();
    sb.AppendLine($"DELETE FROM revit_model_info WHERE project_name = '{Escape(projectName)}';");
    sb.AppendLine($"INSERT INTO revit_model_info (project_name, file_path, export_time, revit_version, total_elements)");
    sb.AppendLine($"VALUES ('{Escape(projectName)}', '{Escape(filePath)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '2024/2026', {items.Count});");
    sb.AppendLine();

    // 2. Summary
    sb.AppendLine("-- 2. BẢNG TỔNG HỢP KHỐI LƯỢNG THEO ĐỀ MỤC CẤU KIỆN (BoQ SUMMARY)");
    sb.AppendLine("CREATE TABLE IF NOT EXISTS revit_boq_summary (");
    sb.AppendLine("    category_code VARCHAR(50) PRIMARY KEY,");
    sb.AppendLine("    category_name NVARCHAR(100),");
    sb.AppendLine("    element_count INT,");
    sb.AppendLine("    total_volume_m3 REAL,");
    sb.AppendLine("    total_area_m2 REAL,");
    sb.AppendLine("    total_rebar_weight_kg REAL");
    sb.AppendLine(");");
    sb.AppendLine();
    sb.AppendLine("DELETE FROM revit_boq_summary;");
    foreach (var s in summaries)
    {
      sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
        "INSERT INTO revit_boq_summary (category_code, category_name, element_count, total_volume_m3, total_area_m2, total_rebar_weight_kg) VALUES ('{0}', '{1}', {2}, {3}, {4}, {5});",
        Escape(s.CategoryCode), Escape(s.CategoryName), s.Count, s.TotalVolumeM3, s.TotalAreaM2, s.TotalWeightKg));
    }
    sb.AppendLine();

    // 3. Chi tiết Elements
    sb.AppendLine("-- 3. BẢNG CHI TIẾT TỪNG CẤU KIỆN (ELEMENT DETAILS)");
    sb.AppendLine("CREATE TABLE IF NOT EXISTS revit_boq_elements (");
    sb.AppendLine("    element_id INT PRIMARY KEY,");
    sb.AppendLine("    unique_id VARCHAR(64),");
    sb.AppendLine("    category_code VARCHAR(50),");
    sb.AppendLine("    category_name NVARCHAR(100),");
    sb.AppendLine("    family_name NVARCHAR(255),");
    sb.AppendLine("    type_name NVARCHAR(255),");
    sb.AppendLine("    level_name NVARCHAR(100),");
    sb.AppendLine("    volume_m3 REAL,");
    sb.AppendLine("    area_m2 REAL,");
    sb.AppendLine("    length_m REAL,");
    sb.AppendLine("    material_name NVARCHAR(100),");
    sb.AppendLine("    comments NVARCHAR(500),");
    sb.AppendLine("    mark NVARCHAR(100),");
    sb.AppendLine("    bsv_phanloai NVARCHAR(255),");
    sb.AppendLine("    bsv_comments NVARCHAR(255),");
    sb.AppendLine("    partition NVARCHAR(100),");
    sb.AppendLine("    rebar_diameter_mm REAL,");
    sb.AppendLine("    rebar_length_m REAL,");
    sb.AppendLine("    rebar_weight_kg REAL");
    sb.AppendLine(");");
    sb.AppendLine();
    sb.AppendLine("DELETE FROM revit_boq_elements;");

    foreach (var it in items)
    {
      sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
        "INSERT INTO revit_boq_elements (element_id, unique_id, category_code, category_name, family_name, type_name, level_name, volume_m3, area_m2, length_m, material_name, comments, mark, bsv_phanloai, bsv_comments, partition, rebar_diameter_mm, rebar_length_m, rebar_weight_kg) VALUES ({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', {7}, {8}, {9}, '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', {16}, {17}, {18});",
        it.ElementId, Escape(it.UniqueId), Escape(it.CategoryCode), Escape(it.CategoryName),
        Escape(it.FamilyName), Escape(it.TypeName), Escape(it.LevelName),
        it.VolumeM3, it.AreaM2, it.LengthM,
        Escape(it.MaterialName), Escape(it.Comments), Escape(it.Mark),
        Escape(it.BsvPhanLoai), Escape(it.BsvComments), Escape(it.Partition),
        it.RebarDiameterMm, it.RebarLengthM, it.RebarWeightKg));
    }

    sb.AppendLine();
    sb.AppendLine("COMMIT;");
    return sb.ToString();
  }

  public static bool ExecuteSqlToDb(string sqlFilePath, string dbFilePath, out string errorMessage)
  {
    errorMessage = string.Empty;
    string? tempPy = null;
    try
    {
      var normalizedDb = dbFilePath.Replace("\\", "/");
      var normalizedSql = sqlFilePath.Replace("\\", "/");

      var pyCode = $@"import sqlite3
import sys

db_path = r'{normalizedDb}'
sql_path = r'{normalizedSql}'

try:
    conn = sqlite3.connect(db_path)
    with open(sql_path, 'r', encoding='utf-8') as f:
        sql = f.read()
    conn.executescript(sql)
    conn.close()
    print('SUCCESS')
except Exception as e:
    print(f'ERROR: {{e}}', file=sys.stderr)
    sys.exit(1)
";

      tempPy = Path.Combine(Path.GetTempPath(), $"kaka_import_{Guid.NewGuid():N}.py");
      File.WriteAllText(tempPy, pyCode, Encoding.UTF8);

      var psi = new ProcessStartInfo
      {
        FileName = "python",
        Arguments = $"\"{tempPy}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      using var proc = Process.Start(psi);
      if (proc == null)
      {
        errorMessage = "Không thể khởi động tiến trình Python.";
        return false;
      }

      var stdErr = proc.StandardError.ReadToEnd();
      var stdOut = proc.StandardOutput.ReadToEnd();
      proc.WaitForExit(15000);

      if (proc.ExitCode != 0)
      {
        errorMessage = !string.IsNullOrWhiteSpace(stdErr) ? stdErr : stdOut;
        return false;
      }

      return true;
    }
    catch (Exception ex)
    {
      errorMessage = ex.Message;
      return false;
    }
    finally
    {
      if (tempPy != null && File.Exists(tempPy))
      {
        try { File.Delete(tempPy); } catch { }
      }
    }
  }

  private static string Escape(string? s)
  {
    return s?.Replace("'", "''") ?? "";
  }
}
