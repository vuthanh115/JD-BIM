#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace kaka.Structure.Structural.App.Utils
{
  public static class TagUtils2
  {
    /// <summary>
    /// Create an IndependentTag for a Rebar in a specific view.
    /// Note: tagId is the tag family symbol id (tag type).
    /// </summary>
    public static IndependentTag CreateIndependentTag(
      ElementId tagId,
      ElementId viewId,
      Rebar rebar,
      bool addLeader,
      TagOrientation orientation,
      XYZ point)
    {
      if (tagId is null) throw new ArgumentNullException(nameof(tagId));
      if (viewId is null) throw new ArgumentNullException(nameof(viewId));
      if (rebar is null) throw new ArgumentNullException(nameof(rebar));
      if (point is null) throw new ArgumentNullException(nameof(point));

      var doc = rebar.Document ?? throw new InvalidOperationException("Rebar has no Document.");

      var view = doc.GetElement(viewId) as View
                 ?? throw new ArgumentException("viewId is not a View.", nameof(viewId));

      // Rebar lives in model; tagging uses Reference / LinkReference
      // Use a simple Reference to the rebar element.
      var reference = new Reference(rebar);

      IndependentTag tag;

      // Revit API differences: there are multiple overloads across versions.
      // We'll try the common one first, then fallback.
      try
      {
        // Common overload (many versions):
        // IndependentTag.Create(Document, ElementId, Reference, bool, TagMode, TagOrientation, XYZ)
        tag = IndependentTag.Create(
          doc,
          view.Id,
          reference,
          addLeader,
          TagMode.TM_ADDBY_CATEGORY,
          orientation,
          point);
      }
      catch
      {
        // Fallback overload (some versions):
        // IndependentTag.Create(Document, ElementId, ElementId, Reference, bool, TagOrientation, XYZ)
        // (tagTypeId included explicitly)
        tag = IndependentTag.Create(
          doc,
          tagId,
          view.Id,
          reference,
          addLeader,
          orientation,
          point);
      }

      // Ensure tag type (tagId) is applied (some overloads ignore it / default type)
      try
      {
        if (tag.GetTypeId() != tagId)
          tag.ChangeTypeId(tagId);
      }
      catch
      {
        // Ignore if API/version doesn't allow or tag already correct
      }

      // Leader setting (some overloads set it; still force to be safe)
      try
      {
        tag.HasLeader = addLeader;
      }
      catch
      {
        // ignore if not supported
      }

      return tag;
    }
  }
}
