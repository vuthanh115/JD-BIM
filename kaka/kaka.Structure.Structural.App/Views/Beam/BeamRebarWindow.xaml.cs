#nullable enable
using System;
using System.Windows;
using MahApps.Metro.Controls;
using kaka.Structure.Structural.App.ViewModels.Beam;

namespace kaka.Structure.Structural.App.Views.Beam
{
    public partial class BeamRebarWindow : MetroWindow
    {
        private readonly BeamRebarViewModel _vm;

        public BeamRebarWindow(BeamRebarViewModel vm)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            DataContext = _vm;

            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _vm.AttachView(this, canvasCrossSection);
            canvasCrossSection.SizeChanged += (s, ev) => _vm.RedrawCrossSection();
        }
    }
}
