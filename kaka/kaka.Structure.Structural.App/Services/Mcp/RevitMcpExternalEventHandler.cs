#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace kaka.Structure.Structural.App.Services.Mcp;

public sealed class RevitMcpExternalEventHandler : IExternalEventHandler
{
  private readonly ConcurrentQueue<McpTaskItem> _queue = new();
  private ExternalEvent? _externalEvent;

  public static RevitMcpExternalEventHandler Instance { get; } = new();

  private RevitMcpExternalEventHandler()
  {
  }

  public void Initialize()
  {
    if (_externalEvent == null)
    {
      _externalEvent = ExternalEvent.Create(this);
    }
  }

  public Task<object?> EnqueueTaskAsync(Func<UIApplication, object?> action)
  {
    if (_externalEvent == null)
    {
      throw new InvalidOperationException("RevitMcpExternalEventHandler is not initialized.");
    }

    var tcs = new TaskCompletionSource<object?>();
    var item = new McpTaskItem(action, tcs);
    _queue.Enqueue(item);
    _externalEvent.Raise();
    return tcs.Task;
  }

  public void Execute(UIApplication app)
  {
    while (_queue.TryDequeue(out var item))
    {
      try
      {
        var result = item.Action(app);
        item.Tcs.TrySetResult(result);
      }
      catch (Exception ex)
      {
        item.Tcs.TrySetException(ex);
      }
    }
  }

  public string GetName() => "RevitMcpExternalEventHandler";

  private sealed class McpTaskItem
  {
    public Func<UIApplication, object?> Action { get; }
    public TaskCompletionSource<object?> Tcs { get; }

    public McpTaskItem(Func<UIApplication, object?> action, TaskCompletionSource<object?> tcs)
    {
      Action = action;
      Tcs = tcs;
    }
  }
}
