#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using kaka.Structure.Structural.App.Models.Column;
using Point = System.Windows.Point;

namespace kaka.Structure.Structural.App.ViewModels.Column;

public sealed class ColumnPreviewModel
{
  public double WidthMm  { get; }
  public double HeightMm { get; }

  public double CoverMm { get; }
  public double TieEndOffsetMm { get; }

  // Diameters (mm) used for preview + quantity info
  public double MainBarDiameterMm { get; }
  public double StirrupDiameterMm { get; }

  // Computed info
  public string SectionText { get; }
  public string TotalBarsText { get; }
  public double RebarAreaCm2 { get; }
  public string RebarAreaText { get; }
  public double RebarRatioPercent { get; }
  public string RebarRatioText { get; }

  public int Cx { get; }
  public int Cy { get; }

  /// <summary>Level marks: Z0=Z1=Level.Elevation(mm)</summary>
  public IReadOnlyList<LevelPreviewItem> Levels { get; }

  /// <summary>Column Z range (mm): UNION các cột cùng trục (hoặc fallback host)</summary>
  public double ColumnZ0Mm { get; }
  public double ColumnZ1Mm { get; }

  public double Aspect => (HeightMm <= 0.0001) ? 1.0 : (WidthMm / HeightMm);

  public double Cover01
  {
    get
    {
      var m = Math.Max(1.0, Math.Min(WidthMm, HeightMm));
      return Math.Max(0, CoverMm / m);
    }
  }

  public IReadOnlyList<Point> BarsUV { get; }

  private ColumnPreviewModel(
    double widthMm,
    double heightMm,
    double coverMm,
    double tieEndOffsetMm,
    int cx,
    int cy,
    double mainBarDiameterMm,
    double stirrupDiameterMm,
    IReadOnlyList<LevelPreviewItem> levels,
    double columnZ0Mm,
    double columnZ1Mm)
  {
    WidthMm  = Math.Max(1, widthMm);
    HeightMm = Math.Max(1, heightMm);

    CoverMm        = Math.Max(0, coverMm);
    TieEndOffsetMm = Math.Max(0, tieEndOffsetMm);

    MainBarDiameterMm = Math.Max(1, mainBarDiameterMm);
    StirrupDiameterMm = Math.Max(1, stirrupDiameterMm);

    Cx = Math.Max(2, cx);
    Cy = Math.Max(2, cy);

    Levels = levels ?? Array.Empty<LevelPreviewItem>();
    ColumnZ0Mm = columnZ0Mm;
    ColumnZ1Mm = columnZ1Mm;

    // ===== computed info =====
    SectionText = $"S={WidthMm:0}x{HeightMm:0}";

    var totalBars = Math.Max(0, 2 * (Cx + Cy) - 4);
    TotalBarsText = $"{totalBars}Ø{MainBarDiameterMm:0}";

    // area (cm2)
    var areaOneMm2 = Math.PI * MainBarDiameterMm * MainBarDiameterMm / 4.0;
    RebarAreaCm2 = totalBars * areaOneMm2 / 100.0; // 1 cm2 = 100 mm2
    RebarAreaText = $"{RebarAreaCm2:0.###} cm²";

    // ratio (%)
    var sectionAreaMm2 = WidthMm * HeightMm;
    RebarRatioPercent = sectionAreaMm2 <= 1e-6
      ? 0
      : (totalBars * areaOneMm2) / sectionAreaMm2 * 100.0;
    RebarRatioText = $"{RebarRatioPercent:0.###}%";

    BarsUV = BuildPerimeterBarsUV(Cx, Cy);
  }

  // ✅ FIX: Create now accepts diameters (with defaults)
  public static ColumnPreviewModel Create(
    double widthMm,
    double heightMm,
    double coverMm,
    double tieEndOffsetMm,
    int cx,
    int cy,
    double mainBarDiameterMm = 16,
    double stirrupDiameterMm = 8,
    IReadOnlyList<LevelPreviewItem>? levels = null,
    double columnZ0Mm = 0,
    double columnZ1Mm = 0)
  {
    return new ColumnPreviewModel(
      widthMm, heightMm,
      coverMm, tieEndOffsetMm,
      cx, cy,
      mainBarDiameterMm,
      stirrupDiameterMm,
      levels ?? Array.Empty<LevelPreviewItem>(),
      columnZ0Mm,
      columnZ1Mm);
  }

  // ==========================================================
  // ✅ CreateFromRevit
  // ==========================================================
  public static ColumnPreviewModel CreateFromRevit(
    Document doc,
    FamilyInstance host,
    double coverMm,
    double tieEndOffsetMm,
    int cx,
    int cy,
    double xyTolMm = 10.0,
    double mainBarDiameterMm = 16,
    double stirrupDiameterMm = 8)
  {
    if (doc is null) throw new ArgumentNullException(nameof(doc));
    if (host is null) throw new ArgumentNullException(nameof(host));

    // SECTION size from bounding box XY
    var bb = host.get_BoundingBox(null);
    var widthMm  = 300.0;
    var heightMm = 300.0;
    if (bb != null)
    {
      widthMm  = FtToMm(Math.Abs(bb.Max.X - bb.Min.X));
      heightMm = FtToMm(Math.Abs(bb.Max.Y - bb.Min.Y));
    }

    // 1) UNION Z của các cột cùng trục XY
    if (!TryGetUnionZRangeSameAxisFt(doc, host, xyTolMm, out var z0UnionFt, out var z1UnionFt))
    {
      // fallback = just host
      if (!TryGetColumnZRangeFt(doc, host, out z0UnionFt, out z1UnionFt))
      {
        if (bb != null) { z0UnionFt = bb.Min.Z; z1UnionFt = bb.Max.Z; }
        else { z0UnionFt = 0; z1UnionFt = 10; }
      }
    }

    var colZ0Mm = FtToMm(z0UnionFt);
    var colZ1Mm = FtToMm(z1UnionFt);
    var colMin  = Math.Min(colZ0Mm, colZ1Mm);
    var colMax  = Math.Max(colZ0Mm, colZ1Mm);

    // 2) Collect level marks inside UNION range (+eps)
    var lvls = new FilteredElementCollector(doc)
      .OfClass(typeof(Level))
      .Cast<Level>()
      .OrderBy(x => x.Elevation)
      .ToList();

    const double eps = 1.0; // 1mm tolerance

    var marks = new List<(string Name, double Zmm)>();
    foreach (var lv in lvls)
    {
      var zMm = FtToMm(lv.Elevation);
      if (zMm >= colMin - eps && zMm <= colMax + eps)
        marks.Add((lv.Name, zMm));
    }

    // Ensure boundary marks exist (base/top)
    var baseName = FindNearestLevelName(lvls, FtFromMm(colMin), preferBelow: true) ?? "BASE";
    var topName  = FindNearestLevelName(lvls, FtFromMm(colMax), preferBelow: false) ?? "TOP";

    bool HasMarkAt(double z) => marks.Any(m => Math.Abs(m.Zmm - z) <= eps);

    if (!HasMarkAt(colMin)) marks.Add((baseName, colMin));
    if (!HasMarkAt(colMax)) marks.Add((topName,  colMax));

    // Remove duplicates by Z, keep sorted by Z ASC
    var finalMarks = marks
      .OrderBy(m => m.Zmm)
      .GroupBy(m => Math.Round(m.Zmm, 3))
      .Select(g => g.First())
      .ToList();

    var levelItems = finalMarks
      .Select(m => LevelPreviewItem.FromLevelElevation(m.Name, m.Zmm))
      .ToList();

    // ✅ pass diameters into Create
    return Create(
      widthMm, heightMm,
      coverMm, tieEndOffsetMm,
      cx, cy,
      mainBarDiameterMm: mainBarDiameterMm,
      stirrupDiameterMm: stirrupDiameterMm,
      levels: levelItems,
      columnZ0Mm: colZ0Mm,
      columnZ1Mm: colZ1Mm);
  }

  // ==========================================================
  // ✅ UNION Z of columns on same axis XY (stacked columns)
  // ==========================================================
  private static bool TryGetUnionZRangeSameAxisFt(
    Document doc,
    FamilyInstance host,
    double xyTolMm,
    out double z0Ft,
    out double z1Ft)
  {
    z0Ft = 0;
    z1Ft = 0;

    var tolFt = UnitUtils.ConvertToInternalUnits(xyTolMm, UnitTypeId.Millimeters);
    var hostPt = GetXYPoint(host);

    var cols = new FilteredElementCollector(doc)
      .OfCategory(BuiltInCategory.OST_StructuralColumns)
      .WhereElementIsNotElementType()
      .OfType<FamilyInstance>()
      .ToList();

    bool any = false;
    double minZ = double.MaxValue;
    double maxZ = double.MinValue;

    foreach (var fi in cols)
    {
      var p = GetXYPoint(fi);
      if (Math.Abs(p.X - hostPt.X) > tolFt || Math.Abs(p.Y - hostPt.Y) > tolFt)
        continue;

      if (!TryGetColumnZRangeFt(doc, fi, out var a, out var b))
        continue;

      var lo = Math.Min(a, b);
      var hi = Math.Max(a, b);

      minZ = Math.Min(minZ, lo);
      maxZ = Math.Max(maxZ, hi);
      any = true;
    }

    if (!any) return false;

    z0Ft = minZ;
    z1Ft = maxZ;
    return true;
  }

  private static XYZ GetXYPoint(FamilyInstance fi)
  {
    if (fi.Location is LocationPoint lp) return lp.Point;

    var bb = fi.get_BoundingBox(null);
    if (bb != null) return (bb.Min + bb.Max) * 0.5;

    return XYZ.Zero;
  }

  // ==========================================================
  // Column Z range: Base/Top (+offset) or unconnected length
  // ==========================================================
  private static bool TryGetColumnZRangeFt(Document doc, FamilyInstance fi, out double z0Ft, out double z1Ft)
  {
    z0Ft = 0;
    z1Ft = 0;

    var baseLv = TryGetLevel(doc, fi.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM))
                 ?? TryGetLevel(doc, fi.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM));
    if (baseLv == null) return false;

    var baseOffset = GetDouble(fi, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
    z0Ft = baseLv.Elevation + baseOffset;

    var topLv = TryGetLevel(doc, fi.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM));
    if (topLv != null)
    {
      var topOffset = GetDouble(fi, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      z1Ft = topLv.Elevation + topOffset;
      return true;
    }

    var len = GetDouble(fi, BuiltInParameter.INSTANCE_LENGTH_PARAM);
    if (len > 1e-6)
    {
      z1Ft = z0Ft + len;
      return true;
    }

    var bb = fi.get_BoundingBox(null);
    if (bb != null)
    {
      z0Ft = bb.Min.Z;
      z1Ft = bb.Max.Z;
      return true;
    }

    return false;
  }

  private static Level? TryGetLevel(Document doc, Parameter? p)
  {
    if (p == null) return null;
    var id = p.AsElementId();
    if (id == ElementId.InvalidElementId) return null;
    return doc.GetElement(id) as Level;
  }

  private static double GetDouble(Element e, BuiltInParameter bip)
  {
    var p = e.get_Parameter(bip);
    return (p != null && p.StorageType == StorageType.Double) ? p.AsDouble() : 0.0;
  }

  private static string? FindNearestLevelName(List<Level> levels, double zFt, bool preferBelow)
  {
    if (levels.Count == 0) return null;

    Level? best;
    if (preferBelow)
    {
      best = levels
        .Where(l => l.Elevation <= zFt + 1e-9)
        .OrderByDescending(l => l.Elevation)
        .FirstOrDefault();
    }
    else
    {
      best = levels
        .Where(l => l.Elevation >= zFt - 1e-9)
        .OrderBy(l => l.Elevation)
        .FirstOrDefault();
    }

    return best?.Name;
  }

  private static double FtToMm(double ft) => ft * 304.8;
  private static double FtFromMm(double mm) => mm / 304.8;

  // ==========================================================
  // Perimeter bars UV
  // ==========================================================
  private static IReadOnlyList<Point> BuildPerimeterBarsUV(int cx, int cy)
  {
    var pts = new List<Point>();

    for (int i = 0; i < cx; i++)
    {
      var u = (cx == 1) ? 0.5 : (double)i / (cx - 1);
      pts.Add(new Point(u, 1));
    }

    for (int j = 1; j < cy - 1; j++)
    {
      var v = (cy == 1) ? 0.5 : 1.0 - (double)j / (cy - 1);
      pts.Add(new Point(1, v));
    }

    for (int i = cx - 1; i >= 0; i--)
    {
      var u = (cx == 1) ? 0.5 : (double)i / (cx - 1);
      pts.Add(new Point(u, 0));
    }

    for (int j = 1; j < cy - 1; j++)
    {
      var v = (cy == 1) ? 0.5 : (double)j / (cy - 1);
      pts.Add(new Point(0, v));
    }

    return pts
      .GroupBy(p => $"{Math.Round(p.X, 6)}_{Math.Round(p.Y, 6)}")
      .Select(g => g.First())
      .ToList();
  }
}
