#nullable enable
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.ViewModels.Database;
using kaka.Structure.Structural.App.Views.Database;

namespace kaka.Structure.Structural.App.Commands.Database;

[Transaction(TransactionMode.Manual)]
public sealed class ExportSqlBoQCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var uiDoc = commandData.Application.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      TaskDialog.Show("KAKA SQL / BoQ", "Vui lòng mở một dự án hoặc mô hình Revit trước khi xuất dữ liệu.");
      return Result.Cancelled;
    }

    var doc = uiDoc.Document;

    try
    {
      var view = new ExportSqlBoQView();
      var vm = new ExportSqlBoQViewModel(doc, view);
      view.DataContext = vm;

      var revitHandle = commandData.Application.MainWindowHandle;
      if (revitHandle != IntPtr.Zero)
      {
        new System.Windows.Interop.WindowInteropHelper(view).Owner = revitHandle;
      }

      view.ShowDialog();
      return Result.Succeeded;
    }
    catch (Exception ex)
    {
      message = ex.Message;
      TaskDialog.Show("KAKA SQL / BoQ Error", ex.ToString());
      return Result.Failed;
    }
  }
}
