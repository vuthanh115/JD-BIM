#nullable enable

namespace kaka.Structure.Structural.App.Models.PrimaveraP6;

public sealed class P6WbsSummaryItem
{
  public string WbsCode { get; set; } = string.Empty;
  public string ComponentCode { get; set; } = string.Empty;
  public string ComponentName { get; set; } = string.Empty;
  public int ElementCount { get; set; }
  public int TaskCount { get; set; }
  public string PrimaryResource { get; set; } = string.Empty;
  public string CostAccount { get; set; } = string.Empty;
  public int DurationDays { get; set; }
}
