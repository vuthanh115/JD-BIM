#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using kaka.Structure.Structural.App.Models.PrimaveraP6;
using kaka.Structure.Structural.App.Services.PrimaveraP6;

namespace kaka.Structure.Structural.App.ViewModels.PrimaveraP6;

public sealed partial class ExportP6ScheduleViewModel : ObservableObject
{
  private readonly Document _doc;
  private readonly Window? _window;
  private readonly List<P6WbsSummaryItem> _summaries;

  public string ProjectName { get; }
  public string ModelTitle { get; }

  [ObservableProperty]
  private int _totalElements;

  [ObservableProperty]
  private int _totalWbsNodes;

  [ObservableProperty]
  private int _totalActivities;

  public ObservableCollection<P6WbsSummaryItem> WbsItems { get; }

  [ObservableProperty]
  private string _xerFilePath = string.Empty;

  [ObservableProperty]
  private string _csvFilePath = string.Empty;

  [ObservableProperty]
  private string _p6DatabasePath = string.Empty;

  [ObservableProperty]
  private bool _syncToP6Database = true;

  [ObservableProperty]
  private bool _exportToXer = true;

  [ObservableProperty]
  private bool _exportToCsv = false;

  [ObservableProperty]
  private bool _exportNavisworks4D = true;

  [ObservableProperty]
  private string _navisworksSetsPath = string.Empty;

  [ObservableProperty]
  private bool _includeLinkedModels = true;

  [ObservableProperty]
  private bool _hasAvailableLinks;

  public ObservableCollection<RevitLinkOptionItem> AvailableLinks { get; } = new();

  [ObservableProperty]
  private bool _openFolderAfterExport = true;

  [ObservableProperty]
  private string _statusMessage = string.Empty;

  [ObservableProperty]
  private bool _isBusy;

  public ExportP6ScheduleViewModel(Document doc, Window? window)
  {
    _doc = doc ?? throw new ArgumentNullException(nameof(doc));
    _window = window;

    ProjectName = doc.ProjectInformation?.Name ?? Path.GetFileNameWithoutExtension(doc.PathName);
    if (string.IsNullOrWhiteSpace(ProjectName))
      ProjectName = "Revit_Project";

    ModelTitle = string.IsNullOrWhiteSpace(doc.Title) ? "Active_Model" : doc.Title;

    _summaries = new List<P6WbsSummaryItem>();
    WbsItems = new ObservableCollection<P6WbsSummaryItem>();

    // Quét danh sách các file Revit Link hiện có trong mô hình
    DiscoverLinkedModels();

    // Xác định thư mục lưu mặc định
    var defaultDir = @"c:\Working\kaka\Mindmap";
    if (!Directory.Exists(defaultDir))
    {
      defaultDir = !string.IsNullOrWhiteSpace(doc.PathName) && File.Exists(doc.PathName)
        ? Path.GetDirectoryName(doc.PathName)!
        : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    var baseName = Path.GetFileNameWithoutExtension(ModelTitle);
    XerFilePath = Path.Combine(defaultDir, $"{baseName}_P6_Schedule.xer");
    CsvFilePath = Path.Combine(defaultDir, $"{baseName}_P6_Schedule.csv");
    NavisworksSetsPath = Path.Combine(defaultDir, $"{baseName}_Navisworks_SearchSets.xml");

    var defaultP6Db = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PPMDBSQLite.db");
    P6DatabasePath = defaultP6Db;
    SyncToP6Database = File.Exists(defaultP6Db);

    ReloadSummaries();
  }

  private void DiscoverLinkedModels()
  {
    try
    {
      var linkInstances = new FilteredElementCollector(_doc)
        .OfClass(typeof(RevitLinkInstance))
        .Cast<RevitLinkInstance>();

      var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var link in linkInstances)
      {
        var linkDoc = link.GetLinkDocument();
        if (linkDoc != null && !seenTitles.Contains(linkDoc.Title))
        {
          seenTitles.Add(linkDoc.Title);
          var item = new RevitLinkOptionItem
          {
            DocumentTitle = linkDoc.Title,
            FilePath = linkDoc.PathName,
            LinkInstanceId = (int)link.Id.Value,
            IsSelected = true
          };

          item.PropertyChanged += (s, e) =>
          {
            if (e.PropertyName == nameof(RevitLinkOptionItem.IsSelected))
            {
              ReloadSummaries();
            }
          };

          AvailableLinks.Add(item);
        }
      }

      HasAvailableLinks = AvailableLinks.Count > 0;
    }
    catch
    {
      HasAvailableLinks = false;
    }
  }

  partial void OnIncludeLinkedModelsChanged(bool value)
  {
    ReloadSummaries();
  }

  private void ReloadSummaries()
  {
    _summaries.Clear();

    IEnumerable<string>? selectedTitles = null;
    if (IncludeLinkedModels && AvailableLinks.Count > 0)
    {
      selectedTitles = AvailableLinks.Where(x => x.IsSelected).Select(x => x.DocumentTitle);
    }

    var newItems = PrimaveraP6ExportService.BuildWbsSummaries(_doc, IncludeLinkedModels, selectedTitles);
    _summaries.AddRange(newItems);

    WbsItems.Clear();
    foreach (var item in _summaries)
    {
      WbsItems.Add(item);
    }

    TotalElements = _summaries.Sum(x => x.ElementCount);
    TotalWbsNodes = _summaries.Count + (_summaries.Any(x => x.Discipline == "KIẾN TRÚC") ? 3 : 2);
    TotalActivities = _summaries.Sum(x => x.TaskCount);
    StatusMessage = $"Đã nạp {TotalElements:N0} cấu kiện ({_summaries.Count} nhóm hạng mục), sẵn sàng xuất sang {TotalWbsNodes} WBS Nodes & {TotalActivities} Activities.";
  }

  [RelayCommand]
  private void BrowseP6Database()
  {
    var dlg = new OpenFileDialog
    {
      Title = "Chọn file CSDL Primavera P6 SQLite (.db)",
      Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
      FileName = P6DatabasePath
    };

    if (dlg.ShowDialog() == true)
    {
      P6DatabasePath = dlg.FileName;
    }
  }

  [RelayCommand]
  private void BrowseXerFile()
  {
    var dlg = new SaveFileDialog
    {
      Title = "Chọn vị trí lưu file Primavera P6 XER",
      Filter = "Primavera P6 XER (*.xer)|*.xer|All files (*.*)|*.*",
      FileName = XerFilePath
    };

    if (dlg.ShowDialog() == true)
    {
      XerFilePath = dlg.FileName;
    }
  }

  [RelayCommand]
  private void BrowseCsvFile()
  {
    var dlg = new SaveFileDialog
    {
      Title = "Chọn vị trí lưu file P6 Spreadsheet CSV",
      Filter = "CSV Spreadsheet (*.csv)|*.csv|All files (*.*)|*.*",
      FileName = CsvFilePath
    };

    if (dlg.ShowDialog() == true)
    {
      CsvFilePath = dlg.FileName;
    }
  }

  [RelayCommand]
  private void BrowseNavisworksSets()
  {
    var dlg = new SaveFileDialog
    {
      Title = "Chọn vị trí lưu Navisworks Search Sets XML",
      Filter = "Navisworks Search Sets (*.xml)|*.xml|All files (*.*)|*.*",
      FileName = NavisworksSetsPath
    };

    if (dlg.ShowDialog() == true)
    {
      NavisworksSetsPath = dlg.FileName;
    }
  }

  [RelayCommand]
  private void Export()
  {
    if (!SyncToP6Database && !ExportToXer && !ExportToCsv)
    {
      MessageBox.Show("Vui lòng chọn ít nhất một hình thức xuất/đồng bộ (P6 Database, .xer hoặc .csv).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    try
    {
      IsBusy = true;
      StatusMessage = "Đang trích xuất và đồng bộ tiến độ Primavera P6...";

      string? exportedFolder = null;
      bool dbSuccess = false;

      if (SyncToP6Database && !string.IsNullOrWhiteSpace(P6DatabasePath))
      {
        if (PrimaveraP6ExportService.SyncToP6Sqlite(P6DatabasePath, "PRJ-KAKA-BIM", ProjectName, _summaries, out var dbError))
        {
          dbSuccess = true;
        }
        else
        {
          MessageBox.Show($"Đồng bộ vào P6 SQLite Database thất bại:\n{dbError}", "Lỗi CSDL P6", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      }

      if (ExportToXer && !string.IsNullOrWhiteSpace(XerFilePath))
      {
        var dir = Path.GetDirectoryName(XerFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        PrimaveraP6ExportService.ExportToXer(XerFilePath, ProjectName, _summaries);
        exportedFolder = dir;
      }

      if (ExportToCsv && !string.IsNullOrWhiteSpace(CsvFilePath))
      {
        var dir = Path.GetDirectoryName(CsvFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        PrimaveraP6ExportService.ExportToCsv(CsvFilePath, ProjectName, _summaries);
        exportedFolder = dir;
      }

      if (ExportNavisworks4D && !string.IsNullOrWhiteSpace(NavisworksSetsPath))
      {
        var dir = Path.GetDirectoryName(NavisworksSetsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
          Directory.CreateDirectory(dir);

        Navisworks4DExportService.ExportNavisworksSearchSetsXml(NavisworksSetsPath, _summaries);
        var timelinerCsv = Path.ChangeExtension(NavisworksSetsPath, ".timeliner.csv");
        Navisworks4DExportService.ExportNavisworksTimelinerCsv(timelinerCsv, ProjectName, _summaries);
        exportedFolder = dir;
      }

      StatusMessage = "Đồng bộ & Xuất dữ liệu Primavera P6 & Navisworks 4D hoàn tất thành công!";

      MessageBox.Show(
        $"Đồng bộ & Xuất dữ liệu BIM 4D thành công!\n\n" +
        $"• Dự án: {ProjectName} (Mã: PRJ-KAKA-BIM)\n" +
        $"• WBS Nodes: {TotalWbsNodes}\n" +
        $"• Activities: {TotalActivities}\n\n" +
        (SyncToP6Database && dbSuccess ? $"• CSDL P6: Đã nạp thành công 100% tiếng Việt có dấu vào PPMDBSQLite.db\n" : "") +
        (ExportToXer ? $"• File XER: {Path.GetFileName(XerFilePath)}\n" : "") +
        (ExportToCsv ? $"• File CSV: {Path.GetFileName(CsvFilePath)}\n" : "") +
        (ExportNavisworks4D ? $"• Navisworks 4D Search Sets: {Path.GetFileName(NavisworksSetsPath)}\n  (Kèm file Timeliner Auto-Attach CSV)\n" : ""),
        "Primavera P6 & Navisworks 4D Integration",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

      if (OpenFolderAfterExport && !string.IsNullOrEmpty(exportedFolder) && Directory.Exists(exportedFolder))
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = exportedFolder,
          UseShellExecute = true
        });
      }

      _window?.Close();
    }
    catch (Exception ex)
    {
      StatusMessage = $"Lỗi: {ex.Message}";
      MessageBox.Show($"Lỗi khi xuất dữ liệu Primavera P6:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
      IsBusy = false;
    }
  }

  [RelayCommand]
  private void Close()
  {
    _window?.Close();
  }
}
