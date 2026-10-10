using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;
using Xunit.Abstractions;
using kaka.Structure.Core;
using System.Linq;

namespace kaka.Structure.Tests
{
    public class ColumnRebarTests
    {
        private readonly ITestOutputHelper _output;
        public ColumnRebarTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public void InspectAssemblies()
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var dll in new[] { @"c:\Working\kaka\libs\2024\BimSpeedApp.dll", @"c:\Working\kaka\libs\2024\BimSpeedUtils.dll" })
            {
                if (!File.Exists(dll)) continue;
                lines.Add("=== DLL: " + dll + " ===");
                using var fs = File.OpenRead(dll);
                using var pe = new PEReader(fs);
                var mr = pe.GetMetadataReader();
                foreach (var th in mr.TypeDefinitions)
                {
                    var t = mr.GetTypeDefinition(th);
                    string name = mr.GetString(t.Name);
                    string ns = mr.GetString(t.Namespace);
                    if (name.Contains("Beam") || ns.Contains("Beam") || name.Contains("Rebar") || ns.Contains("Rebar"))
                    {
                        lines.Add($"{ns}.{name}");
                    }
                }
            }
            File.WriteAllLines(@"c:\Working\kaka\Mindmap\beam_types.txt", lines);
        }
        // Đặc điểm: Có chân bẻ (Lb) và đoạn chôn sâu (Hm) dưới Z=0
        // ---------------------------------------------------------
        [Fact]
        public void Test_Case1_Foundation_WithStarterBar()
        {
            // 1. INPUT
            var input = new RebarInputData
            {
                ColWidth = 500, FloorHeight = 3500, BarDiameter = 20, 
                LapLength = 800, Cover = 25,
                IsBottomColumn = true, // Cột móng
                IsTopColumn = false,
                
                // Cấu hình móng
                HasStarterBar = true,
                EmbedLength = 1000, // Chôn sâu 1m
                FootLength = 200    // Bẻ chân 200mm
            };

            // 2. TÍNH TOÁN
            var points = RebarCalculator.CalculateSingleBarPath(input, 0, 0, 0);

            // Bật lên để xem chân chữ L cắm xuống đất
            // RebarVisualizer.ShowModal(points, 20); 

            // 3. KIỂM TRA
            Assert.Equal(4, points.Count); // Chân Lb -> Góc -> Mặt sàn -> Đỉnh
            
            // P0: Mũi chân bẻ (Z = -1000)
            Assert.Equal(-1000, points[0].Z, 2);
            // Kiểm tra chân bẻ xòe ra ngoài (Outward: khoảng cách đến tâm lớn hơn góc ngàm)
            Assert.True(Math.Abs(points[0].X) > Math.Abs(points[1].X));

            // P1: Góc vuông dưới đất (Z = -1000, X = Chuẩn)
            Assert.Equal(-1000, points[1].Z, 2);
            
            // P2: Mặt sàn (Z = 0)
            Assert.Equal(0, points[2].Z, 2);

            // P3: Đỉnh cột vươn lên (3500 + 800 - 15mm an toàn chống va chạm uốn)
            double expectedTop = 3500 + 800 - 15.0;
            Assert.Equal(expectedTop, points[3].Z, 2);
        }

        [Fact]
        public void Test_Case1_Foundation_StarterBar_Inward()
        {
            var input = new RebarInputData
            {
                ColWidth = 500, FloorHeight = 3500, BarDiameter = 20, 
                LapLength = 800, Cover = 25,
                IsBottomColumn = true,
                IsTopColumn = false,
                HasStarterBar = true,
                EmbedLength = 1000,
                FootLength = 200,
                StarterBarOutward = false // Bẻ quặp vào trong
            };

            var points = RebarCalculator.CalculateSingleBarPath(input, 0, 0, 0);

            Assert.Equal(4, points.Count);
            // Khi bẻ vào trong (Inward): khoảng cách mũi chân đến tâm nhỏ hơn góc ngàm
            Assert.True(Math.Abs(points[0].X) < Math.Abs(points[1].X));
        }

        [Fact]
        public void Test_Case1_Foundation_StarterBar_Rightward()
        {
            var input = new RebarInputData
            {
                ColWidth = 200, ColDepth = 400, FloorHeight = 3500, BarDiameter = 20, 
                LapLength = 800, Cover = 25,
                IsBottomColumn = true,
                IsTopColumn = false,
                HasStarterBar = true,
                EmbedLength = 550,
                FootLength = 250,
                StarterBarRightward = true // Bẻ sang phải (như BimSpeed)
            };

            // Thanh bên trái (localX_Ratio = 0)
            var leftBarPoints = RebarCalculator.CalculateSingleBarPath(input, 0, 0.0, 0.5);
            // Thanh bên phải (localX_Ratio = 1)
            var rightBarPoints = RebarCalculator.CalculateSingleBarPath(input, 1, 1.0, 0.5);

            // Cả 2 thanh đều có 4 điểm
            Assert.Equal(4, leftBarPoints.Count);
            Assert.Equal(4, rightBarPoints.Count);

            // Cả 2 mũi chân đều hướng sang phải (+X so với góc ngàm)
            Assert.True(leftBarPoints[0].X > leftBarPoints[1].X);
            Assert.True(rightBarPoints[0].X > rightBarPoints[1].X);

            // Thanh bên trái vắt qua thanh bên phải nên được nâng lên tầng 2 (layerZOffset = 24mm)
            Assert.True(leftBarPoints[0].Z > rightBarPoints[0].Z);
            Assert.Equal(-550, rightBarPoints[0].Z, 2);
            Assert.Equal(-550 + 24, leftBarPoints[0].Z, 2);
        }

        // ---------------------------------------------------------
        // TRƯỜNG HỢP 2: CỘT GIỮA (Middle)
        // Đặc điểm: Chân bẻ cổ chai (Crank)
        // ---------------------------------------------------------
        [Fact]
        public void Test_Case2_Middle_ShouldCrankBottom()
        {
            var input = new RebarInputData
            {
                ColWidth = 500, ColDepth = 500, Cover = 25, 
                BarDiameter = 20, FloorHeight = 3500, LapLength = 800,
                IsBottomColumn = false, 
                IsTopColumn = false     
            };

            var points = RebarCalculator.CalculateSingleBarPath(input, 0, 0, 0);

            Assert.Equal(4, points.Count); 
            // Chân bẻ xong mới đi lên
            Assert.Equal(800, points[1].Z, 2); // Đoạn thẳng đầu tiên dài bằng LapLength
            Assert.NotEqual(points[0].X, points[3].X); // Chân lệch
        }

        // ---------------------------------------------------------
        // TRƯỜNG HỢP 3: CỘT MÁI (Top)
        // Đặc điểm: Chân bẻ, Đỉnh ngắt tại Cover
        // ---------------------------------------------------------
        [Fact]
        public void Test_Case3_Roof_WithTopHook()
        {
            var input = new RebarInputData
            {
                ColWidth = 500, ColDepth = 500, Cover = 25, 
                BarDiameter = 20, FloorHeight = 3500, LapLength = 800,
                IsBottomColumn = false,
                
                // CẤU HÌNH MÁI + MÓC
                IsTopColumn = true,      
                HasTopHook = true,       // Bật bẻ móc
                TopHookLength = 200      // Móc dài 200mm
            };

            var points = RebarCalculator.CalculateSingleBarPath(input, 0, 0, 0);

            //RebarVisualizer.ShowModal(points, 20); // Sẽ thấy móc ngang ở đỉnh

            // KIỂM TRA
            Assert.Equal(5, points.Count); // Chân lệch -> Góc -> Hết bẻ -> Đỉnh -> Mũi Móc

            // P3: Đỉnh cột (Góc bẻ)
            double expectedTopZ = 3500 - 25; 
            Assert.Equal(expectedTopZ, points[3].Z, 2);

            // P4: Mũi móc neo
            // Z vẫn giữ nguyên (bẻ ngang)
            Assert.Equal(expectedTopZ, points[4].Z, 2);
            
            // X thay đổi (bẻ vào trong) -> X của P4 phải khác X của P3
            Assert.NotEqual(points[3].X, points[4].X);
        }

        [Fact]
        public void Test_Case3_Roof_TopHook_BoundaryConstraint()
        {
            // Cột 200x400 (nhỏ hẹp), nhưng đặt móc dài 500mm
            var input = new RebarInputData
            {
                ColWidth = 200, ColDepth = 400, Cover = 25, 
                BarDiameter = 18, FloorHeight = 3500, LapLength = 720,
                IsBottomColumn = false,
                IsTopColumn = true,      
                HasTopHook = true,       
                TopHookLength = 500 // Cố tình đặt 500mm vượt quá chiều rộng cột (200mm)
            };

            double halfW = input.ColWidth / 2.0;
            double halfD = input.ColDepth / 2.0;

            // Kiểm tra cả 4 góc và các cạnh
            for (double rx = 0.0; rx <= 1.0; rx += 0.5)
            {
                for (double ry = 0.0; ry <= 1.0; ry += 0.33)
                {
                    var points = RebarCalculator.CalculateSingleBarPath(input, 0, rx, ry);
                    Assert.True(points.Count >= 4);
                    var pHook = points.Last();

                    // Mũi móc TUYỆT ĐỐI không được vượt ra ngoài mép cột trừ lớp bê tông bảo vệ!
                    Assert.True(pHook.X >= -halfW + input.Cover - 0.1, $"Hook X ({pHook.X}) breached left face!");
                    Assert.True(pHook.X <= halfW - input.Cover + 0.1, $"Hook X ({pHook.X}) breached right face!");
                    Assert.True(pHook.Y >= -halfD + input.Cover - 0.1, $"Hook Y ({pHook.Y}) breached front face!");
                    Assert.True(pHook.Y <= halfD - input.Cover + 0.1, $"Hook Y ({pHook.Y}) breached back face!");
                }
            }
        }
    }
}