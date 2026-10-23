#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using kaka.Structure.Structural.App.Models.PrimaveraP6;

namespace kaka.Structure.Structural.App.Services.PrimaveraP6;

/// <summary>
/// Dịch vụ sinh bộ Search Sets XML chuẩn Navisworks (Autodesk Navisworks Selection Sets XML)
/// và bảng Mapping Rules phục vụ quy trình 4D Rule-Based tự động (Bexel workflow trên hệ sinh thái Navisworks + P6).
/// </summary>
public static class Navisworks4DExportService
{
  /// <summary>
  /// Xuất file Search Sets XML (*.xml) dùng để import trực tiếp vào Navisworks (Sets -> Import Search Sets)
  /// </summary>
  public static void ExportNavisworksSearchSetsXml(string xmlPath, List<P6WbsSummaryItem> summaries)
  {
    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
    sb.AppendLine("<exchange xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://download.autodesk.com/us/navisworks/schemas/nw-exchange-12.0.xsd\" units=\"m\">");
    sb.AppendLine("  <selectionsets>");
    sb.AppendLine("    <viewfolder name=\"KAKA 4D BIM — RULE-BASED SEARCH SETS\" guid=\"a1b2c3d4-e5f6-7890-abcd-ef1234567890\">");

    // Thư mục con: Kết cấu
    sb.AppendLine("      <viewfolder name=\"01. KẾT CẤU (WBS-KC)\" guid=\"b2c3d4e5-f6a7-8901-bcde-f12345678901\">");
    foreach (var item in summaries)
    {
      if (item.Discipline == "KIẾN TRÚC") continue;
      AppendSearchSetItem(sb, item);
    }
    sb.AppendLine("      </viewfolder>");

    // Thư mục con: Kiến trúc
    var archItems = summaries.FindAll(x => x.Discipline == "KIẾN TRÚC");
    if (archItems.Count > 0)
    {
      sb.AppendLine("      <viewfolder name=\"02. KIẾN TRÚC (WBS-KT)\" guid=\"c3d4e5f6-a7b8-9012-cdef-12345678902\">");
      foreach (var item in archItems)
      {
        AppendSearchSetItem(sb, item);
      }
      sb.AppendLine("      </viewfolder>");
    }

    sb.AppendLine("    </viewfolder>");
    sb.AppendLine("  </selectionsets>");
    sb.AppendLine("</exchange>");

    File.WriteAllText(xmlPath, sb.ToString(), Encoding.UTF8);
  }

  private static void AppendSearchSetItem(StringBuilder sb, P6WbsSummaryItem item)
  {
    var guid = Guid.NewGuid().ToString("D");
    var setName = $"{item.WbsCode} - {item.ComponentName}";
    var categoryTerm = GetRevitCategoryTerm(item.ComponentCode);

    sb.AppendLine($"        <selectionset name=\"{EscapeXml(setName)}\" guid=\"{guid}\">");
    sb.AppendLine("          <findspec mode=\"all\" disjoint=\"0\">");
    sb.AppendLine("            <conditions>");
    sb.AppendLine("              <condition test=\"contains\" flags=\"10\">");
    sb.AppendLine("                <category>");
    sb.AppendLine("                  <name internal=\"LcRevitData_Element\">Element</name>");
    sb.AppendLine("                </category>");
    sb.AppendLine("                <property>");
    sb.AppendLine("                  <name internal=\"LcRevitPropertyElementCategory\">Category</name>");
    sb.AppendLine("                </property>");
    sb.AppendLine($"                <value><data type=\"wstring\">{categoryTerm}</data></value>");
    sb.AppendLine("              </condition>");
    sb.AppendLine("            </conditions>");
    sb.AppendLine("          </findspec>");
    sb.AppendLine("        </selectionset>");
  }

  /// <summary>
  /// Xuất bảng CSV chuẩn Navisworks Timeliner Import (hoặc Timeliner Auto-Attach Rules)
  /// </summary>
  public static void ExportNavisworksTimelinerCsv(string csvPath, string projectName, List<P6WbsSummaryItem> summaries)
  {
    var sb = new StringBuilder();
    // Tiêu đề chuẩn Timeliner CSV
    sb.AppendLine("Activity ID,Activity Name,WBS,Task Type,Synchronization ID,Planned Start,Planned End,Search Set Name");

    int counter = 1000;
    foreach (var item in summaries)
    {
      counter += 10;
      var setName = $"{item.WbsCode} - {item.ComponentName}";
      var taskType = "Construct"; // Mô phỏng thi công lắp dựng
      var syncId = $"{item.WbsCode}_{counter}";
      
      sb.AppendLine($"\"{item.WbsCode}\",\"{EscapeCsv(item.ComponentName)}\",\"{item.WbsCode}\",\"{taskType}\",\"{syncId}\",\"\",\"\",\"{EscapeCsv(setName)}\"");
    }

    File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
  }

  private static string GetRevitCategoryTerm(string compCode)
  {
    return compCode switch
    {
      "01.Coc" => "Structural Foundation",
      "02.Mong" => "Structural Foundation",
      "03.DaKieng" => "Structural Framing",
      "04.Nen" => "Floors",
      "05.Cot" => "Structural Columns",
      "06.Vach" => "Walls",
      "07.Dam" => "Structural Framing",
      "08.San" => "Floors",
      "09.CauThang" => "Stairs",
      "10.TamCap" => "Stairs",
      "11.Ramp" => "Ramps",
      "12.Pit" => "Structural Foundation",
      "13.Dock" => "Floors",
      "14.CauKienPhu" => "Generic Models",
      "KT.01.TuongXay" => "Walls",
      "KT.02.TratBa" => "Walls",
      "KT.03.OpLat" => "Floors",
      "KT.04.CuaDi" => "Doors",
      "KT.05.CuaSo" => "Windows",
      "KT.06.Tran" => "Ceilings",
      "KT.07.SonBa" => "Walls",
      "KT.08.ChongTham" => "Floors",
      "KT.09.LanCan" => "Railings",
      "KT.10.Mai" => "Roofs",
      _ => "Element"
    };
  }

  private static string EscapeXml(string text)
  {
    return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
  }

  private static string EscapeCsv(string text)
  {
    return text.Replace("\"", "\"\"");
  }
}
