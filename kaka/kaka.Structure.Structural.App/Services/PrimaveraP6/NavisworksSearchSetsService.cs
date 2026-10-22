#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using kaka.Structure.Structural.App.Models.PrimaveraP6;

namespace kaka.Structure.Structural.App.Services.PrimaveraP6;

/// <summary>
/// Dịch vụ sinh bộ Search Sets XML chuẩn hóa cho Autodesk Navisworks Manage.
/// Giúp phân nhóm mô hình 3D tự động theo phân khu (Zone), tầng cao (Level) và biện pháp thi công.
/// Thay thế tính năng tạo nhóm thủ công, tương đương luồng Rule-based của Bexel Manager.
/// </summary>
public static class NavisworksSearchSetsService
{
  public sealed class SearchSetDefinition
  {
    public string Name { get; set; } = string.Empty;
    public string CategoryInternal { get; set; } = "LcRevitData_Element";
    public string CategoryDisplayName { get; set; } = "Element";
    public string PropertyInternal { get; set; } = "LcRevitPropertyElementCategory";
    public string PropertyDisplayName { get; set; } = "Category";
    public string Value { get; set; } = string.Empty;
    public string Test { get; set; } = "contains"; // "equals", "contains", "wildcard"
    public string? SecondProperty { get; set; }
    public string? SecondValue { get; set; }
  }

  public static readonly Dictionary<string, List<SearchSetDefinition>> StandardDisciplineSets =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["01. KẾT CẤU (STRUCTURAL WORKS)"] = new()
      {
        new SearchSetDefinition { Name = "KC_01_Coc_Piles", Value = "Structural Foundations", SecondProperty = "Type Name", SecondValue = "Pile" },
        new SearchSetDefinition { Name = "KC_02_Mong_Foundations", Value = "Structural Foundations", SecondProperty = "Type Name", SecondValue = "Footing" },
        new SearchSetDefinition { Name = "KC_03_DaKieng_TieBeams", Value = "Structural Framing", SecondProperty = "Type Name", SecondValue = "DK" },
        new SearchSetDefinition { Name = "KC_04_Nen_SlabOnGrade", Value = "Floors", SecondProperty = "Type Name", SecondValue = "Nền" },
        new SearchSetDefinition { Name = "KC_05_Cot_Columns", Value = "Structural Columns" },
        new SearchSetDefinition { Name = "KC_06_Vach_ShearWalls", Value = "Walls", SecondProperty = "Structural Usage", SecondValue = "Shear" },
        new SearchSetDefinition { Name = "KC_07_Dam_FramingBeams", Value = "Structural Framing" },
        new SearchSetDefinition { Name = "KC_08_San_StructuralSlabs", Value = "Floors" },
        new SearchSetDefinition { Name = "KC_09_CauThang_Stairs", Value = "Stairs" },
        new SearchSetDefinition { Name = "KC_10_TamCap_Steps", Value = "Floors", SecondProperty = "Type Name", SecondValue = "Tam Cấp" },
        new SearchSetDefinition { Name = "KC_11_Ramp_Doc", Value = "Floors", SecondProperty = "Type Name", SecondValue = "Ramp" },
        new SearchSetDefinition { Name = "KC_12_Pit_ElevatorPits", Value = "Structural Foundations", SecondProperty = "Type Name", SecondValue = "Pit" },
        new SearchSetDefinition { Name = "KC_14_CauKienPhu_Secondary", Value = "Generic Models" }
      },

      ["02. KIẾN TRÚC (ARCHITECTURAL WORKS)"] = new()
      {
        new SearchSetDefinition { Name = "KT_01_TuongXay_BrickWalls", Value = "Walls", SecondProperty = "Type Name", SecondValue = "Xây" },
        new SearchSetDefinition { Name = "KT_02_TratBa_Plastering", Value = "Walls", SecondProperty = "Type Name", SecondValue = "Trát" },
        new SearchSetDefinition { Name = "KT_03_OpLat_Tiling", Value = "Floors", SecondProperty = "Type Name", SecondValue = "Lát" },
        new SearchSetDefinition { Name = "KT_04_CuaDi_Doors", Value = "Doors" },
        new SearchSetDefinition { Name = "KT_05_CuaSo_Windows", Value = "Windows" },
        new SearchSetDefinition { Name = "KT_06_Tran_Ceilings", Value = "Ceilings" },
        new SearchSetDefinition { Name = "KT_07_SonBa_Painting", Value = "Walls", SecondProperty = "Type Name", SecondValue = "Sơn" },
        new SearchSetDefinition { Name = "KT_08_ChongTham_Waterproofing", Value = "Floors", SecondProperty = "Type Name", SecondValue = "Chống thấm" },
        new SearchSetDefinition { Name = "KT_09_LanCan_Railings", Value = "Railings" },
        new SearchSetDefinition { Name = "KT_10_Mai_Roofing", Value = "Roofs" }
      },

      ["03. CƠ ĐIỆN (MEP WORKS)"] = new()
      {
        new SearchSetDefinition { Name = "MEP_01_ChuaChay_FireFighting", Value = "Pipes", SecondProperty = "System Type", SecondValue = "Fire" },
        new SearchSetDefinition { Name = "MEP_02_CapThoatNuoc_Plumbing", Value = "Pipes", SecondProperty = "System Type", SecondValue = "Domestic" },
        new SearchSetDefinition { Name = "MEP_03_DienChieuSang_Electrical", Value = "Cable Trays", SecondProperty = "Category", SecondValue = "Lighting Fixtures" },
        new SearchSetDefinition { Name = "MEP_04_DieuHoaThongGio_HVAC", Value = "Ducts", SecondProperty = "Category", SecondValue = "Mechanical Equipment" }
      }
    };

  /// <summary>
  /// Xuất file Search Sets XML hoàn chỉnh cho Navisworks Manage.
  /// </summary>
  public static void ExportSearchSetsXml(string filePath, string projectName, IEnumerable<Bim4DTaskItem>? tasks = null)
  {
    var settings = new XmlWriterSettings
    {
      Indent = true,
      IndentChars = "  ",
      Encoding = new UTF8Encoding(false)
    };

    using var writer = XmlWriter.Create(filePath, settings);
    writer.WriteStartDocument();
    
    // <exchange> root - không đặt default xmlns để tương thích với MSXML Schema validation của Navisworks
    writer.WriteStartElement("exchange");
    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
    writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "http://download.autodesk.com/us/navisworks/schemas/nw-exchange-12.0.xsd");
    writer.WriteAttributeString("units", "m");

    // <selectionsets>
    writer.WriteStartElement("selectionsets");

    // 1. Phân mục theo các bộ môn chuẩn hóa (Discipline folders)
    foreach (var folderKvp in StandardDisciplineSets)
    {
      writer.WriteStartElement("viewfolder");
      writer.WriteAttributeString("name", folderKvp.Key);

      foreach (var setDef in folderKvp.Value)
      {
        WriteSelectionSet(writer, setDef.Name, setDef);
      }

      writer.WriteEndElement(); // viewfolder
    }

    // 2. Nếu có danh sách công tác theo tiến độ (Tasks có SearchSetName riêng)
    if (tasks != null)
    {
      var distinctCustomSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var t in tasks)
      {
        if (!string.IsNullOrWhiteSpace(t.SearchSetName) && distinctCustomSets.Add(t.SearchSetName))
        {
          // Kiểm tra xem đã có trong standard chưa
          bool isStandard = false;
          foreach (var folder in StandardDisciplineSets.Values)
          {
            if (folder.Exists(x => string.Equals(x.Name, t.SearchSetName, StringComparison.OrdinalIgnoreCase)))
            {
              isStandard = true;
              break;
            }
          }

          if (!isStandard)
          {
            // Viết vào folder 04. PHÂN KHU THEO TIẾN ĐỘ 4D (4D WBS ZONES)
          }
        }
      }
    }

    writer.WriteEndElement(); // selectionsets
    writer.WriteEndElement(); // exchange
    writer.WriteEndDocument();
  }

  private static void WriteSelectionSet(XmlWriter writer, string setName, SearchSetDefinition def)
  {
    writer.WriteStartElement("selectionset");
    writer.WriteAttributeString("name", setName);

    writer.WriteStartElement("findspec");
    writer.WriteAttributeString("mode", "all");

    writer.WriteStartElement("conditions");

    // Condition 1: Category
    writer.WriteStartElement("condition");
    writer.WriteAttributeString("test", def.Test);
    writer.WriteAttributeString("flags", "10");

    writer.WriteStartElement("category");
    writer.WriteElementString("name", def.CategoryDisplayName);
    writer.WriteEndElement(); // category

    writer.WriteStartElement("property");
    writer.WriteElementString("name", def.PropertyDisplayName);
    writer.WriteEndElement(); // property

    writer.WriteStartElement("value");
    writer.WriteStartElement("data");
    writer.WriteAttributeString("type", "wstring");
    writer.WriteString(def.Value);
    writer.WriteEndElement(); // data
    writer.WriteEndElement(); // value

    writer.WriteEndElement(); // condition 1

    // Condition 2: Phụ trợ nếu có
    if (!string.IsNullOrWhiteSpace(def.SecondProperty) && !string.IsNullOrWhiteSpace(def.SecondValue))
    {
      writer.WriteStartElement("condition");
      writer.WriteAttributeString("test", "contains");
      writer.WriteAttributeString("flags", "10");

      writer.WriteStartElement("category");
      writer.WriteElementString("name", "Item");
      writer.WriteEndElement();

      writer.WriteStartElement("property");
      writer.WriteElementString("name", def.SecondProperty);
      writer.WriteEndElement();

      writer.WriteStartElement("value");
      writer.WriteStartElement("data");
      writer.WriteAttributeString("type", "wstring");
      writer.WriteString(def.SecondValue);
      writer.WriteEndElement();
      writer.WriteEndElement();

      writer.WriteEndElement(); // condition 2
    }

    writer.WriteEndElement(); // conditions
    writer.WriteElementString("locator", "/");

    writer.WriteEndElement(); // findspec
    writer.WriteEndElement(); // selectionset
  }
}
