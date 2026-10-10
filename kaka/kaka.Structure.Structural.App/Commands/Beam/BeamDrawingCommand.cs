using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Commands.Beam ;

[Transaction(TransactionMode.Manual)]
public sealed class BeamDrawingCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "Beam Drawing (stub)");
    return Result.Succeeded;
  }
}