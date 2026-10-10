#nullable enable
using System;
using Autodesk.Revit.DB;

namespace kaka.Structure.Structural.App.Utils
{
  public class ElementModel
  {
    public ElementId? ElementId { get; set; }
    public Element? Element { get; set; }
    public string Name { get; set; } = string.Empty;

    public ElementModel() { }

    public ElementModel(Element element)
    {
      Element = element ?? throw new ArgumentNullException(nameof(element));
      ElementId = element.Id;
      Name = element.Name ?? string.Empty;
    }

    public ElementModel(ElementId elementId, Element? element = null, string? name = null)
    {
      ElementId = elementId;
      Element = element;
      Name = name ?? element?.Name ?? string.Empty;
    }

    public override string ToString()
      => $"{Name} ({ElementId?.Value.ToString() ?? "null"})";
  }
}