#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.Models.Database;
using kaka.Structure.Structural.App.Services.Database;

namespace kaka.Structure.Structural.App.Commands.Web3D;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class ExportWeb3dDashboardCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var uiDoc = commandData.Application.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      TaskDialog.Show("KAKA Web 3D & Dashboard", "Vui lòng mở một dự án hoặc mô hình Revit trước khi xuất Dashboard.");
      return Result.Cancelled;
    }

    var doc = uiDoc.Document;

    try
    {
      // 1. Trích xuất toàn bộ cấu kiện từ mô hình Revit đang mở
      var items = RevitBoQExtractor.ExtractBoQ(doc);
      int totalElements = items.Count;

      double totalVolume = items.Sum(x => x.VolumeM3);
      double totalRebarWeightKg = items.Sum(x => x.RebarWeightKg);
      var categoryGroups = items.GroupBy(x => x.CategoryName).Select(g => new { Name = g.Key, Count = g.Count(), Vol = g.Sum(x => x.VolumeM3) }).ToList();

      string projectName = doc.ProjectInformation?.Name ?? Path.GetFileNameWithoutExtension(doc.PathName);
      if (string.IsNullOrWhiteSpace(projectName)) projectName = "KAKA_BIM_Project";

      // 2. Tìm hoặc định vị đường dẫn Web Dashboard
      string dashboardPath = @"C:\Users\vophi\Downloads\KAKA_PowerBI_Executive_Dashboard.html";
      string githubDashboardPath = @"c:\Users\vophi\Downloads\Github\CV\JD-BIM\KAKA_PowerBI_Executive_Dashboard.html";

      if (!File.Exists(dashboardPath) && File.Exists(githubDashboardPath))
      {
        dashboardPath = githubDashboardPath;
      }

      // 3. Xuất file dữ liệu thực tế JSON kèm theo Dashboard
      string outputDir = Path.GetDirectoryName(dashboardPath) ?? @"C:\Users\vophi\Downloads";
      string jsonSnapshotPath = Path.Combine(outputDir, "kaka_revit_active_boq.json");

      var sb = new StringBuilder();
      sb.AppendLine("{");
      sb.AppendLine($"  \"projectName\": \"{EscapeJson(projectName)}\",");
      sb.AppendLine($"  \"exportTime\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
      sb.AppendLine($"  \"totalElements\": {totalElements},");
      sb.AppendLine($"  \"totalVolumeM3\": {totalVolume:F2},");
      sb.AppendLine($"  \"totalRebarTon\": {(totalRebarWeightKg / 1000.0):F2},");
      sb.AppendLine("  \"categories\": [");
      for (int i = 0; i < categoryGroups.Count; i++)
      {
        var c = categoryGroups[i];
        string comma = i < categoryGroups.Count - 1 ? "," : "";
        sb.AppendLine($"    {{ \"name\": \"{EscapeJson(c.Name)}\", \"count\": {c.Count}, \"volumeM3\": {c.Vol:F2} }}{comma}");
      }
      sb.AppendLine("  ]");
      sb.AppendLine("}");

      File.WriteAllText(jsonSnapshotPath, sb.ToString(), Encoding.UTF8);

      // 4. Hiển thị thông báo thành công cho người dùng
      var td = new TaskDialog("KAKA BIM • Export Web 3D & Dashboard")
      {
        MainInstruction = "Đã xuất dữ liệu 3D & BOQ sang Web Dashboard thành công!",
        MainContent = $"• Dự án: {projectName}\n" +
                      $"• Tổng số cấu kiện trích xuất: {totalElements:N0} cấu kiện (Revit API)\n" +
                      $"• Thể tích bê tông: {totalVolume:N2} m³\n" +
                      $"• Khối lượng cốt thép: {(totalRebarWeightKg / 1000.0):N1} Tấn\n" +
                      $"• File dữ liệu: {Path.GetFileName(jsonSnapshotPath)}\n" +
                      $"• Đường dẫn Dashboard: {dashboardPath}",
        MainIcon = TaskDialogIcon.TaskDialogIconInformation,
        CommonButtons = TaskDialogCommonButtons.Close,
        DefaultButton = TaskDialogResult.CommandLink1
      };

      td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Mở Web 3D Dashboard Ngay Trên Trình Duyệt", "Khởi chạy trình duyệt xem mô hình 3D, thanh trượt 4D và biểu đồ BOQ tương tác.");
      td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Mở Thư Mục Chứa File Dashboard", "Mở Windows Explorer tại thư mục lưu trữ.");

      var result = td.Show();

      if (result == TaskDialogResult.CommandLink1)
      {
        if (File.Exists(dashboardPath))
        {
          Process.Start(new ProcessStartInfo(dashboardPath) { UseShellExecute = true });
        }
      }
      else if (result == TaskDialogResult.CommandLink2)
      {
        if (Directory.Exists(outputDir))
        {
          Process.Start(new ProcessStartInfo("explorer.exe", outputDir) { UseShellExecute = true });
        }
      }

      return Result.Succeeded;
    }
    catch (Exception ex)
    {
      message = ex.Message;
      TaskDialog.Show("KAKA Web 3D & Dashboard Error", ex.ToString());
      return Result.Failed;
    }
  }

  private static string EscapeJson(string s)
  {
    if (string.IsNullOrEmpty(s)) return "";
    return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
  }
}
