#nullable enable
using System;
using Autodesk.Revit.DB;

namespace kaka.Structure.Structural.App.Utils
{
  public static class CheckError
  {
    /// <summary>
    /// True nếu level/offset của cột bị đảo (Base cao hơn Top).
    /// Áp dụng cho FamilyInstance dạng Column.
    /// </summary>
    public static bool IsColumnLevelReversed(this FamilyInstance column)
    {
      if (column is null) throw new ArgumentNullException(nameof(column));

      var doc = column.Document;
      if (doc is null) return false;

      // Lấy base/top level + offset theo parameter phổ biến của cột Revit
      var baseLevelId = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId()
                       ?? ElementId.InvalidElementId;

      var topLevelId  = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.AsElementId()
                       ?? ElementId.InvalidElementId;

      // Một số family cột có thể không có top level (unconnected), fallback sang unconnected height
      var baseLevel = doc.GetElement(baseLevelId) as Level;
      var topLevel  = doc.GetElement(topLevelId)  as Level;

      var baseOffset = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.AsDouble() ?? 0.0;

      // Top offset param có thể không tồn tại với vài family
      var topOffset = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.AsDouble() ?? 0.0;

      // Nếu có đủ base + top level => so sánh elevation thực
      if (baseLevel is not null && topLevel is not null)
      {
        var baseZ = baseLevel.Elevation + baseOffset;
        var topZ  = topLevel.Elevation + topOffset;

        // Dùng epsilon nhỏ để tránh sai số float
        return baseZ > topZ + 1e-9;
      }

      // Fallback: nếu không có top level, thử dùng Unconnected Height
      var unconnectedHeight = column.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)?.AsDouble();

      if (baseLevel is not null && unconnectedHeight.HasValue)
      {
        // Nếu height âm => chắc chắn đảo/không hợp lệ
        return unconnectedHeight.Value < -1e-9;
      }

      // Không đủ dữ liệu => coi như không đảo (để tránh false positive)
      return false;
    }
  }
}
