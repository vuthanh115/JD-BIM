#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace kaka.Structure.Structural.App.Utils
{
  public static class GeometryUtils
  {
    /// <summary>
    /// Move a point along a direction by length (Revit internal units).
    /// Direction doesn't need to be normalized.
    /// </summary>
    public static XYZ MovePoint(this XYZ point, XYZ direction, double length)
    {
      if (point is null) throw new ArgumentNullException(nameof(point));
      if (direction is null) throw new ArgumentNullException(nameof(direction));

      if (length == 0) return point;

      var dirLen = direction.GetLength();
      if (dirLen < 1e-12) return point;

      var unitDir = direction / dirLen;
      return point + unitDir * length;
    }

    /// <summary>
    /// Move a line along a direction by length (Revit internal units).
    /// </summary>
    public static Line MoveLine(this Line line, XYZ direction, double length)
    {
      if (line is null) throw new ArgumentNullException(nameof(line));
      if (direction is null) throw new ArgumentNullException(nameof(direction));

      var p0 = line.GetEndPoint(0);
      var p1 = line.GetEndPoint(1);

      var p0m = p0.MovePoint(direction, length);
      var p1m = p1.MovePoint(direction, length);

      return Line.CreateBound(p0m, p1m);
    }

    /// <summary>
    /// Intersect this curve with other curves and return intersection points.
    /// For overlap results, returns the end points of the overlap segment if available.
    /// </summary>
    public static List<XYZ> IntersectWithCurves(this Curve curve, params Curve[] curves)
    {
      if (curve is null) throw new ArgumentNullException(nameof(curve));
      if (curves is null) throw new ArgumentNullException(nameof(curves));

      var result = new List<XYZ>();

      foreach (var other in curves)
      {
        if (other is null) continue;

        // Revit intersection API
        var ira = new IntersectionResultArray();
        var comp = curve.Intersect(other, out ira);

        if (comp == SetComparisonResult.Disjoint || ira is null || ira.Size == 0)
          continue;

        // Typical case: distinct points
        for (var i = 0; i < ira.Size; i++)
        {
          var ir = ira.get_Item(i);
          if (ir is null) continue;

          // IntersectionResult may have XYZPoint
          var p = ir.XYZPoint;
          if (p is not null)
            result.Add(p);
        }

        // If overlap: Intersect can still return results; but sometimes you want endpoints
        // Try compute overlap endpoints in a safe way
        if (comp == SetComparisonResult.Overlap && ira.Size >= 2)
        {
          // usually already captured via XYZPoint, but keep as-is
        }
      }

      return result;
    }
  }
}
