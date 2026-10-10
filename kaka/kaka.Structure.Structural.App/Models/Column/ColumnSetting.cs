#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using kaka.Structure.Structural.App.Utils.Attributes;
using kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension;

namespace kaka.Structure.Structural.App.Models.Column
{
  [Schema("DD561A3F-2965-45D9-8CD8-7064F2908D6A", "ColumnSetting")]
  public sealed class ColumnSetting : IRevitEntity, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetProperty<T>(ref T backing, T value, [CallerMemberName] string? name = null)
    {
      if (EqualityComparer<T>.Default.Equals(backing, value)) return false;
      backing = value;
      OnPropertyChanged(name);
      return true;
    }

    // ========= Main bars =========
    private int _cxQuantity;
    [Field]
    public int CxQuantity
    {
      get => _cxQuantity;
      set => SetProperty(ref _cxQuantity, value);
    }

    private int _cyQuantity;
    [Field]
    public int CyQuantity
    {
      get => _cyQuantity;
      set => SetProperty(ref _cyQuantity, value);
    }

    private ElementId? _mainRebarTypeId;
    [Field]
    public ElementId? MainRebarTypeId
    {
      get => _mainRebarTypeId;
      set => SetProperty(ref _mainRebarTypeId, value);
    }

    // ========= Stirrups =========
    private ElementId? _mainStirrupTypeId;
    [Field]
    public ElementId? MainStirrupTypeId
    {
      get => _mainStirrupTypeId;
      set => SetProperty(ref _mainStirrupTypeId, value);
    }

    private ElementId? _additionalStirrupTypeId;
    [Field]
    public ElementId? AdditionalStirrupTypeId
    {
      get => _additionalStirrupTypeId;
      set => SetProperty(ref _additionalStirrupTypeId, value);
    }

    [Field] public List<int> VerticalSingleTieIndexes { get; set; } = new();
    [Field] public List<int> HorizontalSingleTieIndexes { get; set; } = new();

    private int _dQuantity;
    [Field]
    public int DQuantity
    {
      get => _dQuantity;
      set => SetProperty(ref _dQuantity, value);
    }

    [Field] public List<ClosedTieSetting> VerticalClosedTieIndexes { get; set; } = new();
    [Field] public List<ClosedTieSetting> HorizontalClosedTieIndexes { get; set; } = new();

    private bool _closedTie45;
    [Field]
    public bool ClosedTie45
    {
      get => _closedTie45;
      set => SetProperty(ref _closedTie45, value);
    }

    // ========= Distribution =========
    private int _distributionTypeId;
    [Field]
    public int DistributionTypeId
    {
      get => _distributionTypeId;
      set
      {
        if (SetProperty(ref _distributionTypeId, value))
          OnPropertyChanged(nameof(DistributionType));
      }
    }

    // ========= Round ties =========
    [Field] public List<SingleTieSetting> RoundSingleTieIndexes { get; set; } = new();
    [Field] public List<ClosedTieSetting> RoundClosedTieIndexes { get; set; } = new();

    // ========= Runtime-only convenience =========
    private RebarBarType? _mainRebarType;
    public RebarBarType? MainRebarType
    {
      get => _mainRebarType;
      set => SetProperty(ref _mainRebarType, value);
    }

    private RebarBarType? _mainStirrupType;
    public RebarBarType? MainStirrupType
    {
      get => _mainStirrupType;
      set => SetProperty(ref _mainStirrupType, value);
    }

    private RebarBarType? _additionalStirrupType;
    public RebarBarType? AdditionalStirrupType
    {
      get => _additionalStirrupType;
      set => SetProperty(ref _additionalStirrupType, value);
    }

    public EnumDistributionType DistributionType
    {
      get => (EnumDistributionType)DistributionTypeId;
      set
      {
        if (DistributionTypeId != (int)value)
          DistributionTypeId = (int)value; // auto raises
      }
    }
  }
}
