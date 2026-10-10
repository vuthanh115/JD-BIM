#nullable enable
using System.Globalization ;
using System.Windows ;
using System.Windows.Media ;
using kaka.Structure.Core ;
using kaka.Structure.Structural.App.ViewModels.Column ;

namespace kaka.Structure.Structural.App.Views.Column ;

public sealed class ColumnLevelCanvas : FrameworkElement
{
  public static readonly DependencyProperty ModelProperty =
    DependencyProperty.Register(
      nameof(Model),
      typeof(ColumnPreviewModel),
      typeof(ColumnLevelCanvas),
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

    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
    if (m == null || m.Levels.Count == 0) return;

    var pad = 10.0;
    var barW = 10.0;
    var barX = w - pad - barW;

    var yTop = pad;
    var usableH = Math.Max(1.0, h - 2 * pad);

    // ====== 1) Build level marks from model (Z0=Z1=elevation)
    // Use Z0Mm as mark elevation, but make it robust (pick Min/Max if caller filled differently)
    var rawMarks = m.Levels
      .Select(x => (name: x.Name, z: x.Z0Mm))
      .ToList();

    // If model accidentally has duplicates, keep them (you may want distinct by z later).
    // Sort ASC for segmentation, DESC for label/tick drawing.
    var marksAsc  = rawMarks.OrderBy(x => x.z).ToList();
    var marksDesc = rawMarks.OrderByDescending(x => x.z).ToList();

    var minZ = marksAsc.Min(x => x.z);
    var maxZ = marksAsc.Max(x => x.z);
    var span = Math.Max(1e-6, maxZ - minZ);

    double ToY(double zMm)
    {
      var t = (maxZ - zMm) / span; // z=max => top, z=min => bottom
      return yTop + usableH * t;
    }

#if DEBUG
    DebugLogger.Log($"[ColumnLevelCanvas] ===== RULER =====");
    DebugLogger.Log($"[ColumnLevelCanvas] marks={marksAsc.Count} minZ={minZ:0.##} maxZ={maxZ:0.##} span={span:0.##} usableH={usableH:0.##}");
    DebugLogger.Log($"[ColumnLevelCanvas] columnZ0={m.ColumnZ0Mm:0.##} columnZ1={m.ColumnZ1Mm:0.##}");
    for (int i = 0; i < marksDesc.Count; i++)
      DebugLogger.Log($"[ColumnLevelCanvas] mark[{i}] '{marksDesc[i].name}' z={marksDesc[i].z:0.##} => y={ToY(marksDesc[i].z):0.##}");
#endif

    // ====== 2) Draw ruler background
    var barRect = new Rect(barX, yTop, barW, usableH);
    dc.DrawRectangle(
      new SolidColorBrush(Color.FromRgb(230, 245, 235)),
      new Pen(Brushes.DarkGreen, 2),
      barRect);

    // ====== 3) Column overlay (depends on previewed column range)
    var colMinZ = Math.Min(m.ColumnZ0Mm, m.ColumnZ1Mm);
    var colMaxZ = Math.Max(m.ColumnZ0Mm, m.ColumnZ1Mm);

    var yColTop = ToY(colMaxZ);
    var yColBot = ToY(colMinZ);
    if (yColBot < yColTop) (yColTop, yColBot) = (yColBot, yColTop);

    yColTop = Math.Max(barRect.Top, Math.Min(barRect.Bottom, yColTop));
    yColBot = Math.Max(barRect.Top, Math.Min(barRect.Bottom, yColBot));

    var colH = Math.Max(1.0, yColBot - yColTop);
    var colRect = new Rect(barX + 2, yColTop, barW - 4, colH);
    dc.DrawRectangle(Brushes.DarkGreen, null, colRect);

#if DEBUG
    DebugLogger.Log($"[ColumnLevelCanvas] colRect yTop={yColTop:0.##} yBot={yColBot:0.##} Hpx={colH:0.##}");
#endif

    // ====== 4) Build column segments by consecutive marks (ASC)
    // Segment label rule: show the "lower level name" (like Revit schedules: 01->02 => label 01)
    var segs = new List<(double yMid, string name)>();

    if (marksAsc.Count >= 2)
    {
      for (int i = 0; i < marksAsc.Count - 1; i++)
      {
        var z0 = marksAsc[i].z;
        var z1 = marksAsc[i + 1].z;

        // overlap with column range
        var s0 = Math.Max(colMinZ, z0);
        var s1 = Math.Min(colMaxZ, z1);
        if (s1 <= s0 + 1e-6) continue;

        var y0 = ToY(s0);
        var y1 = ToY(s1);
        var yMid = (y0 + y1) * 0.5;

        // clamp
        yMid = Math.Max(yTop, Math.Min(yTop + usableH, yMid));

        segs.Add((yMid, marksAsc[i].name));

#if DEBUG
        DebugLogger.Log($"[ColumnLevelCanvas] seg[{segs.Count-1}] '{marksAsc[i].name}' -> '{marksAsc[i+1].name}' | z0={s0:0.##} z1={s1:0.##} H={s1-s0:0.##} yMid={yMid:0.##}");
#endif
      }
    }

    // ====== 5) Draw ticks at each mark (DESC so top levels appear first)
    var tickPen = new Pen(new SolidColorBrush(Color.FromRgb(220, 220, 220)), 1);

    foreach (var mk in marksDesc)
    {
      var yLv = ToY(mk.z);
      dc.DrawLine(tickPen, new Point(2, yLv), new Point(barX - 4, yLv));
      dc.DrawLine(new Pen(Brushes.DarkGreen, 1.2),
        new Point(barX - 2, yLv),
        new Point(barX + barW + 2, yLv));
    }

    // ====== 6) Label placement (avoid overlap)
    // Here: label the segment centers (so you see TẦNG 1 / TẦNG 2 on each column piece)
    if (segs.Count == 0)
    {
      // fallback: label marks
      segs = marksDesc.Select(mk => (ToY(mk.z), mk.name)).ToList();
    }

    var tf = new Typeface("Segoe UI");
    var fontSize = 11.0;
    var minGap = 12.0;

    var sample = MeasureText("TẦNG 10", fontSize, tf, FontWeights.SemiBold);
    var labelH = sample.Height;

    // sort by y (top->bottom) for stable placement
    var desired = segs
      .OrderBy(x => x.yMid)
      .Select(x => (name: x.name, y: x.yMid))
      .ToList();

    var placed = new List<(string name, double y)>(desired.Count);

    double curY = yTop + labelH * 0.5;
    foreach (var d in desired)
    {
      var yy = Math.Max(d.y, curY);
      placed.Add((d.name, yy));
      curY = yy + (labelH + minGap);
    }

    double bottomLimit = yTop + usableH - labelH * 0.5;
    for (int i = placed.Count - 1; i >= 0; i--)
    {
      var yy = Math.Min(placed[i].y, bottomLimit);
      placed[i] = (placed[i].name, yy);
      bottomLimit = yy - (labelH + minGap);
    }

    for (int i = 0; i < placed.Count; i++)
    {
      var yy = Math.Max(yTop + labelH * 0.5, Math.Min(yTop + usableH - labelH * 0.5, placed[i].y));
      placed[i] = (placed[i].name, yy);
    }

    foreach (var p2 in placed)
      DrawText(dc, p2.name, x: 4, centerY: p2.y, size: fontSize, brush: Brushes.Gray, tf: tf, fw: FontWeights.SemiBold);
  }

  private static FormattedText MeasureText(string text, double size, Typeface tf, FontWeight fw)
  {
    return new FormattedText(
      text,
      CultureInfo.CurrentUICulture,
      FlowDirection.LeftToRight,
      new Typeface(tf.FontFamily, tf.Style, fw, tf.Stretch),
      size,
      Brushes.Black,
      1.0);
  }

  private static void DrawText(
    DrawingContext dc,
    string text,
    double x,
    double centerY,
    double size,
    Brush brush,
    Typeface tf,
    FontWeight fw)
  {
    var ft = new FormattedText(
      text,
      CultureInfo.CurrentUICulture,
      FlowDirection.LeftToRight,
      new Typeface(tf.FontFamily, tf.Style, fw, tf.Stretch),
      size,
      brush,
      1.0);

    dc.DrawText(ft, new Point(x, centerY - ft.Height * 0.5));
  }
}