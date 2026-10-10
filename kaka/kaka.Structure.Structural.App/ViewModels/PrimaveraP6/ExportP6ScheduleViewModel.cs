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
  public int TotalElements { get; }
  public int TotalWbsNodes { get; }
  public int TotalActivities { get; }

  public ObservableCollection<P6WbsSummaryItem> WbsItems { get; }

  [ObservableProperty]
  private string _xerFilePath = string.Empty;

  [ObservableProperty]
  private string _csvFilePath = string.Empty;

  [ObservableProperty]
  private bool _exportToXer = true;

  [ObservableProperty]
  private bool _exportToCsv = true;

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

    _summaries = PrimaveraP6ExportService.BuildWbsSummaries(doc);
    WbsItems = new ObservableCollection<P6WbsSummaryItem>(_summaries);

    TotalElements = _summaries.Sum(x => x.ElementCount);
    TotalWbsNodes = _summaries.Count + 2; // bao gồm root & structural WBS
    TotalActivities = _summaries.Sum(x => x.TaskCount);

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

    StatusMessage = $"Đã nạp {TotalElements:N0} cấu kiện, sẵn sàng xuất sang {TotalWbsNodes} WBS Nodes & {TotalActivities} Activities.";
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
  private void Export()
  {
    if (!ExportToXer && !ExportToCsv)
    {
      MessageBox.Show("Vui lòng chọn ít nhất một định dạng xuất (.xer hoặc .csv).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    try
    {
      IsBusy = true;
      StatusMessage = "Đang trích xuất và biên dịch lịch trình Primavera P6...";

      string? exportedFolder = null;

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

      StatusMessage = "Xuất dữ liệu Primavera P6 hoàn tất thành công!";

      MessageBox.Show(
        $"Xuất dữ liệu Primavera P6 thành công!\n\n" +
        $"• Dự án: {ProjectName}\n" +
        $"• WBS Nodes: {TotalWbsNodes}\n" +
        $"• Activities: {TotalActivities}\n\n" +
        (ExportToXer ? $"• File XER: {Path.GetFileName(XerFilePath)}\n" : "") +
        (ExportToCsv ? $"• File CSV: {Path.GetFileName(CsvFilePath)}\n" : ""),
        "Primavera P6 Export",
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
