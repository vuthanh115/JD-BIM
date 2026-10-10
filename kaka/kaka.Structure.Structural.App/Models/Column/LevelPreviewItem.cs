#nullable enable
namespace kaka.Structure.Structural.App.Models.Column ;

public sealed class LevelPreviewItem
{
  public string Name { get; }
  public double HeightMm { get; }   // optional/legacy
  public string Note { get; }
  public double Z0Mm { get; }
  public double Z1Mm { get; }

  public double MinZMm => (Z0Mm < Z1Mm) ? Z0Mm : Z1Mm;
  public double MaxZMm => (Z0Mm > Z1Mm) ? Z0Mm : Z1Mm;

  // ctor mới (đầy đủ)
  public LevelPreviewItem(string name, double heightMm, string note, double z0Mm, double z1Mm)
  {
    Name = name ?? string.Empty;
    HeightMm = heightMm;
    Note = note ?? string.Empty;
    Z0Mm = z0Mm;
    Z1Mm = z1Mm;
  }

  // overload tương thích: (name, height, z0, z1) -> note=""
  public LevelPreviewItem(string name, double heightMm, double z0Mm, double z1Mm)
    : this(name, heightMm, note: string.Empty, z0Mm, z1Mm)
  {
  }

  // overload legacy (khuyên không dùng nữa):
  public LevelPreviewItem(string name, double heightMm)
    : this(name, heightMm, note: string.Empty, z0Mm: 0, z1Mm: heightMm)
  {
  }

  // ✅ chuẩn: Level mark (Elevation). Z0=Z1=elevation
  public static LevelPreviewItem FromLevelElevation(string name, double elevationMm, string note = "")
    => new LevelPreviewItem(name, heightMm: 0, note, z0Mm: elevationMm, z1Mm: elevationMm);
}