#nullable enable
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.ViewModels.PrimaveraP6;
using kaka.Structure.Structural.App.Views.PrimaveraP6;

namespace kaka.Structure.Structural.App.Commands.PrimaveraP6;

[Transaction(TransactionMode.Manual)]
public sealed class RuleBased4DBimCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    var uiDoc = commandData.Application.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      TaskDialog.Show("Rule-Based 4D BIM", "Vui lòng mở một mô hình Revit trước khi khởi chạy bộ công cụ 4D BIM.");
      return Result.Cancelled;
    }

    var doc = uiDoc.Document;

    try
    {
      var view = new RuleBased4DBimView();
      var vm = new RuleBased4DBimViewModel(doc, view);
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
      TaskDialog.Show("Rule-Based 4D BIM Error", ex.ToString());
      return Result.Failed;
    }
  }
}
