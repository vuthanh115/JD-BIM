#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // Dùng cho Brushes, PointCollection

using kaka.Structure.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Wpf.Controls.PanAndZoom;

using kaka.Structure.Structural.App.Models.Column;
using kaka.Structure.Structural.App.Views.Column;
using kaka.Structure.Core;
using Transform = Autodesk.Revit.DB.Transform ;

namespace kaka.Structure.Structural.App.ViewModels.Column
{
    public sealed partial class ColumnRebarViewModel : ObservableObject
    {
        private readonly UIDocument _uidoc;
        private Document Doc => _uidoc.Document;

        // ===== View handles =====
        private Window? _view;
        private Canvas? _canvasVertical;
        private Canvas? _canvasCross;
        private ZoomBorder? _zoomVertical;
        private ZoomBorder? _zoomCross;

        private IReadOnlyList<FamilyInstance>? _hosts;

        public ColumnRebarViewModel(UIDocument uidoc)
        {
            _uidoc = uidoc ?? throw new ArgumentNullException(nameof(uidoc));

            MainRebarTypes = new List<RebarBarType>();
            StirrupTypes = new List<RebarBarType>();
            StirrupHookTypes = new List<RebarHookType>();
            RebarParameters = new ObservableCollection<string>();

            // --- KHỞI TẠO SETTINGS ---
            StirrupSetting = new StirrupSetting();
            
            // StarterBarSetting khởi tạo với giá trị mặc định để tránh lỗi null
            StarterBarSetting = new StarterBarSetting 
            { 
                HasStarterBar = true, // Mặc định bật
                Outward = true,
                AutoDetectFoundation = true,
                HmValue = 0,                    // Mặc định bẻ chân tại đáy cột (Hm=0), nếu có móng bên dưới code sẽ tự tính Hm!
                LbValue = 250.0 / 304.8         // Mặc định bẻ 250mm
            };

            // Load GeneralSetting từ file JSON
            GeneralSetting = GeneralSetting.Create();
            // Tự động lưu khi có thay đổi
            GeneralSetting.PropertyChanged += (s, e) => GeneralSetting.Save();

            VerticalColumns = new ObservableCollection<ColumnModel>();
            SelectedColumns = new ObservableCollection<ColumnModel>();
            SelectedColumn = new ColumnModel(null);

            DebugLogger.Log("=== [VM] ctor ColumnRebarViewModel ===");

            this.PropertyChanged += ColumnRebarViewModel_PropertyChanged;

            LoadRebarParameters(); 
        }

        private void ColumnRebarViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedColumn))
            {
                if (SelectedColumn != null)
                {
                    SelectedColumn.PropertyChanged -= SelectedColumn_InnerChanged;
                    SelectedColumn.PropertyChanged += SelectedColumn_InnerChanged;
                }
                RedrawAll();
            }
        }

        private void SelectedColumn_InnerChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CxQuantity" ||
                e.PropertyName == "CyQuantity" ||
                e.PropertyName == "DQuantity" ||
                e.PropertyName == "CoverOther" ||
                e.PropertyName == "MainRebarType" ||
                e.PropertyName == "DistributionType" ||
                e.PropertyName == "AdditionalStirrupType")
            {
                RedrawAll();
            }
        }

        // =========================
        // DATA FOR UI
        // =========================
        public ObservableCollection<ColumnModel> VerticalColumns { get; }
        public ObservableCollection<ColumnModel> SelectedColumns { get; }

        public List<RebarBarType> MainRebarTypes { get; private set; }
        public List<RebarBarType> StirrupTypes { get; private set; }
        public List<RebarHookType> StirrupHookTypes { get; private set; }
        public ObservableCollection<string> RebarParameters { get; private set; }

        public StirrupSetting StirrupSetting { get; }
        public StarterBarSetting StarterBarSetting { get; }
        public GeneralSetting GeneralSetting { get; }

        [ObservableProperty] private int selectedTabIndex;
        [ObservableProperty] private ColumnModel selectedColumn;

        public bool IsRoundColumn => GetIsRound(SelectedColumn);
        public bool IsRectangleColumn => !IsRoundColumn;

        public void SetHosts(IReadOnlyList<FamilyInstance> hosts)
        {
            _hosts = hosts;
        }

        // =========================
        // VIEW <-> VM
        // =========================
        public void AttachView(Window view, Canvas canvasVertical, Canvas canvasCross, ZoomBorder zoomVertical, ZoomBorder zoomCross)
        {
            _view = view;
            _canvasVertical = canvasVertical;
            _canvasCross = canvasCross;
            _zoomVertical = zoomVertical;
            _zoomCross = zoomCross;

            try { LoadRebarTypes(); } catch (Exception ex) { DebugLogger.Log($"LoadTypes EX: {ex}"); }
            try 
            {
                if (_hosts != null && _hosts.Count > 0) LoadColumnsFromHosts(_hosts);
                else LoadColumnsFromActiveView();
            }
            catch (Exception ex) { DebugLogger.Log($"LoadCols EX: {ex}"); }

            try { RedrawAll(); } catch (Exception ex) { DebugLogger.Log($"Redraw EX: {ex}"); }
        }

        public void DetachView()
        {
            _view = null;
            _canvasVertical = null; _canvasCross = null;
            _zoomVertical = null; _zoomCross = null;
        }

        partial void OnSelectedColumnChanged(ColumnModel value)
        {
            OnPropertyChanged(nameof(IsRoundColumn));
            OnPropertyChanged(nameof(IsRectangleColumn));
        }

        // =========================
        // LOAD DATA HELPER
        // =========================
        private static double GetBarDiameterFt(RebarBarType t)
        {
            var p = t.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER);
            return p?.AsDouble() ?? 0.0;
        }

        private void LoadRebarTypes()
        {
            var bars = new FilteredElementCollector(Doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().OrderBy(GetBarDiameterFt).ThenBy(x => x.Name).ToList();
            MainRebarTypes = bars;
            StirrupTypes = bars;
            StirrupHookTypes = new FilteredElementCollector(Doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>().OrderBy(h => h.Name).ToList();

            OnPropertyChanged(nameof(MainRebarTypes));
            OnPropertyChanged(nameof(StirrupTypes));
            OnPropertyChanged(nameof(StirrupHookTypes));
        }

        private void LoadColumnsFromHosts(IReadOnlyList<FamilyInstance> hosts)
        {
            VerticalColumns.Clear();
            SelectedColumns.Clear();
            foreach (var fi in hosts.Where(x => x != null))
            {
                var model = CreateAndInitColumnModel(fi);
                VerticalColumns.Add(model);
                SelectedColumns.Add(model);
            }
            SelectedColumn = VerticalColumns.FirstOrDefault() ?? new ColumnModel(null);
            OnPropertyChanged(nameof(IsRoundColumn));
            OnPropertyChanged(nameof(IsRectangleColumn));
        }

        private void LoadColumnsFromActiveView()
        {
            VerticalColumns.Clear();
            SelectedColumns.Clear();
            var cols = new FilteredElementCollector(Doc, Doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            foreach (var fi in cols)
            {
                var model = CreateAndInitColumnModel(fi);
                VerticalColumns.Add(model);
            }
            
            var first = VerticalColumns.FirstOrDefault();
            if (first != null) { SelectedColumn = first; SelectedColumns.Add(first); }
            else SelectedColumn = new ColumnModel(null);

            // Tự động nhận diện móng bên dưới cột thấp nhất để điền HmValue ban đầu cho giao diện
            var botCol = VerticalColumns.OrderBy(c => c.Column?.get_BoundingBox(null)?.Min.Z ?? c.BaseLevelElevation).FirstOrDefault();
            if (botCol?.Column != null && (StarterBarSetting?.AutoDetectFoundation ?? true))
            {
                var fd = FindFoundationUnderColumn(botCol.Column);
                if (fd != null && StarterBarSetting != null)
                {
                    StarterBarSetting.HmValue = UnitUtils.ConvertToInternalUnits(fd.Value.DepthMm, UnitTypeId.Millimeters);
                    if (fd.Value.SuggestedDirection == "Right") StarterBarSetting.RightwardClickCommand.Execute(null);
                    else if (fd.Value.SuggestedDirection == "Left") StarterBarSetting.LeftwardClickCommand.Execute(null);
                    else if (fd.Value.SuggestedDirection == "Back") StarterBarSetting.BackwardClickCommand.Execute(null);
                    else if (fd.Value.SuggestedDirection == "Front") StarterBarSetting.FrontwardClickCommand.Execute(null);
                }
            }

            OnPropertyChanged(nameof(IsRoundColumn));
            OnPropertyChanged(nameof(IsRectangleColumn));
        }

        private ColumnModel CreateAndInitColumnModel(FamilyInstance fi)
        {
            var model = ColumnModel.Create(fi);
            model.InitVariables(VerticalColumns.ToList(), StirrupSetting, StarterBarSetting, GeneralSetting);
            FillColumnInfoForUI(model, fi);
            TryFillComboDefaults(model);
            return model;
        }

        private void FillColumnInfoForUI(ColumnModel model, FamilyInstance fi)
        {
            model.Mark = TryGetString(fi, BuiltInParameter.ALL_MODEL_MARK) ?? fi.Name;
            model.BaseLevel = Doc.GetElement(fi.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId() ?? ElementId.InvalidElementId) as Level;
            model.TopLevel = Doc.GetElement(fi.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.AsElementId() ?? ElementId.InvalidElementId) as Level;

            double topCoverFt = TryGetDouble(fi, BuiltInParameter.CLEAR_COVER_TOP) ?? 0;
            double otherCoverFt = TryGetDouble(fi, BuiltInParameter.CLEAR_COVER_OTHER) ?? 0;

            model.CoverTop = (topCoverFt > 0) 
                ? UnitUtils.ConvertFromInternalUnits(topCoverFt, UnitTypeId.Millimeters) 
                : 25.0;

            model.CoverOther = (otherCoverFt > 0) 
                ? UnitUtils.ConvertFromInternalUnits(otherCoverFt, UnitTypeId.Millimeters) 
                : 25.0;
        }

        private void TryFillComboDefaults(ColumnModel model)
        {
            if (model.CxQuantities.Count == 0) model.CxQuantities.AddRange(Enumerable.Range(2, 9));
            if (model.CyQuantities.Count == 0) model.CyQuantities.AddRange(Enumerable.Range(2, 9));
            if (model.DQuantities.Count == 0) model.DQuantities.AddRange(Enumerable.Range(6, 15));
            if (model.CxQuantity <= 0) model.CxQuantity = 2;
            if (model.CyQuantity <= 0) model.CyQuantity = 2;
            if (model.MainRebarType == null) model.MainRebarType = MainRebarTypes.FirstOrDefault();
            if (model.MainStirrupType == null) model.MainStirrupType = StirrupTypes.FirstOrDefault();
        }

        // =========================
        // COMMANDS
        // =========================

        [RelayCommand]
        private void Run()
        {
            if (VerticalColumns == null || VerticalColumns.Count == 0)
            {
                MessageBox.Show("Không có cột nào trong danh sách!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (VerticalColumns.Any(c => c.MainRebarType == null || c.MainStirrupType == null))
            {
                MessageBox.Show("Vui lòng chọn loại đường kính thép (Main/Stirrup Type) cho tất cả các cột!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (Transaction t = new Transaction(Doc, "Kaka - Create Column Rebar"))
            {
                t.Start();
                try
                {
                    foreach (var colModel in VerticalColumns)
                    {
                        if (colModel.Column == null) continue;

                        CreateMainRebar(colModel);
                        CreateStirrups(colModel);
                    }

                    t.Commit();
                    MessageBox.Show("Bố trí thép thành công!", "Kaka Structure", MessageBoxButton.OK, MessageBoxImage.Information);
                    _view?.Close();
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    MessageBox.Show($"Có lỗi xảy ra: {ex.Message}\n{ex.StackTrace}", "Lỗi Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void DeleteAllStirrupTie()
        {
            foreach (var col in SelectedColumns) col.DeleteAllStirrups();
            RedrawAll();
        }

        [RelayCommand]
        private void ApplyOtherLevels()
        {
            if (_view == null || SelectedColumn == null) return;
            var win = new ApplyOtherLevelView { Owner = _view };
            var vm = new ApplyOtherLevelViewModel(win, VerticalColumns, SelectedColumn);
            win.DataContext = vm;
            if (win.ShowDialog() == true)
            {
                UpdateAllColumnStats();
                RedrawAll();
            }
        }
        
        [RelayCommand]
        private void SelectFramingFloor()
        {
            MessageBox.Show("Tính năng chọn dầm sàn thủ công đang phát triển");
        }

        private void LoadRebarParameters()
        {
            RebarParameters.Clear();
            RebarParameters.Add("Partition");
            RebarParameters.Add("Comments");
            RebarParameters.Add("Mark");
            RebarParameters.Add("Image");

            if (GeneralSetting != null && string.IsNullOrEmpty(GeneralSetting.GroupParameterName))
            {
                GeneralSetting.GroupParameterName = "Partition";
            }
        }
        
        // =========================
        // CLICK EVENT
        // =========================
        private void Element_ColumnClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Shapes.Shape shape && shape.Tag is ColumnModel clickedColumn)
            {
                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                if (isCtrlPressed)
                {
                    if (SelectedColumns.Contains(clickedColumn))
                    {
                        SelectedColumns.Remove(clickedColumn);
                        if (SelectedColumn == clickedColumn)
                            SelectedColumn = SelectedColumns.LastOrDefault() ?? new ColumnModel(null);
                    }
                    else
                    {
                        SelectedColumns.Add(clickedColumn);
                        SelectedColumn = clickedColumn; 
                    }
                }
                else
                {
                    SelectedColumns.Clear();
                    SelectedColumns.Add(clickedColumn);
                    SelectedColumn = clickedColumn;
                }
                RedrawAll();
            }
        }

        // =========================
        // DRAWING LOGIC
        // =========================
        private void RedrawAll()
        {
            if (_canvasVertical == null || _canvasCross == null) return;
            _canvasVertical.Children.Clear();
            _canvasCross.Children.Clear();

            if (SelectedColumn == null && VerticalColumns.Count > 0) SelectedColumn = VerticalColumns[0];

            UpdateAllColumnStats();
            DrawCrossSection();
            DrawVerticalSection();
        }

        private void UpdateAllColumnStats()
        {
            if (VerticalColumns == null) return;
            foreach (var col in VerticalColumns) CalculateRebarStats(col);
        }

        private void CalculateRebarStats(ColumnModel col)
        {
            if (col == null || col.MainRebarType == null) return;
            var (colW, colH) = GetColumnSizeInMm(col);
            if (colW <= 0 || colH <= 0) return;

            int nx = col.CxQuantity;
            int ny = col.CyQuantity;
            int totalBars = (2 * nx) + (2 * ny) - 4;
            if (totalBars < 4) totalBars = 4;

            double diaFt = GetBarDiameterFt(col.MainRebarType);
            double diaMm = UnitUtils.ConvertFromInternalUnits(diaFt, UnitTypeId.Millimeters);

            double areaOneBarMm2 = Math.PI * Math.Pow(diaMm, 2) / 4.0;
            double totalAreaMm2 = totalBars * areaOneBarMm2;
            double concreteAreaMm2 = colW * colH;

            double totalAreaCm2 = totalAreaMm2 / 100.0;
            double ratio = 0;
            if (concreteAreaMm2 > 0)
                ratio = (totalAreaMm2 / concreteAreaMm2) * 100;

            col.SectionReinforcementArea = Math.Round(totalAreaCm2, 2);
            col.SectionReinforcementRatio = Math.Round(ratio, 2);
            col.SectionReinforcementDescription = $"{totalBars}Ø{Math.Round(diaMm)}";
        }
        
        private void DrawCrossSection()
{
    if (_canvasCross == null || SelectedColumn == null) return;
    _canvasCross.Children.Clear();

    var (colW, colH) = GetColumnSizeInMm(SelectedColumn);
    if (colW <= 0 || colH <= 0) return;

    // Tính Scale
    var (scale, offX, offY) = CalculateFitData(_canvasCross, colW, colH, 50);

    // Lấy thông số
    double cover = SelectedColumn.CoverOther > 0 ? SelectedColumn.CoverOther : 25;
    double mainDia = 14; 
    if (SelectedColumn.MainRebarType != null)
        mainDia = UnitUtils.ConvertFromInternalUnits(GetBarDiameterFt(SelectedColumn.MainRebarType), UnitTypeId.Millimeters);
    
    double stirrupDia = 6;
    if (SelectedColumn.MainStirrupType != null)
        stirrupDia = UnitUtils.ConvertFromInternalUnits(GetBarDiameterFt(SelectedColumn.MainStirrupType), UnitTypeId.Millimeters);

    // --- A. VẼ BÊ TÔNG ---
    var concreteRect = new System.Windows.Shapes.Rectangle
    {
        Width = colW * scale, Height = colH * scale,
        Stroke = System.Windows.Media.Brushes.Gray, StrokeThickness = 1.0, Fill = System.Windows.Media.Brushes.WhiteSmoke
    };
    Canvas.SetLeft(concreteRect, offX); Canvas.SetTop(concreteRect, offY);
    _canvasCross.Children.Add(concreteRect);

    // --- B. VẼ ĐAI CHÍNH (Vòng bao ngoài) ---
    double sW = colW - (2 * cover) - stirrupDia;
    double sH = colH - (2 * cover) - stirrupDia;
    
    // Tọa độ gốc của đai chính (góc trên trái)
    double startX = offX + (cover + stirrupDia/2) * scale;
    double startY = offY + (cover + stirrupDia/2) * scale;

    if (sW > 0 && sH > 0)
    {
        var stirrupRect = new System.Windows.Shapes.Rectangle
        {
            Width = sW * scale, Height = sH * scale,
            Stroke = System.Windows.Media.Brushes.Teal, StrokeThickness = 2.0, RadiusX = 2*scale, RadiusY = 2*scale
        };
        Canvas.SetLeft(stirrupRect, startX); Canvas.SetTop(stirrupRect, startY);
        _canvasCross.Children.Add(stirrupRect);
    }

    // --- C. VẼ ĐAI BỔ SUNG (C-LINKS / CROSSTIES) ---
    // Chỉ vẽ khi người dùng đã chọn loại thép đai bổ sung
    if (SelectedColumn.AdditionalStirrupType != null)
    {
        // Vị trí thép chủ
        double distStart = cover + stirrupDia + mainDia/2;
        double effW = colW - 2 * distStart;
        double effH = colH - 2 * distStart;
        
        int nx = SelectedColumn.CxQuantity;
        int ny = SelectedColumn.CyQuantity;
        double gapX = (nx > 1) ? effW / (nx - 1) : 0;
        double gapY = (ny > 1) ? effH / (ny - 1) : 0;

        // 1. Vẽ đai phương Ngang (Nối các thanh dọc ở giữa)
        // Duyệt các hàng giữa (trừ hàng trên cùng j=0 và dưới cùng j=ny-1)
        for (int j = 1; j < ny - 1; j++)
        {
            double localY = distStart + j * gapY;
            double screenY = offY + localY * scale;
            
            double x1 = offX + distStart * scale;           // Từ thanh bên trái
            double x2 = offX + (distStart + effW) * scale;  // Đến thanh bên phải
            
            DrawCrosstie(x1, screenY, x2, screenY);
        }

        // 2. Vẽ đai phương Dọc (Nối các thanh ngang ở giữa)
        // Duyệt các cột giữa (trừ cột trái i=0 và phải i=nx-1)
        for (int i = 1; i < nx - 1; i++)
        {
            double localX = distStart + i * gapX;
            double screenX = offX + localX * scale;

            double y1 = offY + distStart * scale;           // Từ thanh trên
            double y2 = offY + (distStart + effH) * scale;  // Đến thanh dưới
            
            DrawCrosstie(screenX, y1, screenX, y2);
        }
    }

    // --- D. VẼ THÉP CHỦ (Đè lên trên cùng) ---
    DrawMainRebarPoints(colW, colH, scale, offX, offY, cover, stirrupDia, mainDia);
}

        // Hàm phụ trợ vẽ 1 thanh C-Link (Line + 2 móc nhỏ)
        private void DrawCrosstie(double x1, double y1, double x2, double y2)
        {
            // Vẽ thân
            var line = new System.Windows.Shapes.Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = System.Windows.Media.Brushes.Blue, StrokeThickness = 1.5,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 2 }
            };
            _canvasCross?.Children.Add(line);

            // Vẽ móc tượng trưng (Hook)
            double hookSize = 5; 
            // Móc đầu 1
            _canvasCross?.Children.Add(new System.Windows.Shapes.Line { X1 = x1, Y1 = y1, X2 = x1 + hookSize, Y2 = y1 + hookSize, Stroke = System.Windows.Media.Brushes.Blue, StrokeThickness = 1.5 });
            // Móc đầu 2
            _canvasCross?.Children.Add(new System.Windows.Shapes.Line { X1 = x2, Y1 = y2, X2 = x2 - hookSize, Y2 = y2 - hookSize, Stroke = System.Windows.Media.Brushes.Blue, StrokeThickness = 1.5 });
        }

        // Tách hàm vẽ điểm thép chủ ra cho gọn
        private void DrawMainRebarPoints(double colW, double colH, double scale, double offX, double offY, double cover, double stirrupDia, double mainDia)
        {
            double start = cover + stirrupDia + mainDia / 2;
            double effW = colW - 2 * start;
            double effH = colH - 2 * start;
            int nx = SelectedColumn.CxQuantity;
            int ny = SelectedColumn.CyQuantity;
            double gapX = nx > 1 ? effW / (nx - 1) : 0;
            double gapY = ny > 1 ? effH / (ny - 1) : 0;
            double visualSize = Math.Max(mainDia * scale, 6.0);

            for (int i = 0; i < nx; i++)
            {
                DrawBigRebarPoint(start + i * gapX, start, offX, offY, scale, visualSize);
                DrawBigRebarPoint(start + i * gapX, start + effH, offX, offY, scale, visualSize);
            }
            for (int j = 1; j < ny - 1; j++)
            {
                DrawBigRebarPoint(start, start + j * gapY, offX, offY, scale, visualSize);
                DrawBigRebarPoint(start + effW, start + j * gapY, offX, offY, scale, visualSize);
            }
        }

        private void DrawBigRebarPoint(double realLocalX, double realLocalY, double offX, double offY, double scale, double drawSize)
        {
            var dot = new System.Windows.Shapes.Ellipse
            {
                Width = drawSize,
                Height = drawSize,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1
            };
            double screenX = offX + (realLocalX * scale);
            double screenY = offY + (realLocalY * scale);

            Canvas.SetLeft(dot, screenX - drawSize / 2);
            Canvas.SetTop(dot, screenY - drawSize / 2);
            _canvasCross.Children.Add(dot);
        }
        
        private void DrawRebarLine(double x1, double y1, double x2, double y2, System.Windows.Media.Brush brush)
        {
            var line = new System.Windows.Shapes.Line 
            { 
                X1 = x1, 
                Y1 = y1, 
                X2 = x2, 
                Y2 = y2, 
                Stroke = brush, 
                StrokeThickness = 2,

                // Cho phép click xuyên qua
                IsHitTestVisible = false 
            };
            _canvasVertical?.Children.Add(line);
        }

        private void DrawVerticalSection()
{
    if (_canvasVertical == null || VerticalColumns == null || VerticalColumns.Count == 0) return;
    _canvasVertical.Children.Clear();

    var sortedCols = VerticalColumns.OrderBy(c => c.BaseLevelElevation).ToList();
    double globalMinElev = sortedCols.First().BaseLevelElevation;
    var topCol = sortedCols.Last();
    
    // Tính toán chiều cao vẽ
    double topBeamH = topCol.FramingFloorHeight > 0 ? topCol.FramingFloorHeight : 500;
    double totalStackHeight = (topCol.TopLevelElevation + topBeamH) - globalMinElev;
    double maxColW = sortedCols.Max(c => GetColumnWidth(c));
    double drawZoneWidth = maxColW + 2500;

    var (scale, offX, offY) = CalculateFitData(_canvasVertical, drawZoneWidth, totalStackHeight, 20);
    double centerX = offX + (drawZoneWidth * scale) / 2;
    double screenBottomY = offY + (totalStackHeight * scale);

    // Xác định cột nào là móng (cột thấp nhất)
    var bottomCol = sortedCols.FirstOrDefault();

    foreach (var col in sortedCols)
    {
        bool isSelected = SelectedColumns.Contains(col);
        double colW = GetColumnWidth(col);
        double colH = UnitUtils.ConvertFromInternalUnits(col.Height, UnitTypeId.Millimeters);
        double beamH = col.FramingFloorHeight > 0 ? col.FramingFloorHeight : 500;

        double currentBaseY = screenBottomY - ((col.BaseLevelElevation - globalMinElev) * scale);
        double currentTopY = currentBaseY - (colH * scale);

        // 1. Vẽ Level & Dầm & Cột (Giữ nguyên code cũ)
        DrawLevelLine(offX, currentTopY, drawZoneWidth * scale, $"▼ {col.TopLevelText}");
        
        // Vẽ Dầm
        double beamDrawW = (colW + 1200) * scale;
        var beamRect = new System.Windows.Shapes.Rectangle 
        { 
            Width = beamDrawW, Height = beamH * scale, 
            Fill = isSelected ? Brushes.MediumSeaGreen : Brushes.LightGray, Opacity = 0.4
        };
        Canvas.SetLeft(beamRect, centerX - beamDrawW/2); Canvas.SetTop(beamRect, currentTopY);
        _canvasVertical.Children.Add(beamRect);

        // Vẽ Cột
        var colRect = new System.Windows.Shapes.Rectangle 
        { 
            Width = colW * scale, Height = colH * scale, 
            Stroke = Brushes.Gray, Fill = isSelected ? Brushes.WhiteSmoke : Brushes.White,
            Tag = col
        };
        colRect.PreviewMouseLeftButtonDown += Element_ColumnClick;
        Canvas.SetLeft(colRect, centerX - (colW * scale) / 2); Canvas.SetTop(colRect, currentTopY);
        _canvasVertical.Children.Add(colRect);

        // 2. VẼ THÉP ĐAI (Giữ nguyên)
        if (isSelected)
        {
            var stirrupOffsets = CalculateStirrupPositions(colH, beamH, col.DistributionType);
            foreach (var offset in stirrupOffsets)
                DrawStirrupLine(centerX, currentBaseY - (offset * scale), colW * scale, 20 * scale);
        }

        // 3. --- MỚI: VẼ THÉP CHỦ (MAIN REBAR) ---
        if (isSelected)
        {
            // Vẽ 2 thanh tượng trưng 2 bên
            double inset = (25 + 6 + 7) * scale; // Cover + Stirrup + Radius
            double barX_Left = centerX - (colW * scale) / 2 + inset;
            double barX_Right = centerX + (colW * scale) / 2 - inset;

            // Đỉnh thép (chờ lên trên một chút)
            double lapLen = 40 * 14 * scale; // Tượng trưng nối chồng
            double topY = currentTopY - lapLen;
            double botY = currentBaseY;

            // Nếu là cột móng -> Kéo dài xuống Hm và bẻ chân Lb
            bool isFoundation = (col == bottomCol);
            if (isFoundation && StarterBarSetting.HasStarterBar)
            {
                double hm = StarterBarSetting.HmValue * 304.8 * scale; // Đổi Feet -> Mm -> Scale
                double lb = StarterBarSetting.LbValue * 304.8 * scale;

                botY = currentBaseY + hm; // Kéo xuống dưới

                // Vẽ chân Lb theo hướng đã chọn
                if (StarterBarSetting?.Rightward == true)
                {
                    DrawRebarLine(barX_Left, botY, barX_Left + lb, botY, Brushes.Red);
                    DrawRebarLine(barX_Right, botY, barX_Right + lb, botY, Brushes.Red);
                }
                else if (StarterBarSetting?.Leftward == true)
                {
                    DrawRebarLine(barX_Left, botY, barX_Left - lb, botY, Brushes.Red);
                    DrawRebarLine(barX_Right, botY, barX_Right - lb, botY, Brushes.Red);
                }
                else if (StarterBarSetting?.Outward == true)
                {
                    DrawRebarLine(barX_Left, botY, barX_Left - lb, botY, Brushes.Red);  // Chân trái bẻ ra ngoài (hướng trái)
                    DrawRebarLine(barX_Right, botY, barX_Right + lb, botY, Brushes.Red); // Chân phải bẻ ra ngoài (hướng phải)
                }
                else
                {
                    DrawRebarLine(barX_Left, botY, barX_Left + lb, botY, Brushes.Red);  // Chân trái (hướng phải)
                    DrawRebarLine(barX_Right, botY, barX_Right - lb, botY, Brushes.Red); // Chân phải (hướng trái)
                }
            }

            // Vẽ thân thép dọc
            DrawRebarLine(barX_Left, botY, barX_Left, topY, Brushes.Red);
            DrawRebarLine(barX_Right, botY, barX_Right, topY, Brushes.Red);
        }
    }
}
        
        // =========================
        // REBAR GENERATION LOGIC (UPDATED)
        // =========================
        
        // --- HÀM TẠO THÉP CHỦ: Tích hợp Setting ---
        // =========================
        // FOUNDATION AUTO-DETECTION HELPER
        // =========================
        private struct FoundationSupportInfo
        {
            public Element Foundation;
            public double DepthMm;
            public double CoverBottomMm;
            public double MaxFootLengthMm;
            public string SuggestedDirection;
        }

        private FoundationSupportInfo? FindFoundationUnderColumn(FamilyInstance col)
        {
            try
            {
                BoundingBoxXYZ colBb = col.get_BoundingBox(null);
                if (colBb == null) return null;

                double colBottomZ = colBb.Min.Z;
                double colCenterX = (colBb.Min.X + colBb.Max.X) / 2.0;
                double colCenterY = (colBb.Min.Y + colBb.Max.Y) / 2.0;

                // Thu thập tất cả các cấu kiện móng (Foundation, Floor móng bè, kể cả trong RevitLink nếu có)
                var candidates = new List<(Element Element, BoundingBoxXYZ BBox)>();

                // A. Móng trong Host Document
                var fdCollector = new FilteredElementCollector(Doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .WhereElementIsNotElementType()
                    .ToElements();
                foreach (var f in fdCollector)
                {
                    var b = f.get_BoundingBox(null);
                    if (b != null) candidates.Add((f, b));
                }

                // B. Sàn móng (Floors) trong Host Document
                var floorCollector = new FilteredElementCollector(Doc)
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .WhereElementIsNotElementType()
                    .ToElements();
                foreach (var fl in floorCollector)
                {
                    var b = fl.get_BoundingBox(null);
                    if (b != null && b.Max.Z <= colBottomZ + 1.0 && b.Min.Z >= colBottomZ - 10.0)
                    {
                        candidates.Add((fl, b));
                    }
                }

                // C. Móng trong Revit Link Instances (nếu có)
                var linkInstances = new FilteredElementCollector(Doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .Cast<RevitLinkInstance>()
                    .ToList();
                foreach (var link in linkInstances)
                {
                    var linkDoc = link.GetLinkDocument();
                    if (linkDoc == null) continue;
                    var tr = link.GetTotalTransform();
                    var linkFds = new FilteredElementCollector(linkDoc)
                        .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                        .WhereElementIsNotElementType()
                        .ToElements();
                    foreach (var lfd in linkFds)
                    {
                        var lb = lfd.get_BoundingBox(null);
                        if (lb != null)
                        {
                            XYZ pMin = tr.OfPoint(lb.Min);
                            XYZ pMax = tr.OfPoint(lb.Max);
                            var worldBb = new BoundingBoxXYZ
                            {
                                Min = new XYZ(Math.Min(pMin.X, pMax.X), Math.Min(pMin.Y, pMax.Y), Math.Min(pMin.Z, pMax.Z)),
                                Max = new XYZ(Math.Max(pMin.X, pMax.X), Math.Max(pMin.Y, pMax.Y), Math.Max(pMin.Z, pMax.Z))
                            };
                            candidates.Add((lfd, worldBb));
                        }
                    }
                }

                DebugLogger.Log($"[FindFoundationUnderColumn] Col ID={col.Id}, BB: X=[{colBb.Min.X * 304.8:F0}, {colBb.Max.X * 304.8:F0}], Y=[{colBb.Min.Y * 304.8:F0}, {colBb.Max.Y * 304.8:F0}], Z=[{colBb.Min.Z * 304.8:F0}, {colBb.Max.Z * 304.8:F0}]mm. Total candidates: {candidates.Count}");

                Element? bestMatch = null;
                BoundingBoxXYZ? bestBb = null;
                double bestDistZ = double.MaxValue;

                foreach (var (fd, fBb) in candidates)
                {
                    DebugLogger.Log($"  Candidate FD {fd.Id} '{fd.Name}': X=[{fBb.Min.X * 304.8:F0}, {fBb.Max.X * 304.8:F0}], Y=[{fBb.Min.Y * 304.8:F0}, {fBb.Max.Y * 304.8:F0}], Z=[{fBb.Min.Z * 304.8:F0}, {fBb.Max.Z * 304.8:F0}]mm");

                    // 1. Kiểm tra mặt bằng XY: Có giao cắt với tiết diện cột không?
                    double tolXY = 300.0 / 304.8; // dung sai 300mm
                    bool overlapX = (colBb.Max.X + tolXY >= fBb.Min.X) && (colBb.Min.X - tolXY <= fBb.Max.X);
                    bool overlapY = (colBb.Max.Y + tolXY >= fBb.Min.Y) && (colBb.Min.Y - tolXY <= fBb.Max.Y);
                    if (!overlapX || !overlapY)
                    {
                        DebugLogger.Log($"    -> Rejected: No XY overlap (overlapX={overlapX}, overlapY={overlapY})");
                        continue;
                    }

                    // 2. Kiểm tra cao độ Z: Móng phải nằm ở vùng chân cột
                    // - Đáy móng không được cao hơn chân cột quá 300mm
                    // - Đỉnh móng không được nằm quá sâu dưới chân cột (> 3m)
                    if (fBb.Min.Z > colBottomZ + (300.0 / 304.8))
                    {
                        DebugLogger.Log($"    -> Rejected: Footing bottom is above column base by >300mm");
                        continue;
                    }
                    if (fBb.Max.Z < colBottomZ - (3000.0 / 304.8))
                    {
                        DebugLogger.Log($"    -> Rejected: Footing top is >3m below column base");
                        continue;
                    }

                    double distZ = Math.Abs(fBb.Max.Z - colBottomZ);
                    if (distZ < bestDistZ)
                    {
                        bestDistZ = distZ;
                        bestMatch = fd;
                        bestBb = fBb;
                    }
                }

                if (bestMatch != null && bestBb != null)
                {
                    // Khoảng cách từ chân cột đến đáy móng
                    double rawDepthFeet = colBottomZ - bestBb.Min.Z;
                    double rawDepthMm = UnitUtils.ConvertFromInternalUnits(rawDepthFeet, UnitTypeId.Millimeters);

                    // Lớp bảo vệ đáy móng (CLEAR_COVER_BOTTOM)
                    double coverBottomFeet = TryGetDouble(bestMatch, BuiltInParameter.CLEAR_COVER_BOTTOM) ?? 0;
                    double coverBottomMm = (coverBottomFeet > 0)
                        ? UnitUtils.ConvertFromInternalUnits(coverBottomFeet, UnitTypeId.Millimeters)
                        : 50.0;

                    // Lớp bảo vệ cạnh móng (CLEAR_COVER_OTHER)
                    double coverOtherFeet = TryGetDouble(bestMatch, BuiltInParameter.CLEAR_COVER_OTHER) ?? 0;
                    double coverOtherMm = (coverOtherFeet > 0)
                        ? UnitUtils.ConvertFromInternalUnits(coverOtherFeet, UnitTypeId.Millimeters)
                        : 50.0;

                    // TÍNH TOÁN Hm THÔNG MINH:
                    double calculatedHmMm = 0;
                    if (rawDepthMm > coverBottomMm + 25.0)
                    {
                        // Cột ngồi trên đỉnh móng hoặc ăn sâu 1 phần: Cần cắm sâu xuống đáy móng trừ lớp bảo vệ
                        calculatedHmMm = rawDepthMm - coverBottomMm;
                        DebugLogger.Log($"[FindFoundationUnderColumn] Matched FD '{bestMatch.Name}' (ID: {bestMatch.Id}). Column sits on/in footing. rawDepth={rawDepthMm:F0}mm, cover={coverBottomMm:F0}mm -> Hm={calculatedHmMm:F0}mm");
                    }
                    else
                    {
                        // Cột đã cắm xuống sát đáy móng rồi (mô hình Cổ cột trong Revit):
                        // Chân thép chỉ cần bẻ ngay tại đáy cột (Hm = 0)! Không được cắm thêm ra ngoài móng!
                        calculatedHmMm = 0;
                        DebugLogger.Log($"[FindFoundationUnderColumn] Matched FD '{bestMatch.Name}' (ID: {bestMatch.Id}). Column base is ALREADY at footing bottom (diff={rawDepthMm:F0}mm <= cover={coverBottomMm:F0}mm). Setting Hm=0.");
                    }

                    // Khoảng cách từ mép cột ra các mép đài móng
                    double marginPosX = bestBb.Max.X - colBb.Max.X;
                    double marginNegX = colBb.Min.X - bestBb.Min.X;
                    double marginPosY = bestBb.Max.Y - colBb.Max.Y;
                    double marginNegY = colBb.Min.Y - bestBb.Min.Y;

                    double minMarginFeet = Math.Min(Math.Min(marginPosX, marginNegX), Math.Min(marginPosY, marginNegY));
                    double maxFootMm = Math.Max(0, UnitUtils.ConvertFromInternalUnits(minMarginFeet, UnitTypeId.Millimeters) - coverOtherMm);

                    // Phân tích độ lệch tâm để tự động hướng bẻ móc vào lòng đài móng
                    Transform tf = col.GetTransform();
                    XYZ colCenter = (colBb.Min + colBb.Max) / 2.0;
                    XYZ fdCenter = (bestBb.Min + bestBb.Max) / 2.0;
                    double deltaX_local = (fdCenter - colCenter).DotProduct(tf.BasisX);
                    double deltaY_local = (fdCenter - colCenter).DotProduct(tf.BasisY);

                    string suggestedDir = "Right"; // Mặc định sang phải theo phương cạnh ngắn
                    double tolEcc = 80.0 / 304.8; // 80mm dung sai lệch tâm

                    if (Math.Abs(deltaX_local) >= Math.Abs(deltaY_local))
                    {
                        if (deltaX_local < -tolEcc) suggestedDir = "Left";
                        else suggestedDir = "Right";
                    }
                    else
                    {
                        if (deltaY_local > tolEcc) suggestedDir = "Back";
                        else if (deltaY_local < -tolEcc) suggestedDir = "Front";
                    }

                    DebugLogger.Log($"[FindFoundationUnderColumn] Eccentricity: deltaX={deltaX_local*304.8:F0}mm, deltaY={deltaY_local*304.8:F0}mm -> Auto direction: {suggestedDir}");

                    return new FoundationSupportInfo
                    {
                        Foundation = bestMatch,
                        DepthMm = calculatedHmMm,
                        CoverBottomMm = coverBottomMm,
                        MaxFootLengthMm = maxFootMm,
                        SuggestedDirection = suggestedDir
                    };
                }
                else
                {
                    DebugLogger.Log($"[FindFoundationUnderColumn] No foundation matched at column {col.Id}.");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[FindFoundationUnderColumn] Exception: {ex.Message}");
            }

            return null;
        }

        private void CreateMainRebar(ColumnModel colModel)
{
    try
    {
        FamilyInstance col = colModel.Column;
        if (col == null) return;

        DebugLogger.Log($"--------------------------------------------------");
        DebugLogger.Log($"[CreateMainRebar] START: {colModel.Mark} (ID: {col.Id})");

        // --- 1. LẤY SETTINGS & XỬ LÝ ĐƠN VỊ ---

        // A. General Settings
        // Hệ số nối chồng tối thiểu 20d (chuẩn là 40d, phòng trường hợp bị lưu nhầm thành 6)
        double lapCoeff = GeneralSetting.CoefficientLapLength >= 20.0 ? GeneralSetting.CoefficientLapLength : 40.0;
        bool isStaggered = GeneralSetting.ShouldSpliceAtTwoPositions; 
        bool shouldCrank = GeneralSetting.ShouldCrankRebarAtSplice; 

        // [FIX 1] XỬ LÝ ĐƠN VỊ CHO OFFSET (100mm)
        double offsetRaw = GeneralSetting.OffsetFromColumnBottom;
        // Nếu < 50 nghi là Feet (0.328) -> nhân 304.8. Nếu >= 50 là mm -> giữ nguyên.
        double spliceOffsetMm = (offsetRaw > 0 && offsetRaw < 50) ? offsetRaw * 304.8 : offsetRaw;

        // B. Foundation Settings
        bool hasStarter = StarterBarSetting != null && StarterBarSetting.HasStarterBar;
        double hm_Raw = StarterBarSetting?.HmValue ?? 0;
        double lb_Raw = StarterBarSetting?.LbValue ?? 0;

        double hm_Mm = (hm_Raw > 0 && hm_Raw < 50) ? hm_Raw * 304.8 : hm_Raw;
        double lb_Mm = (lb_Raw > 0 && lb_Raw < 50) ? lb_Raw * 304.8 : lb_Raw;

        // C. Top Hook
        bool hasTopHook = GeneralSetting.HasHeadLock;
        double topHookLengthMm = 0;
        if (hasTopHook && colModel.MainRebarType != null)
        {
            double diaMm = UnitUtils.ConvertFromInternalUnits(
                colModel.MainRebarType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble(),
                UnitTypeId.Millimeters);
            
            topHookLengthMm = GeneralSetting.IsHeadLockByLength 
                ? UnitUtils.ConvertFromInternalUnits(GeneralSetting.HeadHookLength, UnitTypeId.Millimeters)
                : (GeneralSetting.HeadHookFactor * diaMm);
        }

        // --- 2. PREPARE INPUT ---
        var (colW_mm, colH_mm) = GetColumnSizeInMm(colModel);
        if (colModel.MainRebarType == null) return;

        double barDia_feet = colModel.MainRebarType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble();
        double barDia_mm = UnitUtils.ConvertFromInternalUnits(barDia_feet, UnitTypeId.Millimeters);

        BoundingBoxXYZ bb = col.get_BoundingBox(null);
        double floorHeight_feet = bb.Max.Z - bb.Min.Z;
        double floorHeight_mm = UnitUtils.ConvertFromInternalUnits(floorHeight_feet, UnitTypeId.Millimeters);

        // Xác định vị trí cột và tính toán chiều cao tầng thực tế đến cột tầng trên (vượt qua toàn bộ dầm/sàn)
        bool isTopColumn = false;
        bool isBottomColumn = false;
        if (VerticalColumns != null && VerticalColumns.Count > 0)
        {
            var sortedCols = VerticalColumns.OrderBy(c => c.Column?.get_BoundingBox(null)?.Min.Z ?? c.BaseLevelElevation).ToList();
            isTopColumn = (col.Id == sortedCols.Last().Column?.Id);
            isBottomColumn = (col.Id == sortedCols.First().Column?.Id);

            // Nếu không phải cột trên cùng: Chiều cao tầng phải tính từ đáy cột này đến đáy cột trên
            // để thanh thép chờ vươn qua hết chiều dày dầm/sàn rồi mới nối trọn vẹn 40d vào cột trên!
            if (!isTopColumn)
            {
                int currentIdx = sortedCols.FindIndex(c => c.Column?.Id == col.Id);
                if (currentIdx >= 0 && currentIdx < sortedCols.Count - 1)
                {
                    var nextCol = sortedCols[currentIdx + 1];
                    var nextBb = nextCol.Column?.get_BoundingBox(null);
                    if (nextBb != null)
                    {
                        double realStoryHeight_ft = nextBb.Min.Z - bb.Min.Z;
                        if (realStoryHeight_ft > 0)
                        {
                            floorHeight_mm = UnitUtils.ConvertFromInternalUnits(realStoryHeight_ft, UnitTypeId.Millimeters);
                        }
                    }
                }
            }
        }

        // Logic Force Straight
        bool coreIsBottom = isBottomColumn;
        if (!shouldCrank && !isTopColumn) coreIsBottom = true;

        bool starterOutward = StarterBarSetting?.Outward ?? true;

        // TỰ ĐỘNG NHẬN DIỆN MÓNG & KHỐNG CHẾ ĐIỀU KIỆN BIÊN THEO ĐÀI MÓNG
        if (isBottomColumn && hasStarter)
        {
            bool autoDetectFd = StarterBarSetting?.AutoDetectFoundation ?? true;
            if (autoDetectFd)
            {
                var fdInfo = FindFoundationUnderColumn(col);
                if (fdInfo != null)
                {
                    hm_Mm = fdInfo.Value.DepthMm;
                    DebugLogger.Log($"[Foundation] Auto-detected foundation '{fdInfo.Value.Foundation.Name}' (ID: {fdInfo.Value.Foundation.Id}). Real depth to bottom rebar layer: {hm_Mm:F0}mm (CoverBottom={fdInfo.Value.CoverBottomMm:F0}mm)");

                    // Tự động áp dụng hướng bẻ móc theo độ lệch tâm đài móng
                    if (StarterBarSetting != null && fdInfo.Value.SuggestedDirection != null)
                    {
                        if (fdInfo.Value.SuggestedDirection == "Right") StarterBarSetting.RightwardClickCommand.Execute(null);
                        else if (fdInfo.Value.SuggestedDirection == "Left") StarterBarSetting.LeftwardClickCommand.Execute(null);
                        else if (fdInfo.Value.SuggestedDirection == "Back") StarterBarSetting.BackwardClickCommand.Execute(null);
                        else if (fdInfo.Value.SuggestedDirection == "Front") StarterBarSetting.FrontwardClickCommand.Execute(null);
                    }

                    // Khống chế cạnh biên đài móng nếu bẻ xòe (Outward)
                    if (starterOutward && fdInfo.Value.MaxFootLengthMm > 0)
                    {
                        if (lb_Mm > fdInfo.Value.MaxFootLengthMm)
                        {
                            if (fdInfo.Value.MaxFootLengthMm >= 150.0)
                            {
                                DebugLogger.Log($"[Foundation] Lb ({lb_Mm:F0}mm) exceeds foundation side margin ({fdInfo.Value.MaxFootLengthMm:F0}mm). Clamping Lb to {fdInfo.Value.MaxFootLengthMm:F0}mm.");
                                lb_Mm = fdInfo.Value.MaxFootLengthMm;
                            }
                            else
                            {
                                DebugLogger.Log($"[Foundation] Side margin ({fdInfo.Value.MaxFootLengthMm:F0}mm) too narrow for outward hook. Switching to Inward hook.");
                                starterOutward = false;
                            }
                        }
                    }
                }
                else
                {
                    DebugLogger.Log($"[Foundation] No foundation detected directly beneath column {col.Id}. Using manual Hm={hm_Mm:F0}mm.");
                }
            }
        }

        var inputData = new RebarInputData
        {
            ColWidth = colW_mm, ColDepth = colH_mm,
            Cover = colModel.CoverOther > 0 ? colModel.CoverOther : 25,
            BarDiameter = barDia_mm,
            FloorHeight = floorHeight_mm,
            LapLength = lapCoeff * barDia_mm,
            
            // [FIX QUAN TRỌNG] Luôn truyền Offset (100mm) cho mọi cột
            // Core sẽ tự xử lý: 
            // - Cột móng: Chân giữ nguyên (0), Đỉnh cộng thêm 100.
            // - Cột trên: Chân cộng thêm 100, Đỉnh cộng thêm 100.
            SpliceOffset = spliceOffsetMm,
            
            IsTopColumn = isTopColumn,
            IsBottomColumn = coreIsBottom, 
            IsStaggered = isStaggered,
            
            HasStarterBar = (isBottomColumn && hasStarter), 
            EmbedLength = hm_Mm, 
            FootLength = lb_Mm,
            StarterBarOutward = starterOutward,
            StarterBarLeftward = StarterBarSetting?.Leftward ?? false,
            StarterBarRightward = StarterBarSetting?.Rightward ?? false,
            StarterBarBackward = StarterBarSetting?.Backward ?? false,
            StarterBarFrontward = StarterBarSetting?.Frontward ?? false,
            
            HasTopHook = (isTopColumn && hasTopHook),
            TopHookLength = topHookLengthMm,
            CrankShift = 50.0 
        };

        DebugLogger.Log($"[Input] BottomReal={isBottomColumn}, Offset={spliceOffsetMm:F0}");

        // --- 3. COORDINATES ---
        Transform tf = col.GetTransform();
        XYZ globalCenter = (bb.Min + bb.Max) / 2.0;
        XYZ basisX = tf.BasisX;
        XYZ basisY = tf.BasisY;
        XYZ basisZ = XYZ.BasisZ; 
        double zMinGlobal = bb.Min.Z;
        XYZ centerBottom = globalCenter - (XYZ.BasisZ * (globalCenter.Z - zMinGlobal));

        // [FIX 2] XÓA ĐOẠN CODE DỜI TỌA ĐỘ Ở ĐÂY
        // Vì Core đã tự động cộng thêm SpliceOffset vào Z rồi.
        // Nếu giữ đoạn này, bạn sẽ bị hở 200mm (100 của Core + 100 của ViewModel).
        /* if (!isBottomColumn && spliceOffsetMm > 0)
        {
            centerBottom += basisZ * UnitUtils.ConvertToInternalUnits(spliceOffsetMm, UnitTypeId.Millimeters);
        }
        */

        // --- 4. GENERATE BAR ---
        void GenerateBar(int index, double ratioX, double ratioY, string label)
        {
            try
            {
                var pointsMM = RebarCalculator.CalculateSingleBarPath(inputData, index, ratioX, ratioY);
                if (pointsMM == null || pointsMM.Count < 2) return;

                // GỘP ĐIỂM
                var optimizedPoints = new List<kaka.Structure.Core.Point3D>();
                optimizedPoints.Add(pointsMM[0]);
                for (int i = 1; i < pointsMM.Count - 1; i++)
                {
                    var pPrev = pointsMM[i - 1];
                    var pCurr = pointsMM[i];
                    var pNext = pointsMM[i + 1];
                    XYZ v1 = new XYZ(pCurr.X - pPrev.X, pCurr.Y - pPrev.Y, pCurr.Z - pPrev.Z).Normalize();
                    XYZ v2 = new XYZ(pNext.X - pCurr.X, pNext.Y - pCurr.Y, pNext.Z - pCurr.Z).Normalize();
                    if (v1.AngleTo(v2) > 0.001) optimizedPoints.Add(pCurr); 
                }
                optimizedPoints.Add(pointsMM.Last());

                List<Curve> curves = new List<Curve>();
                for (int i = 0; i < optimizedPoints.Count - 1; i++)
                {
                    XYZ p1 = ConvertCorePointToRevit(optimizedPoints[i], centerBottom, basisX, basisY, basisZ);
                    XYZ p2 = ConvertCorePointToRevit(optimizedPoints[i+1], centerBottom, basisX, basisY, basisZ);
                    if (p1.DistanceTo(p2) > 0.005) curves.Add(Line.CreateBound(p1, p2));
                }

                if (curves.Count == 0) return;

                XYZ normal = XYZ.Zero;
                if (curves.Count >= 2)
                {
                    XYZ v1 = (curves[0].GetEndPoint(1) - curves[0].GetEndPoint(0)).Normalize();
                    XYZ v2 = (curves[1].GetEndPoint(1) - curves[1].GetEndPoint(0)).Normalize();
                    normal = v1.CrossProduct(v2);
                }
                if (normal.IsZeroLength())
                {
                    bool isSideBar = (ratioX <= 0.01 || ratioX >= 0.99) && (ratioY > 0.01 && ratioY < 0.99);
                    normal = isSideBar ? basisY : basisX;
                }

                Rebar bar = Rebar.CreateFromCurves(Doc, RebarStyle.Standard, colModel.MainRebarType, null, null, col, normal, curves, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                if (bar == null) DebugLogger.Log($"[Warning] Failed to create bar {label}");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"[Error Bar {label}] {ex.Message}");
            }
        }

        // --- 5. LOOP ---
        int nx = colModel.CxQuantity; 
        int ny = colModel.CyQuantity;
        int globalIndex = 0;
        for (int i = 0; i < nx; i++) GenerateBar(globalIndex++, (double)i/(nx-1), 0.0, $"B{i}");
        for (int j = 1; j < ny - 1; j++) GenerateBar(globalIndex++, 1.0, (double)j/(ny-1), $"R{j}");
        for (int i = nx - 1; i >= 0; i--) GenerateBar(globalIndex++, (double)i/(nx-1), 1.0, $"T{i}");
        for (int j = ny - 2; j >= 1; j--) GenerateBar(globalIndex++, 0.0, (double)j/(ny-1), $"L{j}");

        DebugLogger.Log($"[Success] CreateMainRebar FINISHED.");
    }
    catch (Exception ex)
    {
        DebugLogger.Log($"[FATAL] {ex.Message}\n{ex.StackTrace}");
    }
}
        // --- HÀM TẠO ĐAI: Safe Version + Logging ---
        private void CreateStirrups(ColumnModel colModel)
{
    FamilyInstance col = colModel.Column;
    if (col == null) return;

    DebugLogger.Log($"--------------------------------------------------");
    DebugLogger.Log($"[CreateStirrups] START: {colModel.Mark}");

    // 1. TÍNH TOÁN HÌNH HỌC
    Transform tf = col.GetTransform();
    XYZ origin = tf.Origin;
    XYZ basisX = tf.BasisX;
    XYZ basisY = tf.BasisY;
    XYZ basisZ = tf.BasisZ;

    double coverMm = colModel.CoverOther > 0 ? colModel.CoverOther : 25;
    double cover = UnitUtils.ConvertToInternalUnits(coverMm, UnitTypeId.Millimeters);
    
    if (colModel.MainStirrupType == null) return;
    double stirrupDia = colModel.MainStirrupType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble();

    var (colW_mm, colH_mm) = GetColumnSizeInMm(colModel);
    double colW = UnitUtils.ConvertToInternalUnits(colW_mm, UnitTypeId.Millimeters);
    double colH = UnitUtils.ConvertToInternalUnits(colH_mm, UnitTypeId.Millimeters);

    // Box = Kích thước cột - 2*Cover
    double boxW = colW - 2 * cover;
    double boxH = colH - 2 * cover;

    if (boxW <= 0 || boxH <= 0) 
    {
        DebugLogger.Log($"[Error] Invalid dimensions: BoxW={boxW}, BoxH={boxH}");
        return;
    }

    // Vị trí đặt
    BoundingBoxXYZ bb = col.get_BoundingBox(null);
    double zMin = bb.Min.Z;
    double botOffset = (zMin - origin.Z); 
    XYZ centerBottom = origin + basisZ * (botOffset + cover + stirrupDia/2);
    XYZ cornerOrigin = centerBottom - (basisX * boxW / 2.0) - (basisY * boxH / 2.0);

    // 2. TÌM SHAPE "BS_M_T1"
    RebarShape targetShape = GetRebarShape("BS_M_T1");
    if (targetShape == null) targetShape = GetRebarShape("M_T1");

    if (targetShape == null)
    {
        DebugLogger.Log($"[Error] Shape 'BS_M_T1' not found!");
        return;
    }
    DebugLogger.Log($"[Shape Debug] Target Shape: {targetShape.Name} (ID: {targetShape.Id})");

    // 3. TẠO & CO GIÃN
    Rebar stirrup = null;
    try
    {
        // Bước A: Tạo từ Shape
        stirrup = Rebar.CreateFromRebarShape(
            Doc, 
            targetShape, 
            colModel.MainStirrupType, 
            col, 
            cornerOrigin, 
            basisX, 
            basisY
        );

        if (stirrup != null)
        {
            // [LOG DEBUG] Check Shape ban đầu
            ElementId initialShapeId = stirrup.get_Parameter(BuiltInParameter.REBAR_SHAPE)?.AsElementId();
            DebugLogger.Log($"[Shape Debug] Initial Bar Shape ID: {initialShapeId}");

            // Bước B: Scale
            var accessor = stirrup.GetShapeDrivenAccessor();
            XYZ xVec = basisX * boxW;
            XYZ yVec = basisY * boxH;
            
            DebugLogger.Log($"[Action] Scaling to Box: W={boxW*304.8:F0}mm, H={boxH*304.8:F0}mm");
            accessor.ScaleToBox(cornerOrigin, xVec, yVec);
            
            // [LOG DEBUG] Check Shape sau khi Scale (Xem có bị đổi không)
            ElementId finalShapeId = stirrup.get_Parameter(BuiltInParameter.REBAR_SHAPE)?.AsElementId();
            RebarShape finalShape = Doc.GetElement(finalShapeId) as RebarShape;
            
            DebugLogger.Log($"[Shape Debug] Final Bar Shape: {finalShape?.Name} (ID: {finalShapeId})");

            if (targetShape.Id != finalShapeId)
            {
                DebugLogger.Log($"[WARNING] SHAPE CHANGED! Revit created a new shape '{finalShape?.Name}'.");
                DebugLogger.Log($"[Reason] The dimensions {boxW*304.8:F0}x{boxH*304.8:F0} might violate '{targetShape.Name}' constraints.");
            }
            else
            {
                DebugLogger.Log($"[Success] Shape preserved as {targetShape.Name}");
            }
        }
    }
    catch (Exception ex)
    {
        DebugLogger.Log($"[Error] Create/Scale Failed: {ex.Message}");
    }

    // 4. RẢI THÉP (TRỪ DẦM)
    if (stirrup != null)
    {
        double spacing = StirrupSetting.IsSeismicStirrupSpacing ? StirrupSetting.SeismicStirrupSpacing : 0.65;
        if (spacing <= 0) spacing = UnitUtils.ConvertToInternalUnits(150, UnitTypeId.Millimeters);

        double colTotalH = bb.Max.Z - bb.Min.Z;
        double beamH_mm = colModel.FramingFloorHeight > 0 ? colModel.FramingFloorHeight : 500;
        double beamH_ft = UnitUtils.ConvertToInternalUnits(beamH_mm, UnitTypeId.Millimeters);
        double distLength = colTotalH - beamH_ft;
        
        if (distLength <= 0) distLength = 0.1;

        var accessor = stirrup.GetShapeDrivenAccessor();
        accessor.SetLayoutAsMaximumSpacing(spacing, distLength, true, true, true);
    }
}
        // =========================
        // HELPER METHODS
        // =========================
        
        // --- HELPER CHUYỂN ĐỔI TỌA ĐỘ ---
        // Chuyển từ Point3D (Core/mm/Local) -> XYZ (Revit/Feet/Global)
        private XYZ ConvertCorePointToRevit(kaka.Structure.Core.Point3D p, XYZ origin, XYZ bx, XYZ by, XYZ bz)
        {
            // 1. Đổi đơn vị
            double x_ft = UnitUtils.ConvertToInternalUnits(p.X, UnitTypeId.Millimeters);
            double y_ft = UnitUtils.ConvertToInternalUnits(p.Y, UnitTypeId.Millimeters);
            double z_ft = UnitUtils.ConvertToInternalUnits(p.Z, UnitTypeId.Millimeters);

            // 2. Map vào hệ tọa độ Global
            // P_global = Origin + (x * BasisX) + (y * BasisY) + (z * BasisZ)
            return origin + (bx * x_ft) + (by * y_ft) + (bz * z_ft);
        }
        
        private string GetDistributionText(EnumDistributionType type)
        {
            switch (type)
            {
                case EnumDistributionType.L1: return "L1";
                case EnumDistributionType.L1_L2: return "L1, L2";
                case EnumDistributionType.L1_L2_L1: return "L1, L2, L1";
                default: return "L1";
            }
        }

        private List<double> CalculateStirrupPositions(double colHeightMm, double topBeamDepthMm, EnumDistributionType distType)
        {
            var offsets = new List<double>();
            if (colHeightMm <= 0) return offsets;

            // Chiều cao thông thủy
            double clearHeight = colHeightMm - topBeamDepthMm;
            if (clearHeight <= 100) return offsets;

            // KHOẢNG CÁCH ĐAI (Lấy từ Setting hoặc mặc định)
            double sDense = 100; // Đai dày (A1)
            double sSparse = 200; // Đai thưa (A2)
    
            // Vùng gia cường = 1/4 chiều cao thông thủy
            double zoneLength = clearHeight / 4.0;
            // Tối thiểu vùng gia cường là 500mm (theo TCVN/Standard)
            if (zoneLength < 500) zoneLength = 500; 

            // Bắt đầu rải từ chân cột lên (+50mm offset đầu tiên)
            double currentY = 50;

            while (currentY < clearHeight - 20) // Cách đáy dầm trên 20mm thì dừng
            {
                offsets.Add(currentY);

                double step = sSparse; // Mặc định là thưa

                switch (distType)
                {
                    case EnumDistributionType.L1: 
                        // Rải đều 100%
                        step = sDense; // Hoặc giá trị người dùng nhập
                        break;

                    case EnumDistributionType.L1_L2:
                        // Chỉ dày ở chân cột
                        if (currentY <= zoneLength) step = sDense;
                        else step = sSparse;
                        break;

                    case EnumDistributionType.L1_L2_L1:
                        // Dày ở Chân (Bottom) và Đỉnh (Top)
                        bool inBottomZone = currentY <= zoneLength;
                        bool inTopZone = currentY >= (clearHeight - zoneLength);
                
                        if (inBottomZone || inTopZone) step = sDense;
                        else step = sSparse;
                        break;
                }

                currentY += step;
            }
            return offsets;
        }

        private void DrawLevelLine(double x, double y, double width, string label)
        {
            var line = new System.Windows.Shapes.Line 
            { 
                X1 = 0, Y1 = 0, X2 = width, Y2 = 0, 
                Stroke = Brushes.Gray, StrokeThickness = 1, 
                StrokeDashArray = new DoubleCollection { 4, 4 } 
            };
            Canvas.SetLeft(line, x); Canvas.SetTop(line, y); 
            _canvasVertical?.Children.Add(line);

            // FIX LỖI AMBIGUOUS: Dùng System.Windows.Point tường minh
            var tri = new System.Windows.Shapes.Polygon 
            { 
                Points = new PointCollection 
                { 
                    new System.Windows.Point(0, 0), 
                    new System.Windows.Point(10, -5), 
                    new System.Windows.Point(10, 5) 
                }, 
                Fill = Brushes.Black 
            };
            Canvas.SetLeft(tri, x + width); Canvas.SetTop(tri, y); 
            _canvasVertical?.Children.Add(tri);

            DrawMultiLineText(_canvasVertical, label, x + width + 15, y, TextAlignment.Left, Brushes.Black);
        }

        private void DrawMultiLineText(Canvas c, string text, double anchorX, double anchorY, TextAlignment align, Brush color)
        {
            var tb = new TextBlock 
            { 
                Text = text, Foreground = color, FontSize = 11, TextAlignment = align, 
                LineHeight = 16, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                IsHitTestVisible = false 
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double finalX = (align == TextAlignment.Right) ? anchorX - tb.DesiredSize.Width : anchorX;
            Canvas.SetLeft(tb, finalX); 
            Canvas.SetTop(tb, anchorY - (tb.DesiredSize.Height / 2));
            c.Children.Add(tb);
        }

        private void DrawStirrupLine(double x, double y, double w, double cover)
        {
            var line = new System.Windows.Shapes.Line 
            { 
                X1 = x - (w / 2) + cover, Y1 = y, 
                X2 = x + (w / 2) - cover, Y2 = y, 
                Stroke = Brushes.SaddleBrown, StrokeThickness = 1,
                IsHitTestVisible = false 
            };
            _canvasVertical?.Children.Add(line);
        }

        private (double, double, double) CalculateFitData(Canvas c, double realW, double realH, double padding)
        {
            double w = c.ActualWidth > 0 ? c.ActualWidth : 800; double h = c.ActualHeight > 0 ? c.ActualHeight : 600;
            double scale = Math.Min((w - padding * 2) / realW, (h - padding * 2) / realH);
            return (scale, (w - realW * scale) / 2, (h - realH * scale) / 2);
        }

        private double GetColumnWidth(ColumnModel col)
        {
            var (b, _) = GetColumnSizeInMm(col); return b;
        }

        private (double b, double h) GetColumnSizeInMm(ColumnModel? colModel = null)
        {
            var targetCol = colModel ?? SelectedColumn; if (targetCol?.Column == null) return (400, 400);
            var fi = targetCol.Column; var type = fi.Symbol;
            double? b = GetParamValue(type, "b") ?? GetParamValue(type, "Width") ?? GetParamValue(fi, "b");
            double? h = GetParamValue(type, "h") ?? GetParamValue(type, "Height") ?? GetParamValue(fi, "h");
            if (b == null || h == null) { var bb = fi.get_BoundingBox(null); if (bb != null) { b = bb.Max.X - bb.Min.X; h = bb.Max.Y - bb.Min.Y; } }
            return (UnitUtils.ConvertFromInternalUnits(b ?? 1.31, UnitTypeId.Millimeters), UnitUtils.ConvertFromInternalUnits(h ?? 1.31, UnitTypeId.Millimeters));
        }

        private double? GetParamValue(Element e, string name) { var p = e.LookupParameter(name); return p?.AsDouble(); }
        private static string? TryGetString(Element? e, BuiltInParameter bip) { return e?.get_Parameter(bip)?.AsValueString(); }
        private static double? TryGetDouble(Element? e, BuiltInParameter bip) { return e?.get_Parameter(bip)?.AsDouble(); }
        private static bool GetIsRound(ColumnModel? col) { return col?.SectionSizeText?.Contains("D") == true; }
        
        private RebarShape GetRebarShape(string name)
        {
            var shape = new FilteredElementCollector(Doc).OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault(x => x.Name.Contains(name));
            return shape ?? new FilteredElementCollector(Doc).OfClass(typeof(RebarShape)).Cast<RebarShape>().FirstOrDefault();
        }
    }
}