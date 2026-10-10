using System.ComponentModel;

#if NETFRAMEWORK

// 1. Polyfill sửa lỗi MC1000: Tạo nhãn "giả" để trình biên dịch WPF 4.8 không báo lỗi
namespace System.Runtime.Versioning
{
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
  public sealed class TargetPlatformAttribute : Attribute
  {
    public TargetPlatformAttribute(string platformName) => PlatformName = platformName;
    public string PlatformName { get; }
  }

  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
  public sealed class SupportedOSPlatformAttribute : Attribute
  {
    public SupportedOSPlatformAttribute(string platformName) => PlatformName = platformName;
    public string PlatformName { get; }
  }
}

// 2. Polyfill cho record và init properties
namespace System.Runtime.CompilerServices
{
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static class IsExternalInit { }
}

// 3. Polyfill cho toán tử ^ và ..
namespace System
{
  public readonly struct Index : IEquatable<Index>
  {
    private readonly int _value;
    public Index(int value, bool fromEnd = false) { _value = fromEnd ? ~value : value; }
    public int Value => _value < 0 ? ~_value : _value;
    public bool IsFromEnd => _value < 0;
    public int GetOffset(int length) => IsFromEnd ? length - Value : Value;
    public bool Equals(Index other) => _value == other._value;
    public override bool Equals(object obj) => obj is Index other && Equals(other);
    public override int GetHashCode() => _value;
    public static implicit operator Index(int value) => new Index(value);
  }
}
#endif