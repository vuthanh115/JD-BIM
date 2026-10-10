#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB; 
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace kaka.Structure.Structural.App.Models.Column
{
    public sealed class StirrupSetting : ObservableObject
    {
        // ===== Helper đổi đơn vị (Dành riêng cho Revit 2026+) =====
        // Revit 2026 sử dụng UnitTypeId thay vì DisplayUnitType cũ
        private static double MmToFt(double mm)
        {
            return UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
        }

        // ==========================================================
        // BACKING FIELDS (GIÁ TRỊ MẶC ĐỊNH THEO HÌNH ẢNH)
        // ==========================================================

        // Hình: Chọn "Rải tới đáy dầm" (Bottom). 
        // Trong XAML nút Bottom dùng ConverterBoolReverse, nên Property = false là đúng.
        private bool _shouldSpreadToTopFramingFloor = false; 

        // Hình: Chọn "Bố trí theo khoảng cách" -> true
        private bool _isSeismicStirrupSpacing = true; 
        
        // Hình: 100 mm
        private double _seismicStirrupSpacing = MmToFt(100); 
        
        // Hình: 3 (ô nhập liệu bị mờ)
        private int _seismicStirrupQuantity = 3;

        // Hình: Bắt đầu rải cách mép 1 đoạn: 50 mm
        private double _startingOffset = MmToFt(50);

        // Hình: A1 = 100 mm
        private double _a1Value = MmToFt(100);
        
        // Hình: A2 = 200 mm
        private double _a2Value = MmToFt(200);

        // Hình: Giá trị tối thiểu (Checked) -> true
        private bool _hasL1Minimum = true;
        
        // Hình: 500 mm
        private double _l1Minimum = MmToFt(500);

        // Hình: Chiều cao thông thủy cột chia cho (Checked) -> true
        private bool _hasDivisionFactor = true;
        
        // Hình: 4
        private int _divisionFactor = 4;

        // Hình: Chiều cao tiết diện cột (Checked) -> true
        private bool _hasColumnSectionHeight = true;

        private RebarHookType? _mainStirrupHookType;
        private int _mainStirrupHookTypeId;

        // Hình: Bố trí toàn bộ khoảng cách đai bổ sung theo đai chính (Checked) -> true
        private bool _additionalStirrupSpacingFollowingMain = true;
        
        // Hình: 200 mm (bị mờ)
        private double _additionalStirrupSpacing = MmToFt(200);

        private RebarHookType? _additionalStirrupHookType;
        private int _additionalStirrupHookTypeId;

        // ==========================================================
        // PROPERTIES
        // ==========================================================

        public bool ShouldSpreadToTopFramingFloor
        {
            get => _shouldSpreadToTopFramingFloor;
            set => SetProperty(ref _shouldSpreadToTopFramingFloor, value);
        }

        public bool IsSeismicStirrupSpacing 
        {
            get => _isSeismicStirrupSpacing;
            set => SetProperty(ref _isSeismicStirrupSpacing, value);
        }

        public double SeismicStirrupSpacing
        {
            get => _seismicStirrupSpacing;
            set => SetProperty(ref _seismicStirrupSpacing, value);
        }

        public int SeismicStirrupQuantity
        {
            get => _seismicStirrupQuantity;
            set => SetProperty(ref _seismicStirrupQuantity, value);
        }

        public double StartingOffset
        {
            get => _startingOffset;
            set => SetProperty(ref _startingOffset, value);
        }

        public double A1Value
        {
            get => _a1Value;
            set => SetProperty(ref _a1Value, value);
        }

        public double A2Value
        {
            get => _a2Value;
            set => SetProperty(ref _a2Value, value);
        }

        public bool HasL1Minimum
        {
            get => _hasL1Minimum;
            set => SetProperty(ref _hasL1Minimum, value);
        }

        public double L1Minimum
        {
            get => _l1Minimum;
            set => SetProperty(ref _l1Minimum, value);
        }

        public bool HasDivisionFactor
        {
            get => _hasDivisionFactor;
            set => SetProperty(ref _hasDivisionFactor, value);
        }

        public int DivisionFactor
        {
            get => _divisionFactor;
            set
            {
                var v = Math.Max(1, value); // Đảm bảo không chia cho 0 hoặc số âm
                SetProperty(ref _divisionFactor, v);
            }
        }

        public bool HasColumnSectionHeight
        {
            get => _hasColumnSectionHeight;
            set => SetProperty(ref _hasColumnSectionHeight, value);
        }

        [JsonIgnore]
        public RebarHookType? MainStirrupHookType
        {
            get => _mainStirrupHookType;
            set
            {
                if (SetProperty(ref _mainStirrupHookType, value))
                {
                    MainStirrupHookTypeId = (int)(value?.Id.Value ?? 0);
                }
            }
        }

        public int MainStirrupHookTypeId
        {
            get => _mainStirrupHookTypeId;
            set => SetProperty(ref _mainStirrupHookTypeId, value);
        }

        public bool AdditionalStirrupSpacingFollowingMain
        {
            get => _additionalStirrupSpacingFollowingMain;
            set => SetProperty(ref _additionalStirrupSpacingFollowingMain, value);
        }

        public double AdditionalStirrupSpacing
        {
            get => _additionalStirrupSpacing;
            set => SetProperty(ref _additionalStirrupSpacing, value);
        }

        [JsonIgnore]
        public RebarHookType? AdditionalStirrupHookType
        {
            get => _additionalStirrupHookType;
            set
            {
                if (SetProperty(ref _additionalStirrupHookType, value))
                {
                    AdditionalStirrupHookTypeId = (int)(value?.Id.Value ?? 0);
                }
            }
        }

        public int AdditionalStirrupHookTypeId
        {
            get => _additionalStirrupHookTypeId;
            set => SetProperty(ref _additionalStirrupHookTypeId, value);
        }

        // ==========================================================
        // PERSISTENCE (LƯU/ĐỌC FILE JSON)
        // ==========================================================

        public void Save()
        {
            var dto = StirrupSettingDto.From(this);
            var path = GetSettingFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static StirrupSetting Create(List<RebarHookType> stirrupHookTypes)
        {
            var path = GetSettingFilePath();
            StirrupSetting setting;

            if (!File.Exists(path))
            {
                setting = new StirrupSetting(); // Dùng giá trị mặc định đã khai báo ở trên
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var dto = JsonSerializer.Deserialize<StirrupSettingDto>(json);
                    setting = dto?.ToSetting() ?? new StirrupSetting();
                }
                catch
                {
                    setting = new StirrupSetting();
                }
            }

            setting.ResolveHookTypes(stirrupHookTypes);
            return setting;
        }

        public StirrupSetting() { }

        private void ResolveHookTypes(List<RebarHookType> hookTypes)
        {
            if (hookTypes == null || hookTypes.Count == 0) return;
            
            MainStirrupHookType = FindHookTypeById(hookTypes, MainStirrupHookTypeId) 
                                  ?? hookTypes.FirstOrDefault();
            
            AdditionalStirrupHookType = FindHookTypeById(hookTypes, AdditionalStirrupHookTypeId) 
                                        ?? MainStirrupHookType;
        }

        private static RebarHookType? FindHookTypeById(List<RebarHookType> hookTypes, int id)
        {
            if (id == 0) return null;
            return hookTypes.FirstOrDefault(x => x.Id.Value == id);
        }

        private static string GetSettingFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "kaka.Structure.Structural.App");
            return Path.Combine(dir, "StirrupSetting.json");
        }

        // ==========================================================
        // DTO (DATA TRANSFER OBJECT)
        // ==========================================================
        private sealed class StirrupSettingDto
        {
            public bool ShouldSpreadToTopFramingFloor { get; set; }
            public bool IsSeismicStirrupSpacing { get; set; }
            public double SeismicStirrupSpacing { get; set; }
            public int SeismicStirrupQuantity { get; set; }
            public double StartingOffset { get; set; }
            public double A1Value { get; set; }
            public double A2Value { get; set; }
            public bool HasL1Minimum { get; set; }
            public double L1Minimum { get; set; }
            public bool HasDivisionFactor { get; set; }
            public int DivisionFactor { get; set; }
            public bool HasColumnSectionHeight { get; set; }
            public int MainStirrupHookTypeId { get; set; }
            public bool AdditionalStirrupSpacingFollowingMain { get; set; }
            public double AdditionalStirrupSpacing { get; set; }
            public int AdditionalStirrupHookTypeId { get; set; }

            public static StirrupSettingDto From(StirrupSetting s) => new()
            {
                ShouldSpreadToTopFramingFloor = s.ShouldSpreadToTopFramingFloor,
                IsSeismicStirrupSpacing = s.IsSeismicStirrupSpacing,
                SeismicStirrupSpacing = s.SeismicStirrupSpacing,
                SeismicStirrupQuantity = s.SeismicStirrupQuantity,
                StartingOffset = s.StartingOffset,
                A1Value = s.A1Value,
                A2Value = s.A2Value,
                HasL1Minimum = s.HasL1Minimum,
                L1Minimum = s.L1Minimum,
                HasDivisionFactor = s.HasDivisionFactor,
                DivisionFactor = s.DivisionFactor,
                HasColumnSectionHeight = s.HasColumnSectionHeight,
                MainStirrupHookTypeId = s.MainStirrupHookTypeId,
                AdditionalStirrupSpacingFollowingMain = s.AdditionalStirrupSpacingFollowingMain,
                AdditionalStirrupSpacing = s.AdditionalStirrupSpacing,
                AdditionalStirrupHookTypeId = s.AdditionalStirrupHookTypeId
            };

            public StirrupSetting ToSetting()
            {
                return new StirrupSetting
                {
                    ShouldSpreadToTopFramingFloor = ShouldSpreadToTopFramingFloor,
                    IsSeismicStirrupSpacing = IsSeismicStirrupSpacing,
                    SeismicStirrupSpacing = SeismicStirrupSpacing,
                    SeismicStirrupQuantity = SeismicStirrupQuantity,
                    StartingOffset = StartingOffset,
                    A1Value = A1Value,
                    A2Value = A2Value,
                    HasL1Minimum = HasL1Minimum,
                    L1Minimum = L1Minimum,
                    HasDivisionFactor = HasDivisionFactor,
                    DivisionFactor = Math.Max(1, DivisionFactor),
                    HasColumnSectionHeight = HasColumnSectionHeight,
                    MainStirrupHookTypeId = MainStirrupHookTypeId,
                    AdditionalStirrupSpacingFollowingMain = AdditionalStirrupSpacingFollowingMain,
                    AdditionalStirrupSpacing = AdditionalStirrupSpacing,
                    AdditionalStirrupHookTypeId = AdditionalStirrupHookTypeId
                };
            }
        }
    }
}