#nullable enable
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace kaka.Structure.Structural.App.Models.Column
{
  public class RebarModel
  {
    public Rebar? Rebar { get; set; }

    public XYZ? CurrentBarCoordinate { get; set; }

    public XYZ? NextBarCoordinate { get; set; }

    public int CurrentBarIndex { get; set; }

    public int NextBarIndex { get; set; }

    public EnumShapePosition Position { get; set; }

    public EnumRebarStyle RebarStyle { get; set; }

    public RebarModel() { }

    public RebarModel(
      Rebar rebar,
      int currentBarIndex,
      XYZ currentBarCoordinate,
      int nextBarIndex,
      XYZ nextBarCoordinate,
      EnumShapePosition position = default,
      EnumRebarStyle rebarStyle = default)
    {
      Rebar = rebar;
      CurrentBarIndex = currentBarIndex;
      CurrentBarCoordinate = currentBarCoordinate;

      NextBarIndex = nextBarIndex;
      NextBarCoordinate = nextBarCoordinate;

      Position = position;
      RebarStyle = rebarStyle;
    }
  }
}