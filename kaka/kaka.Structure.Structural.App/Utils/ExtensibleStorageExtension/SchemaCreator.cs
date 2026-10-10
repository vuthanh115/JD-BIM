#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB.ExtensibleStorage;
using kaka.Structure.Structural.App.Utils.Attributes;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public sealed class SchemaCreator : ISchemaCreator
  {
    private readonly IFieldFactory _fieldFactory;

    public SchemaCreator() : this(new FieldFactory()) { }

    public SchemaCreator(IFieldFactory fieldFactory)
    {
      _fieldFactory = fieldFactory ?? throw new ArgumentNullException(nameof(fieldFactory));
    }

    public Schema CreateSchema(Type type)
    {
      if (type is null) throw new ArgumentNullException(nameof(type));

      var schemaAttr = type.GetCustomAttribute<SchemaAttribute>();
      if (schemaAttr is null)
        throw new InvalidOperationException($"{type.FullName} is missing [Schema] attribute.");

      // If schema exists already -> return it
      var existing = Schema.Lookup(schemaAttr.GUID);
      if (existing is not null)
        return existing;

      var builder = new SchemaBuilder(schemaAttr.GUID);

      builder.SetSchemaName(schemaAttr.SchemaName);

      if (schemaAttr.ApplicationGUID != Guid.Empty)
        builder.SetApplicationGUID(schemaAttr.ApplicationGUID);

      if (!string.IsNullOrWhiteSpace(schemaAttr.Documentation))
        builder.SetDocumentation(schemaAttr.Documentation);

      builder.SetReadAccessLevel(schemaAttr.ReadAccessLevel);
      builder.SetWriteAccessLevel(schemaAttr.WriteAccessLevel);

      if (!string.IsNullOrWhiteSpace(schemaAttr.VendorId))
        builder.SetVendorId(schemaAttr.VendorId);

      // Add fields based on [Field] properties
      var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(p => p.CanRead && p.CanWrite)
        .Where(p => p.GetCustomAttribute<FieldAttribute>() is not null);

      foreach (var p in props)
        _fieldFactory.AddField(builder, p);

      return builder.Finish();
    }
  }
}
