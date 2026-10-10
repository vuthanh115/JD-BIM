using System;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public interface ISchemaCreator
  {
    Schema CreateSchema(Type type);
  }
}