#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public static class RevitEntityElementExtension
  {
    private static readonly ISchemaCreator _schemaCreator = new SchemaCreator();

    public static Schema GetSchema(Type dataType)
    {
      if (dataType is null) throw new ArgumentNullException(nameof(dataType));
      return _schemaCreator.CreateSchema(dataType);
    }

    public static Schema? LookupSchema(Type dataType)
    {
      if (dataType is null) throw new ArgumentNullException(nameof(dataType));
      var schema = _schemaCreator.CreateSchema(dataType);
      return Schema.Lookup(schema.GUID);
    }

    public static void SetEntity(this Element element, IRevitEntity revitEntity)
    {
      if (element is null) throw new ArgumentNullException(nameof(element));
      if (revitEntity is null) throw new ArgumentNullException(nameof(revitEntity));

      var schema = GetSchema(revitEntity.GetType());
      var entity = new Entity(schema);

      // set fields bằng reflection + [Field]
      foreach (var prop in revitEntity.GetType().GetProperties())
      {
        var field = schema.GetField(prop.Name);
        if (field == null) continue;

        var value = prop.GetValue(revitEntity);
        if (value != null)
          entity.Set(field, value);
      }

      element.SetEntity(entity);
    }

    public static TRevitEntity? GetEntity<TRevitEntity>(this Element element)
      where TRevitEntity : class, IRevitEntity, new()
    {
      if (element is null) throw new ArgumentNullException(nameof(element));

      var schema = GetSchema(typeof(TRevitEntity));
      var entity = element.GetEntity(schema);

      if (!entity.IsValid())
        return null;

      var obj = new TRevitEntity();

      foreach (var prop in typeof(TRevitEntity).GetProperties())
      {
        var field = schema.GetField(prop.Name);
        if (field == null) continue;

        try
        {
          var value = entity.Get<object>(field);
          if (value != null && prop.CanWrite)
            prop.SetValue(obj, value);
        }
        catch
        {
          // ignore incompatible field
        }
      }

      return obj;
    }

    public static bool DeleteEntity<TRevitEntity>(this Element element)
      where TRevitEntity : class, IRevitEntity
    {
      if (element is null) throw new ArgumentNullException(nameof(element));

      var schema = GetSchema(typeof(TRevitEntity));
      var entity = element.GetEntity(schema);

      if (!entity.IsValid())
        return false;

      element.SetEntity(new Entity(schema));
      return true;
    }
  }
}
