using CommunityToolkit.Mvvm.ComponentModel;
using kaka.Structure.Structural.App.Models.Column;

namespace kaka.Structure.Structural.App.ViewModels.Column
{
  // Class đại diện cho 1 dòng trong bảng Apply
  public partial class ApplyLevelItem : ObservableObject
  {
    // Checkbox: Áp dụng thép chủ?
    [ObservableProperty] private bool isApplyMain = true; 

    // Checkbox: Áp dụng thép đai?
    [ObservableProperty] private bool isApplyStirrup = true;

    // Dữ liệu cột đích (Cột sẽ nhận dữ liệu)
    public ColumnModel TargetColumn { get; }

    // Các thông tin hiển thị ra bảng (Chỉ đọc) để binding
    public string Dimension => TargetColumn.SectionSizeText;
    public string BaseLevel => TargetColumn.BaseLevelText;
    public string TopLevel => TargetColumn.TopLevelText;

    // Trạng thái (để disable dòng chính nó nếu cần)
    public bool IsEnabled => true; 

    public ApplyLevelItem(ColumnModel targetColumn)
    {
      TargetColumn = targetColumn;
    }
  }
}