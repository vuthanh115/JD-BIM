#nullable enable
using System;
using System.Globalization;

namespace kaka.Structure.Structural.App.Utils
{
  /// <summary>
  /// Defines helper strings / templates for Beam Rebar detail text.
  /// (Clean implementation: no obfuscator runtime init.)
  /// </summary>
  public static class BeamRebarDefine
  {
    // You can replace this by reading from resource (.resx) or config
    private static Func<string>? _provider;

    /// <summary>
    /// Allow external code to override how "Chi tiết dầm" text is produced.
    /// Example:
    /// BeamRebarDefine.SetProvider(() => MyResources.BEAM_REBAR_DETAIL);
    /// </summary>
    public static void SetProvider(Func<string> provider)
    {
      _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Reset provider back to default.
    /// </summary>
    public static void ResetProvider() => _provider = null;

    /// <summary>
    /// Returns the "Chi tiết dầm" text (Vietnamese).
    /// </summary>
    public static string GetChiTietDam()
    {
      // If caller injected a provider => use it
      if (_provider is not null)
        return SafeNormalize(_provider());

      // Default fallback text (edit to your desired output)
      // You can also make this multi-language if needed.
      return "Chi tiết dầm: thép trên, thép dưới, thép đai (mặc định).";
    }

    private static string SafeNormalize(string? s)
    {
      if (string.IsNullOrWhiteSpace(s))
        return string.Empty;

      // Normalize line endings, trim BOM, etc.
      s = s.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

      // Keep invariant formatting if needed
      return s;
    }
  }
}