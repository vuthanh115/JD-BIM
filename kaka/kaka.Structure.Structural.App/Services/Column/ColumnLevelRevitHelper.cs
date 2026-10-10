#nullable enable
using Autodesk.Revit.DB ;
using kaka.Structure.Structural.App.Models.Column ;

namespace kaka.Structure.Structural.App.Services.Column ;

internal static class ColumnLevelRevitHelper
{
  public static IReadOnlyList<LevelPreviewItem> GetLevelsForSameAxisColumns(
    Document doc,
    FamilyInstance host,
    double xyTolMm = 10.0)
  {
    var tol = UnitUtils.ConvertToInternalUnits(xyTolMm, UnitTypeId.Millimeters);

    var hostPt = GetXYPoint(host);

    // Lấy tất cả Structural Columns
    var cols = new FilteredElementCollector(doc)
      .OfCategory(BuiltInCategory.OST_StructuralColumns)
      .WhereElementIsNotElementType()
      .OfType<FamilyInstance>()
      .ToList();

    var list = new List<(FamilyInstance Fi, Level BaseLv, double HeightFt)>();

    foreach (var fi in cols)
    {
      var p = GetXYPoint(fi);
      if (Math.Abs(p.X - hostPt.X) > tol || Math.Abs(p.Y - hostPt.Y) > tol)
        continue;

      if (!TryGetColumnBaseLevel(doc, fi, out var baseLv)) continue;
      var hFt = ComputeColumnInstanceHeightFt(doc, fi, baseLv);
      if (hFt <= 1e-6) continue;

      list.Add((fi, baseLv, hFt));
    }

    // Sort theo Base Level elevation DESC (tầng trên -> dưới)
    var sorted = list
      .OrderByDescending(x => x.BaseLv.Elevation) // mái -> tầng 1
      .ToList();


    // Map -> preview items
    // Map -> preview items
    var items = new List<LevelPreviewItem>(sorted.Count);

    foreach (var x in sorted)
    {
      var baseOffsetFt = GetDouble(x.Fi, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);

      // compute z0/z1 (ft)
      double z0Ft = x.BaseLv.Elevation + baseOffsetFt;
      double z1Ft;

      if (TryGetColumnTopLevel(doc, x.Fi, out var topLv))
      {
        var topOffsetFt = GetDouble(x.Fi, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
        z1Ft = topLv.Elevation + topOffsetFt;
      }
      else
      {
        z1Ft = z0Ft + x.HeightFt; // unconnected column
      }

      // convert mm
      var z0Mm = UnitUtils.ConvertFromInternalUnits(z0Ft, UnitTypeId.Millimeters);
      var z1Mm = UnitUtils.ConvertFromInternalUnits(z1Ft, UnitTypeId.Millimeters);
      var hMm  = Math.Abs(z1Mm - z0Mm);

      var size = ColumnPreviewRevitHelper.GetColumnSizeMm(x.Fi);
      var note = $"S={size.WidthMm:0}x{size.HeightMm:0}";

      items.Add(new LevelPreviewItem(
        x.BaseLv.Name,
        hMm,
        note,
        z0Mm,
        z1Mm
      ));
    }

    return items;

  }

  // =========================
  // Base/Top level for COLUMN
  // =========================
  private static bool TryGetColumnBaseLevel(Document doc, FamilyInstance fi, out Level baseLv)
  {
    baseLv = null!;

    // Base level param của structural column
    var pBase = fi.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)
                ?? fi.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

    if (pBase == null) return false;

    var id = pBase.AsElementId();
    if (id == ElementId.InvalidElementId) return false;

    baseLv = doc.GetElement(id) as Level ?? null!;
    return baseLv != null;
  }

  private static bool TryGetColumnTopLevel(Document doc, FamilyInstance fi, out Level topLv)
  {
    topLv = null!;

    var pTop = fi.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
    if (pTop == null) return false;

    var id = pTop.AsElementId();
    if (id == ElementId.InvalidElementId) return false;

    topLv = doc.GetElement(id) as Level ?? null!;
    return topLv != null;
  }

  private static double ComputeColumnInstanceHeightFt(Document doc, FamilyInstance fi, Level baseLv)
  {
    var baseOffset = GetDouble(fi, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);

    // Nếu connected Top Level -> dùng top elevation + top offset
    if (TryGetColumnTopLevel(doc, fi, out var topLv))
    {
      var topOffset = GetDouble(fi, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

      var z0 = baseLv.Elevation + baseOffset;
      var z1 = topLv.Elevation + topOffset;
      return Math.Max(0.0, z1 - z0);
    }

    // Nếu unconnected -> INSTANCE_LENGTH_PARAM
    var len = GetDouble(fi, BuiltInParameter.INSTANCE_LENGTH_PARAM);
    if (len > 1e-6) return len;

    // fallback: bbox height
    var bb = fi.get_BoundingBox(null);
    if (bb != null) return Math.Max(0.0, bb.Max.Z - bb.Min.Z);

    return 0.0;
  }

  private static double GetDouble(Element e, BuiltInParameter bip)
  {
    var p = e.get_Parameter(bip);
    return (p != null && p.StorageType == StorageType.Double) ? p.AsDouble() : 0.0;
  }

  private static XYZ GetXYPoint(FamilyInstance fi)
  {
    if (fi.Location is LocationPoint lp) return lp.Point;

    var bb = fi.get_BoundingBox(null);
    if (bb != null) return (bb.Min + bb.Max) * 0.5;

    return XYZ.Zero;
  }
}