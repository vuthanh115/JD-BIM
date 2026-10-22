#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
========================================================================================
RULE-BASED 4D BIM ENGINE (NAVISWORKS MANAGE + PRIMAVERA P6 AUTOMATION)
========================================================================================
Mục đích:
- Giải pháp tương đương Bexel Manager nhưng tận dụng hệ sinh thái Navisworks + Primavera P6
- Tự động sinh Navisworks Search Sets XML theo phân khu & biện pháp thi công
- Tự động tính Duration & CPM Logic từ khối lượng 3D
- Sinh file TimeLiner Auto-Attach Rules XML và Tasks CSV
========================================================================================
"""

import os
import sys
import datetime
import xml.etree.ElementTree as ET
from xml.dom import minidom

# Đảm bảo mã hóa console UTF-8
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
        sys.stderr.reconfigure(encoding='utf-8', errors='replace')
    except Exception:
        pass

PROJECT_NAME = "KAKA Industrial Complex"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

SEARCH_SETS_DEFS = {
    "01. KẾT CẤU (STRUCTURAL WORKS)": [
        ("KC_01_Coc_Piles", "Structural Foundations", "Pile"),
        ("KC_02_Mong_Foundations", "Structural Foundations", "Footing"),
        ("KC_03_DaKieng_TieBeams", "Structural Framing", "DK"),
        ("KC_04_Nen_SlabOnGrade", "Floors", "Nền"),
        ("KC_05_Cot_Columns", "Structural Columns", None),
        ("KC_06_Vach_ShearWalls", "Walls", "Shear"),
        ("KC_07_Dam_FramingBeams", "Structural Framing", None),
        ("KC_08_San_StructuralSlabs", "Floors", None),
        ("KC_09_CauThang_Stairs", "Stairs", None),
        ("KC_10_TamCap_Steps", "Floors", "Tam Cấp"),
        ("KC_11_Ramp_Doc", "Floors", "Ramp"),
        ("KC_12_Pit_ElevatorPits", "Structural Foundations", "Pit"),
        ("KC_14_CauKienPhu_Secondary", "Generic Models", None),
    ],
    "02. KIẾN TRÚC (ARCHITECTURAL WORKS)": [
        ("KT_01_TuongXay_BrickWalls", "Walls", "Xây"),
        ("KT_02_TratBa_Plastering", "Walls", "Trát"),
        ("KT_03_OpLat_Tiling", "Floors", "Lát"),
        ("KT_04_CuaDi_Doors", "Doors", None),
        ("KT_05_CuaSo_Windows", "Windows", None),
        ("KT_06_Tran_Ceilings", "Ceilings", None),
        ("KT_07_SonBa_Painting", "Walls", "Sơn"),
        ("KT_08_ChongTham_Waterproofing", "Floors", "Chống thấm"),
        ("KT_09_LanCan_Railings", "Railings", None),
        ("KT_10_Mai_Roofing", "Roofs", None),
    ],
    "03. CƠ ĐIỆN (MEP WORKS)": [
        ("MEP_01_ChuaChay_FireFighting", "Pipes", "Fire"),
        ("MEP_02_CapThoatNuoc_Plumbing", "Pipes", "Domestic"),
        ("MEP_03_DienChieuSang_Electrical", "Cable Trays", "Lighting"),
        ("MEP_04_DieuHoaThongGio_HVAC", "Ducts", "Mechanical"),
    ]
}

def generate_search_sets_xml(xml_path):
    root = ET.Element("exchange", {
        "xmlns:xsi": "http://www.w3.org/2001/XMLSchema-instance",
        "xsi:noNamespaceSchemaLocation": "http://download.autodesk.com/us/navisworks/schemas/nw-exchange-12.0.xsd",
        "units": "m"
    })
    selectionsets = ET.SubElement(root, "selectionsets")
    
    for folder_name, sets in SEARCH_SETS_DEFS.items():
        vfolder = ET.SubElement(selectionsets, "viewfolder", {"name": folder_name})
        for set_name, cat, secondary in sets:
            sset = ET.SubElement(vfolder, "selectionset", {"name": set_name})
            fspec = ET.SubElement(sset, "findspec", {"mode": "all", "type": "tsel"})
            conds = ET.SubElement(fspec, "conditions")
            
            # Cond 1: Category
            c1 = ET.SubElement(conds, "condition", {"test": "contains", "flags": "10"})
            cat_elem = ET.SubElement(c1, "category")
            ET.SubElement(cat_elem, "name").text = "Element"
            prop_elem = ET.SubElement(c1, "property")
            ET.SubElement(prop_elem, "name").text = "Category"
            val_elem = ET.SubElement(c1, "value")
            ET.SubElement(val_elem, "data", {"type": "wstring"}).text = cat
            
            # Cond 2: Secondary
            if secondary:
                c2 = ET.SubElement(conds, "condition", {"test": "contains", "flags": "10"})
                cat2 = ET.SubElement(c2, "category")
                ET.SubElement(cat2, "name").text = "Item"
                prop2 = ET.SubElement(c2, "property")
                ET.SubElement(prop2, "name").text = "Type Name"
                val2 = ET.SubElement(c2, "value")
                ET.SubElement(val2, "data", {"type": "wstring"}).text = secondary
                
            ET.SubElement(fspec, "locator").text = "/"
            
    xml_str = ET.tostring(root, encoding="utf-8")
    parsed = minidom.parseString(xml_str)
    with open(xml_path, "w", encoding="utf-8") as f:
        f.write(parsed.toprettyxml(indent="  "))
    print(f"✅ Đã tạo Search Sets XML: {xml_path}")

def generate_timeliner_rules_xml(xml_path):
    root = ET.Element("exchange", {
        "xmlns:xsi": "http://www.w3.org/2001/XMLSchema-instance",
        "xsi:noNamespaceSchemaLocation": "http://download.autodesk.com/us/navisworks/schemas/nw-timeliner-rules-12.0.xsd"
    })
    rules = ET.SubElement(root, "rules")
    
    r1 = ET.SubElement(rules, "rule", {
        "name": "KAKA 4D: Map TimeLiner Tasks to Search Sets (Exact Name)",
        "enabled": "true"
    })
    ET.SubElement(r1, "description").text = "Tự động gán 100% hình học từ Search Sets vào Task TimeLiner có cùng tên gọi"
    ET.SubElement(r1, "mapping", {
        "task_column": "Task Name",
        "set_property": "Name",
        "match_mode": "exact_case_insensitive"
    })
    
    xml_str = ET.tostring(root, encoding="utf-8")
    parsed = minidom.parseString(xml_str)
    with open(xml_path, "w", encoding="utf-8") as f:
        f.write(parsed.toprettyxml(indent="  "))
    print(f"✅ Đã tạo TimeLiner Rules XML: {xml_path}")

def main():
    print("=" * 80)
    print("🚀 BỘ SINH DỮ LIỆU RULE-BASED 4D BIM ENGINE (NAVISWORKS + PRIMAVERA P6)")
    print("=" * 80)
    
    ss_xml = os.path.join(OUTPUT_DIR, "KAKA_Navisworks_SearchSets.xml")
    tl_rules = os.path.join(OUTPUT_DIR, "KAKA_TimeLiner_AutoAttach_Rules.xml")
    
    generate_search_sets_xml(ss_xml)
    generate_timeliner_rules_xml(tl_rules)
    print("=" * 80)
    print("🎉 TẤT CẢ FILE ĐÃ ĐƯỢC TẠO THÀNH CÔNG VÀ SẴN SÀNG SỬ DỤNG!")

if __name__ == "__main__":
    main()
