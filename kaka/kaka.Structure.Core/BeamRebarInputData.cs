namespace kaka.Structure.Core
{
    public enum StirrupDistributionMode
    {
        ZoneRatio,  // A1 ở gối L/4, A2 ở bụng L/2
        FixedLength // A1 ở đoạn đầu dầm L (vd: 1000mm), A2 ở phần còn lại
    }

    public class BeamRebarInputData
    {
        // Kích thước hình học dầm (mm)
        public double BeamWidth { get; set; } = 300.0;
        public double BeamHeight { get; set; } = 600.0;
        public double BeamLength { get; set; } = 6000.0;
        public double Cover { get; set; } = 25.0;

        // 1. Thép gia cường lớp trên (Layer 1)
        public bool HasTopExtraL1 { get; set; } = true;
        public int TopExtraL1Quantity { get; set; } = 1;
        public double TopExtraL1Diameter { get; set; } = 16.0;
        public double TopExtraSpanRatio { get; set; } = 0.333333; // Mặc định L/3

        // 2. Thép chính lớp 1 (Top main continuous bars)
        public int TopMainQuantity { get; set; } = 2;
        public double TopMainDiameter { get; set; } = 16.0;

        // 3. Thép gia cường top lớp 2
        public bool HasTopExtraL2 { get; set; } = false;
        public int TopExtraL2Quantity { get; set; } = 2;
        public double TopExtraL2Diameter { get; set; } = 18.0;

        // 4. Thép gia cường bot lớp 2
        public bool HasBottomExtraL2 { get; set; } = false;
        public int BottomExtraL2Quantity { get; set; } = 2;
        public double BottomExtraL2Diameter { get; set; } = 18.0;

        // 5. Thép đai (Stirrups)
        public double StirrupDiameter { get; set; } = 8.0;
        public StirrupDistributionMode StirrupMode { get; set; } = StirrupDistributionMode.ZoneRatio;
        public double StirrupSpacingA1 { get; set; } = 100.0; // mm
        public double StirrupSpacingA2 { get; set; } = 200.0; // mm
        public double StirrupFixedLength { get; set; } = 1000.0; // mm (khi dùng FixedLength)
        public double StirrupEndOffset { get; set; } = 50.0; // Đai đầu tiên cách mép dầm 50mm

        // 6. Thép chính lớp dưới (Bottom main continuous bars)
        public int BottomMainQuantity { get; set; } = 2;
        public double BottomMainDiameter { get; set; } = 16.0;

        // 7. Thép gia cường lớp dưới (Layer 1 - Mid span)
        public bool HasBottomExtraL1 { get; set; } = true;
        public int BottomExtraL1Quantity { get; set; } = 1;
        public double BottomExtraL1Diameter { get; set; } = 16.0;
        public double BottomExtraStartRatio { get; set; } = 0.2; // Cách gối L/5
        public double BottomExtraEndRatio { get; set; } = 0.8;   // Đến L - L/5

        // Cốt giá sườn dầm (Side Rebars / Cốt cấu tạo)
        public bool HasSideRebar { get; set; } = false;
        public int SideRebarQuantity { get; set; } = 2;
        public double SideRebarDiameter { get; set; } = 12.0;

        // Móc neo (Hooks)
        public bool HasHooks { get; set; } = true;
        public double TopHookLength { get; set; } = 250.0; // mm
        public double BottomHookLength { get; set; } = 250.0; // mm

        // Khoảng hở giữa 2 lớp thép (Clear vertical gap between layers)
        public double LayerGap { get; set; } = 30.0; // mm
    }
}
