using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.Resources;
using kaka.Structure.Structural.App.Ribbon;

namespace kaka.Structure.Structural.App.Ribbon ;

internal static class RibbonBuilder
{
  public static void Build(UIControlledApplication application)
  {
    var asm      = Assembly.GetExecutingAssembly();
    var baseDir  = Path.GetDirectoryName(asm.Location) ?? "";
    var jsonPath = Path.Combine(baseDir, "ribbon.json");

    var cfg = RibbonConfig.Load(jsonPath);
    cfg.ValidateOrThrow();

    try { application.CreateRibbonTab(cfg.TabName); } catch { }

    // Column | Beam | ... mỗi split = 1 cột + separator
    for (int i = 0; i < cfg.Splits.Count; i++)
    {
      var s = cfg.Splits[i];
      var panelName = string.IsNullOrWhiteSpace(s.PanelName) ? cfg.PanelName : s.PanelName;
      var panel = GetOrCreatePanel(application, cfg.TabName, panelName);

      var data = new SplitButtonData($"KAKA_Split_{s.Id}", s.Text);
      var split = panel.AddItem(data) as SplitButton
                  ?? throw new InvalidOperationException(
                    $"Failed to create SplitButton '{s.Id}'");

      PopulateSplit(split, asm.Location, s);

      if (i < cfg.Splits.Count - 1)
      {
        var next = cfg.Splits[i + 1];
        var nextPanelName = string.IsNullOrWhiteSpace(next.PanelName) ? cfg.PanelName : next.PanelName;
        if (string.Equals(panelName, nextPanelName, StringComparison.OrdinalIgnoreCase))
          panel.AddSeparator();
      }
    }

    // Thêm AI Prompt Bar và AI Copilot button
    AddAiPromptBar(application, cfg.TabName, asm.Location);
  }

  private static void AddAiPromptBar(
    UIControlledApplication app,
    string tabName,
    string asmPath)
  {
    var panel = GetOrCreatePanel(app, tabName, "AI Copilot");

    // 1. Nút mở cửa sổ AI Copilot
    var btnData = new PushButtonData(
      "KAKA_Btn_AICopilot",
      "AI Copilot",
      asmPath,
      "kaka.Structure.Structural.App.Commands.Ai.AiCopilotCommand");

    if (panel.AddItem(btnData) is PushButton btn)
    {
      btn.ToolTip = "Mở cửa sổ trò chuyện và điều khiển mô hình Revit bằng KAKA AI Copilot";
      TrySetIcons(btn,
        "kaka.Structure.Structural.App.Resources.Icons.Ai.ai_copilot16.png",
        "kaka.Structure.Structural.App.Resources.Icons.Ai.ai_copilot.png");
    }

    // 2. Ô nhập Prompt nhanh trên Ribbon
    var promptBoxData = new TextBoxData("KAKA_Ribbon_AIPromptBox");
    if (panel.AddItem(promptBoxData) is Autodesk.Revit.UI.TextBox promptBox)
    {
      promptBox.PromptText = "💬 Nhập prompt AI (Enter)...";
      promptBox.ToolTip = "Gõ yêu cầu AI (vd: chọn cột tầng 2, bóc boq) rồi nhấn Enter để thực thi";
      promptBox.Width = 220;
      promptBox.EnterPressed += OnPromptBoxEnterPressed;
    }
  }

  private static void OnPromptBoxEnterPressed(object? sender, Autodesk.Revit.UI.Events.TextBoxEnterPressedEventArgs e)
  {
    if (sender is Autodesk.Revit.UI.TextBox box)
    {
      var text = box.Value?.ToString()?.Trim() ?? "";
      if (!string.IsNullOrWhiteSpace(text))
      {
        Commands.Ai.AiCopilotCommand.OpenWithPrompt(e.Application, text);
        box.Value = "";
      }
    }
  }

  private static RibbonPanel GetOrCreatePanel(
    UIControlledApplication app,
    string tabName,
    string panelName)
  {
    if (string.IsNullOrWhiteSpace(panelName))
      panelName = "\u2800";

    var panels = app.GetRibbonPanels(tabName);
    return panels.FirstOrDefault(p =>
             string.Equals(p.Name, panelName, StringComparison.OrdinalIgnoreCase))
           ?? app.CreateRibbonPanel(tabName, panelName);
  }

  private static void PopulateSplit(
    SplitButton split,
    string asmPath,
    RibbonSplit s)
  {
    PushButton? firstBtn   = null;
    PushButton? defaultBtn = null;

    foreach (var it in s.Items)
    {
      var btnData = new PushButtonData(
        $"KAKA_{s.Id}_{it.Id}",
        it.Text,
        asmPath,
        it.CommandClass);

      var btn = split.AddPushButton(btnData);
      btn.ToolTip = string.IsNullOrWhiteSpace(it.Tooltip)
        ? it.Text
        : it.Tooltip;

      TrySetIcons(btn, it.Icon16, it.Icon32);

      firstBtn ??= btn;

      if (!string.IsNullOrWhiteSpace(s.DefaultItemId) &&
          string.Equals(it.Id, s.DefaultItemId,
            StringComparison.OrdinalIgnoreCase))
      {
        defaultBtn = btn;
      }
    }

    defaultBtn ??= firstBtn;

    split.IsSynchronizedWithCurrentItem = true;
    if (defaultBtn != null)
      split.CurrentButton = defaultBtn;
  }

  private static void TrySetIcons(
    PushButton btn,
    string? icon16,
    string? icon32)
  {
    try
    {
      if (!string.IsNullOrWhiteSpace(icon16))
        btn.Image = ImageUtils.LoadPngFromResource(icon16);

      if (!string.IsNullOrWhiteSpace(icon32))
        btn.LargeImage = ImageUtils.LoadPngFromResource(icon32);
    }
    catch
    {
      // ignore icon errors
    }
  }
}