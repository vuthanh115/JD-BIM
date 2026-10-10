#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace kaka.Structure.Core
{
    public class CalculatedBar
    {
        public string Label { get; set; } = string.Empty;
        public double Diameter { get; set; }
        public bool IsClosedLoop { get; set; }
        public List<Point3D> Points { get; set; } = new();
    }

    public static class BeamRebarCalculator
    {
        public static List<CalculatedBar> CalculateAllBars(BeamRebarInputData input)
        {
            var result = new List<CalculatedBar>();

            // 1. Thép chính lớp trên (Top main continuous bars)
            result.AddRange(CalculateTopMainBars(input));

            // 2. Thép gia cường lớp trên lớp 1 (Top extra layer 1 at supports)
            if (input.HasTopExtraL1 && input.TopExtraL1Quantity > 0)
            {
                result.AddRange(CalculateTopExtraL1Bars(input));
            }

            // 3. Thép gia cường lớp trên lớp 2 (Top extra layer 2 at supports)
            if (input.HasTopExtraL2 && input.TopExtraL2Quantity > 0)
            {
                result.AddRange(CalculateTopExtraL2Bars(input));
            }

            // 4. Thép chính lớp dưới (Bottom main continuous bars)
            result.AddRange(CalculateBottomMainBars(input));

            // 5. Thép gia cường lớp dưới lớp 1 (Bottom extra layer 1 at mid-span)
            if (input.HasBottomExtraL1 && input.BottomExtraL1Quantity > 0)
            {
                result.AddRange(CalculateBottomExtraL1Bars(input));
            }

            // 6. Thép gia cường lớp dưới lớp 2 (Bottom extra layer 2 at mid-span)
            if (input.HasBottomExtraL2 && input.BottomExtraL2Quantity > 0)
            {
                result.AddRange(CalculateBottomExtraL2Bars(input));
            }

            // 7. Thép giá sườn dầm (Side rebars)
            bool autoSide = input.HasSideRebar || (input.BeamHeight >= 700.0);
            if (autoSide && input.SideRebarQuantity > 0)
            {
                result.AddRange(CalculateSideBars(input));
            }

            // 8. Thép đai (Stirrups)
            result.AddRange(CalculateStirrups(input));

            return result;
        }

        public static List<CalculatedBar> CalculateTopMainBars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = Math.Max(2, input.TopMainQuantity);
            double dia = input.TopMainDiameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double h = input.BeamHeight;
            double L = input.BeamLength;

            double yMin = cover + sDia + dia / 2.0;
            double yMax = b - (cover + sDia + dia / 2.0);
            if (yMax < yMin) { yMin = b / 2.0; yMax = b / 2.0; }

            double zTop = h - (cover + sDia + dia / 2.0);
            double xStart = cover + sDia;
            double xEnd = L - (cover + sDia);

            double maxHook = Math.Max(50.0, zTop - (cover + sDia + 20.0));
            double hookLen = input.HasHooks ? Math.Min(input.TopHookLength, maxHook) : 0;

            for (int i = 0; i < n; i++)
            {
                double y = (n > 1) ? yMin + i * (yMax - yMin) / (n - 1) : b / 2.0;
                var pts = new List<Point3D>();

                if (hookLen > 0)
                {
                    pts.Add(new Point3D(xStart, y, zTop - hookLen)); // Chân móc trái đi xuống
                }
                pts.Add(new Point3D(xStart, y, zTop));
                pts.Add(new Point3D(xEnd, y, zTop));
                if (hookLen > 0)
                {
                    pts.Add(new Point3D(xEnd, y, zTop - hookLen));   // Chân móc phải đi xuống
                }

                bars.Add(new CalculatedBar
                {
                    Label = $"TopMain_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = pts
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateTopExtraL1Bars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = input.TopExtraL1Quantity;
            if (n <= 0) return bars;

            double dia = input.TopExtraL1Diameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double h = input.BeamHeight;
            double L = input.BeamLength;

            double yMin = cover + sDia + dia / 2.0;
            double yMax = b - (cover + sDia + dia / 2.0);
            if (yMax < yMin) { yMin = b / 2.0; yMax = b / 2.0; }

            double zTop = h - (cover + sDia + dia / 2.0);
            double xStart = cover + sDia;
            double xEnd = L - (cover + sDia);

            double spanRatio = (input.TopExtraSpanRatio > 0 && input.TopExtraSpanRatio < 0.5) 
                ? input.TopExtraSpanRatio 
                : (1.0 / 3.0);
            double extraLen = L * spanRatio;

            double maxHook = Math.Max(50.0, zTop - (cover + sDia + 20.0));
            double hookLen = input.HasHooks ? Math.Min(input.TopHookLength, maxHook) : 0;

            for (int i = 0; i < n; i++)
            {
                // Bố trí xen kẽ đều giữa 2 thanh góc
                double y = yMin + (i + 1) * (yMax - yMin) / (n + 1);

                // Gối trái (Start support)
                var ptsLeft = new List<Point3D>();
                if (hookLen > 0)
                {
                    ptsLeft.Add(new Point3D(xStart, y, zTop - hookLen));
                }
                ptsLeft.Add(new Point3D(xStart, y, zTop));
                ptsLeft.Add(new Point3D(xStart + extraLen, y, zTop));

                bars.Add(new CalculatedBar
                {
                    Label = $"TopExtraL1_Start_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = ptsLeft
                });

                // Gối phải (End support)
                var ptsRight = new List<Point3D>();
                ptsRight.Add(new Point3D(xEnd - extraLen, y, zTop));
                ptsRight.Add(new Point3D(xEnd, y, zTop));
                if (hookLen > 0)
                {
                    ptsRight.Add(new Point3D(xEnd, y, zTop - hookLen));
                }

                bars.Add(new CalculatedBar
                {
                    Label = $"TopExtraL1_End_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = ptsRight
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateTopExtraL2Bars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = input.TopExtraL2Quantity;
            if (n <= 0) return bars;

            double dia = input.TopExtraL2Diameter;
            double mainDia = input.TopMainDiameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double h = input.BeamHeight;
            double L = input.BeamLength;

            double yMin = cover + sDia + dia / 2.0;
            double yMax = b - (cover + sDia + dia / 2.0);
            if (yMax < yMin) { yMin = b / 2.0; yMax = b / 2.0; }

            // Lớp 2 nằm dưới lớp 1 một khoảng LayerGap + bán kính
            double zL1 = h - (cover + sDia + mainDia / 2.0);
            double zL2 = zL1 - (mainDia / 2.0 + input.LayerGap + dia / 2.0);

            double xStart = cover + sDia;
            double xEnd = L - (cover + sDia);
            double spanRatio = (input.TopExtraSpanRatio > 0 && input.TopExtraSpanRatio < 0.5) 
                ? input.TopExtraSpanRatio 
                : (1.0 / 3.0);
            double extraLen = L * spanRatio;

            for (int i = 0; i < n; i++)
            {
                double y = (n > 1) ? yMin + i * (yMax - yMin) / (n - 1) : b / 2.0;

                // Thép lớp 2 thường là thanh thẳng
                bars.Add(new CalculatedBar
                {
                    Label = $"TopExtraL2_Start_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = new List<Point3D>
                    {
                        new Point3D(xStart, y, zL2),
                        new Point3D(xStart + extraLen, y, zL2)
                    }
                });

                bars.Add(new CalculatedBar
                {
                    Label = $"TopExtraL2_End_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = new List<Point3D>
                    {
                        new Point3D(xEnd - extraLen, y, zL2),
                        new Point3D(xEnd, y, zL2)
                    }
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateBottomMainBars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = Math.Max(2, input.BottomMainQuantity);
            double dia = input.BottomMainDiameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double h = input.BeamHeight;
            double L = input.BeamLength;

            double yMin = cover + sDia + dia / 2.0;
            double yMax = b - (cover + sDia + dia / 2.0);
            if (yMax < yMin) { yMin = b / 2.0; yMax = b / 2.0; }

            double zBot = cover + sDia + dia / 2.0;
            double xStart = cover + sDia;
            double xEnd = L - (cover + sDia);

            double maxHook = Math.Max(50.0, h - zBot - (cover + sDia + 20.0));
            double hookLen = input.HasHooks ? Math.Min(input.BottomHookLength, maxHook) : 0;

            for (int i = 0; i < n; i++)
            {
                double y = (n > 1) ? yMin + i * (yMax - yMin) / (n - 1) : b / 2.0;
                var pts = new List<Point3D>();

                if (hookLen > 0)
                {
                    pts.Add(new Point3D(xStart, y, zBot + hookLen)); // Chân móc trái đi lên
                }
                pts.Add(new Point3D(xStart, y, zBot));
                pts.Add(new Point3D(xEnd, y, zBot));
                if (hookLen > 0)
                {
                    pts.Add(new Point3D(xEnd, y, zBot + hookLen));   // Chân móc phải đi lên
                }

                bars.Add(new CalculatedBar
                {
                    Label = $"BottomMain_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = pts
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateBottomExtraL1Bars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = input.BottomExtraL1Quantity;
            if (n <= 0) return bars;

            double dia = input.BottomExtraL1Diameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double L = input.BeamLength;

            double yMin = cover + sDia + dia / 2.0;
            double yMax = b - (cover + sDia + dia / 2.0);
            if (yMax < yMin) { yMin = b / 2.0; yMax = b / 2.0; }

            double zBot = cover + sDia + dia / 2.0;
            double startRatio = input.BottomExtraStartRatio > 0 ? input.BottomExtraStartRatio : 0.2;
            double endRatio = input.BottomExtraEndRatio < 1.0 ? input.BottomExtraEndRatio : 0.8;

            double xStart = L * startRatio;
            double xEnd = L * endRatio;

            for (int i = 0; i < n; i++)
            {
                double y = yMin + (i + 1) * (yMax - yMin) / (n + 1);
                bars.Add(new CalculatedBar
                {
                    Label = $"BottomExtraL1_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = new List<Point3D>
                    {
                        new Point3D(xStart, y, zBot),
                        new Point3D(xEnd, y, zBot)
                    }
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateBottomExtraL2Bars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = input.BottomExtraL2Quantity;
            if (n <= 0) return bars;

            double dia = input.BottomExtraL2Diameter;
            double mainDia = input.BottomMainDiameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double L = input.BeamLength;

            double yMin = cover + sDia + dia / 2.0;
            double yMax = b - (cover + sDia + dia / 2.0);
            if (yMax < yMin) { yMin = b / 2.0; yMax = b / 2.0; }

            double zL1 = cover + sDia + mainDia / 2.0;
            double zL2 = zL1 + (mainDia / 2.0 + input.LayerGap + dia / 2.0);

            double startRatio = input.BottomExtraStartRatio > 0 ? input.BottomExtraStartRatio : 0.2;
            double endRatio = input.BottomExtraEndRatio < 1.0 ? input.BottomExtraEndRatio : 0.8;
            double xStart = L * startRatio;
            double xEnd = L * endRatio;

            for (int i = 0; i < n; i++)
            {
                double y = (n > 1) ? yMin + i * (yMax - yMin) / (n - 1) : b / 2.0;
                bars.Add(new CalculatedBar
                {
                    Label = $"BottomExtraL2_{i}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = new List<Point3D>
                    {
                        new Point3D(xStart, y, zL2),
                        new Point3D(xEnd, y, zL2)
                    }
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateSideBars(BeamRebarInputData input)
        {
            var bars = new List<CalculatedBar>();
            int n = Math.Max(2, input.SideRebarQuantity);
            double dia = input.SideRebarDiameter;
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double h = input.BeamHeight;
            double L = input.BeamLength;

            double yLeft = cover + sDia + dia / 2.0;
            double yRight = b - (cover + sDia + dia / 2.0);

            double xStart = cover + sDia;
            double xEnd = L - (cover + sDia);

            // Phân bổ đều theo chiều cao giữa thép trên và thép dưới
            int pairs = n / 2;
            double zBot = cover + sDia + 30.0;
            double zTop = h - (cover + sDia + 30.0);
            double gapZ = (pairs > 0) ? (zTop - zBot) / (pairs + 1) : 0;

            for (int p = 0; p < pairs; p++)
            {
                double z = zBot + (p + 1) * gapZ;

                bars.Add(new CalculatedBar
                {
                    Label = $"SideRebar_Left_{p}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = new List<Point3D>
                    {
                        new Point3D(xStart, yLeft, z),
                        new Point3D(xEnd, yLeft, z)
                    }
                });

                bars.Add(new CalculatedBar
                {
                    Label = $"SideRebar_Right_{p}",
                    Diameter = dia,
                    IsClosedLoop = false,
                    Points = new List<Point3D>
                    {
                        new Point3D(xStart, yRight, z),
                        new Point3D(xEnd, yRight, z)
                    }
                });
            }

            return bars;
        }

        public static List<CalculatedBar> CalculateStirrups(BeamRebarInputData input)
        {
            var stirrups = new List<CalculatedBar>();
            double sDia = input.StirrupDiameter;
            double cover = input.Cover;
            double b = input.BeamWidth;
            double h = input.BeamHeight;
            double L = input.BeamLength;

            double y0 = cover + sDia / 2.0;
            double y1 = b - (cover + sDia / 2.0);
            double z0 = cover + sDia / 2.0;
            double z1 = h - (cover + sDia / 2.0);

            var xPositions = GetStirrupXPositions(input);

            for (int i = 0; i < xPositions.Count; i++)
            {
                double x = xPositions[i];
                var loop = new List<Point3D>
                {
                    new Point3D(x, y0, z0),
                    new Point3D(x, y1, z0),
                    new Point3D(x, y1, z1),
                    new Point3D(x, y0, z1),
                    new Point3D(x, y0, z0) // Khép kín vòng đai
                };

                stirrups.Add(new CalculatedBar
                {
                    Label = $"Stirrup_{i}",
                    Diameter = sDia,
                    IsClosedLoop = true,
                    Points = loop
                });
            }

            return stirrups;
        }

        public static List<double> GetStirrupXPositions(BeamRebarInputData input)
        {
            var positions = new List<double>();
            double L = input.BeamLength;
            double startX = Math.Max(30.0, input.StirrupEndOffset);
            double endX = L - startX;
            if (endX <= startX) return positions;

            double a1 = Math.Max(50.0, input.StirrupSpacingA1);
            double a2 = Math.Max(50.0, input.StirrupSpacingA2);

            double zone1End;
            double zone2End;

            if (input.StirrupMode == StirrupDistributionMode.FixedLength)
            {
                zone1End = Math.Min(input.StirrupFixedLength, L / 2.0);
                zone2End = Math.Max(zone1End, L - zone1End);
            }
            else
            {
                zone1End = L / 4.0;
                zone2End = 3.0 * L / 4.0;
            }

            // Đoạn 1 (Gối trái): bước a1
            double currX = startX;
            while (currX <= zone1End)
            {
                positions.Add(currX);
                currX += a1;
            }

            // Đoạn 2 (Bụng giữa nhịp): bước a2
            while (currX < zone2End)
            {
                positions.Add(currX);
                currX += a2;
            }

            // Đoạn 3 (Gối phải): bước a1
            while (currX <= endX)
            {
                positions.Add(currX);
                currX += a1;
            }

            if (positions.Count > 0 && Math.Abs(positions.Last() - endX) > a1 * 0.5 && positions.Last() < endX)
            {
                positions.Add(endX);
            }

            return positions;
        }
    }
}
