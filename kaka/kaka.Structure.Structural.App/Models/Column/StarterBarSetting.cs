#nullable enable
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Autodesk.Revit.DB;

namespace kaka.Structure.Structural.App.Models.Column
{
    public sealed partial class StarterBarSetting : ObservableObject
    {
        // --- CONSTRUCTOR: Gán giá trị mặc định ngay khi tạo mới ---
        public StarterBarSetting()
        {
            // 1. Cài đặt các thông số mặc định (Fix lỗi hiển thị số 0)
            HasStarterBar = true;
            ShouldSeparateSegment = false;
            
            HasStirrupSpacing = true; // Để mặc định là True để thấy số (hoặc false tùy bạn)
            StirrupSpacing = MmToFt(300); // 300mm
            
            HmValue = 0; // Mặc định 0 (bẻ chân tại đáy cột/cổ cột), tự tính Hm nếu cột ngồi trên đỉnh móng
            LbValue = MmToFt(250); // 250mm
            AutoDetectFoundation = true; // Mặc định tự động nhận diện móng bên dưới

            // 2. Reset trạng thái hướng (Mặc định bẻ sang phải theo phương cạnh cột như BimSpeed)
            Rightward = true;
            Outward = false;
            Leftward = false;
            Backward = false;
            Frontward = false;

            // 3. Cập nhật ảnh mũi tên ngay lập tức
            UpdateDirectionImages();
        }

        // Helper đổi mm -> feet (Dành cho Revit 2026+)
        private static double MmToFt(double mm)
        {
            return UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
        }

        // Helper tạo đường dẫn ảnh
        private string GetImageUri(string imageName)
        {
            return $"/kaka.Structure.Structural.App;component/Resources/Images/Column/{imageName}";
        }

        public void Save() { }

        // Hàm này giờ chỉ cần gọi constructor là đủ
        public static StarterBarSetting Create()
        {
            return new StarterBarSetting();
        }

        // ====== Properties ======
        [ObservableProperty] private bool hasStarterBar;
        [ObservableProperty] private bool shouldSeparateSegment;
        [ObservableProperty] private bool hasStirrupSpacing;
        [ObservableProperty] private double stirrupSpacing;
        [ObservableProperty] private double hmValue;
        [ObservableProperty] private double lbValue;
        [ObservableProperty] private bool autoDetectFoundation = true;

        [ObservableProperty] private bool outward;
        [ObservableProperty] private bool leftward;
        [ObservableProperty] private bool rightward;
        [ObservableProperty] private bool backward;
        [ObservableProperty] private bool frontward;

        public bool IsCenterActive => Outward && !Leftward && !Rightward && !Backward && !Frontward;

        [ObservableProperty] private string leftwardImage = "";
        [ObservableProperty] private string rightwardImage = "";
        [ObservableProperty] private string backwardImage = "";
        [ObservableProperty] private string frontwardImage = "";

        // ====== COMMANDS ======
        [RelayCommand] private void LeftwardClick()
        {
            Leftward = true;
            Rightward = false;
            Backward = false;
            Frontward = false;
            Outward = false;
            OnPropertyChanged(nameof(IsCenterActive));
        }

        [RelayCommand] private void RightwardClick()
        {
            Rightward = true;
            Leftward = false;
            Backward = false;
            Frontward = false;
            Outward = false;
            OnPropertyChanged(nameof(IsCenterActive));
        }

        [RelayCommand] private void BackwardClick()
        {
            Backward = true;
            Leftward = false;
            Rightward = false;
            Frontward = false;
            Outward = false;
            OnPropertyChanged(nameof(IsCenterActive));
        }

        [RelayCommand] private void FrontwardClick()
        {
            Frontward = true;
            Leftward = false;
            Rightward = false;
            Backward = false;
            Outward = false;
            OnPropertyChanged(nameof(IsCenterActive));
        }

        [RelayCommand] private void InwardOutwardClick() 
        { 
            Outward = !Outward; 
            Leftward = false; 
            Rightward = false; 
            Backward = false; 
            Frontward = false; 
            OnPropertyChanged(nameof(IsCenterActive));
        }

        // ====== Logic Change Handlers ======
        partial void OnLeftwardChanged(bool value) { if(value) { Rightward = false; Backward = false; Frontward = false; Outward = false; } OnPropertyChanged(nameof(IsCenterActive)); UpdateDirectionImages(); }
        partial void OnRightwardChanged(bool value) { if(value) { Leftward = false; Backward = false; Frontward = false; Outward = false; } OnPropertyChanged(nameof(IsCenterActive)); UpdateDirectionImages(); }
        partial void OnBackwardChanged(bool value) { if(value) { Leftward = false; Rightward = false; Frontward = false; Outward = false; } OnPropertyChanged(nameof(IsCenterActive)); UpdateDirectionImages(); }
        partial void OnFrontwardChanged(bool value) { if(value) { Leftward = false; Rightward = false; Backward = false; Outward = false; } OnPropertyChanged(nameof(IsCenterActive)); UpdateDirectionImages(); }
        partial void OnOutwardChanged(bool value) { OnPropertyChanged(nameof(IsCenterActive)); UpdateDirectionImages(); }

        private void UpdateDirectionImages()
        {
            LeftwardImage  = GetImageUri("left.png"); 
            RightwardImage = GetImageUri("right.png");
            BackwardImage  = GetImageUri("up.png");
            FrontwardImage = GetImageUri("down.png");
        }
    }
}