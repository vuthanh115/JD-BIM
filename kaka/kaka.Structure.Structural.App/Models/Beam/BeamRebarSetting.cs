#nullable enable
using System;
using Autodesk.Revit.DB.Structure;
using CommunityToolkit.Mvvm.ComponentModel;
using kaka.Structure.Core;

namespace kaka.Structure.Structural.App.Models.Beam
{
    public partial class BeamRebarSetting : ObservableObject
    {
        // 1. Thép gia cường lớp trên (Layer 1)
        [ObservableProperty]
        private bool _hasTopExtraL1 = true;

        [ObservableProperty]
        private int _topExtraL1Quantity = 1;

        [ObservableProperty]
        private RebarBarType? _topExtraL1Type;

        // 2. Thép chính lớp 1 (Top main)
        [ObservableProperty]
        private int _topMainQuantity = 2;

        [ObservableProperty]
        private RebarBarType? _topMainType;

        // 3. Thép gia cường top lớp 2
        [ObservableProperty]
        private bool _hasTopExtraL2 = false;

        [ObservableProperty]
        private int _topExtraL2Quantity = 2;

        [ObservableProperty]
        private RebarBarType? _topExtraL2Type;

        // 4. Thép gia cường bot lớp 2
        [ObservableProperty]
        private bool _hasBottomExtraL2 = false;

        [ObservableProperty]
        private int _bottomExtraL2Quantity = 2;

        [ObservableProperty]
        private RebarBarType? _bottomExtraL2Type;

        // 5. Thép đai (Stirrups)
        [ObservableProperty]
        private RebarBarType? _stirrupType;

        [ObservableProperty]
        private bool _isModeA1A2 = true;

        [ObservableProperty]
        private bool _isModeA1L = false;

        [ObservableProperty]
        private double _stirrupA1 = 100.0;

        [ObservableProperty]
        private double _stirrupA2 = 200.0;

        [ObservableProperty]
        private double _stirrupL = 1000.0;

        // 6. Thép chính lớp dưới (Bottom main)
        [ObservableProperty]
        private int _bottomMainQuantity = 2;

        [ObservableProperty]
        private RebarBarType? _bottomMainType;

        // 7. Thép gia cường lớp dưới (Layer 1 - Mid span)
        [ObservableProperty]
        private bool _hasBottomExtraL1 = true;

        [ObservableProperty]
        private int _bottomExtraL1Quantity = 1;

        [ObservableProperty]
        private RebarBarType? _bottomExtraL1Type;

        // Cốt giá sườn dầm (Side rebar)
        [ObservableProperty]
        private bool _hasSideRebar = false;

        [ObservableProperty]
        private int _sideRebarQuantity = 2;

        [ObservableProperty]
        private RebarBarType? _sideRebarType;

        // Thông số cấu tạo
        [ObservableProperty]
        private double _cover = 25.0; // mm

        [ObservableProperty]
        private bool _hasHooks = true;

        [ObservableProperty]
        private double _topHookLength = 250.0; // mm

        [ObservableProperty]
        private double _bottomHookLength = 250.0; // mm

        [ObservableProperty]
        private double _topExtraSpanRatio = 0.333333; // L/3

        [ObservableProperty]
        private double _bottomExtraStartRatio = 0.2; // L/5

        [ObservableProperty]
        private double _bottomExtraEndRatio = 0.8;   // L - L/5

        [ObservableProperty]
        private double _layerGap = 30.0; // mm

        public BeamRebarInputData ToInputData(double widthMm, double heightMm, double lengthMm)
        {
            var data = new BeamRebarInputData
            {
                BeamWidth = widthMm,
                BeamHeight = heightMm,
                BeamLength = lengthMm,
                Cover = Cover,

                // 1
                HasTopExtraL1 = HasTopExtraL1,
                TopExtraL1Quantity = TopExtraL1Quantity,
                TopExtraL1Diameter = GetBarDiameterMm(TopExtraL1Type, 16.0),
                TopExtraSpanRatio = TopExtraSpanRatio,

                // 2
                TopMainQuantity = TopMainQuantity,
                TopMainDiameter = GetBarDiameterMm(TopMainType, 16.0),

                // 3
                HasTopExtraL2 = HasTopExtraL2,
                TopExtraL2Quantity = TopExtraL2Quantity,
                TopExtraL2Diameter = GetBarDiameterMm(TopExtraL2Type, 18.0),

                // 4
                HasBottomExtraL2 = HasBottomExtraL2,
                BottomExtraL2Quantity = BottomExtraL2Quantity,
                BottomExtraL2Diameter = GetBarDiameterMm(BottomExtraL2Type, 18.0),

                // 5
                StirrupDiameter = GetBarDiameterMm(StirrupType, 8.0),
                StirrupMode = IsModeA1L ? StirrupDistributionMode.FixedLength : StirrupDistributionMode.ZoneRatio,
                StirrupSpacingA1 = StirrupA1,
                StirrupSpacingA2 = StirrupA2,
                StirrupFixedLength = StirrupL,

                // 6
                BottomMainQuantity = BottomMainQuantity,
                BottomMainDiameter = GetBarDiameterMm(BottomMainType, 16.0),

                // 7
                HasBottomExtraL1 = HasBottomExtraL1,
                BottomExtraL1Quantity = BottomExtraL1Quantity,
                BottomExtraL1Diameter = GetBarDiameterMm(BottomExtraL1Type, 16.0),
                BottomExtraStartRatio = BottomExtraStartRatio,
                BottomExtraEndRatio = BottomExtraEndRatio,

                // Side
                HasSideRebar = HasSideRebar,
                SideRebarQuantity = SideRebarQuantity,
                SideRebarDiameter = GetBarDiameterMm(SideRebarType, 12.0),

                // Hooks
                HasHooks = HasHooks,
                TopHookLength = TopHookLength,
                BottomHookLength = BottomHookLength
            };

            return data;
        }

        private static double GetBarDiameterMm(RebarBarType? barType, double defaultMm)
        {
            if (barType == null) return defaultMm;
            try
            {
                var p = barType.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.REBAR_BAR_DIAMETER);
                if (p != null && p.StorageType == Autodesk.Revit.DB.StorageType.Double)
                {
                    return Autodesk.Revit.DB.UnitUtils.ConvertFromInternalUnits(p.AsDouble(), Autodesk.Revit.DB.UnitTypeId.Millimeters);
                }
            }
            catch { }
            return defaultMm;
        }
    }
}
