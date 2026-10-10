namespace kaka.Structure.Structural.App.Models.Column;

public sealed class ColumnRebarModel
{
  public bool IsRound { get; set; }
  public double WidthMm { get; set; }
  public double HeightMm { get; set; }

  public int Cx { get; set; } = 4;
  public int Cy { get; set; } = 4;

  public double CoverTopMm { get; set; } = 25;
  public double CoverOtherMm { get; set; } = 25;

  // rebar type ids/names bạn map ở VM
}