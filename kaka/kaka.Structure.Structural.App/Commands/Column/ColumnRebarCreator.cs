#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;


namespace kaka.Structure.Structural.App.Commands.Column ;

internal sealed record ColumnRebarResult(
  IReadOnlyList<ElementId> LongitudinalBarIds,
  IReadOnlyList<ElementId> TieIds
);

internal static class ColumnRebarCreator
{
  public static ColumnRebarResult Create(
    Document doc,
    FamilyInstance column,
    ColumnRebarOptions opt)
  {
    var longIds = new List<ElementId>();
    var tieIds  = new List<ElementId>();

    var bb = column.get_BoundingBox(null)
             ?? throw new InvalidOperationException("Column has no bounding box.");

    var min = bb.Min;
    var max = bb.Max;

    // =========================
    // Z RANGE (apply cover)
    // =========================
    var z0 = min.Z + opt.Cover;
    var z1 = max.Z - opt.Cover;
    if (z1 <= z0)
      throw new InvalidOperationException("Invalid Z range after cover.");

    // =========================
    // INNER RECT (COVER)
    // =========================
    var x0 = min.X + opt.Cover;
    var x1 = max.X - opt.Cover;
    var y0 = min.Y + opt.Cover;
    var y1 = max.Y - opt.Cover;
    if (x1 <= x0 || y1 <= y0)
      throw new InvalidOperationException("Cover too large for column section.");

    // =========================
    // PERIMETER XY points
    // =========================
    var pts = BuildPerimeterXY(x0, x1, y0, y1, opt.MainBarCountX, opt.MainBarCountY);

    // =========================
    // SPLIT BY LEVELS (segments)
    // =========================
    var cuts = GetLevelCutsWithin(doc, z0, z1);
    EnsureBoundaryCuts(cuts, z0, z1);

    // Nếu vì lý do nào đó cuts không hợp lệ
    if (cuts.Count < 2)
    {
      cuts.Clear();
      cuts.Add(z0);
      cuts.Add(z1);
    }

    // =========================
    // LONG BARS: per segment + lap 40d
    // =========================
    var dFt = GetBarDiameterFt(opt.MainBarType);

    var lapFt = 40.0 * dFt;                 // 40d

    for (int si = 0; si < cuts.Count - 1; si++)
    {
      var segZ0 = cuts[si];
      var segZ1 = cuts[si + 1];

      // clamp into [z0..z1]
      segZ0 = Math.Max(z0, Math.Min(z1, segZ0));
      segZ1 = Math.Max(z0, Math.Min(z1, segZ1));
      if (segZ1 <= segZ0 + 1e-9) continue;

      var isLast = (si == cuts.Count - 2);

      // bar end = segment top + lap (except last), but do not exceed global top z1
      var barZ1 = isLast ? segZ1 : Math.Min(z1, segZ1 + lapFt);

      foreach (var p in pts)
      {
        var line = Line.CreateBound(
          new XYZ(p.X, p.Y, segZ0),
          new XYZ(p.X, p.Y, barZ1));

        var rebar = Rebar.CreateFromCurves(
          doc,
          RebarStyle.Standard,
          opt.MainBarType,
          null,
          null,
          column,
          XYZ.BasisX, // any vector not parallel to the bar line
          new List<Curve> { line },
          RebarHookOrientation.Left,
          RebarHookOrientation.Left,
          true,
          true
        );

        if (rebar != null) longIds.Add(rebar.Id);
      }
    }

    // =========================
    // TIES: per segment, inset at both ends (50mm in your UI => opt.TieEndOffset)
    // =========================
    for (int si = 0; si < cuts.Count - 1; si++)
    {
      var segZ0 = cuts[si];
      var segZ1 = cuts[si + 1];

      segZ0 = Math.Max(z0, Math.Min(z1, segZ0));
      segZ1 = Math.Max(z0, Math.Min(z1, segZ1));
      if (segZ1 <= segZ0 + 1e-9) continue;

      // Tie zone inside this segment
      var tieZ0 = segZ0 + opt.TieEndOffset;
      var tieZ1 = segZ1 - opt.TieEndOffset;

      // Nếu đoạn tầng quá thấp (sau khi thụt 2 đầu) thì skip đai
      if (tieZ1 <= tieZ0 + 1e-9) continue;

      var height = tieZ1 - tieZ0;
      var count  = Math.Max(1, (int)Math.Floor(height / opt.TieSpacing) + 1);

      for (int i = 0; i < count; i++)
      {
        var z = Math.Min(tieZ0 + i * opt.TieSpacing, tieZ1);

        var loop = new List<Curve>
        {
          Line.CreateBound(new XYZ(x0, y0, z), new XYZ(x1, y0, z)),
          Line.CreateBound(new XYZ(x1, y0, z), new XYZ(x1, y1, z)),
          Line.CreateBound(new XYZ(x1, y1, z), new XYZ(x0, y1, z)),
          Line.CreateBound(new XYZ(x0, y1, z), new XYZ(x0, y0, z)),
        };

        var tie = Rebar.CreateFromCurves(
          doc,
          RebarStyle.StirrupTie,
          opt.TieBarType,
          null,
          null,
          column,
          XYZ.BasisZ,
          loop,
          RebarHookOrientation.Left,
          RebarHookOrientation.Left,
          true,
          true
        );

        if (tie != null) tieIds.Add(tie.Id);
      }
    }

    return new ColumnRebarResult(longIds, tieIds);
  }
    
  private static double GetBarDiameterFt(RebarBarType barType)
  {
    if (barType == null)
      throw new ArgumentNullException(nameof(barType));

    var p = barType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER);
    if (p == null || p.StorageType != StorageType.Double)
      throw new InvalidOperationException("Cannot read REBAR_BAR_DIAMETER.");

    return p.AsDouble(); // feet
  }


  // ============================================================
  // Level cuts inside [z0..z1] (ft) using Level.Elevation
  // ============================================================
  private static List<double> GetLevelCutsWithin(Document doc, double z0, double z1)
  {
    // eps ~ 1mm in feet
    const double epsFt = 1.0 / 304.8;

    var cuts = new FilteredElementCollector(doc)
      .OfClass(typeof(Level))
      .Cast<Level>()
      .Select(lv => lv.Elevation)
      .Where(z => z >= z0 - epsFt && z <= z1 + epsFt)
      .OrderBy(z => z)
      .ToList();

    // unique (by small rounding)
    return cuts
      .GroupBy(z => Math.Round(z, 8))
      .Select(g => g.First())
      .ToList();
  }

  private static void EnsureBoundaryCuts(List<double> cuts, double z0, double z1)
  {
    bool Has(double z) => cuts.Any(x => Math.Abs(x - z) < 1e-8);

    if (!Has(z0)) cuts.Insert(0, z0);
    if (!Has(z1)) cuts.Add(z1);

    cuts.Sort();

    // remove duplicates again after adding boundaries
    var uniq = cuts
      .GroupBy(z => Math.Round(z, 8))
      .Select(g => g.First())
      .OrderBy(z => z)
      .ToList();

    cuts.Clear();
    cuts.AddRange(uniq);
  }

  // -----------------------------
  // Perimeter points builder
  // -----------------------------
  private static List<XYZ> BuildPerimeterXY(
    double x0, double x1, double y0, double y1,
    int cx, int cy)
  {
    cx = Math.Max(2, cx);
    cy = Math.Max(2, cy);

    var pts = new List<XYZ>();

    // top (y1): Cx points
    for (int i = 0; i < cx; i++)
    {
      var t = (double)i / (cx - 1);
      pts.Add(new XYZ(Lerp(x0, x1, t), y1, 0));
    }

    // right (x1): Cy points, skip corners
    for (int j = 1; j < cy - 1; j++)
    {
      var t = (double)j / (cy - 1);
      pts.Add(new XYZ(x1, Lerp(y1, y0, t), 0));
    }

    // bottom (y0): Cx points (reverse), includes corners
    for (int i = cx - 1; i >= 0; i--)
    {
      var t = (double)i / (cx - 1);
      pts.Add(new XYZ(Lerp(x0, x1, t), y0, 0));
    }

    // left (x0): Cy points, skip corners
    for (int j = 1; j < cy - 1; j++)
    {
      var t = (double)j / (cy - 1);
      pts.Add(new XYZ(x0, Lerp(y0, y1, t), 0));
    }

    // remove duplicates
    var uniq = new Dictionary<string, XYZ>();
    foreach (var p in pts)
    {
      var k = $"{Math.Round(p.X, 6)}_{Math.Round(p.Y, 6)}";
      if (!uniq.ContainsKey(k)) uniq.Add(k, p);
    }

    return new List<XYZ>(uniq.Values);
  }

  private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}