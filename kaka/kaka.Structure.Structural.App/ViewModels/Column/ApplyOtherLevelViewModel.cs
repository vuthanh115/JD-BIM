#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kaka.Structure.Structural.App.Models.Column;
using kaka.Structure.Structural.App.Views.Column;

namespace kaka.Structure.Structural.App.ViewModels.Column
{
    public sealed partial class ApplyOtherLevelViewModel : ObservableObject
    {
        private readonly ApplyOtherLevelView _window;

        public ObservableCollection<ColumnModel> VerticalColumns { get; }

        [ObservableProperty]
        private ColumnModel? sourceColumn;

        // --- SỬA LỖI: Cập nhật Constructor nhận 3 tham số ---
        public ApplyOtherLevelViewModel(
            ApplyOtherLevelView window,
            ObservableCollection<ColumnModel> verticalColumns,
            ColumnModel sourceColumn) // <--- Thêm tham số này
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            VerticalColumns = verticalColumns ?? throw new ArgumentNullException(nameof(verticalColumns));
            SourceColumn = sourceColumn ?? throw new ArgumentNullException(nameof(sourceColumn));
        }

        [RelayCommand]
        private void Apply()
        {
            if (SourceColumn == null)
            {
                MessageBox.Show("Source column is null.", "Apply", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Target = dòng nào IsEnabled == true và tick ít nhất 1 checkbox
            // (Dùng các thuộc tính IsChecked... có sẵn trong ColumnModel của bạn)
            var targets = VerticalColumns
                .Where(c => c != null
                            && c.IsEnabled
                            && (c.IsCheckedMainRebar || c.IsCheckedStirrup))
                .ToList();

            if (targets.Count == 0)
            {
                MessageBox.Show(
                    "Chưa chọn dòng nào để áp dụng (tick Main rebar hoặc Stirrup).",
                    "Apply",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            foreach (var t in targets)
            {
                if (ReferenceEquals(t, SourceColumn)) continue;

                // 1. Copy Thép Chủ
                if (t.IsCheckedMainRebar)
                    CopyMainRebar(SourceColumn, t);

                // 2. Copy Thép Đai
                if (t.IsCheckedStirrup)
                    CopyStirrup(SourceColumn, t);

                // Reset checkbox sau khi apply (tránh user bấm nhầm lần sau)
                t.IsCheckedMainRebar = false;
                t.IsCheckedStirrup = false;
            }

            _window.DialogResult = true;
            _window.Close();
        }

        [RelayCommand]
        private void Cancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }

        // =========================
        // COPY LOGIC (Đã map theo ColumnModel.cs bạn gửi)
        // =========================
        private static void CopyMainRebar(ColumnModel src, ColumnModel dst)
        {
            // Copy số lượng
            dst.CxQuantity = src.CxQuantity;
            dst.CyQuantity = src.CyQuantity;
            dst.DQuantity = src.DQuantity;

            // Copy đường kính/loại thép (Setter sẽ tự update Setting.MainRebarTypeId)
            dst.MainRebarType = src.MainRebarType; 
            
            // Copy lớp bảo vệ (thường đi theo thép chủ)
            dst.CoverTop = src.CoverTop;
            dst.CoverOther = src.CoverOther;
        }

        private static void CopyStirrup(ColumnModel src, ColumnModel dst)
        {
            // Copy loại đai
            dst.MainStirrupType = src.MainStirrupType;
            dst.AdditionalStirrupType = src.AdditionalStirrupType;
            
            // Copy kiểu phân bố (L1, L2...)
            dst.DistributionType = src.DistributionType;
            
            // Copy tùy chọn đai kín/hở (nếu có)
            dst.UseClosedLoopStirrup = src.UseClosedLoopStirrup;

            // Nếu bạn có HookType trong Setting thì copy luôn (ví dụ)
            // if (src.Setting != null && dst.Setting != null) { ... }
        }
    }
}