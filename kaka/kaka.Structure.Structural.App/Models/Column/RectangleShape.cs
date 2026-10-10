using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kaka.Structure.Structural.App.Models.Column
{
  public sealed class RectangleShape : ShapeBase
  {
    public Rectangle Rectangle { get; private set; }

    public RectangleShape(Canvas canvas, double width, double height, double left, double top)
      : base(canvas)
    {
      var rect = new Rectangle
      {
        Width = width,
        Height = height,
        Stroke = Stroke ?? Brushes.Black,
        StrokeThickness = StrokeThickness > 0 ? StrokeThickness : 1.0,
        Fill = Brushes.Transparent,
        SnapsToDevicePixels = true,
        IsHitTestVisible = true
      };

      Canvas.SetLeft(rect, left);
      Canvas.SetTop(rect, top);

      Rectangle = rect;
      this.shape = rect;

      canvas.Children.Add(rect);
    }

    public override void MakeHighLight()
    {
      if (Rectangle == null) return;
      Rectangle.StrokeThickness = StrokeThickness * 2;
      Rectangle.Stroke = Brushes.Red;
    }

    public override void ResetHighLight()
    {
      if (Rectangle == null) return;
      Rectangle.StrokeThickness = StrokeThickness;
      Rectangle.Stroke = Stroke ?? Brushes.Black;
    }

    public override bool CheckMoveOver(Point currentPoint)
    {
      if (Rectangle == null) return false;

      var left = Canvas.GetLeft(Rectangle);
      var top  = Canvas.GetTop(Rectangle);

      if (double.IsNaN(left)) left = 0;
      if (double.IsNaN(top))  top  = 0;

      var rect = new Rect(left, top, Rectangle.Width, Rectangle.Height);
      return rect.Contains(currentPoint);
    }
  }
}