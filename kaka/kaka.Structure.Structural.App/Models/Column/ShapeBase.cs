#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace kaka.Structure.Structural.App.Models.Column
{
  public class ShapeBase
  {
    protected Shape? shape;

    public ColumnModel? Column { get; set; }

    public double StrokeThickness { get; set; } = 1.0;

    public Canvas Canvas { get; set; }

    public Brush Stroke { get; set; } = Brushes.Black;

    public int Index { get; set; }

    private bool _isMoveOver;
    public bool IsMoveOver
    {
      get => _isMoveOver;
      set
      {
        if (_isMoveOver == value) return;
        _isMoveOver = value;

        // Hover highlight nhẹ
        if (_isMoveOver)
          MakeHighLight();
        else if (!IsSelected)
          ResetHighLight();
      }
    }

    private bool _isSelected;
    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected == value) return;
        _isSelected = value;

        // Selected highlight mạnh hơn hover
        if (_isSelected)
          MakeHighLight();
        else if (!IsMoveOver)
          ResetHighLight();
      }
    }

    // Store original strokes to be able to reset
    private Brush? _originalStroke;
    private double? _originalStrokeThickness;

    public ShapeBase(Canvas canvas)
    {
      Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
    }

    /// <summary>
    /// Highlight the shape (hover/selected).
    /// Derived classes can override for custom styling.
    /// </summary>
    public virtual void MakeHighLight()
    {
      if (shape is null) return;

      _originalStroke ??= shape.Stroke;
      _originalStrokeThickness ??= shape.StrokeThickness;

      // Nếu selected thì dày hơn hover
      var thickness = Math.Max(StrokeThickness, shape.StrokeThickness);
      shape.StrokeThickness = IsSelected ? thickness * 2.0 : thickness * 1.5;

      // Ưu tiên dùng Stroke hiện tại của object
      shape.Stroke = Stroke;
      shape.SnapsToDevicePixels = true;
    }

    /// <summary>
    /// Reset highlight to original style.
    /// </summary>
    public virtual void ResetHighLight()
    {
      if (shape is null) return;

      if (_originalStroke is not null) shape.Stroke = _originalStroke;
      if (_originalStrokeThickness.HasValue) shape.StrokeThickness = _originalStrokeThickness.Value;
    }

    /// <summary>
    /// Check whether mouse is over the shape.
    /// Default uses geometry hit test. Derived classes can override.
    /// </summary>
    public virtual bool CheckMoveOver(Point currentPoint)
    {
      if (shape is null) return false;

      try
      {
        // RenderedGeometry is reliable for Rectangle/Ellipse/Path...
        var geo = shape.RenderedGeometry;
        if (geo is null) return false;

        // include stroke thickness -> use a widened geometry for better hit
        var pen = new Pen(Brushes.Transparent, Math.Max(1.0, shape.StrokeThickness + 4.0));
        var widened = geo.GetWidenedPathGeometry(pen);

        return widened.FillContains(currentPoint);
      }
      catch
      {
        // fallback: bounding box
        var left = Canvas.GetLeft(shape);
        var top = Canvas.GetTop(shape);

        if (double.IsNaN(left)) left = 0;
        if (double.IsNaN(top)) top = 0;

        var rect = new Rect(left, top, shape.RenderSize.Width, shape.RenderSize.Height);
        return rect.Contains(currentPoint);
      }
    }

    /// <summary>
    /// Helper for derived classes to apply common stroke settings.
    /// </summary>
    protected void ApplyStrokeStyle(Shape s)
    {
      if (s is null) throw new ArgumentNullException(nameof(s));

      s.Stroke = Stroke;
      s.StrokeThickness = StrokeThickness;
      s.SnapsToDevicePixels = true;
      RenderOptions.SetEdgeMode(s, EdgeMode.Aliased);
    }
  }
}
