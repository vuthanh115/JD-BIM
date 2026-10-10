namespace kaka.Structure.Structural.App.Models.Column
{
  /// <summary>
  /// Lưu trạng thái zoom/pan cho control (ZoomBorder / Canvas viewer).
  /// ZoomX/ZoomY: hệ số scale theo trục X/Y
  /// OffsetX/OffsetY: translate (pan) theo trục X/Y
  /// </summary>
  public sealed class ZoomState
  {
    public double ZoomX { get; set; } = 1.0;
    public double ZoomY { get; set; } = 1.0;

    public double OffsetX { get; set; } = 0.0;
    public double OffsetY { get; set; } = 0.0;

    public ZoomState() { }

    public ZoomState(double zoomX, double zoomY, double offsetX, double offsetY)
    {
      ZoomX = zoomX;
      ZoomY = zoomY;
      OffsetX = offsetX;
      OffsetY = offsetY;
    }

    public ZoomState Clone()
      => new ZoomState(ZoomX, ZoomY, OffsetX, OffsetY);

    public override string ToString()
      => $"Zoom=({ZoomX:0.###},{ZoomY:0.###}), Offset=({OffsetX:0.###},{OffsetY:0.###})";
  }
}