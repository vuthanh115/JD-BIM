using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace kaka.Structure.Structural.App.Commands.Slab ;

[Transaction(TransactionMode.Manual)]
public sealed class SlabRebarBottomCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "SlabRebarBottomCommand");
    return Result.Succeeded;
  }
}