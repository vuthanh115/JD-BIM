using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kaka.Structure.Structural.App.Models.Column
{
  public class RoundedCornersPolygon : Shape
  {
    private readonly Path _path;

    public Path Path => _path;

    public RoundedCornersPolygon()
    {
      _path = new Path { Data = Geometry.Empty };
      Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
      // Đồng bộ style nếu bạn có dùng Path nội bộ để add lên Canvas
      _path.Stroke = Stroke;
      _path.Fill = Fill;
      _path.StrokeThickness = StrokeThickness;
      _path.StrokeDashArray = StrokeDashArray;
      _path.StrokeDashOffset = StrokeDashOffset;
      _path.StrokeLineJoin = StrokeLineJoin;
      _path.StrokeMiterLimit = StrokeMiterLimit;
      _path.StrokeStartLineCap = StrokeStartLineCap;
      _path.StrokeEndLineCap = StrokeEndLineCap;

      _path.Data = DefiningGeometry;
    }

    // ====== Dependency Properties ======

    public static readonly DependencyProperty PointsProperty =
      DependencyProperty.Register(
        nameof(Points),
        typeof(PointCollection),
        typeof(RoundedCornersPolygon),
        new FrameworkPropertyMetadata(
          new PointCollection(),
          FrameworkPropertyMetadataOptions.AffectsMeasure |
          FrameworkPropertyMetadataOptions.AffectsRender,
          OnAnyPropertyChanged));

    public PointCollection Points
    {
      get => (PointCollection)GetValue(PointsProperty);
      set => SetValue(PointsProperty, value);
    }

    public static readonly DependencyProperty IsClosedProperty =
      DependencyProperty.Register(
        nameof(IsClosed),
        typeof(bool),
        typeof(RoundedCornersPolygon),
        new FrameworkPropertyMetadata(
          true,
          FrameworkPropertyMetadataOptions.AffectsRender,
          OnAnyPropertyChanged));

    public bool IsClosed
    {
      get => (bool)GetValue(IsClosedProperty);
      set => SetValue(IsClosedProperty, value);
    }

    public static readonly DependencyProperty UseRoundnessPercentageProperty =
      DependencyProperty.Register(
        nameof(UseRoundnessPercentage),
        typeof(bool),
        typeof(RoundedCornersPolygon),
        new FrameworkPropertyMetadata(
          true,
          FrameworkPropertyMetadataOptions.AffectsRender,
          OnAnyPropertyChanged));

    public bool UseRoundnessPercentage
    {
      get => (bool)GetValue(UseRoundnessPercentageProperty);
      set => SetValue(UseRoundnessPercentageProperty, value);
    }

    public static readonly DependencyProperty ArcRoundnessProperty =
      DependencyProperty.Register(
        nameof(ArcRoundness),
        typeof(double),
        typeof(RoundedCornersPolygon),
        new FrameworkPropertyMetadata(
          0.15, // mặc định 15% nếu UseRoundnessPercentage=true
          FrameworkPropertyMetadataOptions.AffectsRender,
          OnAnyPropertyChanged));

    public double ArcRoundness
    {
      get => (double)GetValue(ArcRoundnessProperty);
      set => SetValue(ArcRoundnessProperty, value);
    }

    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not RoundedCornersPolygon p) return;
      p.InvalidateVisual();
      p.InvalidateMeasure();
      p._path.Data = p.DefiningGeometry;
    }

    // ====== Public helpers ======

    public Geometry Data => DefiningGeometry;

    protected override Geometry DefiningGeometry => BuildGeometry();

    // ====== Core geometry builder ======

    private Geometry BuildGeometry()
    {
      var pts = Points;
      if (pts == null || pts.Count < 2)
        return Geometry.Empty;

      // Nếu không đóng, vẫn vẽ bo góc trên polyline (đầu/cuối sẽ không bo trọn)
      var closed = IsClosed && pts.Count >= 3;

      // copy ra array cho dễ xử lý
      var pArr = pts.ToArray();

      // bỏ điểm trùng cuối nếu user đưa vào (đóng hình bằng điểm đầu)

      if (closed && pArr.Length >= 2 && AreClose(pArr[0], pArr[^1]))
        pArr = pArr.Take(pArr.Length - 1).ToArray();

      if (pArr.Length < 2)
        return Geometry.Empty;

      var fig = new PathFigure { IsClosed = closed, IsFilled = true };

      int n = pArr.Length;

      // helper lấy điểm i theo vòng tròn
      Point Get(int i) => pArr[(i % n + n) % n];

      // nếu open polyline: start ở p0
      if (!closed)
      {
        fig.StartPoint = pArr[0];
        for (int i = 1; i < n; i++)
          fig.Segments.Add(new LineSegment(pArr[i], true));

        return new PathGeometry(new[] { fig });
      }

      // closed polygon with rounded corners
      // Với mỗi góc B, bo bằng 1 Arc giữa 2 điểm cắt trên AB và BC.
      for (int i = 0; i < n; i++)
      {
        var a = Get(i - 1);
        var b = Get(i);
        var c = Get(i + 1);

        // vector BA và BC
        var v1 = a - b;
        var v2 = c - b;

        var len1 = v1.Length;
        var len2 = v2.Length;

        if (len1 < 1e-6 || len2 < 1e-6)
        {
          // điểm bị trùng / cạnh quá ngắn -> bỏ qua bo
          if (i == 0) fig.StartPoint = b;
          else fig.Segments.Add(new LineSegment(b, true));
          continue;
        }

        // roundness: nếu percentage thì lấy theo min(len1,len2)
        double r;
        if (UseRoundnessPercentage)
        {
          // clamp 0..0.49 để không “ăn” hết cạnh
          var t = Clamp01(ArcRoundness);
          t = Math.Min(t, 0.49);
          r = Math.Min(len1, len2) * t;
        }
        else
        {
          r = Math.Max(0, ArcRoundness);
          // không cho vượt quá nửa cạnh
          r = Math.Min(r, Math.Min(len1, len2) * 0.49);
        }

        // 2 điểm cắt trên 2 cạnh (từ B đi ra)
        var p1 = b + (v1 / len1) * r; // trên BA
        var p2 = b + (v2 / len2) * r; // trên BC

        if (i == 0)
        {
          fig.StartPoint = p1;
        }
        else
        {
          fig.Segments.Add(new LineSegment(p1, true));
        }

        // Arc từ p1 -> p2, với bán kính r
        // SweepDirection dựa vào hướng quay (cross product)
        var cross = Cross(v1, v2); // z-component
        var sweep = cross < 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

        fig.Segments.Add(new ArcSegment
        {
          Point = p2,
          Size = new Size(r, r),
          IsLargeArc = false,
          SweepDirection = sweep,
          RotationAngle = 0,
          IsStroked = true
        });
      }

      return new PathGeometry(new[] { fig });
    }

    private static bool AreClose(Point p1, Point p2)
      => Math.Abs(p1.X - p2.X) < 1e-6 && Math.Abs(p1.Y - p2.Y) < 1e-6;

    private static double Clamp01(double v)
      => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static double Cross(Vector v1, Vector v2)
      => v1.X * v2.Y - v1.Y * v2.X;
  }
}
