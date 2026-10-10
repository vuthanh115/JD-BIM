using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace kaka.Structure.Structural.App.Commands.Foundation ;

[Transaction(TransactionMode.Manual)]
public sealed class FoundationRebarCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "FoundationRebarCommand");
    return Result.Succeeded;
  }
}