using System;
using System.IO;
using System.Text;

namespace kaka.Structure.Core
{
  public static class DebugLogger
  {
    private static readonly string LogPath =
      Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "Column.log"
      );

    public static void Clear()
    {
      try
      {
        if (File.Exists(LogPath))
          File.Delete(LogPath);
      }
      catch { }
    }

    public static void Log(string message)
    {
      try
      {
        File.AppendAllText(
          LogPath,
          $"{DateTime.Now:HH:mm:ss.fff} | {message}\r\n",
          Encoding.UTF8
        );
      }
      catch { }
    }
  }
}