#nullable enable
using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.Services.Mcp;

namespace kaka.Structure.Structural.App.Commands.Mcp;

[Transaction(TransactionMode.Manual)]
public sealed class McpServerCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var srv = RevitMcpHttpServer.Instance;
    var statusText = srv.IsRunning ? "ONLINE (Đang hoạt động)" : "OFFLINE (Đã tắt)";
    var url = $"http://localhost:{srv.Port}/health";

    var dialog = new TaskDialog("KAKA — Revit MCP Server")
    {
      MainInstruction = $"Trạng thái MCP Server: {statusText}",
      MainContent = $"• Cổng mạng nội bộ: http://localhost:{srv.Port}/\n" +
                    $"• Số yêu cầu đã xử lý: {srv.TotalRequestsHandled}\n" +
                    $"• Kết nối với Antigravity AI Agent: Sẵn sàng nhận lệnh JSON-RPC.\n\n" +
                    $"Bạn có thể kiểm tra trạng thái máy chủ trực tiếp trên trình duyệt web.",
      CommonButtons = TaskDialogCommonButtons.Close
    };

    dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Mở kiểm tra trạng thái trên trình duyệt", url);
    if (srv.IsRunning)
    {
      dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Khởi động lại MCP Server");
    }
    else
    {
      dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Bật MCP Server");
    }

    var result = dialog.Show();
    if (result == TaskDialogResult.CommandLink1)
    {
      try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
    else if (result == TaskDialogResult.CommandLink2)
    {
      if (srv.IsRunning) srv.Stop();
      srv.Start(srv.Port);
      TaskDialog.Show("Revit MCP Server", $"Đã khởi động lại server trên cổng {srv.Port} thành công!");
    }

    return Result.Succeeded;
  }
}
