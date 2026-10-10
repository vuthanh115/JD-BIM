#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using BimSpeedUtils;
using BimSpeedUtils.JsonData;

namespace kaka.Structure.Structural.App.ViewModels
{
  /// <summary>
  /// Build naming text by mixing free text + selected parameter tokens.
  /// Provides preview + undo.
  /// </summary>
  public class NamingViewModel : ViewModelBase
  {
    private string _text = string.Empty;
    private string _preview = string.Empty;
    private string _parameter = string.Empty;

    // history stack for undo
    private readonly Stack<string> _history = new();

    public string Text
    {
      get => _text;
      set
      {
        if (Set(ref _text, value))
          GetPreview();
      }
    }

    public string Preview
    {
      get => _preview;
      private set => Set(ref _preview, value);
    }

    /// <summary>
    /// Available parameter names (for ComboBox).
    /// </summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>
    /// Currently selected parameter name.
    /// </summary>
    public string Parameter
    {
      get => _parameter;
      set => Set(ref _parameter, value);
    }

    /// <summary>
    /// Optional: record models list (if UI needs it).
    /// </summary>
    public List<RecordModel> RecordModels { get; set; } = new();

    public RelayCommand AddTextCommand { get; set; }
    public RelayCommand AddParameterCommand { get; set; }
    public RelayCommand UndoCommand { get; set; }

    public NamingViewModel(List<string> parameters)
    {
      Parameters = parameters ?? new List<string>();
      Parameter = Parameters.FirstOrDefault() ?? string.Empty;

      AddTextCommand = new RelayCommand(_ => AddText());
      AddParameterCommand = new RelayCommand(_ => AddParameter());
      UndoCommand = new RelayCommand(_ => Undo(), _ => _history.Count > 0);

      GetPreview();
    }

    private void PushHistory()
    {
      _history.Push(Text ?? string.Empty);
      // cập nhật CanExecute cho Undo nếu RelayCommand hỗ trợ
      
    }

    private void AddText()
    {
      // Nếu bạn có textbox riêng để nhập đoạn “text to append” thì sửa hàm này theo UI của bạn.
      // Hiện tại: chỉ lưu state để Undo + update preview.
      PushHistory();
      GetPreview();
    }

    private void AddParameter()
    {
      if (string.IsNullOrWhiteSpace(Parameter))
        return;

      PushHistory();

      // Quy ước token: {PARAM_NAME}
      var token = "{" + Parameter.Trim() + "}";
      Text = (Text ?? string.Empty) + token;

      GetPreview();
    }

    private void Undo()
    {
      if (_history.Count == 0) return;

      Text = _history.Pop();
      
      GetPreview();
    }

    /// <summary>
    /// Build Preview from Text by replacing {Param} with sample values if available.
    /// </summary>
    public void GetPreview()
    {
      Preview = BuildPreview(Text, RecordModels);
    }

    private static string BuildPreview(string? text, List<RecordModel>? records)
    {
      var s = (text ?? string.Empty);

      // Nếu không có sample data -> preview giữ nguyên token để người dùng thấy cấu trúc
      if (records is null || records.Count == 0)
        return s;

      // Lấy record đầu tiên làm sample preview
      var sample = records[0];

      // RecordModel của bạn có thể khác cấu trúc.
      // Thường kiểu JsonData record sẽ có Dictionary/fields.
      // Mình xử lý "an toàn": dùng reflection/dynamic nhẹ để lấy value theo key.
      foreach (var token in ExtractTokens(s))
      {
        var val = TryGetRecordValue(sample, token) ?? $"<{token}>";
        s = s.Replace("{" + token + "}", val);
      }

      return s;
    }

    private static IEnumerable<string> ExtractTokens(string s)
    {
      // parse {TOKEN}
      var list = new List<string>();
      var i = 0;
      while (i < s.Length)
      {
        var open = s.IndexOf('{', i);
        if (open < 0) break;
        var close = s.IndexOf('}', open + 1);
        if (close < 0) break;

        var token = s.Substring(open + 1, close - open - 1).Trim();
        if (token.Length > 0) list.Add(token);

        i = close + 1;
      }
      return list.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? TryGetRecordValue(RecordModel record, string key)
    {
      try
      {
        // 1) Nếu RecordModel có indexer this[string]
        var idx = record.GetType().GetProperty("Item", new[] { typeof(string) });
        if (idx is not null)
        {
          var v = idx.GetValue(record, new object[] { key });
          return v?.ToString();
        }

        // 2) Nếu RecordModel có Dictionary-like property (Data/Values/Fields)
        foreach (var propName in new[] { "Data", "Values", "Fields", "Items" })
        {
          var p = record.GetType().GetProperty(propName);
          var obj = p?.GetValue(record);
          if (obj is System.Collections.IDictionary dict)
          {
            foreach (var k in dict.Keys)
            {
              if (k is null) continue;
              if (string.Equals(k.ToString(), key, StringComparison.OrdinalIgnoreCase))
                return dict[k]?.ToString();
            }
          }
        }

        // 3) Nếu RecordModel có property trùng tên key
        var prop = record.GetType().GetProperty(key);
        return prop?.GetValue(record)?.ToString();
      }
      catch
      {
        return null;
      }
    }

    // Helper: tương thích ViewModelBase nếu nó KHÔNG có Set(ref,..)
    // Nếu ViewModelBase của bạn đã có Set thì bỏ vùng này đi.
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return false;
      field = value;

      // ViewModelBase thường có RaisePropertyChanged / OnPropertyChanged.
      // Nếu class base có method khác tên, bạn sửa dòng dưới.
     
      return true;
    }
  }
}
