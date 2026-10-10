using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D; 
using HelixToolkit.Wpf;             

// [CHÍNH XÁC] Namespace khớp với file Point3D.cs bạn gửi
using CorePoint = kaka.Structure.Core.Point3D; 

namespace kaka.Structure.Tests
{
    public class RebarVisualizer : Window
    {
        private HelixViewport3D _viewport;

        public RebarVisualizer(List<CorePoint> points, double diameter)
        {
            Title = "Rebar 3D Debugger";
            Width = 800; Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // 1. Setup Viewport
            _viewport = new HelixViewport3D();
            _viewport.ShowCoordinateSystem = true;
            _viewport.Background = Brushes.Black; 
            _viewport.Camera.Position = new Point3D(1000, -2000, 2000); 
            _viewport.Camera.LookDirection = new Vector3D(-1000, 2000, -2000);

            _viewport.Children.Add(new DefaultLights());
            _viewport.Children.Add(new GridLinesVisual3D { 
                Center = new Point3D(0, 0, 0), Length = 5000, Width = 5000, 
                MinorDistance = 100, MajorDistance = 500, Thickness = 2, Fill = Brushes.Gray 
            });

            // 2. Vẽ Thép
            var builder = new MeshBuilder(); // Lúc này sẽ hết lỗi đỏ
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                // Convert từ Point của bạn (CorePoint) sang Point của WPF (Point3D)
                var p1 = new Point3D(points[i].X, points[i].Y, points[i].Z);
                var p2 = new Point3D(points[i+1].X, points[i+1].Y, points[i+1].Z);
                
                builder.AddCylinder(p1, p2, diameter, 10); 
                builder.AddSphere(p2, diameter);           
            }

            if (builder.Positions.Count > 0)
            {
                var geometry = builder.ToMesh();
                var material = MaterialHelper.CreateMaterial(Colors.OrangeRed); 
                var model = new GeometryModel3D(geometry, material);
                _viewport.Children.Add(new ModelVisual3D { Content = model });
            }

            Content = _viewport;
        }

        public static void ShowModal(List<CorePoint> points, double dia)
        {
            var thread = new System.Threading.Thread(() =>
            {
                var win = new RebarVisualizer(points, dia);
                win.ShowDialog();
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}