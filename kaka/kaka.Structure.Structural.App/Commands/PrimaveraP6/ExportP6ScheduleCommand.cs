#nullable enable
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.ViewModels.PrimaveraP6;
using kaka.Structure.Structural.App.Views.PrimaveraP6;

namespace kaka.Structure.Structural.App.Commands.PrimaveraP6;

[Transaction(TransactionMode.Manual)]
public sealed class ExportP6ScheduleCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var uiDoc = commandData.Application.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      TaskDialog.Show("Primavera P6 Export", "Vui lòng mở một dự án hoặc mô hình Revit trước khi xuất dữ liệu.");
      return Result.Cancelled;
    }

    var doc = uiDoc.Document;

    try
    {
      var view = new ExportP6ScheduleView();
      var vm = new ExportP6ScheduleViewModel(doc, view);
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
      TaskDialog.Show("Primavera P6 Export Error", ex.ToString());
      return Result.Failed;
    }
  }
}
