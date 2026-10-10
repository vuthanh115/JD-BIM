#nullable enable
using kaka.Structure.Structural.App.Utils.Attributes;
using kaka.Structure.Structural.App.Utils.ExtensibleStorageExtension;

namespace kaka.Structure.Structural.App.Models.Column
{
  [Schema("579061E4-9439-4A5E-922E-A1E5463A4146", "ClosedTieSetting")]
  public class ClosedTieSetting : IRevitEntity
  {
    [Field]
    public int FirstIndex { get; set; }

    [Field]
    public int SecondIndex { get; set; }

    public ClosedTieSetting() { }

    public ClosedTieSetting(int firstIndex, int secondIndex)
    {
      FirstIndex = firstIndex;
      SecondIndex = secondIndex;
    }
  }
}