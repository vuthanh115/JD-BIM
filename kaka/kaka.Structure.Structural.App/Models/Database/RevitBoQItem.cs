#nullable enable
using System;

namespace kaka.Structure.Structural.App.Models.Database;

public class RevitBoQItem
{
  public long ElementId { get; set; }
  public string UniqueId { get; set; } = "";
  public string CategoryCode { get; set; } = "";
  public string CategoryName { get; set; } = "";
  public string FamilyName { get; set; } = "";
  public string TypeName { get; set; } = "";
  public string LevelName { get; set; } = "";
  public double VolumeM3 { get; set; }
  public double AreaM2 { get; set; }
  public double LengthM { get; set; }
  public string MaterialName { get; set; } = "";
  public string Comments { get; set; } = "";
  public string Mark { get; set; } = "";
  public string BsvPhanLoai { get; set; } = "";
  public string BsvComments { get; set; } = "";
  public string Partition { get; set; } = "";
  public double RebarDiameterMm { get; set; }
  public double RebarLengthM { get; set; }
  public double RebarWeightKg { get; set; }
}

public class BoQCategorySummary
{
  public string CategoryCode { get; set; } = "";
  public string CategoryName { get; set; } = "";
  public int Count { get; set; }
  public double TotalVolumeM3 { get; set; }
  public double TotalAreaM2 { get; set; }
  public double TotalWeightKg { get; set; }
}
