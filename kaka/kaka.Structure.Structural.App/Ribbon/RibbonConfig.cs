#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace kaka.Structure.Structural.App.Ribbon ;

public sealed class RibbonConfig
{
  [JsonPropertyName("schemaVersion")]
  public int SchemaVersion { get; set; } = 1;

  [JsonPropertyName("tabName")]
  public string TabName { get; set; } = "KAKA";

  [JsonPropertyName("panelName")]
  public string PanelName { get; set; } = "\u2800";

  [JsonPropertyName("splits")]
  public List<RibbonSplit> Splits { get; set; } = new();

  public static RibbonConfig Load(string path)
  {
    var json = File.ReadAllText(path);

    var opts = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip,
      AllowTrailingCommas = true
    };

    return JsonSerializer.Deserialize<RibbonConfig>(json, opts)
           ?? throw new InvalidOperationException("Failed to deserialize ribbon.json");
  }

  public void ValidateOrThrow()
  {
    var errs = new List<string>();

    if (string.IsNullOrWhiteSpace(TabName)) errs.Add("tabName is empty.");
    if (string.IsNullOrWhiteSpace(PanelName)) errs.Add("panelName is empty.");
    if (Splits.Count == 0) errs.Add("splits is empty.");

    foreach (var s in Splits)
    {
      if (string.IsNullOrWhiteSpace(s.Id)) errs.Add("split.id is empty.");
      if (string.IsNullOrWhiteSpace(s.Text)) errs.Add($"split '{s.Id}' text is empty.");
      if (s.Items.Count == 0) errs.Add($"split '{s.Id}' items is empty.");

      if (!string.IsNullOrWhiteSpace(s.DefaultItemId) &&
          s.Items.All(x => !string.Equals(x.Id, s.DefaultItemId, StringComparison.OrdinalIgnoreCase)))
      {
        errs.Add($"split '{s.Id}' defaultItemId '{s.DefaultItemId}' not found in items.");
      }

      foreach (var it in s.Items)
      {
        if (string.IsNullOrWhiteSpace(it.Id)) errs.Add($"split '{s.Id}' item.id is empty.");
        if (string.IsNullOrWhiteSpace(it.Text)) errs.Add($"split '{s.Id}' item '{it.Id}' text is empty.");
        if (string.IsNullOrWhiteSpace(it.CommandClass)) errs.Add($"split '{s.Id}' item '{it.Id}' commandClass is empty.");
      }
    }

    if (errs.Count > 0)
      throw new InvalidOperationException("Invalid ribbon.json:\n- " + string.Join("\n- ", errs));
  }
}

public sealed class RibbonSplit
{
  [JsonPropertyName("id")]
  public string Id { get; set; } = "";

  [JsonPropertyName("text")]
  public string Text { get; set; } = "";

  [JsonPropertyName("panelName")]
  public string? PanelName { get; set; }

  [JsonPropertyName("defaultItemId")]
  public string? DefaultItemId { get; set; }

  [JsonPropertyName("items")]
  public List<RibbonItem> Items { get; set; } = new();
}

public sealed class RibbonItem
{
  [JsonPropertyName("id")]
  public string Id { get; set; } = "";

  [JsonPropertyName("text")]
  public string Text { get; set; } = "";

  [JsonPropertyName("commandClass")]
  public string CommandClass { get; set; } = "";

  [JsonPropertyName("icon16")]
  public string? Icon16 { get; set; }

  [JsonPropertyName("icon32")]
  public string? Icon32 { get; set; }

  [JsonPropertyName("tooltip")]
  public string? Tooltip { get; set; }
}