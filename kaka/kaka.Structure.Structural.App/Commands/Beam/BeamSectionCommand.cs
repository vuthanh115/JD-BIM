using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Commands.Beam ;

[Transaction(TransactionMode.Manual)]
public sealed class BeamSectionCommand : IExternalCommand
{
  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    TaskDialog.Show("KAKA", "Beam Section (stub)");
    return Result.Succeeded;
  }
}