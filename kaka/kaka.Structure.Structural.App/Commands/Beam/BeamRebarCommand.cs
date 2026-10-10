#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using kaka.Structure.Structural.App.ViewModels.Beam;
using kaka.Structure.Structural.App.Views.Beam;

namespace kaka.Structure.Structural.App.Commands.Beam
{
    [Transaction(TransactionMode.Manual)]
    public sealed class BeamRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Nạp assembly phụ thuộc để tránh lỗi nạp XAML resource
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string utilsDllPath = Path.Combine(assemblyFolder, "BimSpeedUtils.dll");
                if (File.Exists(utilsDllPath))
                {
                    try { Assembly.LoadFrom(utilsDllPath); } catch { }
                }

                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                if (uidoc == null)
                {
                    message = "No active document.";
                    return Result.Failed;
                }

                // 1. Lấy danh sách dầm được chọn
                var beams = PickBeams(uidoc);
                if (beams.Count == 0)
                {
                    message = "Không có dầm nào được chọn.";
                    return Result.Cancelled;
                }

                // 2. Khởi tạo ViewModel & Window
                var vm = new BeamRebarViewModel(uidoc, beams);
                var win = new BeamRebarWindow(vm);

                win.ShowDialog();

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }

        private static List<FamilyInstance> PickBeams(UIDocument uidoc)
        {
            var sel = uidoc.Selection;
            var doc = uidoc.Document;

            // 1. Nếu đã chọn trước khi bấm nút
            var preSelectedIds = sel.GetElementIds();
            if (preSelectedIds != null && preSelectedIds.Count > 0)
            {
                var preBeams = preSelectedIds
                    .Select(id => doc.GetElement(id))
                    .OfType<FamilyInstance>()
                    .Where(fi => fi.Category != null &&
                                 fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming)
                    .ToList();

                if (preBeams.Count > 0)
                {
                    return preBeams;
                }
            }

            // 2. Chưa chọn -> Yêu cầu quét chọn
            try
            {
                var refs = sel.PickObjects(
                    ObjectType.Element,
                    new BeamSelectionFilter(),
                    "Chọn các dầm cần bố trí cốt thép rồi bấm Finish");

                return refs
                    .Select(r => doc.GetElement(r))
                    .OfType<FamilyInstance>()
                    .Where(fi => fi.Category != null &&
                                 fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming)
                    .ToList();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new List<FamilyInstance>();
            }
        }

        private sealed class BeamSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is not FamilyInstance fi) return false;
                return fi.Category?.Id.Value == (int)BuiltInCategory.OST_StructuralFraming;
            }

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}