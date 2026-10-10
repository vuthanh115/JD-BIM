#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.ViewModels.Column;
using MahApps.Metro.Controls;
using kaka.Structure.Core; // <-- DebugLogger ở đây (theo ảnh bạn gửi)

namespace kaka.Structure.Structural.App.Views.Column
{
  public partial class ColumnRebarWindow : MetroWindow
  {
    private ColumnRebarViewModel? _vm;
    private bool _attached;

    // ---- debug tracking (avoid spam) ----
    private int _lastCrossChildren = -1;
    private int _lastVertChildren  = -1;
    private bool _didDispatcherDump;

    // ✅ Cách dùng phổ biến trong Revit ExternalCommand:
    // var win = new ColumnRebarWindow(uidoc);
    public ColumnRebarWindow(UIDocument uidoc)
    {
      InitializeComponent();

      _vm = new ColumnRebarViewModel(uidoc);
      DataContext = _vm;

      Loaded += OnWindowLoaded;
      Closed += OnWindowClosed;
    }

    // ✅ Nếu bạn muốn tự tạo VM bên ngoài rồi truyền vào:
    // var vm = new ColumnRebarViewModel(uidoc);
    // var win = new ColumnRebarWindow(vm);
    public ColumnRebarWindow(ColumnRebarViewModel vm)
    {
      InitializeComponent();

      _vm = vm ?? throw new ArgumentNullException(nameof(vm));
      DataContext = _vm;

      Loaded += OnWindowLoaded;
      Closed += OnWindowClosed;
    }

    // ✅ Constructor rỗng cho designer
    public ColumnRebarWindow()
    {
      InitializeComponent();

      Loaded += OnWindowLoaded;
      Closed += OnWindowClosed;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
      // Start fresh log per open
      DebugLogger.Clear();
      DebugLogger.Log("=== ColumnRebarWindow Loaded ===");
      DebugLogger.Log($"Window Actual={ActualWidth:0.##}x{ActualHeight:0.##}");
      DebugLogger.Log($"DataContext={(DataContext?.GetType().FullName ?? "null")}");

      // Nếu tạo rỗng rồi set DataContext sau thì lấy lại
      _vm ??= DataContext as ColumnRebarViewModel;
      if (_vm == null)
      {
        DebugLogger.Log("VM is NULL -> return");
        return;
      }

      DebugLogger.Log($"VM={_vm.GetType().FullName}");

      // Attach 1 lần
      if (_attached)
      {
        DebugLogger.Log("Already attached -> skip AttachView");
        return;
      }

      // Dump BEFORE attach (may still be 0x0)
      DumpState("BeforeAttach");

      _vm.AttachView(
        this,
        canvasVerticalSection,
        canvasCrossSection,
        zoomVerticalSection,
        zoomCrossSection);

      _attached = true;

      DebugLogger.Log("AttachView DONE");
      DumpState("AfterAttach");

      // ---- Watch size changes (to know when canvas becomes non-zero) ----
      HookSizeLogs(canvasCrossSection, "canvasCrossSection");
      HookSizeLogs(canvasVerticalSection, "canvasVerticalSection");
      HookSizeLogs(zoomCrossSection, "zoomCrossSection");
      HookSizeLogs(zoomVerticalSection, "zoomVerticalSection");

      // ---- Watch children count changes (Add/Clear) by polling on LayoutUpdated ----
      HookChildrenCount(canvasCrossSection, isCross: true);
      HookChildrenCount(canvasVerticalSection, isCross: false);

      // Dump once after layout pass (size stable)
      if (!_didDispatcherDump)
      {
        _didDispatcherDump = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
          DebugLogger.Log("---- Dispatcher Dump (post layout) ----");
          DumpState("AfterDispatcher");
        }));
      }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
      DebugLogger.Log($"=== ColumnRebarWindow Closed (attached={_attached}) ===");

      if (_vm != null && _attached)
      {
        try
        {
          _vm.DetachView();
          DebugLogger.Log("DetachView DONE");
        }
        catch (Exception ex)
        {
          DebugLogger.Log($"DetachView EX: {ex}");
        }
        finally
        {
          _attached = false;
        }
      }
    }

    // ------------------- helpers -------------------

    private void DumpState(string tag)
    {
      DebugLogger.Log($"[{tag}] Window Actual={ActualWidth:0.##}x{ActualHeight:0.##}");

      DumpElement(tag, canvasCrossSection, "canvasCrossSection");
      DumpElement(tag, canvasVerticalSection, "canvasVerticalSection");
      DumpElement(tag, zoomCrossSection, "zoomCrossSection");
      DumpElement(tag, zoomVerticalSection, "zoomVerticalSection");

      DebugLogger.Log($"[{tag}] canvasCrossSection.Children={canvasCrossSection?.Children.Count ?? -1}");
      DebugLogger.Log($"[{tag}] canvasVerticalSection.Children={canvasVerticalSection?.Children.Count ?? -1}");
    }

    private void DumpElement(string tag, FrameworkElement? fe, string name)
    {
      if (fe == null)
      {
        DebugLogger.Log($"[{tag}] {name}=null");
        return;
      }

      DebugLogger.Log(
        $"[{tag}] {name}: " +
        $"Actual={fe.ActualWidth:0.##}x{fe.ActualHeight:0.##}, " +
        $"RenderSize={fe.RenderSize.Width:0.##}x{fe.RenderSize.Height:0.##}, " +
        $"Visibility={fe.Visibility}, " +
        $"IsLoaded={fe.IsLoaded}");
    }

    private void HookSizeLogs(FrameworkElement fe, string name)
    {
      fe.Loaded += (_, __) =>
        DebugLogger.Log($"[{name}] Loaded Actual={fe.ActualWidth:0.##}x{fe.ActualHeight:0.##}");

      fe.SizeChanged += (_, __) =>
        DebugLogger.Log($"[{name}] SizeChanged Actual={fe.ActualWidth:0.##}x{fe.ActualHeight:0.##}");
    }

    private void HookChildrenCount(Canvas canvas, bool isCross)
    {
      canvas.LayoutUpdated += (_, __) =>
      {
        var count = canvas.Children.Count;

        if (isCross)
        {
          if (count != _lastCrossChildren)
          {
            _lastCrossChildren = count;
            DebugLogger.Log($"[canvasCrossSection] ChildrenCount={count} (Actual={canvas.ActualWidth:0.##}x{canvas.ActualHeight:0.##})");
            LogLastChild(canvas, "canvasCrossSection");
          }
        }
        else
        {
          if (count != _lastVertChildren)
          {
            _lastVertChildren = count;
            DebugLogger.Log($"[canvasVerticalSection] ChildrenCount={count} (Actual={canvas.ActualWidth:0.##}x{canvas.ActualHeight:0.##})");
            LogLastChild(canvas, "canvasVerticalSection");
          }
        }
      };
    }

    private void LogLastChild(Canvas canvas, string name)
    {
      try
      {
        if (canvas.Children.Count <= 0) return;
        var last = canvas.Children[canvas.Children.Count - 1];
        DebugLogger.Log($"[{name}] LastChild={last.GetType().FullName}");
      }
      catch (Exception ex)
      {
        DebugLogger.Log($"[{name}] LastChild EX: {ex.Message}");
      }
    }
  }
}
