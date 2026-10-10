using Autodesk.Revit.DB.ExtensibleStorage;

namespace kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension
{
  public interface IEntityConverter
  {
    Entity Convert(IRevitEntity revitEntity);

    TRevitEntity Convert<TRevitEntity>(Entity entity)
      where TRevitEntity : class, IRevitEntity;
  }
}