#nullable enable
using System;
using System.Windows;
using System.Windows.Media;
using kaka.Structure.Structural.App.ViewModels.Column;

namespace kaka.Structure.Structural.App.Views.Column;

public sealed class ColumnPreviewCanvas : FrameworkElement
{
  public static readonly DependencyProperty ModelProperty =
    DependencyProperty.Register(
      nameof(Model),
      typeof(ColumnPreviewModel),
      typeof(ColumnPreviewCanvas),
      new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

  public ColumnPreviewModel? Model
  {
    get => (ColumnPreviewModel?)GetValue(ModelProperty);
    set => SetValue(ModelProperty, value);
  }

  protected override void OnRender(DrawingContext dc)
  {
    base.OnRender(dc);

    var m = Model;
    var w = ActualWidth;
    var h = ActualHeight;

    if (w <= 2 || h <= 2) return;

    // Background
    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
    if (m == null) return;

    // ===== scale mm -> px (fit)
    const double pad = 16.0;
    var availW = Math.Max(1, w - 2 * pad);
    var availH = Math.Max(1, h - 2 * pad);

    var mmW = Math.Max(1.0, m.WidthMm);
    var mmH = Math.Max(1.0, m.HeightMm);

    // px per mm
    var scale = Math.Min(availW / mmW, availH / mmH);
    var pxPerMm = scale; // ✅ FIX: define pxPerMm

    var pxW = mmW * scale;
    var pxH = mmH * scale;

    var x0 = (w - pxW) / 2.0;
    var y0 = (h - pxH) / 2.0;

    var outer = new Rect(x0, y0, pxW, pxH);

    // ===== 1) Column frame (dark green)
    var framePen = new Pen(Brushes.DarkGreen, 4.0) { LineJoin = PenLineJoin.Miter };
    dc.DrawRectangle(null, framePen, outer);

    // ===== 2) Stirrup (light cyan) — position = cover + stirrupDia/2
    var coverMm = Math.Max(0.0, m.CoverMm);
    var stirrupDiaMm = Math.Max(0.0, m.StirrupDiameterMm);

    // Centerline inset for stirrup
    var stirrupInsetMm = coverMm + (stirrupDiaMm * 0.5);

    // Add a small visual padding so it doesn't stick to frame when dia=0
    stirrupInsetMm = Math.Max(stirrupInsetMm, 6.0);

    var stirrupInsetPx = stirrupInsetMm * pxPerMm;
    var stirrup = InflateSafe(outer, -stirrupInsetPx);

    var cornerR = Math.Max(10, Math.Min(stirrup.Width, stirrup.Height) * 0.08);

    var stirrupPen = new Pen(
      new SolidColorBrush(Color.FromRgb(64, 180, 180)), // xanh nhạt
      3.2)
    {
      LineJoin = PenLineJoin.Round,
      StartLineCap = PenLineCap.Round,
      EndLineCap = PenLineCap.Round
    };

    dc.DrawGeometry(null, stirrupPen, BuildRoundedRectGeometry(stirrup, cornerR));

    // ===== 3) Bars (red corners, gray others)
    // IMPORTANT: barRect should be derived from OUTER so bars never collide with stirrup
    var mainDiaMm = Math.Max(1.0, m.MainBarDiameterMm);

    var barGapMm = 2.0; // visual clearance
    // bar center offset from outer boundary:
    // cover + stirrupDia + mainDia/2 + gap
    var barInsetFromOuterMm = coverMm + stirrupDiaMm + (mainDiaMm * 0.5) + barGapMm;
    var barInsetFromOuterPx = barInsetFromOuterMm * pxPerMm;

    var barRect = InflateSafe(outer, -barInsetFromOuterPx);

    // bar radius in px
    var r = Math.Max(3.0, (mainDiaMm * 0.5) * pxPerMm);

    foreach (var p in m.BarsUV)
    {
      var px = barRect.X + barRect.Width * p.X;
      var py = barRect.Y + barRect.Height * (1.0 - p.Y);

      var isCorner =
        (p.X <= 0.12 || p.X >= 0.88) &&
        (p.Y <= 0.12 || p.Y >= 0.88);

      var fill = isCorner ? Brushes.Firebrick : Brushes.LightGray;
      dc.DrawEllipse(fill, new Pen(Brushes.DimGray, 1.2), new Point(px, py), r, r);
    }
  }

  private static Rect InflateSafe(Rect r, double delta)
  {
    var x = r.X - delta;
    var y = r.Y - delta;
    var w = r.Width + 2 * delta;
    var h = r.Height + 2 * delta;

    if (w < 1) w = 1;
    if (h < 1) h = 1;

    return new Rect(x, y, w, h);
  }

  private static Geometry BuildRoundedRectGeometry(Rect r, double radius)
  {
    radius = Math.Max(0, Math.Min(radius, Math.Min(r.Width, r.Height) / 2.0));

    var g = new StreamGeometry();
    using (var ctx = g.Open())
    {
      var p0 = new Point(r.Left + radius, r.Top);
      var p1 = new Point(r.Right - radius, r.Top);
      var p2 = new Point(r.Right, r.Top + radius);
      var p3 = new Point(r.Right, r.Bottom - radius);
      var p4 = new Point(r.Right - radius, r.Bottom);
      var p5 = new Point(r.Left + radius, r.Bottom);
      var p6 = new Point(r.Left, r.Bottom - radius);
      var p7 = new Point(r.Left, r.Top + radius);

      ctx.BeginFigure(p0, isFilled: false, isClosed: true);

      ctx.LineTo(p1, true, false);
      ctx.ArcTo(p2, new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);

      ctx.LineTo(p3, true, false);
      ctx.ArcTo(p4, new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);

      ctx.LineTo(p5, true, false);
      ctx.ArcTo(p6, new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);

      ctx.LineTo(p7, true, false);
      ctx.ArcTo(p0, new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
    }
    g.Freeze();
    return g;
  }
  
}
