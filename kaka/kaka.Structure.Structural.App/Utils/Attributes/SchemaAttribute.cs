#nullable enable
using System;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace kaka.Structure.Structural.App.Utils.Attributes
{
  /// <summary>
  /// Attribute to define ExtensibleStorage schema metadata on a class.
  /// Example:
  /// [Schema("DD561A3F-2965-45D9-8CD8-7064F2908D6A", "ColumnSetting")]
  /// public class ColumnSetting { ... }
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  public sealed class SchemaAttribute : Attribute
  {
    private readonly string _schemaName;
    private readonly Guid _guid;

    public SchemaAttribute(string guid, string schemaName)
    {
      if (string.IsNullOrWhiteSpace(guid))
        throw new ArgumentException("GUID string is null/empty.", nameof(guid));
      if (string.IsNullOrWhiteSpace(schemaName))
        throw new ArgumentException("Schema name is null/empty.", nameof(schemaName));

      _guid = Guid.Parse(guid);
      _schemaName = schemaName;

      // sensible defaults (can be overridden via properties in attribute usage)
      ApplicationGUID = Guid.Empty;                 // optional
      Documentation = string.Empty;
      ReadAccessLevel = AccessLevel.Public;
      WriteAccessLevel = AccessLevel.Public;
      VendorId = string.Empty;
    }

    /// <summary>Schema name.</summary>
    public string SchemaName => _schemaName;

    /// <summary>Schema GUID.</summary>
    public Guid GUID => _guid;

    /// <summary>
    /// Application GUID (optional). Revit schema supports app GUID.
    /// </summary>
    public Guid ApplicationGUID { get; set; }

    /// <summary>Optional documentation.</summary>
    public string Documentation { get; set; } = string.Empty;

    /// <summary>Read access level (default: Public).</summary>
    public AccessLevel ReadAccessLevel { get; set; } = AccessLevel.Public;

    /// <summary>Write access level (default: Public).</summary>
    public AccessLevel WriteAccessLevel { get; set; } = AccessLevel.Public;

    /// <summary>Optional vendor id string.</summary>
    public string VendorId { get; set; } = string.Empty;
  }
}
