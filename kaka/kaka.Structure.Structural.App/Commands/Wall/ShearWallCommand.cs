using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Commands.Wall ;

[Transaction(TransactionMode.Manual)]
public sealed class ShearWallCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "ShearWallCommand");
    return Result.Succeeded;
  }
}