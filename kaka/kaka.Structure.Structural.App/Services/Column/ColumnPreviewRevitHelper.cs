#nullable enable
using Autodesk.Revit.DB ;

namespace kaka.Structure.Structural.App.Services.Column ;

internal static class ColumnPreviewRevitHelper
{
  public static (double WidthMm, double HeightMm) GetColumnSizeMm(FamilyInstance column)
  {
    var bb = column.get_BoundingBox(null)
             ?? throw new InvalidOperationException("Column has no bounding box.");

    // 8 corners in world
    var min = bb.Min;
    var max = bb.Max;

    XYZ[] corners =
    {
      new XYZ(min.X, min.Y, min.Z),
      new XYZ(max.X, min.Y, min.Z),
      new XYZ(max.X, max.Y, min.Z),
      new XYZ(min.X, max.Y, min.Z),

      new XYZ(min.X, min.Y, max.Z),
      new XYZ(max.X, min.Y, max.Z),
      new XYZ(max.X, max.Y, max.Z),
      new XYZ(min.X, max.Y, max.Z),
    };

    // transform to local coords of the instance (so rotation won't break size)
    var tr = column.GetTransform();
    var inv = tr.Inverse;

    double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
    double minY = double.PositiveInfinity, maxY = double.NegativeInfinity;

    foreach (var c in corners)
    {
      var p = inv.OfPoint(c);
      minX = Math.Min(minX, p.X);
      maxX = Math.Max(maxX, p.X);
      minY = Math.Min(minY, p.Y);
      maxY = Math.Max(maxY, p.Y);
    }

    var widthFt  = Math.Abs(maxX - minX);
    var heightFt = Math.Abs(maxY - minY);

    var widthMm  = UnitUtils.ConvertFromInternalUnits(widthFt, UnitTypeId.Millimeters);
    var heightMm = UnitUtils.ConvertFromInternalUnits(heightFt, UnitTypeId.Millimeters);

    // đảm bảo width >= height (tuỳ bạn muốn)
    if (heightMm > widthMm) (widthMm, heightMm) = (heightMm, widthMm);

    return (widthMm, heightMm);
  }
}