#nullable enable
using Autodesk.Revit.DB.Structure;

namespace kaka.Structure.Structural.App.Commands.Column ;

public sealed class ColumnRebarOptions
{
  public RebarBarType MainBarType { get; }
  public RebarBarType TieBarType  { get; }

  public int MainBarCountX { get; }
  public int MainBarCountY { get; }

  public double TieSpacing { get; }     // ft
  public double Cover { get; }          // ft
  public double TieEndOffset { get; }   // ft  <-- NEW

  private ColumnRebarOptions(
    RebarBarType mainBarType,
    RebarBarType tieBarType,
    int mainBarCountX,
    int mainBarCountY,
    double tieSpacing,
    double cover,
    double tieEndOffset)
  {
    MainBarType   = mainBarType;
    TieBarType    = tieBarType;
    MainBarCountX = mainBarCountX;
    MainBarCountY = mainBarCountY;
    TieSpacing    = tieSpacing;
    Cover         = cover;
    TieEndOffset  = tieEndOffset;
  }

  public static ColumnRebarOptions Create(
    RebarBarType mainBarType,
    RebarBarType tieBarType,
    int mainBarCountX,
    int mainBarCountY,
    double tieSpacing,
    double cover,
    double tieEndOffset)
  {
    return new ColumnRebarOptions(
      mainBarType, tieBarType, mainBarCountX, mainBarCountY, tieSpacing, cover, tieEndOffset
    );
  }

  public bool IsValid(out string reason)
  {
    if (MainBarCountX < 2) { reason = "MainBarCountX must be >= 2"; return false; }
    if (MainBarCountY < 2) { reason = "MainBarCountY must be >= 2"; return false; }
    if (TieSpacing <= 0)   { reason = "TieSpacing must be > 0"; return false; }
    if (Cover < 0)         { reason = "Cover must be >= 0"; return false; }
    if (TieEndOffset < 0)  { reason = "TieEndOffset must be >= 0"; return false; }

    reason = string.Empty;
    return true;
  }
}