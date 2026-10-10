using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Commands.Column ;

[Transaction(TransactionMode.Manual)]
public sealed class ColumnRebarV2Command : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "Column Rebar v2.0 (stub)");
    return Result.Succeeded;
  }
}