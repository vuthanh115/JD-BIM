using System.ComponentModel; // Nếu dùng Description

namespace kaka.Structure.Structural.App.Models.Column
{
  public enum EnumDistributionType
  {
    // Bạn có thể thêm Description nếu muốn hiển thị text đẹp hơn
    [Description("L1")]
    L1 = 0,

    [Description("L1, L2")]
    L1_L2 = 1,

    [Description("L1, L2, L1")]
    L1_L2_L1 = 2,
        
    // Thêm các kiểu khác nếu cần
    // [Description("L1, L2, L3")]
    // L1_L2_L3 = 3 
  }
}