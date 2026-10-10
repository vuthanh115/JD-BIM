#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public static class EntityExtension
  {
    public static void SetWrapper<T>(this Entity entity, Field field, IList<T> value)
    {
      if (entity is null) throw new ArgumentNullException(nameof(entity));
      if (field is null) throw new ArgumentNullException(nameof(field));
      if (value is null) throw new ArgumentNullException(nameof(value));

      // Revit API supports IList<T> for array fields (ArrayField)
      entity.Set(field, value);
    }

    public static void SetWrapper<T>(this Entity entity, Field field, IList<T> value, ForgeTypeId displayUnitType)
    {
      if (entity is null) throw new ArgumentNullException(nameof(entity));
      if (field is null) throw new ArgumentNullException(nameof(field));
      if (value is null) throw new ArgumentNullException(nameof(value));
      if (displayUnitType is null) throw new ArgumentNullException(nameof(displayUnitType));

      // For floating-point fields with units, Revit requires a unit type id (ForgeTypeId)
      entity.Set(field, value, displayUnitType);
    }

    public static void SetWrapper<TKey, TValue>(this Entity entity, Field field, IDictionary<TKey, TValue> value)
    {
      if (entity is null) throw new ArgumentNullException(nameof(entity));
      if (field is null) throw new ArgumentNullException(nameof(field));
      if (value is null) throw new ArgumentNullException(nameof(value));

      // Revit API supports IDictionary<TKey,TValue> for map fields (MapField)
      entity.Set(field, value);
    }

    public static void SetWrapper<TKey, TValue>(this Entity entity, Field field, IDictionary<TKey, TValue> value, ForgeTypeId displayUnitType)
    {
      if (entity is null) throw new ArgumentNullException(nameof(entity));
      if (field is null) throw new ArgumentNullException(nameof(field));
      if (value is null) throw new ArgumentNullException(nameof(value));
      if (displayUnitType is null) throw new ArgumentNullException(nameof(displayUnitType));

      entity.Set(field, value, displayUnitType);
    }
  }
}
