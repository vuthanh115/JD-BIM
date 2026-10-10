using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Commands.Column ;

[Transaction(TransactionMode.Manual)]
public sealed class UpdateColumnDrawingCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "Update Column Drawing (stub)");
    return Result.Succeeded;
  }
}