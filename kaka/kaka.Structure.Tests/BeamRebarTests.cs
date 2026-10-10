using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using kaka.Structure.Core;

namespace kaka.Structure.Tests
{
    public class BeamRebarTests
    {
        private readonly ITestOutputHelper _output;

        public BeamRebarTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test_Beam_TopAndBottom_MainBars_Geometry()
        {
            var input = new BeamRebarInputData
            {
                BeamWidth = 300,
                BeamHeight = 600,
                BeamLength = 6000,
                Cover = 25,
                StirrupDiameter = 8,
                TopMainQuantity = 3,
                TopMainDiameter = 18,
                BottomMainQuantity = 3,
                BottomMainDiameter = 20,
                HasHooks = true,
                TopHookLength = 250,
                BottomHookLength = 250
            };

            var topBars = BeamRebarCalculator.CalculateTopMainBars(input);
            Assert.Equal(3, topBars.Count);

            foreach (var bar in topBars)
            {
                Assert.Equal(4, bar.Points.Count);
                // Hook at start goes down
                Assert.True(bar.Points[0].Z < bar.Points[1].Z);
                // Top run is horizontal
                Assert.Equal(bar.Points[1].Z, bar.Points[2].Z);
                // Hook at end goes down
                Assert.True(bar.Points[3].Z < bar.Points[2].Z);
                _output.WriteLine($"TopBar {bar.Label}: Y={bar.Points[1].Y:F1}, Z={bar.Points[1].Z:F1}, HookStart={bar.Points[0].Z:F1}");
            }

            var botBars = BeamRebarCalculator.CalculateBottomMainBars(input);
            Assert.Equal(3, botBars.Count);

            foreach (var bar in botBars)
            {
                Assert.Equal(4, bar.Points.Count);
                // Hook at start goes up
                Assert.True(bar.Points[0].Z > bar.Points[1].Z);
                // Bottom run is horizontal
                Assert.Equal(bar.Points[1].Z, bar.Points[2].Z);
                // Hook at end goes up
                Assert.True(bar.Points[3].Z > bar.Points[2].Z);
                _output.WriteLine($"BottomBar {bar.Label}: Y={bar.Points[1].Y:F1}, Z={bar.Points[1].Z:F1}, HookStart={bar.Points[0].Z:F1}");
            }
        }

        [Fact]
        public void Test_Beam_TopExtraBars_SupportSpanRatio()
        {
            var input = new BeamRebarInputData
            {
                BeamWidth = 300,
                BeamHeight = 600,
                BeamLength = 6000,
                Cover = 25,
                StirrupDiameter = 8,
                TopExtraL1Quantity = 2,
                TopExtraL1Diameter = 16,
                TopExtraSpanRatio = 1.0 / 3.0, // 2000mm
                HasHooks = true,
                TopHookLength = 200
            };

            var extraBars = BeamRebarCalculator.CalculateTopExtraL1Bars(input);
            // 2 bars at start support + 2 bars at end support = 4 bars
            Assert.Equal(4, extraBars.Count);

            var startBars = extraBars.Where(b => b.Label.Contains("Start")).ToList();
            var endBars = extraBars.Where(b => b.Label.Contains("End")).ToList();

            Assert.Equal(2, startBars.Count);
            Assert.Equal(2, endBars.Count);

            // Verify start support bar length ~ 2000mm
            foreach (var bar in startBars)
            {
                double runLen = bar.Points[2].X - bar.Points[1].X;
                Assert.InRange(runLen, 1999.0, 2001.0);
                _output.WriteLine($"Start extra bar length: {runLen:F1}mm");
            }

            // Verify end support bar length ~ 2000mm
            foreach (var bar in endBars)
            {
                double runLen = bar.Points[1].X - bar.Points[0].X;
                Assert.InRange(runLen, 1999.0, 2001.0);
                _output.WriteLine($"End extra bar length: {runLen:F1}mm");
            }
        }

        [Fact]
        public void Test_Beam_BottomExtraBars_SpanMidRatio()
        {
            var input = new BeamRebarInputData
            {
                BeamWidth = 300,
                BeamHeight = 600,
                BeamLength = 6000,
                BottomExtraL1Quantity = 1,
                BottomExtraL1Diameter = 18,
                BottomExtraStartRatio = 0.2, // 1200mm
                BottomExtraEndRatio = 0.8    // 4800mm -> Length = 3600mm
            };

            var botExtra = BeamRebarCalculator.CalculateBottomExtraL1Bars(input);
            Assert.Single(botExtra);

            var bar = botExtra[0];
            Assert.Equal(2, bar.Points.Count);
            Assert.Equal(1200.0, bar.Points[0].X);
            Assert.Equal(4800.0, bar.Points[1].X);
            _output.WriteLine($"Mid extra bar: Start={bar.Points[0].X}mm, End={bar.Points[1].X}mm");
        }

        [Fact]
        public void Test_Beam_StirrupSpacing_Distribution()
        {
            var input = new BeamRebarInputData
            {
                BeamWidth = 300,
                BeamHeight = 600,
                BeamLength = 6000,
                Cover = 25,
                StirrupDiameter = 8,
                StirrupMode = StirrupDistributionMode.ZoneRatio, // L/4 = 1500mm
                StirrupSpacingA1 = 100,
                StirrupSpacingA2 = 200,
                StirrupEndOffset = 50
            };

            var stirrups = BeamRebarCalculator.CalculateStirrups(input);
            Assert.True(stirrups.Count > 25);

            // First stirrup at 50mm
            Assert.Equal(50.0, stirrups[0].Points[0].X);

            // All stirrups must be closed rectangular loops (5 points)
            foreach (var s in stirrups)
            {
                Assert.True(s.IsClosedLoop);
                Assert.Equal(5, s.Points.Count);
                // First point equals last point
                Assert.Equal(s.Points[0].Y, s.Points[4].Y);
                Assert.Equal(s.Points[0].Z, s.Points[4].Z);
            }

            _output.WriteLine($"Generated {stirrups.Count} stirrups for 6m beam.");
        }

        [Fact]
        public void Test_Beam_SideRebar_WhenHeightExceeds700()
        {
            var input = new BeamRebarInputData
            {
                BeamWidth = 300,
                BeamHeight = 800, // h = 800mm >= 700mm
                BeamLength = 5000,
                Cover = 25,
                SideRebarQuantity = 2,
                SideRebarDiameter = 12
            };

            var allBars = BeamRebarCalculator.CalculateAllBars(input);
            var sideBars = allBars.Where(b => b.Label.StartsWith("SideRebar")).ToList();

            Assert.Equal(2, sideBars.Count);
            // Height at midpoint
            foreach (var bar in sideBars)
            {
                _output.WriteLine($"SideBar: {bar.Label}, Z={bar.Points[0].Z:F1}");
                Assert.InRange(bar.Points[0].Z, 350.0, 450.0);
            }
        }
    }
}
