#nullable enable
using System;
using System.Collections.Generic;

namespace kaka.Structure.Structural.App.Models.PrimaveraP6;

public sealed class Bim4DTaskItem
{
  public int Id { get; set; }
  public string TaskCode { get; set; } = string.Empty;
  public string TaskName { get; set; } = string.Empty;
  public string WbsCode { get; set; } = string.Empty;
  public string Discipline { get; set; } = "KẾT CẤU";
  public string ZoneLevel { get; set; } = "Toàn dự án";
  public string SearchSetName { get; set; } = string.Empty;
  
  public double Quantity { get; set; }
  public string Unit { get; set; } = "m3";
  public double ProductivityRate { get; set; }
  public int DurationDays { get; set; }
  
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  
  public List<string> PredecessorCodes { get; set; } = new();
  public string PredecessorsDisplay => string.Join(", ", PredecessorCodes);
  
  public string TaskType { get; set; } = "Construct";
  public string PrimaryResource { get; set; } = string.Empty;
  public string CostAccount { get; set; } = string.Empty;
  public bool IsCritical { get; set; } = false;
  public int TotalFloatDays { get; set; } = 0;
  public int ElementCount { get; set; } = 0;
}
