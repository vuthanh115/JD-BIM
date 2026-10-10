#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.Services.Database;

namespace kaka.Structure.Structural.App.Services.Mcp;

public sealed class RevitMcpHttpServer
{
  private HttpListener? _listener;
  private Thread? _listenerThread;
  private volatile bool _isRunning;

  public static RevitMcpHttpServer Instance { get; } = new();

  public int Port { get; private set; } = 8080;
  public bool IsRunning => _isRunning;
  public int TotalRequestsHandled { get; private set; }

  private readonly JsonSerializerOptions _jsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
  };

  private RevitMcpHttpServer()
  {
  }

  public void Start(int preferredPort = 8080)
  {
    if (_isRunning) return;

    RevitMcpExternalEventHandler.Instance.Initialize();

    var portsToTry = new[] { preferredPort, 8081, 8082, 8088 };
    foreach (var port in portsToTry)
    {
      try
      {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();

        Port = port;
        _isRunning = true;
        break;
      }
      catch
      {
        _listener?.Close();
        _listener = null;
      }
    }

    if (_listener == null || !_isRunning)
    {
      return;
    }

    _listenerThread = new Thread(ListenLoop)
    {
      IsBackground = true,
      Name = "RevitMcpHttpServerThread"
    };
    _listenerThread.Start();
  }

  public void Stop()
  {
    if (!_isRunning) return;
    _isRunning = false;

    try
    {
      _listener?.Stop();
      _listener?.Close();
    }
    catch
    {
      // ignore
    }
  }

  private void ListenLoop()
  {
    while (_isRunning && _listener != null && _listener.IsListening)
    {
      try
      {
        var context = _listener.GetContext();
        TotalRequestsHandled++;
        ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
      }
      catch (HttpListenerException)
      {
        break;
      }
      catch
      {
        // continue loop
      }
    }
  }

  private async void HandleRequest(HttpListenerContext context)
  {
    var req = context.Request;
    var resp = context.Response;

    resp.Headers.Add("Access-Control-Allow-Origin", "*");
    resp.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

    if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
      resp.StatusCode = (int)HttpStatusCode.NoContent;
      resp.Close();
      return;
    }

    var path = req.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? "";

    try
    {
      object? responseData = null;

      switch (path)
      {
        case "" or "/health" or "/api/status":
          responseData = GetStatusResponse();
          break;

        case "/api/model-info":
          responseData = await RevitMcpExternalEventHandler.Instance.EnqueueTaskAsync(GetModelInfo);
          break;

        case "/api/elements/query":
          var queryBody = await ReadRequestBodyJson<ElementQueryRequest>(req);
          responseData = await RevitMcpExternalEventHandler.Instance.EnqueueTaskAsync(app => QueryElements(app, queryBody));
          break;

        case "/api/elements/select":
          var selectBody = await ReadRequestBodyJson<ElementSelectRequest>(req);
          responseData = await RevitMcpExternalEventHandler.Instance.EnqueueTaskAsync(app => SelectElements(app, selectBody));
          break;

        case "/api/elements/set-parameter":
          var setParamBody = await ReadRequestBodyJson<SetParameterRequest>(req);
          responseData = await RevitMcpExternalEventHandler.Instance.EnqueueTaskAsync(app => SetElementParameter(app, setParamBody));
          break;

        case "/api/boq/extract":
          responseData = await RevitMcpExternalEventHandler.Instance.EnqueueTaskAsync(ExtractBoQ);
          break;

        default:
          resp.StatusCode = (int)HttpStatusCode.NotFound;
          responseData = new { error = $"Endpoint '{path}' not found." };
          break;
      }

      var json = JsonSerializer.Serialize(responseData, _jsonOptions);
      var buffer = Encoding.UTF8.GetBytes(json);
      resp.ContentType = "application/json; charset=utf-8";
      resp.ContentLength64 = buffer.Length;
      resp.StatusCode = (int)HttpStatusCode.OK;
      await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
    catch (Exception ex)
    {
      try
      {
        resp.StatusCode = (int)HttpStatusCode.InternalServerError;
        var errJson = JsonSerializer.Serialize(new { error = ex.Message, details = ex.ToString() }, _jsonOptions);
        var errBuffer = Encoding.UTF8.GetBytes(errJson);
        resp.ContentType = "application/json; charset=utf-8";
        resp.ContentLength64 = errBuffer.Length;
        await resp.OutputStream.WriteAsync(errBuffer, 0, errBuffer.Length);
      }
      catch { }
    }
    finally
    {
      try { resp.Close(); } catch { }
    }
  }

  private object GetStatusResponse()
  {
    return new
    {
      status = "connected",
      service = "Revit MCP Server (kaka.Structure)",
      port = Port,
      isRunning = _isRunning,
      totalRequests = TotalRequestsHandled
    };
  }

  private object GetModelInfo(UIApplication app)
  {
    var uiDoc = app.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      return new { error = "No active Revit document is open." };
    }

    var doc = uiDoc.Document;
    var levels = new FilteredElementCollector(doc)
      .OfClass(typeof(Level))
      .Cast<Level>()
      .OrderBy(l => l.Elevation)
      .Select(l => new
      {
        id = l.Id.Value,
        name = l.Name,
        elevationM = Math.Round(l.Elevation * 0.3048, 3)
      })
      .ToList();

    return new
    {
      projectName = doc.ProjectInformation?.Name ?? doc.Title,
      projectNumber = doc.ProjectInformation?.Number ?? "",
      title = doc.Title,
      path = doc.PathName,
      isFamilyDocument = doc.IsFamilyDocument,
      activeView = doc.ActiveView?.Name ?? "",
      activeViewType = doc.ActiveView?.ViewType.ToString() ?? "",
      levels
    };
  }

  private object QueryElements(UIApplication app, ElementQueryRequest? req)
  {
    var uiDoc = app.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      return new { error = "No active Revit document." };
    }

    var doc = uiDoc.Document;
    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

    // Lọc theo Category
    if (!string.IsNullOrWhiteSpace(req?.Category))
    {
      var bic = ParseBuiltInCategory(req.Category);
      if (bic.HasValue)
      {
        collector = collector.OfCategory(bic.Value);
      }
    }

    var limit = req?.Limit > 0 ? Math.Min(req.Limit, 500) : 100;
    var results = new List<object>();

    foreach (var elem in collector)
    {
      if (elem.Category == null) continue;

      var typeId = elem.GetTypeId();
      var elemType = typeId != ElementId.InvalidElementId ? doc.GetElement(typeId) as ElementType : null;
      var typeName = elemType?.Name ?? elem.Name ?? "";
      var familyName = elemType?.FamilyName ?? "";

      // Lọc Level
      var levelName = "";
      var levelId = elem.LevelId;
      if (levelId != ElementId.InvalidElementId)
      {
        levelName = doc.GetElement(levelId)?.Name ?? "";
      }
      else
      {
        var lvlParam = elem.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM)
                       ?? elem.get_Parameter(BuiltInParameter.LEVEL_PARAM);
        levelName = lvlParam?.AsValueString() ?? lvlParam?.AsString() ?? "";
      }

      if (!string.IsNullOrWhiteSpace(req?.Level) &&
          levelName.IndexOf(req.Level, StringComparison.OrdinalIgnoreCase) < 0)
      {
        continue;
      }

      // Lọc search
      if (!string.IsNullOrWhiteSpace(req?.Search))
      {
        var match = typeName.IndexOf(req.Search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    familyName.IndexOf(req.Search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (elem.Name ?? "").IndexOf(req.Search, StringComparison.OrdinalIgnoreCase) >= 0;
        if (!match) continue;
      }

      var comments = elem.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
      var mark = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";

      results.Add(new
      {
        id = elem.Id.Value,
        uniqueId = elem.UniqueId,
        category = elem.Category.Name,
        familyName,
        typeName,
        level = levelName,
        mark,
        comments
      });

      if (results.Count >= limit) break;
    }

    return new
    {
      count = results.Count,
      elements = results
    };
  }

  private object SelectElements(UIApplication app, ElementSelectRequest? req)
  {
    var uiDoc = app.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      return new { error = "No active Revit document." };
    }

    if (req?.ElementIds == null || req.ElementIds.Count == 0)
    {
      return new { error = "elementIds list is empty." };
    }

    var ids = req.ElementIds.Select(id => new ElementId(id)).ToList();
    uiDoc.Selection.SetElementIds(ids);

    if (req.Zoom && ids.Count > 0)
    {
      uiDoc.ShowElements(ids);
    }

    return new
    {
      success = true,
      selectedCount = ids.Count
    };
  }

  private object SetElementParameter(UIApplication app, SetParameterRequest? req)
  {
    var uiDoc = app.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      return new { error = "No active Revit document." };
    }

    if (req == null || req.ElementId <= 0 || string.IsNullOrWhiteSpace(req.ParameterName))
    {
      return new { error = "Invalid elementId or parameterName." };
    }

    var doc = uiDoc.Document;
    var elem = doc.GetElement(new ElementId(req.ElementId));
    if (elem == null)
    {
      return new { error = $"Element with ID {req.ElementId} not found." };
    }

    var param = elem.LookupParameter(req.ParameterName);
    if (param == null)
    {
      return new { error = $"Parameter '{req.ParameterName}' not found on element {req.ElementId}." };
    }

    if (param.IsReadOnly)
    {
      return new { error = $"Parameter '{req.ParameterName}' is read-only." };
    }

    using var tx = new Transaction(doc, $"MCP: Set {req.ParameterName}");
    tx.Start();

    var success = false;
    switch (param.StorageType)
    {
      case StorageType.String:
        success = param.Set(req.Value ?? "");
        break;
      case StorageType.Integer:
        if (int.TryParse(req.Value, out var iVal))
          success = param.Set(iVal);
        break;
      case StorageType.Double:
        if (double.TryParse(req.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal))
          success = param.Set(dVal);
        break;
      default:
        success = param.SetValueString(req.Value ?? "");
        break;
    }

    tx.Commit();

    return new
    {
      success,
      elementId = req.ElementId,
      parameterName = req.ParameterName,
      newValue = req.Value
    };
  }

  private object ExtractBoQ(UIApplication app)
  {
    var uiDoc = app.ActiveUIDocument;
    if (uiDoc == null || uiDoc.Document == null)
    {
      return new { error = "No active Revit document." };
    }

    var doc = uiDoc.Document;
    var items = RevitBoQExtractor.ExtractBoQ(doc);
    var summaries = RevitBoQExtractor.Summarize(items);

    return new
    {
      projectName = doc.ProjectInformation?.Name ?? doc.Title,
      modelTitle = doc.Title,
      totalElements = items.Count,
      summaries = summaries.Select(s => new
      {
        categoryCode = s.CategoryCode,
        categoryName = s.CategoryName,
        count = s.Count,
        volumeM3 = s.TotalVolumeM3,
        areaM2 = s.TotalAreaM2,
        rebarWeightKg = s.TotalWeightKg
      })
    };
  }

  private static BuiltInCategory? ParseBuiltInCategory(string name)
  {
    if (Enum.TryParse<BuiltInCategory>(name, true, out var bic))
      return bic;

    return name.ToLowerInvariant() switch
    {
      "columns" or "column" or "cot" => BuiltInCategory.OST_StructuralColumns,
      "beams" or "beam" or "dam" => BuiltInCategory.OST_StructuralFraming,
      "floors" or "floor" or "san" => BuiltInCategory.OST_Floors,
      "walls" or "wall" or "vach" => BuiltInCategory.OST_Walls,
      "foundations" or "foundation" or "mong" => BuiltInCategory.OST_StructuralFoundation,
      "stairs" or "stair" or "thang" => BuiltInCategory.OST_Stairs,
      "doors" or "door" or "cua" => BuiltInCategory.OST_Doors,
      "windows" or "window" or "cuaso" => BuiltInCategory.OST_Windows,
      "rebar" or "thep" => BuiltInCategory.OST_Rebar,
      "rooms" or "room" or "phong" => BuiltInCategory.OST_Rooms,
      _ => null
    };
  }

  private static async Task<T?> ReadRequestBodyJson<T>(HttpListenerRequest req) where T : class
  {
    if (!req.HasEntityBody) return null;
    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
    var json = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(json)) return null;

    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    return JsonSerializer.Deserialize<T>(json, opts);
  }

  public sealed class ElementQueryRequest
  {
    public string? Category { get; set; }
    public string? Level { get; set; }
    public string? Search { get; set; }
    public int Limit { get; set; } = 100;
  }

  public sealed class ElementSelectRequest
  {
    public List<long>? ElementIds { get; set; }
    public bool Zoom { get; set; } = true;
  }

  public sealed class SetParameterRequest
  {
    public long ElementId { get; set; }
    public string ParameterName { get; set; } = "";
    public string? Value { get; set; }
  }
}
