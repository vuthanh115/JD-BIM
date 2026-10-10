using System;
using System.Collections.Generic;
using System.IO;          // <--- Cần thêm
using System.Linq;
using System.Reflection;  // <--- Cần thêm

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using kaka.Structure.Structural.App.ViewModels.Column;
using kaka.Structure.Structural.App.Views.Column;

namespace kaka.Structure.Structural.App.Commands.Column
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ColumnRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            try
            {
                // =======================================================================
                // 🔥 FIX LỖI XamlParseException: NẠP THỦ CÔNG DLL PHỤ THUỘC
                // =======================================================================
                // Lấy thư mục chứa file .dll hiện tại của Add-in (AppData/.../kaka.Structure/)
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                
                // Đường dẫn đến file BimSpeedUtils.dll
                string utilsDllPath = Path.Combine(assemblyFolder, "BimSpeedUtils.dll");

                // Nạp vào bộ nhớ Revit để WPF (XAML) có thể tìm thấy Resource
                if (File.Exists(utilsDllPath))
                {
                    try 
                    {
                        Assembly.LoadFrom(utilsDllPath); 
                    }
                    catch { /* Bỏ qua nếu đã load rồi */ }
                }
                // =======================================================================

                var uiapp = data.Application;
                var uidoc = uiapp.ActiveUIDocument;
                if (uidoc == null)
                {
                    message = "No active document.";
                    return Result.Failed;
                }

                // 1) Pick columns (multi)
                var hosts = PickColumns(uidoc);
                if (hosts.Count == 0)
                {
                    message = "No column selected.";
                    return Result.Cancelled;
                }

                // 2) Create VM
                var vm = new ColumnRebarViewModel(uidoc);
                
                // Đẩy danh sách cột vừa chọn vào VM
                vm.SetHosts(hosts); 

                // 3) Create window
                // Lỗi XAML sẽ biến mất nhờ đoạn code load DLL ở trên
                var win = new ColumnRebarWindow(vm);

                // 4) (Optional) Set Owner để cửa sổ không bị chìm xuống dưới Revit
                // System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(win);
                // helper.Owner = uiapp.MainWindowHandle;

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

        private static List<FamilyInstance> PickColumns(UIDocument uidoc)
        {
            var sel = uidoc.Selection;
            var doc = uidoc.Document;

            // 1) Nếu đã chọn sẵn cột trước khi click lệnh -> Sử dụng luôn
            var preSelectedIds = sel.GetElementIds();
            if (preSelectedIds != null && preSelectedIds.Count > 0)
            {
                var preCols = preSelectedIds
                    .Select(id => doc.GetElement(id))
                    .OfType<FamilyInstance>()
                    .Where(fi => fi.Category != null &&
                                 fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns)
                    .ToList();

                if (preCols.Count > 0)
                {
                    return preCols;
                }
            }

            // 2) Nếu chưa chọn cột nào -> Yêu cầu người dùng quét chọn rồi bấm Finish
            try 
            {
                var refs = sel.PickObjects(
                  ObjectType.Element,
                  new ColumnSelectionFilter(),
                  "Select structural columns (multiple) then press Finish");

                return refs
                  .Select(r => doc.GetElement(r))
                  .OfType<FamilyInstance>()
                  .Where(fi => fi.Category != null &&
                               fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns)
                  .ToList();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new List<FamilyInstance>(); // Trả về list rỗng nếu user bấm Cancel
            }
        }

        private sealed class ColumnSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
            {
                if (e is not FamilyInstance fi) return false;
                return fi.Category?.Id.Value == (int)BuiltInCategory.OST_StructuralColumns;
            }

            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}