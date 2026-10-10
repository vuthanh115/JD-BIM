#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using kaka.Structure.Structural.App.Utils.Attributes;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public sealed class FieldFactory : IFieldFactory
  {
    public void AddField(SchemaBuilder builder, PropertyInfo property)
    {
      var attr = property.GetCustomAttribute<FieldAttribute>();
      if (attr is null) return;

      var name = property.Name;
      var type = property.PropertyType;

      FieldBuilder fb;

      // IList<T>  -> Array field
      if (TryGetListElementType(type, out var elemType))
      {
        fb = builder.AddArrayField(name, elemType);
      }
      // IDictionary<K,V> -> Map field
      else if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
      {
        fb = builder.AddMapField(name, keyType, valueType);
      }
      else
      {
        fb = builder.AddSimpleField(name, type);
      }

      if (!string.IsNullOrWhiteSpace(attr.Documentation))
        fb.SetDocumentation(attr.Documentation);

#if REVIT2022_OR_GREATER
      // Optional: map SpecTypeId string -> ForgeTypeId for schema field.
      // Only enable if you define a symbol and want strict unit typing.
#endif
      // If you want this always-on (Revit 2024/2026):
      if (!string.IsNullOrWhiteSpace(attr.SpecTypeId))
      {
        try
        {
          fb.SetSpec(new ForgeTypeId(attr.SpecTypeId));
        }
        catch
        {
          // Ignore invalid SpecTypeId strings
        }
      }
    }

    private static bool TryGetListElementType(Type t, out Type elementType)
    {
      elementType = null!;

      if (t.IsArray)
      {
        elementType = t.GetElementType()!;
        return true;
      }

      if (t.IsGenericType)
      {
        var def = t.GetGenericTypeDefinition();
        if (def == typeof(System.Collections.Generic.IList<>) ||
            def == typeof(System.Collections.Generic.List<>))
        {
          elementType = t.GetGenericArguments()[0];
          return true;
        }
      }

      var iList = t.GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>));

      if (iList is not null)
      {
        elementType = iList.GetGenericArguments()[0];
        return true;
      }

      return false;
    }

    private static bool TryGetDictionaryTypes(Type t, out Type keyType, out Type valueType)
    {
      keyType = null!;
      valueType = null!;

      if (t.IsGenericType)
      {
        var def = t.GetGenericTypeDefinition();
        if (def == typeof(System.Collections.Generic.IDictionary<,>) ||
            def == typeof(System.Collections.Generic.Dictionary<,>))
        {
          var args = t.GetGenericArguments();
          keyType = args[0];
          valueType = args[1];
          return true;
        }
      }

      var iDict = t.GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>));

      if (iDict is not null)
      {
        var args = iDict.GetGenericArguments();
        keyType = args[0];
        valueType = args[1];
        return true;
      }

      return false;
    }
  }
}
