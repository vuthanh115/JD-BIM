#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace kaka.Structure.Structural.App.Utils
{
  public static class DocumentUtils
  {
    /// <summary>
    /// Run an action inside a Revit Transaction (with rollback on failure).
    /// </summary>
    public static void Transaction(this Document document, string transactionName, Action action)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (action is null) throw new ArgumentNullException(nameof(action));
      transactionName ??= "Transaction";

      // If already in a transaction, just execute (caller controls commit/rollback)
      if (document.IsModifiable)
      {
        action();
        return;
      }

      using var t = new Transaction(document, transactionName);
      var status = t.Start();
      if (status != TransactionStatus.Started)
        throw new InvalidOperationException($"Cannot start transaction '{transactionName}'. Status={status}");

      try
      {
        action();
        status = t.Commit();
        if (status != TransactionStatus.Committed)
          throw new InvalidOperationException($"Transaction '{transactionName}' was not committed. Status={status}");
      }
      catch
      {
        if (t.HasStarted())
          t.RollBack();
        throw;
      }
    }

    /// <summary>
    /// Find a DimensionType by name (case-insensitive). Returns null if not found.
    /// </summary>
    public static DimensionType? GetDimensionType(this Document document, string name)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (string.IsNullOrWhiteSpace(name)) return null;

      return new FilteredElementCollector(document)
        .OfClass(typeof(DimensionType))
        .Cast<DimensionType>()
        .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find a ViewFamilyType by name (case-insensitive). Returns null if not found.
    /// </summary>
    public static ViewFamilyType? GetViewFamilyType(this Document document, string name)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (string.IsNullOrWhiteSpace(name)) return null;

      return new FilteredElementCollector(document)
        .OfClass(typeof(ViewFamilyType))
        .Cast<ViewFamilyType>()
        .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all ViewFamilyType names (sorted).
    /// </summary>
    public static List<string> GetAllViewTypes(this Document document)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));

      return new FilteredElementCollector(document)
        .OfClass(typeof(ViewFamilyType))
        .Cast<ViewFamilyType>()
        .Select(x => x.Name)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Find a view template by name (case-insensitive). Returns null if not found.
    /// </summary>
    public static View? GetViewTemplate(this Document document, string name)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (string.IsNullOrWhiteSpace(name)) return null;

      // View templates are Views with IsTemplate == true
      return new FilteredElementCollector(document)
        .OfClass(typeof(View))
        .Cast<View>()
        .Where(v => v.IsTemplate)
        .FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find a Scope Box by name (case-insensitive). Returns null if not found.
    /// </summary>
    public static Element? GetScopeBox(this Document document, string name)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (string.IsNullOrWhiteSpace(name)) return null;

      // Scope Box category: OST_VolumeOfInterest
      return new FilteredElementCollector(document)
        .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
        .WhereElementIsNotElementType()
        .FirstOrDefault(e =>
        {
          var p = e.get_Parameter(BuiltInParameter.DATUM_TEXT);
          var n = p?.AsString() ?? e.Name; // some Revit versions store name here
          return string.Equals(n, name, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Find a Title Block type (FamilySymbol) by name (case-insensitive). Returns null if not found.
    /// </summary>
    public static FamilySymbol? GetTitleBlock(this Document document, string name)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      if (string.IsNullOrWhiteSpace(name)) return null;

      return new FilteredElementCollector(document)
        .OfCategory(BuiltInCategory.OST_TitleBlocks)
        .WhereElementIsElementType()
        .OfClass(typeof(FamilySymbol))
        .Cast<FamilySymbol>()
        .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)
                          || string.Equals($"{x.FamilyName}: {x.Name}", name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Make a unique ViewPlan (or generally View) name by adding suffix " (n)" if needed.
    /// </summary>
    public static string MakeUniqueViewPlanName(this Document document, string name)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      name ??= string.Empty;
      name = name.Trim();
      if (name.Length == 0) name = "View";

      // Collect all view names once
      var existing = new HashSet<string>(
        new FilteredElementCollector(document)
          .OfClass(typeof(View))
          .Cast<View>()
          .Select(v => v.Name)
          .Where(n => !string.IsNullOrWhiteSpace(n)),
        StringComparer.OrdinalIgnoreCase
      );

      if (!existing.Contains(name)) return name;

      for (var i = 2; i < 10000; i++)
      {
        var candidate = $"{name} ({i})";
        if (!existing.Contains(candidate))
          return candidate;
      }

      // Extremely unlikely, but safe fallback
      return $"{name} ({Guid.NewGuid():N})";
    }

    /// <summary>
    /// Make a unique sheet number like prefix + number.
    /// Returns (sheetNumber, nextNumberToUse).
    /// Example: ("S-001", 2) if prefix="S-" and number=1 and S-001 is free.
    /// </summary>
    public static (string sheetNumber, int nextNumber) MakeUniqueViewSheetNumber(
      this Document document,
      string prefix,
      int number)
    {
      if (document is null) throw new ArgumentNullException(nameof(document));
      prefix ??= string.Empty;
      if (number < 0) number = 0;

      // Existing sheet numbers
      var existing = new HashSet<string>(
        new FilteredElementCollector(document)
          .OfClass(typeof(ViewSheet))
          .Cast<ViewSheet>()
          .Select(s => s.SheetNumber)
          .Where(n => !string.IsNullOrWhiteSpace(n)),
        StringComparer.OrdinalIgnoreCase
      );

      // Keep simple formatting: if number < 1000 -> pad to 3 digits like 001, 010...
      for (var n = number; n < number + 100000; n++)
      {
        var numPart = n < 1000 ? n.ToString("D3") : n.ToString();
        var candidate = $"{prefix}{numPart}";
        if (!existing.Contains(candidate))
          return (candidate, n + 1);
      }

      // Fallback
      var fallback = $"{prefix}{Guid.NewGuid():N}".Substring(0, Math.Min(prefix.Length + 12, 64));
      return (fallback, number + 1);
    }
  }
}
