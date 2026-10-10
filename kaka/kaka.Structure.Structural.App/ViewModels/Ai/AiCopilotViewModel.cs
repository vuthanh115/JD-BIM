#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kaka.Structure.Structural.App.Services.Ai;
using kaka.Structure.Structural.App.Services.Mcp;

namespace kaka.Structure.Structural.App.ViewModels.Ai;

public sealed class AiChatMessage
{
  public bool IsUser { get; set; }
  public string Sender => IsUser ? "Bạn" : "KAKA AI Copilot";
  public string Message { get; set; } = string.Empty;
  public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss");
  public string? ActionType { get; set; }
  public List<long> AffectedIds { get; set; } = new();
  public bool HasAffectedElements => AffectedIds.Count > 0;

  public System.Windows.Visibility UserMsgVisibility => IsUser ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
  public System.Windows.Visibility AiMsgVisibility => IsUser ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
  public System.Windows.Visibility AffectedVisibility => HasAffectedElements ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
}

public sealed partial class AiCopilotViewModel : ObservableObject
{
  private readonly UIApplication _uiApp;

  public string ModelTitle { get; }

  public ObservableCollection<AiChatMessage> Messages { get; } = new();

  [ObservableProperty]
  private string _inputPrompt = string.Empty;

  [ObservableProperty]
  private bool _isThinking;

  public AiCopilotViewModel(UIApplication uiApp)
  {
    _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));
    var doc = uiApp.ActiveUIDocument?.Document;
    ModelTitle = doc != null ? $"{doc.Title}.rvt" : "Chưa mở mô hình";

    // Lời chào mở đầu
    Messages.Add(new AiChatMessage
    {
      IsUser = false,
      Message = "👋 Xin chào! Tôi là **KAKA AI Copilot** hỗ trợ điều khiển Revit bằng ngôn ngữ tự nhiên.\n\n" +
                "Bạn có thể yêu cầu tôi:\n" +
                "• *'Chọn các tường Tole tầng 0'*\n" +
                "• *'Bóc tách khối lượng BoQ dự án'*\n" +
                "• *'Tìm các cột ở tầng +4.500'*\n" +
                "• *'Xem thông tin dự án và danh sách tầng'*",
      ActionType = "GREETING"
    });
  }

  [RelayCommand]
  private void SendMessage()
  {
    var prompt = InputPrompt?.Trim();
    if (string.IsNullOrWhiteSpace(prompt)) return;

    ProcessPrompt(prompt);
  }

  [RelayCommand]
  private void ExecuteQuickPrompt(string? prompt)
  {
    if (string.IsNullOrWhiteSpace(prompt)) return;
    ProcessPrompt(prompt);
  }

  public void ProcessPrompt(string prompt)
  {
    // Thêm tin nhắn của User
    Messages.Add(new AiChatMessage
    {
      IsUser = true,
      Message = prompt
    });

    InputPrompt = string.Empty;
    IsThinking = true;

    try
    {
      // Thực thi trên UI Thread của Revit
      var result = AiPromptEngine.Execute(_uiApp, prompt);

      Messages.Add(new AiChatMessage
      {
        IsUser = false,
        Message = result.Message,
        ActionType = result.ActionType,
        AffectedIds = result.AffectedElementIds
      });
    }
    catch (Exception ex)
    {
      Messages.Add(new AiChatMessage
      {
        IsUser = false,
        Message = $"⚠️ Có lỗi xảy ra khi thực thi: {ex.Message}"
      });
    }
    finally
    {
      IsThinking = false;
    }
  }

  [RelayCommand]
  private void SelectAffectedElements(AiChatMessage? msg)
  {
    if (msg == null || msg.AffectedIds.Count == 0) return;

    try
    {
      var uiDoc = _uiApp.ActiveUIDocument;
      if (uiDoc != null)
      {
        var ids = msg.AffectedIds.Select(id => new ElementId(id)).ToList();
        uiDoc.Selection.SetElementIds(ids);
        uiDoc.ShowElements(ids);
      }
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Lỗi chọn đối tượng: {ex.Message}", "KAKA AI", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
  }
}
