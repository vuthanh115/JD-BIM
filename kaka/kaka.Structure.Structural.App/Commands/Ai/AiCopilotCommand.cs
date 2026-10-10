#nullable enable
using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.ViewModels.Ai;
using kaka.Structure.Structural.App.Views.Ai;

namespace kaka.Structure.Structural.App.Commands.Ai;

[Transaction(TransactionMode.Manual)]
public sealed class AiCopilotCommand : IExternalCommand
{
  private static AiCopilotView? _activeWindow;

  public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
  {
    try
    {
      ShowWindow(commandData.Application, null);
      return Result.Succeeded;
    }
    catch (Exception ex)
    {
      message = ex.Message;
      TaskDialog.Show("KAKA AI Copilot Error", ex.ToString());
      return Result.Failed;
    }
  }

  public static void OpenWithPrompt(UIApplication uiapp, string? prompt)
  {
    ShowWindow(uiapp, prompt);
  }

  private static void ShowWindow(UIApplication uiapp, string? prompt)
  {
    if (_activeWindow != null && _activeWindow.IsLoaded)
    {
      if (_activeWindow.WindowState == System.Windows.WindowState.Minimized)
      {
        _activeWindow.WindowState = System.Windows.WindowState.Normal;
      }
      _activeWindow.Activate();

      if (!string.IsNullOrWhiteSpace(prompt))
      {
        _activeWindow.ExecutePrompt(prompt);
      }
      return;
    }

    var vm = new AiCopilotViewModel(uiapp);
    _activeWindow = new AiCopilotView
    {
      DataContext = vm
    };

    var revitHandle = uiapp.MainWindowHandle;
    if (revitHandle != IntPtr.Zero)
    {
      new WindowInteropHelper(_activeWindow).Owner = revitHandle;
    }

    _activeWindow.Closed += (s, e) => _activeWindow = null;
    _activeWindow.Show();

    if (!string.IsNullOrWhiteSpace(prompt))
    {
      _activeWindow.ExecutePrompt(prompt);
    }
  }
}
