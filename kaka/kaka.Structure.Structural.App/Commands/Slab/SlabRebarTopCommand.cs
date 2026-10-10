using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace kaka.Structure.Structural.App.Commands.Slab ;

[Transaction(TransactionMode.Manual)]
public sealed class SlabRebarTopCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "SlabRebarTopCommand");
    return Result.Succeeded;
  }
}