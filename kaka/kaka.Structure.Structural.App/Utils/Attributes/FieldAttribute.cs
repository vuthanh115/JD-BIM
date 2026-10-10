#nullable enable
using System;

namespace kaka.Structure.Structural.App.Utils.Attributes
{
  /// <summary>
  /// Marks a property as a field in ExtensibleStorage schema mapping.
  /// Optional metadata: Documentation, SpecTypeId.
  /// </summary>
  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  public sealed class FieldAttribute : Attribute
  {
    /// <summary>
    /// Optional documentation/description for the field.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Optional spec type id (e.g. ForgeTypeId string) for units/spec in Revit.
    /// Store as string to keep it version-agnostic.
    /// </summary>
    public string? SpecTypeId { get; set; }

    public FieldAttribute() { }

    public FieldAttribute(string? documentation = null, string? specTypeId = null)
    {
      Documentation = documentation;
      SpecTypeId = specTypeId;
    }
  }
}