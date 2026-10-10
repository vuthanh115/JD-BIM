using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace kaka.Structure.Structural.App.Models.Column
{
  public sealed class RoundColumnModel : ColumnModel
  {
    public Curve BottomCurve { get; private set; }
    public double Diameter { get; private set; }

    public List<int> DQuantities { get; } = new();
    public int DQuantity
    {
      get => DQuantities.Count > 0 ? DQuantities[0] : 0;
      set
      {
        DQuantities.Clear();
        if (value > 0) DQuantities.Add(value);
      }
    }

    public RebarBarType MainRebarType { get; set; }
    public RebarBarType MainStirrupType { get; set; }
    public RebarBarType AdditionalStirrupType { get; set; }

    public RoundColumnModel(FamilyInstance column)
      : base(column)
    {
    }

    public override void InitGeometry(GeneralSetting generalSetting)
    {
      BottomCurve = (Column.Location as LocationCurve)?.Curve;
      Diameter = GetColumnDiameter(Column);
    }

    public override void LoadSetting(ColumnSetting setting)
    {
      if (setting != null)
        DQuantity = setting.CxQuantity;
    }

    public override void SaveSetting()
    {
      // handled by base
    }

    private static double GetColumnDiameter(FamilyInstance column)
    {
      var opt = new Options();
      var geo = column.get_Geometry(opt);

      foreach (var obj in geo)
      {
        if (obj is not Solid solid) continue;

        foreach (Face face in solid.Faces)
        {
          if (face is CylindricalFace cyl)
          {
            // Radius là XYZ (vector), phải gọi get_Radius(0) / Radius(0) rồi lấy length
            var rVec = cyl.get_Radius(0);            // XYZ
            var r    = rVec.GetLength();             // double (bán kính)
            return r * 2.0;                          // đường kính
          }
        }
      }

      return 0.0;
    }


  }

}
