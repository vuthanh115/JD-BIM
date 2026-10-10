#nullable enable
using Autodesk.Revit.DB;

namespace kaka.Structure.Structural.App.Models.Column
{
  public class FramingFloorModel
  {
    public EnumFramingFloorType FramingFloorType { get; set; }

    public EnumFramingSide FramingSide { get; set; }

    /// <summary> Width of framing / slab (model units – feet) </summary>
    public double Width { get; set; }

    /// <summary> Height (thickness) (model units – feet) </summary>
    public double Height { get; set; }

    public double TopElevation { get; set; }

    public double BottomElevation { get; set; }

    public ElementId? Id { get; set; }

    public Curve? LocationCurve { get; set; }

    public XYZ? Direction { get; set; }

    public string? CategoryName { get; set; }

    public string? Name { get; set; }

    #region Constructors

    public FramingFloorModel() { }

    public FramingFloorModel(FamilyInstance framing)
    {
      if (framing == null) return;

      Id = framing.Id;
      Name = framing.Name;
      CategoryName = framing.Category?.Name;

      FramingFloorType = EnumFramingFloorType.Framing;
      FramingSide = EnumFramingSide.Unknown;

      // Location
      if (framing.Location is LocationCurve lc)
      {
        LocationCurve = lc.Curve;
        Direction = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
      }

      // Size parameters (best-effort)
      Width = GetDoubleParam(framing, BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH);
      Height = GetDoubleParam(framing, BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);

      // Elevations
      var bbox = framing.get_BoundingBox(null);
      if (bbox != null)
      {
        BottomElevation = bbox.Min.Z;
        TopElevation = bbox.Max.Z;
      }
    }

    public FramingFloorModel(Floor floor)
    {
      if (floor == null) return;

      Id = floor.Id;
      Name = floor.Name;
      CategoryName = floor.Category?.Name;

      FramingFloorType = EnumFramingFloorType.Floor;
      FramingSide = EnumFramingSide.Unknown;

      Height = GetDoubleParam(floor, BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);

      var bbox = floor.get_BoundingBox(null);
      if (bbox != null)
      {
        BottomElevation = bbox.Min.Z;
        TopElevation = bbox.Max.Z;
      }

      // Direction & curve are usually not meaningful for slabs
      LocationCurve = null;
      Direction = null;
      Width = 0;
    }

    #endregion

    #region Factory

    public static FramingFloorModel? Create(Element element)
    {
      if (element is FamilyInstance fi)
        return new FramingFloorModel(fi);

      if (element is Floor floor)
        return new FramingFloorModel(floor);

      return null;
    }

    #endregion

    #region Helpers

    private static double GetDoubleParam(Element e, BuiltInParameter bip)
    {
      var p = e.get_Parameter(bip);
      return p != null && p.HasValue ? p.AsDouble() : 0.0;
    }

    #endregion
  }
}
