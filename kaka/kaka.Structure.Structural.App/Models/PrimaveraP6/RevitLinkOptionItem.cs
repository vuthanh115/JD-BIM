#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace kaka.Structure.Structural.App.Models.PrimaveraP6;

public sealed partial class RevitLinkOptionItem : ObservableObject
{
  [ObservableProperty]
  private bool _isSelected = true;

  public string DocumentTitle { get; set; } = string.Empty;

  public string FilePath { get; set; } = string.Empty;

  public int LinkInstanceId { get; set; }

  public string DisplayName => string.IsNullOrWhiteSpace(DocumentTitle) ? "Unnamed Link" : DocumentTitle;
}
