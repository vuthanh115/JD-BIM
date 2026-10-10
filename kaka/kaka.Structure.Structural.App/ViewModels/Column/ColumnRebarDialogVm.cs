#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

using kaka.Structure.Structural.App.Commands.Column;

namespace kaka.Structure.Structural.App.ViewModels.Column;

public sealed class ColumnRebarDialogVm : INotifyPropertyChanged
{
  private readonly Document _doc;

  // ✅ Allow View to close window
  public Action? RequestClose { get; set; }

  // ✅ MULTI HOSTS
  public IReadOnlyList<FamilyInstance> Hosts { get; }
  public IReadOnlyList<string> HostDisplayItems { get; }

  private int _selectedHostIndex;
  public int SelectedHostIndex
  {
    get => _selectedHostIndex;
    set
    {
      var clamped = ClampIndex(value, Hosts.Count);
      if (Set(ref _selectedHostIndex, clamped))
      {
        Raise(nameof(SelectedHost));
        Raise(nameof(SelectedHostInfo));
        RefreshPreview();
      }
    }
  }

  // Safe: may be null if Hosts is empty
  public FamilyInstance? SelectedHost
  {
    get
    {
      if (Hosts.Count == 0) return null;
      var idx = ClampIndex(_selectedHostIndex, Hosts.Count);
      return Hosts[idx];
    }
  }

  // ===== OPTIONS =====
  public IReadOnlyList<int> CxOptions { get; } = new[] { 2, 3, 4, 5, 6, 8, 10 };
  public IReadOnlyList<int> CyOptions { get; } = new[] { 2, 3, 4, 5, 6, 8, 10 };
  public IReadOnlyList<int> TieSpacingOptions { get; } = new[] { 100, 120, 150, 200 };

  public IReadOnlyList<RebarBarType> MainBarTypes { get; }
  public IReadOnlyList<RebarBarType> TieBarTypes  { get; }

  private int _cx = 2;
  public int Cx
  {
    get => _cx;
    set
    {
      if (Set(ref _cx, Math.Max(2, value)))
      {
        RefreshPreview();
        Raise(nameof(IsValid));
      }
    }
  }

  private int _cy = 2;
  public int Cy
  {
    get => _cy;
    set
    {
      if (Set(ref _cy, Math.Max(2, value)))
      {
        RefreshPreview();
        Raise(nameof(IsValid));
      }
    }
  }

  private int _tieSpacingMm = 120;
  public int TieSpacingMm
  {
    get => _tieSpacingMm;
    set
    {
      if (Set(ref _tieSpacingMm, Math.Max(1, value)))
      {
        // Tie spacing không ảnh hưởng preview hình học mặt cắt nhiều,
        // nhưng bật refresh để UI phản ứng đồng bộ (bạn có thể bỏ nếu muốn).
        RefreshPreview();
        Raise(nameof(IsValid));
      }
    }
  }

  private double _coverMm = 15;
  public double CoverMm
  {
    get => _coverMm;
    set
    {
      if (Set(ref _coverMm, Math.Max(0, value)))
      {
        RefreshPreview();
        Raise(nameof(IsValid));
      }
    }
  }

  private double _tieEndOffsetMm = 50;
  public double TieEndOffsetMm
  {
    get => _tieEndOffsetMm;
    set
    {
      if (Set(ref _tieEndOffsetMm, Math.Max(0, value)))
      {
        RefreshPreview();
        Raise(nameof(IsValid));
      }
    }
  }

  private RebarBarType? _selectedMainBar;
  public RebarBarType? SelectedMainBar
  {
    get => _selectedMainBar;
    set
    {
      if (Set(ref _selectedMainBar, value))
        Raise(nameof(IsValid));
    }
  }

  private RebarBarType? _selectedTieBar;
  public RebarBarType? SelectedTieBar
  {
    get => _selectedTieBar;
    set
    {
      if (Set(ref _selectedTieBar, value))
        Raise(nameof(IsValid));
    }
  }

  private ColumnPreviewModel? _previewModel;
  public ColumnPreviewModel? PreviewModel
  {
    get => _previewModel;
    private set => Set(ref _previewModel, value);
  }

  public string SelectedHostInfo
  {
    get
    {
      var host = SelectedHost;
      if (host is null) return "No host selected.";
      return $"Host Id: {host.Id.Value}";
    }
  }

  public bool IsValid
  {
    get
    {
      if (Hosts.Count == 0) return false;
      if (SelectedHost is null) return false;
      if (SelectedMainBar is null) return false;
      if (SelectedTieBar  is null) return false;
      if (Cx < 2 || Cy < 2) return false;
      if (TieSpacingMm <= 0) return false;
      if (CoverMm < 0) return false;
      if (TieEndOffsetMm < 0) return false;
      return true;
    }
  }

  private ColumnRebarDialogVm(Document doc, IReadOnlyList<FamilyInstance> hosts, List<RebarBarType> bars)
  {
    _doc = doc ?? throw new ArgumentNullException(nameof(doc));
    Hosts = hosts ?? throw new ArgumentNullException(nameof(hosts));
    if (Hosts.Count == 0) throw new InvalidOperationException("No column selected.");

    HostDisplayItems = Hosts.Select(BuildHostDisplay).ToList();

    MainBarTypes = bars ?? throw new ArgumentNullException(nameof(bars));
    TieBarTypes  = bars;

    SelectedMainBar = bars.FirstOrDefault();
    SelectedTieBar  = bars.FirstOrDefault();

    _selectedHostIndex = 0;

    RefreshPreview();
  }

  public static ColumnRebarDialogVm Create(Document doc, List<FamilyInstance> hosts)
  {
    if (hosts == null || hosts.Count == 0)
      throw new InvalidOperationException("No column selected.");

    var bars = new FilteredElementCollector(doc)
      .OfClass(typeof(RebarBarType))
      .Cast<RebarBarType>()
      .OrderBy(x => x.Name)
      .ToList();

    if (bars.Count == 0)
      throw new InvalidOperationException("No RebarBarType found in project.");

    return new ColumnRebarDialogVm(doc, hosts, bars);
  }

  private static string BuildHostDisplay(FamilyInstance fi)
  {
    var id = fi.Id.Value;
    var typeName = fi.Symbol?.Name ?? "Column";
    return $"#{id}  ({typeName})";
  }

  private void RefreshPreview()
  {
    if (Hosts.Count == 0)
    {
      PreviewModel = null;
      return;
    }

    var host = SelectedHost;
    if (host is null)
    {
      PreviewModel = null;
      return;
    }

    // ✅ Preview theo host đang chọn
    PreviewModel = ColumnPreviewModel.CreateFromRevit(
      doc: _doc,
      host: host,
      coverMm: CoverMm,
      tieEndOffsetMm: TieEndOffsetMm,
      cx: Cx,
      cy: Cy);

    Raise(nameof(SelectedHostInfo));
  }

  public ColumnRebarOptions BuildOptions()
  {
    if (SelectedMainBar is null) throw new InvalidOperationException("Main bar type is not selected.");
    if (SelectedTieBar  is null) throw new InvalidOperationException("Tie bar type is not selected.");

    var tieSpacingFt = UnitUtils.ConvertToInternalUnits(TieSpacingMm, UnitTypeId.Millimeters);
    var coverFt      = UnitUtils.ConvertToInternalUnits(CoverMm, UnitTypeId.Millimeters);
    var tieEndFt     = UnitUtils.ConvertToInternalUnits(TieEndOffsetMm, UnitTypeId.Millimeters);

    return ColumnRebarOptions.Create(
      mainBarType: SelectedMainBar,
      tieBarType: SelectedTieBar,
      mainBarCountX: Cx,
      mainBarCountY: Cy,
      tieSpacing: tieSpacingFt,
      cover: coverFt,
      tieEndOffset: tieEndFt
    );
  }

  public void Close() => RequestClose?.Invoke();

  public event PropertyChangedEventHandler? PropertyChanged;

  private void Raise([CallerMemberName] string? n = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

  private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value)) return false;
    field = value;
    Raise(name);
    return true;
  }

  private static int ClampIndex(int index, int count)
  {
    if (count <= 0) return 0;
    if (index < 0) return 0;
    if (index >= count) return count - 1;
    return index;
  }
  private bool _singleTie = true;
  public bool SingleTie
  {
    get => _singleTie;
    set
    {
      if (Set(ref _singleTie, value))
        Set(ref _closedTie, !value, nameof(ClosedTie));
    }
  }

  private bool _closedTie;
  public bool ClosedTie
  {
    get => _closedTie;
    set
    {
      if (Set(ref _closedTie, value))
        Set(ref _singleTie, !value, nameof(SingleTie));
    }
  }

}
