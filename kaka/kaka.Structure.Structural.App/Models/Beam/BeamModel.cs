#nullable enable
using System;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace kaka.Structure.Structural.App.Models.Beam
{
    public class BeamModel : ObservableObject
    {
        public FamilyInstance Beam { get; }
        public string Name { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;

        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;
        public double LengthMm { get; set; } = 6000.0;
        public double CoverMm { get; set; } = 25.0;

        public XYZ StartPoint { get; set; } = XYZ.Zero;
        public XYZ EndPoint { get; set; } = XYZ.BasisX;
        public XYZ BasisX { get; set; } = XYZ.BasisX;
        public XYZ BasisY { get; set; } = XYZ.BasisY;
        public XYZ BasisZ { get; set; } = XYZ.BasisZ;

        public BeamModel(FamilyInstance fi)
        {
            Beam = fi ?? throw new ArgumentNullException(nameof(fi));
            ExtractGeometryAndParameters();
        }

        private void ExtractGeometryAndParameters()
        {
            Name = Beam.Name;
            Mark = Beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? Beam.Name;

            var levelId = Beam.LevelId;
            if (levelId != ElementId.InvalidElementId && Beam.Document.GetElement(levelId) is Level level)
            {
                LevelName = level.Name;
            }

            // Cover
            double topCoverFt = Beam.get_Parameter(BuiltInParameter.CLEAR_COVER_TOP)?.AsDouble() ?? 0;
            double otherCoverFt = Beam.get_Parameter(BuiltInParameter.CLEAR_COVER_OTHER)?.AsDouble() ?? 0;
            double coverFt = (otherCoverFt > 0) ? otherCoverFt : topCoverFt;
            CoverMm = (coverFt > 0) 
                ? UnitUtils.ConvertFromInternalUnits(coverFt, UnitTypeId.Millimeters) 
                : 25.0;

            // Geometry & Axes
            if (Beam.Location is LocationCurve locCurve && locCurve.Curve != null)
            {
                StartPoint = locCurve.Curve.GetEndPoint(0);
                EndPoint = locCurve.Curve.GetEndPoint(1);

                XYZ dir = (EndPoint - StartPoint);
                LengthMm = UnitUtils.ConvertFromInternalUnits(dir.GetLength(), UnitTypeId.Millimeters);

                BasisX = dir.Normalize();
                BasisZ = XYZ.BasisZ;

                // Nếu dầm nghiêng, hiệu chỉnh BasisZ vuông góc với BasisX
                if (Math.Abs(BasisX.DotProduct(XYZ.BasisZ)) > 0.999)
                {
                    BasisY = XYZ.BasisY;
                    BasisZ = BasisX.CrossProduct(BasisY).Normalize();
                }
                else
                {
                    BasisY = XYZ.BasisZ.CrossProduct(BasisX).Normalize();
                    BasisZ = BasisX.CrossProduct(BasisY).Normalize();
                }
            }

            // Dimensions: b and h
            var type = Beam.Symbol;
            double? bFt = GetParamValue(type, "b") 
                       ?? GetParamValue(type, "Width") 
                       ?? GetParamValue(type, "Chiều rộng")
                       ?? GetParamValue(Beam, "b");

            double? hFt = GetParamValue(type, "h") 
                       ?? GetParamValue(type, "Height") 
                       ?? GetParamValue(type, "Chiều cao")
                       ?? GetParamValue(Beam, "h");

            if (bFt == null || hFt == null)
            {
                var bb = Beam.get_BoundingBox(null);
                if (bb != null)
                {
                    double dx = Math.Abs(bb.Max.X - bb.Min.X);
                    double dy = Math.Abs(bb.Max.Y - bb.Min.Y);
                    double dz = Math.Abs(bb.Max.Z - bb.Min.Z);
                    hFt = dz;
                    bFt = Math.Min(dx, dy);
                }
            }

            if (bFt.HasValue && bFt.Value > 0)
                WidthMm = UnitUtils.ConvertFromInternalUnits(bFt.Value, UnitTypeId.Millimeters);
            if (hFt.HasValue && hFt.Value > 0)
                HeightMm = UnitUtils.ConvertFromInternalUnits(hFt.Value, UnitTypeId.Millimeters);
        }

        private static double? GetParamValue(Element e, string name)
        {
            var p = e.LookupParameter(name);
            if (p != null && p.StorageType == StorageType.Double)
                return p.AsDouble();
            return null;
        }
    }
}
