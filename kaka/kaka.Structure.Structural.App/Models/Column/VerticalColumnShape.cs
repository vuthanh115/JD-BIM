using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kaka.Structure.Structural.App.Models.Column
{
  public class VerticalColumnShape : ShapeBase
  {
    private readonly List<Point> _points;

    public Polygon Polygon { get; private set; } = null!;

    public VerticalColumnShape(Canvas canvas, List<Point> points) : base(canvas)
    {
      _points = points ?? throw new ArgumentNullException(nameof(points));
      if (_points.Count < 3)
        throw new ArgumentException("Polygon requires at least 3 points.", nameof(points));

      Polygon = new Polygon
      {
        Points = new PointCollection(_points),
        StrokeThickness = StrokeThickness,
        Stroke = Stroke,
        // Fill để hit-test dễ hơn (trong suốt vẫn bắt chuột được)
        Fill = Brushes.Transparent,
        SnapsToDevicePixels = true
      };

      // lưu shape về base để base có thể dùng chung
      shape = Polygon;

      Canvas.Children.Add(Polygon);
    }

    public override void MakeHighLight()
    {
      // cách đơn giản: tăng thickness + đổi stroke
      // (nếu bạn muốn dùng màu theo theme, bạn sửa ở đây)
      if (Polygon == null) return;

      Polygon.StrokeThickness = Math.Max(StrokeThickness, 1.0) * 2.0;
      Polygon.Stroke = Brushes.OrangeRed;
      Polygon.Fill = new SolidColorBrush(Color.FromArgb(30, 255, 165, 0)); // hơi nổi nhẹ
    }

    public override void ResetHighLight()
    {
      if (Polygon == null) return;

      Polygon.StrokeThickness = StrokeThickness;
      Polygon.Stroke = Stroke;
      Polygon.Fill = Brushes.Transparent;
    }

    public override bool CheckMoveOver(Point currentPoint)
    {
      // Ưu tiên hit-test WPF (nhanh + chuẩn theo Fill)
      if (Polygon != null)
      {
        try
        {
          // IsMouseOver chỉ đúng nếu currentPoint là theo Mouse position.
          // Ở đây ta dùng VisualTreeHelper.HitTest theo currentPoint trên Canvas.
          var hit = VisualTreeHelper.HitTest(Canvas, currentPoint);
          if (hit != null && (hit.VisualHit == Polygon || IsChildOf(hit.VisualHit, Polygon)))
            return true;
        }
        catch
        {
          // fallback xuống thuật toán point-in-polygon
        }
      }

      // Fallback: thuật toán ray casting (point in polygon)
      return PointInPolygon(currentPoint, _points);
    }

    private static bool IsChildOf(DependencyObject child, DependencyObject parent)
    {
      var cur = child;
      while (cur != null)
      {
        if (cur == parent) return true;
        cur = VisualTreeHelper.GetParent(cur);
      }
      return false;
    }

    private static bool PointInPolygon(Point p, IList<Point> poly)
    {
      // Ray casting
      bool inside = false;
      int n = poly.Count;
      for (int i = 0, j = n - 1; i < n; j = i++)
      {
        var pi = poly[i];
        var pj = poly[j];

        bool intersect =
          ((pi.Y > p.Y) != (pj.Y > p.Y)) &&
          (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y + double.Epsilon) + pi.X);

        if (intersect) inside = !inside;
      }
      return inside;
    }
  }
}
