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

public sealed partial class RuleBased4DBimViewModel : ObservableObject
{
  private readonly Document _doc;
  private readonly Window? _window;

  public string ProjectName { get; }
  public string ModelTitle { get; }

  [ObservableProperty]
  private int _totalElements;

  [ObservableProperty]
  private int _totalTasks;

  [ObservableProperty]
  private int _totalDurationDays;

  [ObservableProperty]
  private int _criticalTasksCount;

  [ObservableProperty]
  private int _totalLogicLinks;

  [ObservableProperty]
  private int _totalSearchSets;

  public ObservableCollection<Bim4DTaskItem> Tasks { get; } = new();

  [ObservableProperty]
  private string _outputDirectory = string.Empty;

  [ObservableProperty]
  private bool _exportSearchSetsXml = true;

  [ObservableProperty]
  private bool _exportTimeLinerRulesXml = true;

  [ObservableProperty]
  private bool _exportTimeLinerCsv = true;

  [ObservableProperty]
  private bool _exportP6CpmXer = true;

  [ObservableProperty]
  private bool _exportWorkflowGuide = true;

  [ObservableProperty]
  private bool _openFolderAfterExport = true;

  [ObservableProperty]
  private string _statusMessage = string.Empty;

  [ObservableProperty]
  private bool _isBusy;

  public RuleBased4DBimViewModel(Document doc, Window? window)
  {
    _doc = doc ?? throw new ArgumentNullException(nameof(doc));
    _window = window;

    ProjectName = doc.ProjectInformation?.Name ?? Path.GetFileNameWithoutExtension(doc.PathName);
    if (string.IsNullOrWhiteSpace(ProjectName))
      ProjectName = "KAKA_Project_4D";

    ModelTitle = string.IsNullOrWhiteSpace(doc.Title) ? "Active_Model" : doc.Title;

    var defaultDir = @"c:\Working\kaka\Mindmap";
    if (!Directory.Exists(defaultDir))
    {
      defaultDir = !string.IsNullOrWhiteSpace(doc.PathName) && File.Exists(doc.PathName)
        ? Path.GetDirectoryName(doc.PathName)!
        : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    OutputDirectory = defaultDir;

    Reload4DTasks();
  }

  private void Reload4DTasks()
  {
    Tasks.Clear();
    var list = Bim4DSchedulingEngine.Generate4DTasks(_doc, true);

    foreach (var t in list)
    {
      Tasks.Add(t);
    }

    TotalElements = list.Sum(x => x.ElementCount);
    TotalTasks = list.Count;
    TotalDurationDays = list.Count > 0 ? (int)(list.Max(x => x.EndDate) - list.Min(x => x.StartDate)).TotalDays : 0;
    CriticalTasksCount = list.Count(x => x.IsCritical);
    TotalLogicLinks = list.Sum(x => x.PredecessorCodes.Count);

    int setCount = 0;
    foreach (var folder in NavisworksSearchSetsService.StandardDisciplineSets.Values)
    {
      setCount += folder.Count;
    }
    TotalSearchSets = setCount;

    StatusMessage = $"Đã thiết lập {TotalTasks} công tác 4D BIM, {TotalLogicLinks} quan hệ đường găng CPM, sẵn sàng đồng bộ Navisworks + P6.";
  }

  [RelayCommand]
  private void BrowseOutputDirectory()
  {
#if NET8_0_OR_GREATER
    var dlg = new OpenFolderDialog
    {
      Title = "Chọn thư mục xuất gói 4D BIM Suite (Navisworks + P6)",
      InitialDirectory = OutputDirectory
    };

    if (dlg.ShowDialog() == true)
    {
      OutputDirectory = dlg.FolderName;
    }
#else
    var dlg = new SaveFileDialog
    {
      Title = "Chọn thư mục xuất gói 4D BIM Suite (Bấm Save trong thư mục mong muốn)",
      FileName = "Luu_Goi_4D_Tai_Day",
      Filter = "Thư mục hiện hành|*.folder",
      InitialDirectory = OutputDirectory
    };

    if (dlg.ShowDialog() == true)
    {
      var dir = Path.GetDirectoryName(dlg.FileName);
      if (!string.IsNullOrEmpty(dir))
        OutputDirectory = dir;
    }
#endif
  }

  [RelayCommand]
  private void Export4DSuite()
  {
    if (string.IsNullOrWhiteSpace(OutputDirectory) || !Directory.Exists(OutputDirectory))
    {
      try
      {
        Directory.CreateDirectory(OutputDirectory);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Thư mục xuất không hợp lệ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
    }

    try
    {
      IsBusy = true;
      StatusMessage = "Đang tạo bộ Search Sets XML, TimeLiner Rules và xuất P6 XER CPM...";

      var cleanName = ProjectName.Replace(" ", "_").Replace("-", "_");
      var searchSetsPath = Path.Combine(OutputDirectory, $"{cleanName}_Navisworks_SearchSets.xml");
      var timeLinerRulesPath = Path.Combine(OutputDirectory, $"{cleanName}_TimeLiner_AutoAttach_Rules.xml");
      var timeLinerCsvPath = Path.Combine(OutputDirectory, $"{cleanName}_TimeLiner_Schedule.csv");
      var p6XerPath = Path.Combine(OutputDirectory, $"{cleanName}_P6_4D_CPM_Schedule.xer");
      var guidePath = Path.Combine(OutputDirectory, $"{cleanName}_4D_Workflow_Guide.md");

      var taskList = Tasks.ToList();

      // 1. Search Sets XML
      if (ExportSearchSetsXml)
      {
        NavisworksSearchSetsService.ExportSearchSetsXml(searchSetsPath, ProjectName, taskList);
      }

      // 2. TimeLiner Rules XML
      if (ExportTimeLinerRulesXml)
      {
        NavisworksTimeLinerService.ExportTimeLinerRulesXml(timeLinerRulesPath);
      }

      // 3. TimeLiner CSV
      if (ExportTimeLinerCsv)
      {
        NavisworksTimeLinerService.ExportTimeLinerCsv(timeLinerCsvPath, taskList);
      }

      // 4. P6 CPM XER
      if (ExportP6CpmXer)
      {
        Bim4DSchedulingEngine.Export4DToP6Xer(p6XerPath, ProjectName, taskList);
      }

      // 5. Workflow Guide Markdown
      if (ExportWorkflowGuide)
      {
        NavisworksTimeLinerService.ExportWorkflowGuideMd(guidePath, ProjectName);
      }

      StatusMessage = "Xuất trọn bộ 4D BIM Package hoàn tất thành công!";

      MessageBox.Show(
        $"XUẤT TRỌN BỘ 4D BIM SUITE THÀNH CÔNG!\n\n" +
        $"• Dự án: {ProjectName}\n" +
        $"• Tổng số 4D Tasks: {TotalTasks} công tác\n" +
        $"• Quan hệ CPM Logic: {TotalLogicLinks} liên kết Finish-to-Start\n" +
        $"• Search Sets Navisworks: {TotalSearchSets} bộ lọc tự động\n\n" +
        $"Các tệp tin đã tạo sẵn sàng tại thư mục:\n" +
        $"1. {Path.GetFileName(searchSetsPath)} (Import vào Navisworks Sets)\n" +
        $"2. {Path.GetFileName(timeLinerRulesPath)} (Navisworks TimeLiner Rules)\n" +
        $"3. {Path.GetFileName(timeLinerCsvPath)} (Import Tasks vào TimeLiner)\n" +
        $"4. {Path.GetFileName(p6XerPath)} (Import vào P6 chạy F9 CPM)\n" +
        $"5. {Path.GetFileName(guidePath)} (Tài liệu SOP 5 bước hướng dẫn)",
        "KAKA Rule-based 4D BIM Suite",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

      if (OpenFolderAfterExport && Directory.Exists(OutputDirectory))
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = OutputDirectory,
          UseShellExecute = true
        });
      }

      _window?.Close();
    }
    catch (Exception ex)
    {
      StatusMessage = $"Lỗi: {ex.Message}";
      MessageBox.Show($"Lỗi khi xuất gói 4D BIM:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
