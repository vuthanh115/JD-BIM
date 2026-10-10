using System.Reflection;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public interface IFieldFactory
  {
    void AddField(SchemaBuilder builder, PropertyInfo property);
  }
}