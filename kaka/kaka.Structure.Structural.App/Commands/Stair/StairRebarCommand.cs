using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace kaka.Structure.Structural.App.Commands.Stair ;

[Transaction(TransactionMode.Manual)]
public sealed class StairRebarCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "StairRebarCommand");
    return Result.Succeeded;
  }
}