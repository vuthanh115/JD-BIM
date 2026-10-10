#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kaka.Structure.Core;
using kaka.Structure.Structural.App.Models.Beam;

namespace kaka.Structure.Structural.App.ViewModels.Beam
{
    public partial class BeamRebarViewModel : ObservableObject
    {
        private readonly UIDocument _uidoc;
        private Document Doc => _uidoc.Document;

        private Window? _view;
        private Canvas? _canvasCross;

        [ObservableProperty]
        private ObservableCollection<BeamModel> _beams = new();

        [ObservableProperty]
        private BeamModel? _selectedBeam;

        [ObservableProperty]
        private BeamRebarSetting _setting = new();

        public List<RebarBarType> AvailableBarTypes { get; } = new();
        public List<RebarBarType> AvailableStirrupTypes { get; } = new();

        public List<int> QuantitiesWithZero { get; } = Enumerable.Range(0, 7).ToList(); // 0..6
        public List<int> Quantities1To8 { get; } = Enumerable.Range(1, 8).ToList();     // 1..8
        public List<int> Quantities2To8 { get; } = Enumerable.Range(2, 7).ToList();     // 2..8

        public BeamRebarViewModel(UIDocument uidoc, IEnumerable<FamilyInstance> selectedBeams)
        {
            _uidoc = uidoc ?? throw new ArgumentNullException(nameof(uidoc));

            LoadRebarTypes();

            foreach (var fi in selectedBeams)
            {
                Beams.Add(new BeamModel(fi));
            }

            SelectedBeam = Beams.FirstOrDefault();

            InitDefaultSettings();

            Setting.PropertyChanged += (s, e) => RedrawCrossSection();
        }

        private void LoadRebarTypes()
        {
            var types = new FilteredElementCollector(Doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .OrderBy(t => t.Name)
                .ToList();

            AvailableBarTypes.AddRange(types);
            AvailableStirrupTypes.AddRange(types);
        }

        private void InitDefaultSettings()
        {
            if (AvailableBarTypes.Count > 0)
            {
                // Tìm thép chủ phù hợp (D16, D18, D20)
                var mainType = AvailableBarTypes.FirstOrDefault(t => t.Name.Contains("16") || t.Name.Contains("D16") || t.Name.Contains("18") || t.Name.Contains("20")) 
                               ?? AvailableBarTypes.FirstOrDefault();

                // Tìm thép đai phù hợp (D8, D6, D10)
                var stirrupType = AvailableStirrupTypes.FirstOrDefault(t => t.Name.Contains("8") || t.Name.Contains("D8") || t.Name.Contains("6") || t.Name.Contains("10")) 
                                  ?? AvailableStirrupTypes.FirstOrDefault();

                Setting.TopMainType = mainType;
                Setting.TopExtraL1Type = mainType;
                Setting.TopExtraL2Type = mainType;
                Setting.BottomMainType = mainType;
                Setting.BottomExtraL1Type = mainType;
                Setting.BottomExtraL2Type = mainType;
                Setting.SideRebarType = mainType;
                Setting.StirrupType = stirrupType;
            }

            if (SelectedBeam != null)
            {
                Setting.Cover = SelectedBeam.CoverMm;
                if (SelectedBeam.HeightMm >= 700.0)
                {
                    Setting.HasSideRebar = true;
                }
            }
        }

        public void AttachView(Window view, Canvas canvasCross)
        {
            _view = view;
            _canvasCross = canvasCross;
            RedrawCrossSection();
        }

        partial void OnSelectedBeamChanged(BeamModel? value)
        {
            if (value != null)
            {
                Setting.Cover = value.CoverMm;
                if (value.HeightMm >= 700.0)
                {
                    Setting.HasSideRebar = true;
                }
                RedrawCrossSection();
            }
        }

        // ============================================================
        // VẼ MẶT CẮT 2D TRỰC QUAN (GIỐNG Bimspeed / 22132f99...)
        // ============================================================
        public void RedrawCrossSection()
        {
            if (_canvasCross == null) return;
            _canvasCross.Children.Clear();

            double b = SelectedBeam?.WidthMm ?? 300;
            double h = SelectedBeam?.HeightMm ?? 600;

            double canvasW = _canvasCross.ActualWidth > 0 ? _canvasCross.ActualWidth : 360;
            double canvasH = _canvasCross.ActualHeight > 0 ? _canvasCross.ActualHeight : 450;
            double padding = 60;

            double scale = Math.Min((canvasW - padding * 2) / b, (canvasH - padding * 2) / h);
            double offX = (canvasW - b * scale) / 2.0;
            double offY = (canvasH - h * scale) / 2.0;

            // 1. Viền bê tông dầm
            var concreteRect = new System.Windows.Shapes.Rectangle
            {
                Width = b * scale,
                Height = h * scale,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)),
                Stroke = Brushes.DimGray,
                StrokeThickness = 2.0
            };
            Canvas.SetLeft(concreteRect, offX);
            Canvas.SetTop(concreteRect, offY);
            _canvasCross.Children.Add(concreteRect);

            // 2. Viền đai đỏ (5)
            double cover = Setting.Cover;
            double sDia = 8.0;
            double sW = (b - 2 * cover) * scale;
            double sH = (h - 2 * cover) * scale;
            double sX = offX + cover * scale;
            double sY = offY + cover * scale;

            if (sW > 0 && sH > 0)
            {
                var stirrupRect = new System.Windows.Shapes.Rectangle
                {
                    Width = sW,
                    Height = sH,
                    Stroke = Brushes.Red,
                    StrokeThickness = 3.0,
                    RadiusX = 4 * scale,
                    RadiusY = 4 * scale
                };
                Canvas.SetLeft(stirrupRect, sX);
                Canvas.SetTop(stirrupRect, sY);
                _canvasCross.Children.Add(stirrupRect);
            }

            // 3. Vị trí thép chủ & gia cường
            double yMin = cover + sDia + 8.0;
            double yMax = b - (cover + sDia + 8.0);
            double effW = yMax - yMin;

            double zTopL1 = cover + sDia + 8.0; // Khoảng cách từ đỉnh xuống
            double zTopL2 = zTopL1 + Setting.LayerGap + 16.0;
            double zBotL1 = h - (cover + sDia + 8.0); // Khoảng cách từ đáy lên (tính từ đỉnh Canvas)
            double zBotL2 = zBotL1 - (Setting.LayerGap + 16.0);

            // (2) Thép chính lớp trên: 2 thanh góc
            DrawRebarDot(offX + yMin * scale, offY + zTopL1 * scale, 14, Brushes.DarkRed);
            DrawRebarDot(offX + yMax * scale, offY + zTopL1 * scale, 14, Brushes.DarkRed);
            if (Setting.TopMainQuantity > 2)
            {
                int extraTopMain = Setting.TopMainQuantity - 2;
                for (int i = 0; i < extraTopMain; i++)
                {
                    double y = yMin + (i + 1) * effW / (extraTopMain + 1);
                    DrawRebarDot(offX + y * scale, offY + zTopL1 * scale, 14, Brushes.DarkRed);
                }
            }

            // (1) Thép gia cường lớp trên lớp 1: ở giữa
            if (Setting.HasTopExtraL1 && Setting.TopExtraL1Quantity > 0)
            {
                for (int i = 0; i < Setting.TopExtraL1Quantity; i++)
                {
                    double y = yMin + (i + 1) * effW / (Setting.TopExtraL1Quantity + 1);
                    DrawRebarDot(offX + y * scale, offY + zTopL1 * scale, 14, Brushes.Crimson);
                }
            }

            // (3) Thép gia cường top lớp 2
            if (Setting.HasTopExtraL2 && Setting.TopExtraL2Quantity > 0)
            {
                for (int i = 0; i < Setting.TopExtraL2Quantity; i++)
                {
                    double y = (Setting.TopExtraL2Quantity > 1) 
                        ? yMin + i * effW / (Setting.TopExtraL2Quantity - 1) 
                        : (yMin + yMax) / 2.0;
                    DrawRebarDot(offX + y * scale, offY + zTopL2 * scale, 13, Brushes.Salmon);
                }
            }

            // (6) Thép chính lớp dưới: 2 thanh góc
            DrawRebarDot(offX + yMin * scale, offY + zBotL1 * scale, 14, Brushes.DarkRed);
            DrawRebarDot(offX + yMax * scale, offY + zBotL1 * scale, 14, Brushes.DarkRed);
            if (Setting.BottomMainQuantity > 2)
            {
                int extraBotMain = Setting.BottomMainQuantity - 2;
                for (int i = 0; i < extraBotMain; i++)
                {
                    double y = yMin + (i + 1) * effW / (extraBotMain + 1);
                    DrawRebarDot(offX + y * scale, offY + zBotL1 * scale, 14, Brushes.DarkRed);
                }
            }

            // (7) Thép gia cường lớp dưới lớp 1: ở giữa
            if (Setting.HasBottomExtraL1 && Setting.BottomExtraL1Quantity > 0)
            {
                for (int i = 0; i < Setting.BottomExtraL1Quantity; i++)
                {
                    double y = yMin + (i + 1) * effW / (Setting.BottomExtraL1Quantity + 1);
                    DrawRebarDot(offX + y * scale, offY + zBotL1 * scale, 14, Brushes.Crimson);
                }
            }

            // (4) Thép gia cường bot lớp 2
            if (Setting.HasBottomExtraL2 && Setting.BottomExtraL2Quantity > 0)
            {
                for (int i = 0; i < Setting.BottomExtraL2Quantity; i++)
                {
                    double y = (Setting.BottomExtraL2Quantity > 1) 
                        ? yMin + i * effW / (Setting.BottomExtraL2Quantity - 1) 
                        : (yMin + yMax) / 2.0;
                    DrawRebarDot(offX + y * scale, offY + zBotL2 * scale, 13, Brushes.Salmon);
                }
            }

            // Thép giá sườn (Side rebar)
            if (Setting.HasSideRebar)
            {
                double zMid = offY + (h / 2.0) * scale;
                DrawRebarDot(offX + yMin * scale, zMid, 12, Brushes.OrangeRed);
                DrawRebarDot(offX + yMax * scale, zMid, 12, Brushes.OrangeRed);
            }

            // 4. Các đường chỉ dẫn đánh số (1) đến (7) bên phải
            double badgeX = offX + b * scale + 25;
            DrawBadge(badgeX, offY + zTopL1 * scale - 20, "1", Brushes.Black);
            DrawBadge(badgeX, offY + zTopL1 * scale, "2", Brushes.Black);
            DrawBadge(badgeX, offY + zTopL2 * scale, "3", Brushes.Black);
            DrawBadge(badgeX, offY + zBotL2 * scale, "4", Brushes.Black);
            DrawBadge(badgeX, offY + (h / 2.0) * scale, "5", Brushes.Red);
            DrawBadge(badgeX, offY + zBotL1 * scale - 15, "6", Brushes.Black);
            DrawBadge(badgeX, offY + zBotL1 * scale + 10, "7", Brushes.Black);

            // Ghi chú kích thước b x h
            DrawText(offX + (b * scale) / 2.0, offY - 20, $"{b:F0} x {h:F0} mm", 12, Brushes.DarkSlateGray, true);
        }

        private void DrawRebarDot(double cx, double cy, double size, Brush brush)
        {
            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ellipse, cx - size / 2.0);
            Canvas.SetTop(ellipse, cy - size / 2.0);
            _canvasCross?.Children.Add(ellipse);
        }

        private void DrawBadge(double cx, double cy, string text, Brush strokeBrush)
        {
            var border = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                BorderBrush = strokeBrush,
                BorderThickness = new Thickness(1.5),
                Background = Brushes.White,
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = strokeBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                IsHitTestVisible = false
            };
            Canvas.SetLeft(border, cx - 10);
            Canvas.SetTop(border, cy - 10);
            _canvasCross?.Children.Add(border);
        }

        private void DrawText(double cx, double cy, string text, double fontSize, Brush brush, bool isCenter = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = brush,
                FontWeight = FontWeights.SemiBold,
                IsHitTestVisible = false
            };
            tb.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double x = isCenter ? cx - tb.DesiredSize.Width / 2.0 : cx;
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, cy);
            _canvasCross?.Children.Add(tb);
        }

        // ============================================================
        // TẠO CỐT THÉP TRONG REVIT
        // ============================================================
        [RelayCommand]
        private void Run()
        {
            if (Beams.Count == 0)
            {
                MessageBox.Show("Không có dầm nào được chọn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Setting.TopMainType == null || Setting.BottomMainType == null || Setting.StirrupType == null)
            {
                MessageBox.Show("Vui lòng chọn loại đường kính thép trước khi thực hiện!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int createdCount = 0;

            using (var trans = new Transaction(Doc, "Kaka - Create Beam Rebar"))
            {
                trans.Start();
                try
                {
                    foreach (var beam in Beams)
                    {
                        var input = Setting.ToInputData(beam.WidthMm, beam.HeightMm, beam.LengthMm);
                        var calculatedBars = BeamRebarCalculator.CalculateAllBars(input);

                        CreateBeamRebarsInRevit(beam, calculatedBars);
                        createdCount++;
                    }

                    trans.Commit();
                    MessageBox.Show($"Bố trí cốt thép thành công cho {createdCount} dầm!", "Kaka Structure", MessageBoxButton.OK, MessageBoxImage.Information);
                    _view?.Close();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    MessageBox.Show($"Lỗi khi tạo cốt thép dầm:\n{ex.Message}\n{ex.StackTrace}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateBeamRebarsInRevit(BeamModel beam, List<CalculatedBar> bars)
        {
            // Tọa độ chuẩn của dầm
            BoundingBoxXYZ bb = beam.Beam.get_BoundingBox(null);
            double zBottom = bb.Min.Z; // Đáy bê tông thực tế trong Revit (feet)

            XYZ startPt = beam.StartPoint;
            XYZ basisX = beam.BasisX;
            XYZ basisY = beam.BasisY;
            XYZ basisZ = beam.BasisZ;

            double widthFt = UnitUtils.ConvertToInternalUnits(beam.WidthMm, UnitTypeId.Millimeters);

            foreach (var bar in bars)
            {
                if (bar.Points.Count < 2) continue;

                // Chọn đúng RebarBarType
                var barType = ResolveBarType(bar);
                if (barType == null) continue;

                // Chuyển đổi các điểm Point3D (mm) sang Revit XYZ (feet)
                var revitPoints = new List<XYZ>();
                foreach (var p in bar.Points)
                {
                    double xFt = UnitUtils.ConvertToInternalUnits(p.X, UnitTypeId.Millimeters);
                    double yFt = UnitUtils.ConvertToInternalUnits(p.Y, UnitTypeId.Millimeters) - widthFt / 2.0;
                    double zFt = UnitUtils.ConvertToInternalUnits(p.Z, UnitTypeId.Millimeters);

                    XYZ pt = startPt + basisX * xFt + basisY * yFt;
                    // Đặt cao độ Z tính từ đáy dầm lên
                    pt = new XYZ(pt.X, pt.Y, zBottom + zFt);
                    revitPoints.Add(pt);
                }

                // Tạo các đường Curve
                var curves = new List<Curve>();
                for (int i = 0; i < revitPoints.Count - 1; i++)
                {
                    if (revitPoints[i].DistanceTo(revitPoints[i + 1]) > 0.005)
                    {
                        curves.Add(Line.CreateBound(revitPoints[i], revitPoints[i + 1]));
                    }
                }

                if (curves.Count == 0) continue;

                // Xác định vector pháp tuyến
                XYZ normal = XYZ.Zero;
                if (bar.IsClosedLoop)
                {
                    // Đai nằm trong mặt phẳng YZ -> pháp tuyến theo trục dầm X
                    normal = basisX;
                }
                else
                {
                    // Thép dọc có móc neo trong mặt phẳng XZ -> pháp tuyến theo trục ngang Y
                    normal = basisY;
                }

                var rebarStyle = bar.IsClosedLoop ? RebarStyle.StirrupTie : RebarStyle.Standard;

                var rebar = Rebar.CreateFromCurves(
                    Doc,
                    rebarStyle,
                    barType,
                    null,
                    null,
                    beam.Beam,
                    normal,
                    curves,
                    RebarHookOrientation.Right,
                    RebarHookOrientation.Right,
                    true,
                    true
                );

                if (rebar != null)
                {
                    rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set($"Beam-{beam.Mark}");
                    rebar.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(bar.Label);
                }
            }
        }

        private RebarBarType? ResolveBarType(CalculatedBar bar)
        {
            if (bar.IsClosedLoop)
            {
                return Setting.StirrupType;
            }
            if (bar.Label.StartsWith("TopMain"))
            {
                return Setting.TopMainType;
            }
            if (bar.Label.StartsWith("TopExtraL1"))
            {
                return Setting.TopExtraL1Type ?? Setting.TopMainType;
            }
            if (bar.Label.StartsWith("TopExtraL2"))
            {
                return Setting.TopExtraL2Type ?? Setting.TopMainType;
            }
            if (bar.Label.StartsWith("BottomMain"))
            {
                return Setting.BottomMainType;
            }
            if (bar.Label.StartsWith("BottomExtraL1"))
            {
                return Setting.BottomExtraL1Type ?? Setting.BottomMainType;
            }
            if (bar.Label.StartsWith("BottomExtraL2"))
            {
                return Setting.BottomExtraL2Type ?? Setting.BottomMainType;
            }
            if (bar.Label.StartsWith("SideRebar"))
            {
                return Setting.SideRebarType ?? Setting.TopMainType;
            }

            return Setting.TopMainType;
        }

        [RelayCommand]
        private void Cancel()
        {
            _view?.Close();
        }
    }
}
