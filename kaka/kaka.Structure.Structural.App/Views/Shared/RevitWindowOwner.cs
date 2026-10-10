#nullable enable
using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Views ;

internal static class RevitWindowOwner
{
  public static Window? GetRevitWindow(UIApplication uiapp)
  {
    // Revit exposes MainWindowHandle (IntPtr)
    var h = uiapp.MainWindowHandle;
    if (h == IntPtr.Zero) return null;

    // Create dummy Window as owner wrapper
    var w = new Window { ShowInTaskbar = false, WindowStyle = WindowStyle.None, Width = 0, Height = 0 };
    var helper = new WindowInteropHelper(w) { Owner = h };
    return w;
  }
}