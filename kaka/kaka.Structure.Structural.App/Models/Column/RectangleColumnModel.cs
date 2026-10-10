using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace kaka.Structure.Structural.App.Models.Column
{
  public sealed class RectangleColumnModel : ColumnModel
  {
    public List<RebarModel> LeftRebars  { get; private set; } = new();
    public List<RebarModel> RightRebars { get; private set; } = new();
    public List<RebarModel> FrontRebars { get; private set; } = new();
    public List<RebarModel> BackRebars  { get; private set; } = new();

    public Face FaceLeft  { get; private set; }
    public Face FaceRight { get; private set; }
    public Face FaceFront { get; private set; }
    public Face FaceBack  { get; private set; }

    public double LeftE  { get; private set; }
    public double RightE { get; private set; }
    public double FrontE { get; private set; }
    public double BackE  { get; private set; }

    public double LengthX { get; private set; }
    public double LengthY { get; private set; }

    public ObservableCollection<int> CxQuantities { get; private set; } = new();
    public ObservableCollection<int> CyQuantities { get; private set; } = new();

    public List<int> VerticalSingleTieIndexes   { get; private set; } = new();
    public List<int> HorizontalSingleTieIndexes { get; private set; } = new();

    public List<ClosedTieSetting> VerticalClosedTieIndexes   { get; private set; } = new();
    public List<ClosedTieSetting> HorizontalClosedTieIndexes { get; private set; } = new();

    public bool ClosedTie45 { get; set; }

    private int _cxQuantity;
    public int CxQuantity
    {
      get => _cxQuantity;
      set
      {
        _cxQuantity = value;
        // thường sẽ recompute layout thép cạnh X
        // TODO: gọi InitGeometry/UpdatePreview nếu bạn có
      }
    }

    private int _cyQuantity;
    public int CyQuantity
    {
      get => _cyQuantity;
      set
      {
        _cyQuantity = value;
        // TODO
      }
    }

    private RebarBarType _mainRebarType;
    public RebarBarType MainRebarType
    {
      get => _mainRebarType;
      set => _mainRebarType = value;
    }

    private RebarBarType _mainStirrupType;
    public RebarBarType MainStirrupType
    {
      get => _mainStirrupType;
      set => _mainStirrupType = value;
    }

    private RebarBarType _additionalStirrupType;
    public RebarBarType AdditionalStirrupType
    {
      get => _additionalStirrupType;
      set => _additionalStirrupType = value;
    }

    public RectangleColumnModel(FamilyInstance column) : base(column)
    {
      // TODO: lấy geometry / faces / dimensions từ Revit
      // Init default quantities để ComboBox có data
      ResetCxQuantities();
      ResetCyQuantities();
    }

    public void ResetCxQuantities()
    {
      CxQuantities.Clear();
      for (int i = 2; i <= 20; i++) CxQuantities.Add(i);
      if (CxQuantity <= 0) CxQuantity = 4;
    }

    public void ResetCyQuantities()
    {
      CyQuantities.Clear();
      for (int i = 2; i <= 20; i++) CyQuantities.Add(i);
      if (CyQuantity <= 0) CyQuantity = 4;
    }

    public ClosedTieSetting FindVerticalClosedTie(int firstIndex, int secondIndex)
      => VerticalClosedTieIndexes.Find(x => x.FirstIndex == firstIndex && x.SecondIndex == secondIndex);

    public ClosedTieSetting FindHorizontalClosedTie(int firstIndex, int secondIndex)
      => HorizontalClosedTieIndexes.Find(x => x.FirstIndex == firstIndex && x.SecondIndex == secondIndex);

    public override string GetAdditionalStirrupInformation()
    {
      // TODO: build string mô tả (spacing/qty/...)
      return string.Empty;
    }

    public override void InitGeometry(GeneralSetting generalSetting)
    {
      // TODO: tính LeftE/RightE/FrontE/BackE, LengthX/LengthY,
      // set Faces, build list điểm thép, v.v.
    }

    public override void LoadSetting(ColumnSetting setting)
    {
      // TODO: map setting -> properties
    }

    public override void SaveSetting()
    {
      // TODO: properties -> setting storage
    }

    public override void CopyMainRebarFrom(ColumnModel fromColumn)
    {
      // TODO
    }

    public override void CopyStirrupFrom(ColumnModel fromColumn)
    {
      // TODO
    }

    public override void DeleteAllStirrups()
    {
      // TODO: xóa rebar đai đã tạo trong Revit
    }

    public override void CreateRebar()
    {
      // TODO: tạo rebar chính + đai + đai phụ
    }

    public override void InitVariables(
      List<ColumnModel> columnModels,
      StirrupSetting stirrupSetting,
      StarterBarSetting starterBarSetting,
      GeneralSetting generalSetting)
    {
      // TODO: init shared context / rule / setting
    }
  }
}
