#nullable enable
using kaka.Structure.Structural.App.Utils.Attributes;
using kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension;

namespace kaka.Structure.Structural.App.Models.Column
{
  [Schema("28540DBC-0CA5-4CF0-B12B-9715727BB041", "SingleTieSetting")]
  public class SingleTieSetting : IRevitEntity
  {
    [Field]
    public int FirstIndex { get; set; }

    [Field]
    public int SecondIndex { get; set; }

    public SingleTieSetting() { }

    public SingleTieSetting(int firstIndex, int secondIndex)
    {
      FirstIndex = firstIndex;
      SecondIndex = secondIndex;
    }
  }
}