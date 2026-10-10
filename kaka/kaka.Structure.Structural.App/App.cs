using System;
using Autodesk.Revit.UI;
using kaka.Structure.Structural.App.Ribbon;

namespace kaka.Structure.Structural.App ;

public sealed class App : IExternalApplication
{
  public Result OnStartup(UIControlledApplication application)
  {
    try
    {
      AppDomain.CurrentDomain.AssemblyResolve += ResolveAddinAssemblies;
      RibbonBuilder.Build(application);

      try
      {
        kaka.Structure.Structural.App.Services.Mcp.RevitMcpHttpServer.Instance.Start(8080);
      }
      catch
      {
        // ignore
      }

      return Result.Succeeded;
    }
    catch (Exception ex)
    {
      TaskDialog.Show("kaka.Structure", ex.ToString());
      return Result.Failed;
    }
  }

  private static System.Reflection.Assembly? ResolveAddinAssemblies(object? sender, ResolveEventArgs args)
  {
    try
    {
      var folder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      if (string.IsNullOrEmpty(folder)) return null;

      var assemblyName = new System.Reflection.AssemblyName(args.Name).Name;
      if (string.IsNullOrEmpty(assemblyName)) return null;

      var targetPath = System.IO.Path.Combine(folder, assemblyName + ".dll");
      if (System.IO.File.Exists(targetPath))
      {
        return System.Reflection.Assembly.LoadFrom(targetPath);
      }
    }
    catch
    {
      // ignore
    }
    return null;
  }

  public Result OnShutdown(UIControlledApplication application)
  {
    try
    {
      kaka.Structure.Structural.App.Services.Mcp.RevitMcpHttpServer.Instance.Stop();
    }
    catch
    {
      // ignore
    }
    return Result.Succeeded;
  }
}