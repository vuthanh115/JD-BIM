#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

using CommunityToolkit.Mvvm.ComponentModel;

namespace kaka.Structure.Structural.App.Models.Column
{
  /// <summary>
  /// ColumnModel - bind UI + skeleton theo BimSpeed.
  /// </summary>
  public class ColumnModel : ObservableObject
  {
    // =========================================================
    // ✅ (B) SETTINGS + UI BINDING (để ComboBox/Radio có dữ liệu)
    // =========================================================

    private ColumnSetting? _setting;

    /// <summary>
    /// Setting (ExtensibleStorage) - giữ data của cột.
    /// </summary>
    public ColumnSetting? Setting
    {
      get => _setting;
      set => SetProperty(ref _setting, value);
    }

    private ColumnSetting EnsureSetting()
    {
      if (Setting == null) Setting = new ColumnSetting();
      return Setting;
    }

    private List<int>? _cxQuantities;
    public List<int> CxQuantities
    {
      get => _cxQuantities ??= new List<int>();
      set => SetProperty(ref _cxQuantities, value);
    }

    private List<int>? _cyQuantities;
    public List<int> CyQuantities
    {
      get => _cyQuantities ??= new List<int>();
      set => SetProperty(ref _cyQuantities, value);
    }

    private List<int>? _dQuantities;
    public List<int> DQuantities
    {
      get => _dQuantities ??= new List<int>();
      set => SetProperty(ref _dQuantities, value);
    }
    
    // 1. Thêm 2 thuộc tính lưu cao độ Level (để vẽ)
    private double _baseLevelElevation;
    public double BaseLevelElevation
    {
      get => _baseLevelElevation;
      set => SetProperty(ref _baseLevelElevation, value);
    }

    private double _topLevelElevation;
    public double TopLevelElevation
    {
      get => _topLevelElevation;
      set => SetProperty(ref _topLevelElevation, value);
    }
    
    
    

    public int CxQuantity
    {
      get => Setting?.CxQuantity ?? 0;
      set
      {
        var s = EnsureSetting();
        if (s.CxQuantity == value) return;
        s.CxQuantity = value;
        OnPropertyChanged();
      }
    }

    public int CyQuantity
    {
      get => Setting?.CyQuantity ?? 0;
      set
      {
        var s = EnsureSetting();
        if (s.CyQuantity == value) return;
        s.CyQuantity = value;
        OnPropertyChanged();
      }
    }

    public int DQuantity
    {
      get => Setting?.DQuantity ?? 0;
      set
      {
        var s = EnsureSetting();
        if (s.DQuantity == value) return;
        s.DQuantity = value;
        OnPropertyChanged();
      }
    }

    public RebarBarType? MainRebarType
    {
      get => Setting?.MainRebarType;
      set
      {
        var s = EnsureSetting();
        if (ReferenceEquals(s.MainRebarType, value)) return;
        s.MainRebarType = value;
        s.MainRebarTypeId = value?.Id;
        OnPropertyChanged();
      }
    }

    public RebarBarType? MainStirrupType
    {
      get => Setting?.MainStirrupType;
      set
      {
        var s = EnsureSetting();
        if (ReferenceEquals(s.MainStirrupType, value)) return;
        s.MainStirrupType = value;
        s.MainStirrupTypeId = value?.Id;
        OnPropertyChanged();
      }
    }

    public RebarBarType? AdditionalStirrupType
    {
      get => Setting?.AdditionalStirrupType;
      set
      {
        var s = EnsureSetting();
        if (ReferenceEquals(s.AdditionalStirrupType, value)) return;
        s.AdditionalStirrupType = value;
        s.AdditionalStirrupTypeId = value?.Id;
        OnPropertyChanged();
      }
    }

    public EnumDistributionType DistributionType
    {
      get => EnsureSetting().DistributionType;
      set
      {
        var s = EnsureSetting();
        if (s.DistributionType.Equals(value)) return;
        s.DistributionType = value; // set DistributionTypeId
        OnPropertyChanged();
      }
    }

    private bool _useClosedLoopStirrup;
    public bool UseClosedLoopStirrup
    {
      get => _useClosedLoopStirrup;
      set => SetProperty(ref _useClosedLoopStirrup, value);
    }

    private void EnsureDefaultQuantitiesIfEmpty()
    {
      // bạn chỉnh range theo UI thật nếu muốn
      if (CxQuantities.Count == 0) CxQuantities.AddRange(Enumerable.Range(2, 9)); // 2..10
      if (CyQuantities.Count == 0) CyQuantities.AddRange(Enumerable.Range(2, 9)); // 2..10
      if (DQuantities.Count == 0)  DQuantities.AddRange(Enumerable.Range(6, 15)); // 6..20 (ví dụ)

      if (CxQuantity <= 0) CxQuantity = CxQuantities.FirstOrDefault();
      if (CyQuantity <= 0) CyQuantity = CyQuantities.FirstOrDefault();
      if (DQuantity  <= 0) DQuantity  = DQuantities.FirstOrDefault();
    }

    // =========================
    // Basic geometry / Revit refs
    // =========================
    private Document? _doc;
    public Document? Doc
    {
      get => _doc;
      set => SetProperty(ref _doc, value);
    }

    private FamilyInstance? _column;
    public FamilyInstance? Column
    {
      get => _column;
      set => SetProperty(ref _column, value);
    }

    private ElementId? _id;
    public ElementId? Id
    {
      get => _id;
      set => SetProperty(ref _id, value);
    }

    private XYZ? _directionX;
    public XYZ? DirectionX
    {
      get => _directionX;
      set => SetProperty(ref _directionX, value);
    }

    private XYZ? _directionY;
    public XYZ? DirectionY
    {
      get => _directionY;
      set => SetProperty(ref _directionY, value);
    }

    private XYZ? _directionZ;
    public XYZ? DirectionZ
    {
      get => _directionZ;
      set => SetProperty(ref _directionZ, value);
    }

    private Solid? _instanceSolid;
    public Solid? InstanceSolid
    {
      get => _instanceSolid;
      set => SetProperty(ref _instanceSolid, value);
    }

    private Solid? _symbolSolid;
    public Solid? SymbolSolid
    {
      get => _symbolSolid;
      set => SetProperty(ref _symbolSolid, value);
    }

    private List<Face>? _symbolFaces;
    public List<Face>? SymbolFaces
    {
      get => _symbolFaces;
      set => SetProperty(ref _symbolFaces, value);
    }

    private Level? _baseLevel;
    public Level? BaseLevel
    {
      get => _baseLevel;
      set => SetProperty(ref _baseLevel, value);
    }

    private Level? _topLevel;
    public Level? TopLevel
    {
      get => _topLevel;
      set => SetProperty(ref _topLevel, value);
    }

    private XYZ? _location;
    public XYZ? Location
    {
      get => _location;
      set => SetProperty(ref _location, value);
    }

    private XYZ? _locationBottom;
    public XYZ? LocationBottom
    {
      get => _locationBottom;
      set => SetProperty(ref _locationBottom, value);
    }

    private BoundingBoxXYZ? _boundingBox;
    public BoundingBoxXYZ? BoundingBox
    {
      get => _boundingBox;
      set => SetProperty(ref _boundingBox, value);
    }

    private ShapeBase? _verticalSectionShape;
    public ShapeBase? VerticalSectionShape
    {
      get => _verticalSectionShape;
      set => SetProperty(ref _verticalSectionShape, value);
    }

    // =========================
    // Covers / height
    // =========================
    private double _coverTop;
    public double CoverTop
    {
      get => _coverTop;
      set => SetProperty(ref _coverTop, value);
    }

    private double _coverBottom;
    public double CoverBottom
    {
      get => _coverBottom;
      set => SetProperty(ref _coverBottom, value);
    }

    private double _coverOther;
    public double CoverOther
    {
      get => _coverOther;
      set => SetProperty(ref _coverOther, value);
    }

    private double _height;
    public double Height
    {
      get => _height;
      set => SetProperty(ref _height, value);
    }

    private bool _isHeightValidForRebar = true;
    public bool IsHeightValidForRebar
    {
      get => _isHeightValidForRebar;
      set => SetProperty(ref _isHeightValidForRebar, value);
    }

    // =========================
    // Diameters / lengths
    // =========================
    private double _mainBarDiameter;
    public double MainBarDiameter
    {
      get => _mainBarDiameter;
      set => SetProperty(ref _mainBarDiameter, value);
    }

    private double _mainStirrupDiameter;
    public double MainStirrupDiameter
    {
      get => _mainStirrupDiameter;
      set => SetProperty(ref _mainStirrupDiameter, value);
    }

    private double _additionalStirrupDiameter;
    public double AdditionalStirrupDiameter
    {
      get => _additionalStirrupDiameter;
      set => SetProperty(ref _additionalStirrupDiameter, value);
    }

    private double _lapLength;
    public double LapLength
    {
      get => _lapLength;
      set => SetProperty(ref _lapLength, value);
    }

    private double _lapLengthAtBottom;
    public double LapLengthAtBottom
    {
      get => _lapLengthAtBottom;
      set => SetProperty(ref _lapLengthAtBottom, value);
    }

    private double _lapLengthAtTop;
    public double LapLengthAtTop
    {
      get => _lapLengthAtTop;
      set => SetProperty(ref _lapLengthAtTop, value);
    }

    private double _distanceBetweenTwoLaps;
    public double DistanceBetweenTwoLaps
    {
      get => _distanceBetweenTwoLaps;
      set => SetProperty(ref _distanceBetweenTwoLaps, value);
    }

    private double _anchorageLength;
    public double AnchorageLength
    {
      get => _anchorageLength;
      set => SetProperty(ref _anchorageLength, value);
    }

    private double _crankLength;
    public double CrankLength
    {
      get => _crankLength;
      set => SetProperty(ref _crankLength, value);
    }

    // =========================
    // Collections / settings
    // =========================
    private List<(ElementId id, bool joined)>? _joinedElementIds;
    public List<(ElementId id, bool joined)>? JoinedElementIds
    {
      get => _joinedElementIds;
      set => SetProperty(ref _joinedElementIds, value);
    }

    private List<RebarModel>? _allRebars;
    public List<RebarModel>? AllRebars
    {
      get => _allRebars;
      set => SetProperty(ref _allRebars, value);
    }

    private List<RebarModel>? _allStirrups;
    public List<RebarModel>? AllStirrups
    {
      get => _allStirrups;
      set => SetProperty(ref _allStirrups, value);
    }

    private GeneralSetting? _generalSetting;
    public GeneralSetting? GeneralSetting
    {
      get => _generalSetting;
      set => SetProperty(ref _generalSetting, value);
    }

    private StirrupSetting? _stirrupSetting;
    public StirrupSetting? StirrupSetting
    {
      get => _stirrupSetting;
      set => SetProperty(ref _stirrupSetting, value);
    }

    private StarterBarSetting? _starterBarSetting;
    public StarterBarSetting? StarterBarSetting
    {
      get => _starterBarSetting;
      set => SetProperty(ref _starterBarSetting, value);
    }

    private List<FramingFloorModel>? _relatedFramingFloors;
    public List<FramingFloorModel>? RelatedFramingFloors
    {
      get => _relatedFramingFloors;
      set => SetProperty(ref _relatedFramingFloors, value);
    }

    private Face? _topFace;
    public Face? TopFace
    {
      get => _topFace;
      set => SetProperty(ref _topFace, value);
    }

    private Face? _bottomFace;
    public Face? BottomFace
    {
      get => _bottomFace;
      set => SetProperty(ref _bottomFace, value);
    }

    private double _topElevation;
    public double TopElevation
    {
      get => _topElevation;
      set => SetProperty(ref _topElevation, value);
    }

    private double _bottomElevation;
    public double BottomElevation
    {
      get => _bottomElevation;
      set => SetProperty(ref _bottomElevation, value);
    }

    private double _middleElevation;
    public double MiddleElevation
    {
      get => _middleElevation;
      set => SetProperty(ref _middleElevation, value);
    }

    // =========================
    // Text for ApplyOtherLevelView DataGrid
    // =========================
    private string? _baseLevelText;
    public string? BaseLevelText
    {
      get => _baseLevelText;
      set => SetProperty(ref _baseLevelText, value);
    }

    private string? _topLevelText;
    public string? TopLevelText
    {
      get => _topLevelText;
      set => SetProperty(ref _topLevelText, value);
    }

    private string? _sectionSizeText;
    public string? SectionSizeText
    {
      get => _sectionSizeText;
      set => SetProperty(ref _sectionSizeText, value);
    }

    // =========================
    // Navigation / selection
    // =========================
    private bool _isFirstColumn;
    public bool IsFirstColumn
    {
      get => _isFirstColumn;
      set => SetProperty(ref _isFirstColumn, value);
    }

    private bool _isLastColumn;
    public bool IsLastColumn
    {
      get => _isLastColumn;
      set => SetProperty(ref _isLastColumn, value);
    }

    private int _columnIndex;
    public int ColumnIndex
    {
      get => _columnIndex;
      set => SetProperty(ref _columnIndex, value);
    }

    private string? _mark;
    public string? Mark
    {
      get => _mark;
      set => SetProperty(ref _mark, value);
    }

    private Canvas? _canvasCrossSection;
    public Canvas? CanvasCrossSection
    {
      get => _canvasCrossSection;
      set => SetProperty(ref _canvasCrossSection, value);
    }

    private ColumnModel? _nextColumn;
    public ColumnModel? NextColumn
    {
      get => _nextColumn;
      set => SetProperty(ref _nextColumn, value);
    }

    private ColumnModel? _previousColumn;
    public ColumnModel? PreviousColumn
    {
      get => _previousColumn;
      set => SetProperty(ref _previousColumn, value);
    }

    // 2 checkbox trong ApplyOtherLevelView
    private bool _isCheckedMainRebar = true;
    public bool IsCheckedMainRebar
    {
      get => _isCheckedMainRebar;
      set => SetProperty(ref _isCheckedMainRebar, value);
    }

    public void SetIsCheckedMainRebar(bool value) => IsCheckedMainRebar = value;

    private bool _isCheckedStirrup = true;
    public bool IsCheckedStirrup
    {
      get => _isCheckedStirrup;
      set => SetProperty(ref _isCheckedStirrup, value);
    }

    public void SetIsCheckedStirrup(bool value) => IsCheckedStirrup = value;

    // DataGrid row IsEnabled
    private bool _isEnabled = true;
    public bool IsEnabled
    {
      get => _isEnabled;
      set => SetProperty(ref _isEnabled, value);
    }

    // =========================
    // Cross-section combo UI bindings (giữ, nhưng DistributionType wrapper ở trên sẽ dùng Setting)
    // =========================
    private string? _sectionReinforcementDescription;
    public string? SectionReinforcementDescription
    {
      get => _sectionReinforcementDescription;
      set => SetProperty(ref _sectionReinforcementDescription, value);
    }

    private double _sectionReinforcementArea;
    public double SectionReinforcementArea
    {
      get => _sectionReinforcementArea;
      set => SetProperty(ref _sectionReinforcementArea, value);
    }

    private double _sectionReinforcementRatio;
    public double SectionReinforcementRatio
    {
      get => _sectionReinforcementRatio;
      set => SetProperty(ref _sectionReinforcementRatio, value);
    }

    private double _framingFloorHeight;
    public double FramingFloorHeight
    {
      get => _framingFloorHeight;
      set => SetProperty(ref _framingFloorHeight, value);
    }

    private string? _framingFloorName;
    public string? FramingFloorName
    {
      get => _framingFloorName;
      set => SetProperty(ref _framingFloorName, value);
    }

    // =========================
    // Ctors / factory
    // =========================
    public ColumnModel(FamilyInstance? column)
    {
      Column = column;
      Id = column?.Id;
      Doc = column?.Document;

      Setting = new ColumnSetting();
      EnsureDefaultQuantitiesIfEmpty();
    }

    public static ColumnModel Create(FamilyInstance column) => new ColumnModel(column);

    // =========================
    // ✅ Read basic info from Revit for UI binding
    // =========================
    // 2. Cập nhật hàm InitFromRevit
    public void InitFromRevit()
    {
      if (Column == null || Doc == null) return;

      BoundingBox = Column.get_BoundingBox(null);
      Mark = Column.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? Column.Name;

      // --- LẤY LEVEL OBJECT ---
      BaseLevel = TryGetLevel(Column, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      TopLevel  = TryGetLevel(Column, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

      // --- LẤY TÊN LEVEL ---
      BaseLevelText = BaseLevel?.Name ?? "Base";
      TopLevelText  = TopLevel?.Name ?? "Top";

      // --- LẤY CAO ĐỘ LEVEL (Convert Feet -> mm) ---
      // Lưu ý: Level.Elevation trả về Feet
      if (BaseLevel != null)
        BaseLevelElevation = UnitUtils.ConvertFromInternalUnits(BaseLevel.Elevation, UnitTypeId.Millimeters);
    
      if (TopLevel != null)
        TopLevelElevation = UnitUtils.ConvertFromInternalUnits(TopLevel.Elevation, UnitTypeId.Millimeters);

      // --- CÁC THÔNG SỐ KHÁC (Giữ nguyên) ---
      Height = TryGetDouble(Column, BuiltInParameter.FAMILY_HEIGHT_PARAM)
               ?? TryGetDouble(Column, BuiltInParameter.INSTANCE_LENGTH_PARAM)
               ?? TryGetHeightFromBBox(Column)
               ?? 0.0;

      CoverTop    = TryGetDouble(Column, BuiltInParameter.CLEAR_COVER_TOP)     ?? CoverTop;
      CoverBottom = TryGetDouble(Column, BuiltInParameter.CLEAR_COVER_BOTTOM)  ?? CoverBottom;
      CoverOther  = TryGetDouble(Column, BuiltInParameter.CLEAR_COVER_OTHER)   ?? CoverOther;

      SectionSizeText = BuildSectionSizeText(Column);
      // ...
    }

    private static double? TryGetDouble(Element? e, BuiltInParameter bip)
    {
      if (e == null) return null;
      var p = e.get_Parameter(bip);
      if (p == null) return null;
      return p.StorageType == StorageType.Double ? p.AsDouble() : null;
    }

    private static Level? TryGetLevel(Element e, BuiltInParameter bip)
    {
      var p = e.get_Parameter(bip);
      if (p == null) return null;
      var id = p.AsElementId();
      return e.Document.GetElement(id) as Level;
    }

    private static double? TryGetHeightFromBBox(FamilyInstance fi)
    {
      var bb = fi.get_BoundingBox(null);
      if (bb == null) return null;
      return Math.Abs(bb.Max.Z - bb.Min.Z);
    }

    private static string BuildSectionSizeText(FamilyInstance fi)
    {
      // 1) Rectangular: Width + Height (type param / instance param)
      var b = TryGetDouble(fi.Symbol, BuiltInParameter.FAMILY_WIDTH_PARAM)
              ?? TryGetDouble(fi, BuiltInParameter.FAMILY_WIDTH_PARAM);

      // Revit thường dùng FAMILY_HEIGHT_PARAM cho chiều còn lại
      var h = TryGetDouble(fi.Symbol, BuiltInParameter.FAMILY_HEIGHT_PARAM)
              ?? TryGetDouble(fi, BuiltInParameter.FAMILY_HEIGHT_PARAM);

      // 2) Fallback theo tên param thường gặp (b/h)
      b ??= TryGetDoubleByName(fi, "b") ?? TryGetDoubleByName(fi, "B") ?? TryGetDoubleByName(fi, "Width");
      h ??= TryGetDoubleByName(fi, "h") ?? TryGetDoubleByName(fi, "H") ?? TryGetDoubleByName(fi, "Height");

      if (b.HasValue && h.HasValue && b.Value > 1e-9 && h.Value > 1e-9)
      {
        var bMm = UnitUtils.ConvertFromInternalUnits(b.Value, UnitTypeId.Millimeters);
        var hMm = UnitUtils.ConvertFromInternalUnits(h.Value, UnitTypeId.Millimeters);
        return $"{bMm:0}x{hMm:0}";
      }

      // 3) Round: Diameter theo tên param phổ biến
      var d = TryGetDoubleByName(fi, "Diameter")
              ?? TryGetDoubleByName(fi, "DIAMETER")
              ?? TryGetDoubleByName(fi, "Dia")
              ?? TryGetDoubleByName(fi, "D")
              ?? TryGetDoubleByName(fi, "Ø");

      if (d.HasValue && d.Value > 1e-9)
      {
        var dMm = UnitUtils.ConvertFromInternalUnits(d.Value, UnitTypeId.Millimeters);
        return $"D{dMm:0}";
      }

      // 4) Fallback: bbox symbol để suy ra kích thước
      try
      {
        var bb = fi.Symbol?.get_BoundingBox(null);
        if (bb != null)
        {
          var sx = Math.Abs(bb.Max.X - bb.Min.X);
          var sy = Math.Abs(bb.Max.Y - bb.Min.Y);

          if (sx > 1e-9 && sy > 1e-9)
          {
            var sxMm = UnitUtils.ConvertFromInternalUnits(sx, UnitTypeId.Millimeters);
            var syMm = UnitUtils.ConvertFromInternalUnits(sy, UnitTypeId.Millimeters);

            if (Math.Abs(sxMm - syMm) <= 1.0)
            {
              var dm = (sxMm + syMm) * 0.5;
              return $"D{dm:0}";
            }

            return $"{sxMm:0}x{syMm:0}";
          }
        }
      }
      catch
      {
        // ignore
      }

      return fi.Symbol?.Name ?? "N/A";
    }

    private static double? TryGetDoubleByName(FamilyInstance fi, string name)
    {
      var p = fi.LookupParameter(name) ?? fi.Symbol?.LookupParameter(name);
      if (p == null) return null;
      return p.StorageType == StorageType.Double ? p.AsDouble() : null;
    }

    // =========================
    // Skeleton methods (bạn nối logic sau)
    // =========================
    public virtual void InitGeometry(GeneralSetting generalSetting)
    {
      GeneralSetting = generalSetting;
    }

    public virtual void LoadSetting(ColumnSetting setting)
    {
      Setting = setting ?? new ColumnSetting();

      // nếu có Id -> resolve runtime types
      if (Doc != null)
      {
        Setting.MainRebarType = (Setting.MainRebarTypeId != null && Setting.MainRebarTypeId != ElementId.InvalidElementId)
          ? Doc.GetElement(Setting.MainRebarTypeId) as RebarBarType
          : null;

        Setting.MainStirrupType = (Setting.MainStirrupTypeId != null && Setting.MainStirrupTypeId != ElementId.InvalidElementId)
          ? Doc.GetElement(Setting.MainStirrupTypeId) as RebarBarType
          : null;

        Setting.AdditionalStirrupType = (Setting.AdditionalStirrupTypeId != null && Setting.AdditionalStirrupTypeId != ElementId.InvalidElementId)
          ? Doc.GetElement(Setting.AdditionalStirrupTypeId) as RebarBarType
          : null;
      }

      // notify wrappers
      OnPropertyChanged(nameof(CxQuantity));
      OnPropertyChanged(nameof(CyQuantity));
      OnPropertyChanged(nameof(DQuantity));
      OnPropertyChanged(nameof(MainRebarType));
      OnPropertyChanged(nameof(MainStirrupType));
      OnPropertyChanged(nameof(AdditionalStirrupType));
      OnPropertyChanged(nameof(DistributionType));

      EnsureDefaultQuantitiesIfEmpty();
    }

    public virtual void SaveSetting()
    {
      // Bạn sẽ ghi ExtensibleStorage ở nơi khác.
      // Ở đây chỉ đảm bảo các Id đã sync.
      var s = EnsureSetting();
      s.MainRebarTypeId = s.MainRebarType?.Id;
      s.MainStirrupTypeId = s.MainStirrupType?.Id;
      s.AdditionalStirrupTypeId = s.AdditionalStirrupType?.Id;
    }

    public virtual void CopyStirrupFrom(ColumnModel fromColumn)
    {
      if (fromColumn is null) return;
      StirrupSetting = fromColumn.StirrupSetting;
      MainStirrupDiameter = fromColumn.MainStirrupDiameter;
      AdditionalStirrupDiameter = fromColumn.AdditionalStirrupDiameter;

      // copy stirrup related setting
      if (fromColumn.Setting != null)
      {
        EnsureSetting().MainStirrupTypeId = fromColumn.Setting.MainStirrupTypeId;
        EnsureSetting().AdditionalStirrupTypeId = fromColumn.Setting.AdditionalStirrupTypeId;
        MainStirrupType = fromColumn.MainStirrupType;
        AdditionalStirrupType = fromColumn.AdditionalStirrupType;
      }
    }

    public virtual void CopyMainRebarFrom(ColumnModel fromColumn)
    {
      if (fromColumn is null) return;
      MainBarDiameter = fromColumn.MainBarDiameter;

      if (fromColumn.Setting != null)
      {
        EnsureSetting().MainRebarTypeId = fromColumn.Setting.MainRebarTypeId;
        MainRebarType = fromColumn.MainRebarType;
      }
    }

    public virtual void DeleteAllStirrups()
    {
      AllStirrups?.Clear();
    }

    public virtual void CreateRebar()
    {
      // TODO
    }

    public virtual void SetSolidRebarInView()
    {
      // TODO
    }

    public virtual string GetAdditionalStirrupInformation()
    {
      return string.Empty;
    }

    public void AddInformation()
    {
      // TODO
    }

    public bool CheckHeightValidForRebar()
    {
      IsHeightValidForRebar = true;
      return IsHeightValidForRebar;
    }

    public void UnjoinAll()
    {
      // TODO
    }

    public void RejoinAll()
    {
      // TODO
    }

    public virtual void InitVariables(
      List<ColumnModel> columnModels,
      StirrupSetting stirrupSetting,
      StarterBarSetting starterBarSetting,
      GeneralSetting generalSetting)
    {
      StirrupSetting = stirrupSetting;
      StarterBarSetting = starterBarSetting;
      GeneralSetting = generalSetting;

      // ✅ rất quan trọng: đọc info để UI bind được
      InitFromRevit();
      EnsureDefaultQuantitiesIfEmpty();
    }
  }
}
