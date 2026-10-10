#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace kaka.Structure.Structural.App.Models.Column
{
    public sealed class GeneralSetting : ObservableObject
    {
        // ======== 1. CẤU HÌNH MẶC ĐỊNH (DEFAULT VALUES) ========
        // Bạn nên đặt các số này giống thực tế để người dùng đỡ phải sửa nhiều

        private bool _isHeadLockByLength = false; // Mặc định dùng Factor (nhân đường kính)
        private double _headHookLength = 200.0 / 304.8;     // Nếu nhập tay thì mặc định 200mm
        private double _headHookFactor = 10.0;    // Mặc định móc đầu = 10d
        private bool _hasHeadLock = true;         // Có bẻ móc

        private bool _shouldSetLevelForRebar = true;     // Gán tham số Level
        private bool _shouldSetPartitionForRebar = true; // Gán tham số Partition

        private double _coefficientCrankLength = 10.0;      // Đoạn nhấn cổ chai (thường 1:6 hoặc 10d)
        private double _coefficientLapLength = 40.0;        // Chiều dài nối chồng (thường 30d - 40d)
        private double _coefficientAnchorageLength = 30.0;  // Chiều dài neo (thường 30d)

        private bool _shouldCrankRebarAtSplice = true; // Mặc định bật nhấn thép bẻ crank 1:6
        private bool _shouldGroupRebarSameNumber = true; // Gom nhóm Rebar Number
        private string _groupParameterName = "Partition"; // Tham số mặc định để gom nhóm

        private double _maxE = 450.0 / 304.8; // Vùng gia cường đai (thường H/4 hoặc Max(H/6, 450mm))
        private double _coefficientHdE = 2.0; // Hệ số khoảng cách đai vùng nối

        private bool _shouldSpliceAtBottom = true;   // Nối chân cột
        private double _offsetFromColumnBottom = 0;  // Nối ngay mặt sàn (hoặc nhập 0 để tự tính theo code)

        private double _coefficientDistanceBetweenTwoLaps = 1.5; // Khoảng cách giữa 2 vị trí nối so le (thường 1.5 Ld)
        private bool _shouldSpliceAtTwoPositions = true;        // Mặc định bật nối so le 50%

        private EnumFramingFloorSelectionType _framingFloorSelectionType = EnumFramingFloorSelectionType.Framing;

        private IList<Reference> _selectedFramingFloors = new List<Reference>();
        private double _framingFloorHeightDefault = 500.0 / 304.8; // Dầm mặc định cao 500mm

        // ======== Properties (Giữ nguyên logic Binding) ========

        public bool IsHeadLockByLength
        {
            get => _isHeadLockByLength;
            set => SetProperty(ref _isHeadLockByLength, value);
        }

        public double HeadHookLength
        {
            get => _headHookLength;
            set => SetProperty(ref _headHookLength, value);
        }

        public double HeadHookFactor
        {
            get => _headHookFactor;
            set => SetProperty(ref _headHookFactor, value);
        }

        public bool HasHeadLock
        {
            get => _hasHeadLock;
            set => SetProperty(ref _hasHeadLock, value);
        }

        public bool ShouldSetLevelForRebar
        {
            get => _shouldSetLevelForRebar;
            set => SetProperty(ref _shouldSetLevelForRebar, value);
        }

        public bool ShouldSetPartitionForRebar
        {
            get => _shouldSetPartitionForRebar;
            set => SetProperty(ref _shouldSetPartitionForRebar, value);
        }

        public double CoefficientCrankLength
        {
            get => _coefficientCrankLength;
            set => SetProperty(ref _coefficientCrankLength, value);
        }

        public double CoefficientLapLength
        {
            get => _coefficientLapLength;
            set => SetProperty(ref _coefficientLapLength, value);
        }

        public double CoefficientAnchorageLength
        {
            get => _coefficientAnchorageLength;
            set => SetProperty(ref _coefficientAnchorageLength, value);
        }

        public bool ShouldCrankRebarAtSplice
        {
            get => _shouldCrankRebarAtSplice;
            set => SetProperty(ref _shouldCrankRebarAtSplice, value);
        }

        public bool ShouldGroupRebarSameNumber
        {
            get => _shouldGroupRebarSameNumber;
            set => SetProperty(ref _shouldGroupRebarSameNumber, value);
        }

        public string GroupParameterName
        {
            get => _groupParameterName;
            set => SetProperty(ref _groupParameterName, value ?? string.Empty);
        }

        public double MaxE
        {
            get => _maxE;
            set => SetProperty(ref _maxE, value);
        }

        public double CoefficientHdE
        {
            get => _coefficientHdE;
            set => SetProperty(ref _coefficientHdE, value);
        }

        public bool ShouldSpliceAtBottom
        {
            get => _shouldSpliceAtBottom;
            set => SetProperty(ref _shouldSpliceAtBottom, value);
        }

        public double OffsetFromColumnBottom
        {
            get => _offsetFromColumnBottom;
            set => SetProperty(ref _offsetFromColumnBottom, value);
        }

        public double CoefficientDistanceBetweenTwoLaps
        {
            get => _coefficientDistanceBetweenTwoLaps;
            set => SetProperty(ref _coefficientDistanceBetweenTwoLaps, value);
        }

        public bool ShouldSpliceAtTwoPositions
        {
            get => _shouldSpliceAtTwoPositions;
            set => SetProperty(ref _shouldSpliceAtTwoPositions, value);
        }

        public EnumFramingFloorSelectionType FramingFloorSelectionType
        {
            get => _framingFloorSelectionType;
            set
            {
                if (SetProperty(ref _framingFloorSelectionType, value))
                {
                    OnFramingFloorSelectionTypeChanged(value);
                }
            }
        }

        /// <summary>
        /// Runtime only – do not persist.
        /// </summary>
        public IList<Reference> SelectedFramingFloors
        {
            get => _selectedFramingFloors;
            set => SetProperty(ref _selectedFramingFloors, value ?? new List<Reference>());
        }

        public double FramingFloorHeightDefault
        {
            get => _framingFloorHeightDefault;
            set => SetProperty(ref _framingFloorHeightDefault, value);
        }

        // ======== Hooks ========

        private void OnFramingFloorSelectionTypeChanged(EnumFramingFloorSelectionType newValue)
        {
            SelectedFramingFloors = new List<Reference>();
        }

        // ======== Persistence (Giữ nguyên) ========

        public void Save()
        {
            try
            {
                var dto = GeneralSettingDto.From(this);
                var path = GetSettingFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { /* Ignore error logs */ }
        }

        public static GeneralSetting Create()
        {
            var path = GetSettingFilePath();
            if (!File.Exists(path)) return new GeneralSetting();

            try
            {
                var json = File.ReadAllText(path);
                var dto = JsonSerializer.Deserialize<GeneralSettingDto>(json);
                return dto?.ToSetting() ?? new GeneralSetting();
            }
            catch
            {
                return new GeneralSetting();
            }
        }

        private static string GetSettingFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "kaka.Structure.Structural.App");
            return Path.Combine(dir, "GeneralSetting.json");
        }

        // ======== DTO for JSON (Giữ nguyên cấu trúc) ========
        private sealed class GeneralSettingDto
        {
            public bool IsHeadLockByLength { get; set; }
            public double HeadHookLength { get; set; }
            public double HeadHookFactor { get; set; }
            public bool HasHeadLock { get; set; }
            public bool ShouldSetLevelForRebar { get; set; }
            public bool ShouldSetPartitionForRebar { get; set; }
            public double CoefficientCrankLength { get; set; }
            public double CoefficientLapLength { get; set; }
            public double CoefficientAnchorageLength { get; set; }
            public bool ShouldCrankRebarAtSplice { get; set; }
            public bool ShouldGroupRebarSameNumber { get; set; }
            public string GroupParameterName { get; set; } = string.Empty;
            public double MaxE { get; set; }
            public double CoefficientHdE { get; set; }
            public bool ShouldSpliceAtBottom { get; set; }
            public double OffsetFromColumnBottom { get; set; }
            public double CoefficientDistanceBetweenTwoLaps { get; set; }
            public bool ShouldSpliceAtTwoPositions { get; set; }
            public EnumFramingFloorSelectionType FramingFloorSelectionType { get; set; }
            public double FramingFloorHeightDefault { get; set; }

            public static GeneralSettingDto From(GeneralSetting s) => new()
            {
                IsHeadLockByLength = s.IsHeadLockByLength,
                HeadHookLength = s.HeadHookLength,
                HeadHookFactor = s.HeadHookFactor,
                HasHeadLock = s.HasHeadLock,
                ShouldSetLevelForRebar = s.ShouldSetLevelForRebar,
                ShouldSetPartitionForRebar = s.ShouldSetPartitionForRebar,
                CoefficientCrankLength = s.CoefficientCrankLength,
                CoefficientLapLength = s.CoefficientLapLength,
                CoefficientAnchorageLength = s.CoefficientAnchorageLength,
                ShouldCrankRebarAtSplice = s.ShouldCrankRebarAtSplice,
                ShouldGroupRebarSameNumber = s.ShouldGroupRebarSameNumber,
                GroupParameterName = s.GroupParameterName,
                MaxE = s.MaxE,
                CoefficientHdE = s.CoefficientHdE,
                ShouldSpliceAtBottom = s.ShouldSpliceAtBottom,
                OffsetFromColumnBottom = s.OffsetFromColumnBottom,
                CoefficientDistanceBetweenTwoLaps = s.CoefficientDistanceBetweenTwoLaps,
                ShouldSpliceAtTwoPositions = s.ShouldSpliceAtTwoPositions,
                FramingFloorSelectionType = s.FramingFloorSelectionType,
                FramingFloorHeightDefault = s.FramingFloorHeightDefault
            };

            public GeneralSetting ToSetting()
            {
                return new GeneralSetting
                {
                    IsHeadLockByLength = IsHeadLockByLength,
                    HeadHookLength = HeadHookLength,
                    HeadHookFactor = HeadHookFactor,
                    HasHeadLock = HasHeadLock,
                    ShouldSetLevelForRebar = ShouldSetLevelForRebar,
                    ShouldSetPartitionForRebar = ShouldSetPartitionForRebar,
                    CoefficientCrankLength = CoefficientCrankLength,
                    CoefficientLapLength = CoefficientLapLength,
                    CoefficientAnchorageLength = CoefficientAnchorageLength,
                    ShouldCrankRebarAtSplice = ShouldCrankRebarAtSplice,
                    ShouldGroupRebarSameNumber = ShouldGroupRebarSameNumber,
                    GroupParameterName = GroupParameterName ?? "Partition", // Fallback
                    MaxE = MaxE,
                    CoefficientHdE = CoefficientHdE,
                    ShouldSpliceAtBottom = ShouldSpliceAtBottom,
                    OffsetFromColumnBottom = OffsetFromColumnBottom,
                    CoefficientDistanceBetweenTwoLaps = CoefficientDistanceBetweenTwoLaps,
                    ShouldSpliceAtTwoPositions = ShouldSpliceAtTwoPositions,
                    FramingFloorSelectionType = FramingFloorSelectionType,
                    FramingFloorHeightDefault = FramingFloorHeightDefault,
                    SelectedFramingFloors = new List<Reference>()
                };
            }
        }
    }
}