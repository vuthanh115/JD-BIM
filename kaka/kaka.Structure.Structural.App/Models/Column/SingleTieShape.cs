using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kaka.Structure.Structural.App.Models.Column
{
  public sealed class SingleTieShape : ShapeBase
  {
    public Path SingleTie { get; private set; }

    private readonly MainRebarShape _a;
    private readonly MainRebarShape _b;

    public SingleTieShape(Canvas canvas, MainRebarShape ellipse1, MainRebarShape ellipse2, double thickness)
      : base(canvas)
    {
      _a = ellipse1 ?? throw new ArgumentNullException(nameof(ellipse1));
      _b = ellipse2 ?? throw new ArgumentNullException(nameof(ellipse2));

      // 1) tạo path
      var path = new Path
      {
        Stroke = Stroke ?? Brushes.Black,
        StrokeThickness = thickness <= 0 ? 1.0 : thickness,
        SnapsToDevicePixels = true,
        IsHitTestVisible = true
      };

      // 2) geometry nối 2 điểm
      var p1 = GetAnchorPoint(_a);
      var p2 = GetAnchorPoint(_b);

      path.Data = BuildTieGeometry(p1, p2);

      // 3) gán field + add lên canvas
      SingleTie = path;
      this.shape = path;

      canvas.Children.Add(path);
    }

    private static Geometry BuildTieGeometry(Point p1, Point p2)
    {
      // Đơn giản: 1 đoạn thẳng. (Nếu muốn “móc đai” cong, bạn đổi sang Bezier/Arc)
      var figure = new PathFigure { StartPoint = p1, IsClosed = false, IsFilled = false };
      figure.Segments.Add(new LineSegment(p2, true));
      return new PathGeometry(new[] { figure });
    }

    private static Point GetAnchorPoint(MainRebarShape s)
    {
      // === CÁCH 1 (khuyến nghị): nếu MainRebarShape có CenterPoint ===
      // return s.Center; // <-- nếu bạn có property này

      // === CÁCH 2: nếu MainRebarShape có Ellipse và set Canvas.Left/Top ===
      // giả sử Ellipse nằm đúng vị trí, ta lấy center từ Left/Top + Width/Height
      if (s is null) return default;

      // Bạn sửa lại đúng property thật của bạn:
      // ví dụ: s.Ellipse (Shape) hoặc s.MainRebar (Ellipse)
      var ellipse = s.Ellipse; // <-- cần tồn tại trong MainRebarShape

      if (ellipse == null)
        throw new InvalidOperationException("MainRebarShape.Ellipse is null. Provide an anchor point for SingleTieShape.");

      var left = Canvas.GetLeft(ellipse);
      var top  = Canvas.GetTop(ellipse);

      // WPF: nếu chưa set Left/Top thì GetLeft/GetTop trả NaN
      if (double.IsNaN(left)) left = 0;
      if (double.IsNaN(top))  top  = 0;

      return new Point(left + ellipse.Width * 0.5, top + ellipse.Height * 0.5);
    }
  }
}
