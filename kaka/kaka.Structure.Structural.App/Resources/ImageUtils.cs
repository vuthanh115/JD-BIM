using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Resources ;

internal static class ImageUtils
{
  private static Assembly AddinAssembly
    => typeof(App).Assembly; // 🔥 QUAN TRỌNG

  public static ImageSource LoadPngFromResource(string resourceName)
  {
    var asm = AddinAssembly;

    using var stream = asm.GetManifestResourceStream(resourceName);
    if (stream is null)
    {
      throw new InvalidOperationException(
        $"Resource not found:\n{resourceName}\n\n" +
        DumpResourceNames(asm, GuessTail(resourceName))
      );
    }

    using var ms = new MemoryStream();
    stream.CopyTo(ms);
    ms.Position = 0;

    var img = new BitmapImage();
    img.BeginInit();
    img.CacheOption = BitmapCacheOption.OnLoad;
    img.StreamSource = ms;
    img.EndInit();
    img.Freeze(); // bắt buộc với Revit

    return img;
  }

  // ---------------- DEBUG ----------------

  private static string DumpResourceNames(Assembly asm, string? contains = null)
  {
    IEnumerable<string> names =
      asm.GetManifestResourceNames()
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

    if (!string.IsNullOrWhiteSpace(contains))
      names = names.Where(n => n.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0);

    var sb = new StringBuilder();
    sb.AppendLine("Embedded resources:");
    foreach (var n in names)
      sb.AppendLine("  " + n);

    return sb.ToString();
  }

  private static string GuessTail(string name)
  {
    var parts = name.Split('.');
    return parts.Length >= 2 ? parts[^2] + "." + parts[^1] : name;
  }
}