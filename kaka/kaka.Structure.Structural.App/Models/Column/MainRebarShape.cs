using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kaka.Structure.Structural.App.Models.Column
{
  public sealed class MainRebarShape : ShapeBase
  {
    public Ellipse Ellipse { get; private set; }

    public EnumShapePosition Position { get; set; }
    public EnumOrientation Orientation { get; set; }

    public MainRebarShape(
      Canvas canvas,
      double width,
      double height,
      double left,
      double top)
      : base(canvas)
    {
      if (canvas == null) throw new ArgumentNullException(nameof(canvas));

      Ellipse = new Ellipse
      {
        Width  = width,
        Height = height,
        Stroke = Stroke ?? Brushes.Black,
        StrokeThickness = StrokeThickness > 0 ? StrokeThickness : 1.0,
        Fill = Brushes.Transparent,
        SnapsToDevicePixels = true,
        IsHitTestVisible = true
      };

      Canvas.SetLeft(Ellipse, left);
      Canvas.SetTop(Ellipse, top);

      shape = Ellipse;
      canvas.Children.Add(Ellipse);
    }

    public Point GetCenter()
    {
      if (Ellipse == null)
        throw new InvalidOperationException("Ellipse is not initialized.");

      var left = Canvas.GetLeft(Ellipse);
      var top  = Canvas.GetTop(Ellipse);

      if (double.IsNaN(left)) left = 0;
      if (double.IsNaN(top))  top  = 0;

      return new Point(
        left + Ellipse.Width  * 0.5,
        top  + Ellipse.Height * 0.5
      );
    }

    public override void MakeHighLight()
    {
      if (Ellipse != null)
        Ellipse.StrokeThickness = (StrokeThickness > 0 ? StrokeThickness : 1.0) * 1.8;
    }

    public override void ResetHighLight()
    {
      if (Ellipse != null)
        Ellipse.StrokeThickness = StrokeThickness > 0 ? StrokeThickness : 1.0;
    }

    public override bool CheckMoveOver(Point currentPoint)
    {
      var center = GetCenter();
      var r = Ellipse.Width * 0.5;
      return (currentPoint - center).Length <= r;
    }
  }
}
