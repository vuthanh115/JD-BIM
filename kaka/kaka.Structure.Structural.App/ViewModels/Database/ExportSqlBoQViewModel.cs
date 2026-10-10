#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using kaka.Structure.Structural.App.Models.Database;
using kaka.Structure.Structural.App.Services.Database;

namespace kaka.Structure.Structural.App.ViewModels.Database;

public sealed partial class ExportSqlBoQViewModel : ObservableObject
{
  private readonly Document _doc;
  private readonly Window? _window;
  private readonly List<RevitBoQItem> _items;

  public string ProjectName { get; }
  public string ModelTitle { get; }
  public int TotalElements { get; }

  public ObservableCollection<BoQCategorySummary> Categories { get; }

  [ObservableProperty]
  private string _sqlFilePath = string.Empty;

  [ObservableProperty]
  private string _dbFilePath = string.Empty;

  [ObservableProperty]
  private bool _exportToSql = true;

  [ObservableProperty]
  private bool _exportToDb = true;

  [ObservableProperty]
  private bool _openFolderAfterExport = true;

  [ObservableProperty]
  private string _statusMessage = string.Empty;

  [ObservableProperty]
  private bool _isBusy;

  public ExportSqlBoQViewModel(Document doc, Window? window)
  {
    _doc = doc ?? throw new ArgumentNullException(nameof(doc));
    _window = window;

    ProjectName = doc.ProjectInformation?.Name ?? Path.GetFileNameWithoutExtension(doc.PathName);
    if (string.IsNullOrWhiteSpace(ProjectName))
      ProjectName = "Revit_Project";

    ModelTitle = string.IsNullOrWhiteSpace(doc.Title) ? "Active_Model" : doc.Title;

    // Trích xuất dữ liệu BoQ
    _items = RevitBoQExtractor.ExtractBoQ(doc);
    TotalElements = _items.Count;

    var summaries = RevitBoQExtractor.Summarize(_items);
    Categories = new ObservableCollection<BoQCategorySummary>(summaries);

    // Xác định đường dẫn mặc định
    var defaultDir = @"c:\Working\kaka\Mindmap";
    if (!Directory.Exists(defaultDir))
    {
      defaultDir = !string.IsNullOrWhiteSpace(doc.PathName) && File.Exists(doc.PathName)
        ? Path.GetDirectoryName(doc.PathName)!
        : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    DbFilePath = Path.Combine(defaultDir, "mindmap.db");
    SqlFilePath = Path.Combine(defaultDir, $"{Path.GetFileNameWithoutExtension(ModelTitle)}_boq.sql");

    StatusMessage = $"Đã nạp {TotalElements} cấu kiện thuộc {Categories.Count} nhóm đề mục.";
  }

  [RelayCommand]
  private void BrowseDbFile()
  {
    var dlg = new OpenFileDialog
    {
      Title = "Chọn file SQLite Database",
      Filter = "SQLite Database (*.db;*.sqlite)|*.db;*.sqlite|All files (*.*)|*.*",
      FileName = DbFilePath
    };

    if (dlg.ShowDialog() == true)
    {
      DbFilePath = dlg.FileName;
    }
  }

  [RelayCommand]
  private void BrowseSqlFile()
  {
    var dlg = new SaveFileDialog
    {
      Title = "Chọn vị trí lưu file script SQL",
      Filter = "SQL Script (*.sql)|*.sql|All files (*.*)|*.*",
      FileName = SqlFilePath
    };

    if (dlg.ShowDialog() == true)
    {
      SqlFilePath = dlg.FileName;
    }
  }

  [RelayCommand]
  private void Export()
  {
    if (!ExportToSql && !ExportToDb)
    {
      MessageBox.Show("Vui lòng chọn ít nhất một định dạng xuất (File SQL hoặc SQLite DB).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    if (ExportToSql && string.IsNullOrWhiteSpace(SqlFilePath))
    {
      MessageBox.Show("Vui lòng chỉ định đường dẫn lưu file SQL.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    if (ExportToDb && string.IsNullOrWhiteSpace(DbFilePath))
    {
      MessageBox.Show("Vui lòng chỉ định đường dẫn file SQLite (.db).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    IsBusy = true;
    StatusMessage = "Đang tạo mã SQL và xuất dữ liệu...";

    try
    {
      var summaries = new List<BoQCategorySummary>(Categories);
      var sqlScript = RevitBoQExtractor.GenerateSqlScript(ProjectName, _doc.PathName, _items, summaries);

      string? tempSql = null;
      var targetSql = SqlFilePath;

      if (ExportToSql)
      {
        var sqlDir = Path.GetDirectoryName(SqlFilePath);
        if (!string.IsNullOrEmpty(sqlDir) && !Directory.Exists(sqlDir))
          Directory.CreateDirectory(sqlDir);

        File.WriteAllText(SqlFilePath, sqlScript, System.Text.Encoding.UTF8);
        targetSql = SqlFilePath;
      }
      else
      {
        tempSql = Path.Combine(Path.GetTempPath(), $"revit_boq_{Guid.NewGuid():N}.sql");
        File.WriteAllText(tempSql, sqlScript, System.Text.Encoding.UTF8);
        targetSql = tempSql;
      }

      var dbSuccess = true;
      var dbError = string.Empty;

      if (ExportToDb)
      {
        var dbDir = Path.GetDirectoryName(DbFilePath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
          Directory.CreateDirectory(dbDir);

        dbSuccess = RevitBoQExtractor.ExecuteSqlToDb(targetSql, DbFilePath, out dbError);
      }

      if (tempSql != null && File.Exists(tempSql))
      {
        try { File.Delete(tempSql); } catch { }
      }

      if (!dbSuccess)
      {
        StatusMessage = $"Lỗi ghi vào DB: {dbError}";
        MessageBox.Show($"Đã xuất file SQL nhưng nạp vào SQLite thất bại:\n{dbError}", "Lỗi SQLite", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var msg = $"Xuất thành công {TotalElements} cấu kiện!\n";
      if (ExportToSql) msg += $"- Script SQL: {SqlFilePath}\n";
      if (ExportToDb) msg += $"- Database: {DbFilePath}\n";

      StatusMessage = "Xuất dữ liệu hoàn tất thành công!";
      MessageBox.Show(msg, "Hoàn tất xuất BoQ", MessageBoxButton.OK, MessageBoxImage.Information);

      if (OpenFolderAfterExport)
      {
        var targetFile = ExportToSql ? SqlFilePath : DbFilePath;
        if (File.Exists(targetFile))
        {
          Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{targetFile}\"") { UseShellExecute = true });
        }
      }
    }
    catch (Exception ex)
    {
      StatusMessage = $"Lỗi: {ex.Message}";
      MessageBox.Show($"Có lỗi xảy ra trong quá trình xuất:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
