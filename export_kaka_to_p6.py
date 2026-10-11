"""
========================================================================================
KAKA REVIT DATABASE TO PRIMAVERA P6 AUTOMATED EXPORT ENGINE
========================================================================================
Source: C:\\Working\\kaka\\Mindmap\\mindmap.db
Standards: Encore Group Primavera P6 Training Manual Architecture
Outputs:
  1. KAKA_P6_Full_Structural_Schedule.csv (P6 Spreadsheet Import Format)
  2. KAKA_P6_Full_Structural_Schedule.xer (Native Primavera P6 XER Exchange Format)
========================================================================================
"""

import sqlite3
import csv
import os
import sys
import datetime

DB_PATH = r"C:\Working\kaka\Mindmap\mindmap.db"
OUTPUT_DIR = r"c:\Users\thanhvp\Documents\GitHub\JD BIM"
CSV_PATH = os.path.join(OUTPUT_DIR, "KAKA_P6_Full_Structural_Schedule.csv")
XER_PATH = os.path.join(OUTPUT_DIR, "KAKA_P6_Full_Structural_Schedule.xer")

# 14 Structural Components Mapping
STRUCT_CODES = [
    '01.Coc', '02.Mong', '03.DaKieng', '04.Nen', '05.Cot', 
    '06.Vach', '07.Dam', '08.San', '09.CauThang', '10.TamCap', 
    '11.Ramp', '12.Pit', '13.Dock', '14.CauKienPhu'
]

COMPONENT_NAMES_VI = {
    '01.Coc': 'Công tác Cọc (Pile Works)',
    '02.Mong': 'Công tác Móng (Foundations)',
    '03.DaKieng': 'Công tác Đà kiềng (Tie Beams)',
    '04.Nen': 'Công tác Nền & Sàn trệt (Slab on Grade)',
    '05.Cot': 'Công tác Cột (Columns)',
    '06.Vach': 'Công tác Vách bê tông (Shear Walls)',
    '07.Dam': 'Công tác Dầm kết cấu (Beams)',
    '08.San': 'Công tác Sàn kết cấu (Slabs)',
    '09.CauThang': 'Công tác Cầu thang (Stairs)',
    '10.TamCap': 'Công tác Tam cấp (Steps)',
    '11.Ramp': 'Công tác Ramp dốc (Ramps)',
    '12.Pit': 'Công tác Hố Pit (Elevator Pits)',
    '13.Dock': 'Công tác Sàn Dock (Loading Docks)',
    '14.CauKienPhu': 'Công tác Cấu kiện phụ (Secondary Elements)'
}

def clean_text(text):
    if not text:
        return ""
    # Remove excessive newlines
    lines = [line.strip() for line in text.strip().split('\n') if line.strip()]
    return " - ".join(lines)

def extract_kaka_structure():
    if not os.path.exists(DB_PATH):
        raise FileNotFoundError(f"Database not found: {DB_PATH}")

    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    wbs_list = []
    activities = []

    # 1. Root WBS
    wbs_list.append({
        'wbs_id': 100,
        'parent_wbs_id': None,
        'wbs_code': 'PROJ-FACTORY',
        'wbs_name': 'Dự án Nhà máy Công nghiệp Hải Phòng 30.000m2'
    })
    wbs_list.append({
        'wbs_id': 110,
        'parent_wbs_id': 100,
        'wbs_code': 'WBS-KC',
        'wbs_name': 'Phân hệ Kết cấu Kaka (Structural Engineering Works)'
    })

    wbs_counter = 120
    act_counter = 1000

    # Query Level 1 components
    for comp_code in STRUCT_CODES:
        comp_name = COMPONENT_NAMES_VI.get(comp_code, comp_code)
        comp_wbs_id = wbs_counter
        wbs_counter += 1

        wbs_code = f"WBS-KC.{comp_code}"
        wbs_list.append({
            'wbs_id': comp_wbs_id,
            'parent_wbs_id': 110,
            'wbs_code': wbs_code,
            'wbs_name': f"{comp_code} - {comp_name}"
        })

        # Fetch Level 2 and Level 3 nodes from DB
        c.execute("""
            SELECT c2.node_id, c2.title, c2.labels
            FROM categories c1
            JOIN categories c2 ON c2.parent_id = c1.node_id
            WHERE c1.module_id = 'KET_CAU' AND c1.item_code = ?
            ORDER BY c2.node_id
        """, (comp_code,))
        l2_rows = c.fetchall()

        for l2_id, l2_title, l2_labels in l2_rows:
            cleaned_l2_title = clean_text(l2_title)

            # Check if l2 has children (Level 3)
            c.execute("""
                SELECT c3.node_id, c3.title, c3.labels
                FROM categories c3
                WHERE c3.parent_id = ?
                ORDER BY c3.node_id
            """, (l2_id,))
            l3_rows = c.fetchall()

            if l3_rows:
                # Add sub-activities for each l3
                for l3_id, l3_title, l3_labels in l3_rows:
                    cleaned_l3_title = clean_text(l3_title)
                    act_counter += 10
                    act_code = f"ACT-KC-{comp_code[:2]}-{act_counter}"
                    act_name = f"{cleaned_l2_title}: {cleaned_l3_title}"
                    
                    # Determine resource type based on keyword
                    res = "RES-LABOR (Đội thi công)"
                    cost_acc = "CA-DIRECT"
                    dur = 5
                    if "Bê tông" in act_name or "bê tông" in act_name:
                        res = "RES-CONC (Bê tông thương phẩm B25/B7.5)"
                        cost_acc = "CA-CONC-01"
                        dur = 3
                    elif "Thép" in act_name or "thép" in act_name:
                        res = "RES-STEEL (Thép cây CB400-V/CB300)"
                        cost_acc = "CA-STEEL-02"
                        dur = 6
                    elif "Ván khuôn" in act_name or "ván khuôn" in act_name:
                        res = "RES-FORMWORK (Ván phủ phim & Giàn giáo)"
                        cost_acc = "CA-FORM-03"
                        dur = 4
                    elif "Cọc" in act_name or "cọc" in act_name:
                        res = "RES-PILE (Máy ép cọc Robot & Cọc D400)"
                        cost_acc = "CA-PILE-00"
                        dur = 10
                    elif "Trám lỗ ti" in act_name:
                        res = "RES-LABOR (Thợ hoàn thiện vữa Sika)"
                        cost_acc = "CA-FINISH-04"
                        dur = 2

                    notes = f"Kaka Label: {l3_labels or l2_labels or 'Revit Standard'}; Module: KET_CAU"

                    activities.append({
                        'act_id': act_counter,
                        'act_code': act_code,
                        'act_name': act_name[:120], # limit length
                        'wbs_id': comp_wbs_id,
                        'wbs_code': wbs_code,
                        'duration': dur,
                        'resource': res,
                        'cost_account': cost_acc,
                        'notes': notes
                    })
            else:
                act_counter += 10
                act_code = f"ACT-KC-{comp_code[:2]}-{act_counter}"
                act_name = cleaned_l2_title

                res = "RES-LABOR (Đội thợ kết cấu)"
                cost_acc = "CA-DIRECT"
                dur = 4
                if "Bê tông" in act_name:
                    res = "RES-CONC (Bê tông B25)"
                    cost_acc = "CA-CONC-01"
                elif "Thép" in act_name:
                    res = "RES-STEEL (Thép cốt)"
                    cost_acc = "CA-STEEL-02"
                elif "Ván khuôn" in act_name:
                    res = "RES-FORMWORK (Ván khuôn phủ phim)"
                    cost_acc = "CA-FORM-03"

                notes = f"Kaka Label: {l2_labels or 'Revit Category'}; Module: KET_CAU"

                activities.append({
                    'act_id': act_counter,
                    'act_code': act_code,
                    'act_name': act_name[:120],
                    'wbs_id': comp_wbs_id,
                    'wbs_code': wbs_code,
                    'duration': dur,
                    'resource': res,
                    'cost_account': cost_acc,
                    'notes': notes
                })

    conn.close()
    return wbs_list, activities

def generate_csv(wbs_list, activities):
    with open(CSV_PATH, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.writer(f)
        # Header according to Primavera P6 Spreadsheet format
        writer.writerow(['Activity ID', 'Activity Name', 'WBS Code', 'Original Duration (Days)', 'Primary Resource', 'Cost Account', 'Status', 'Notes'])
        for a in activities:
            writer.writerow([
                a['act_code'],
                a['act_name'],
                a['wbs_code'],
                a['duration'],
                a['resource'],
                a['cost_account'],
                'Not Started',
                a['notes']
            ])
    print(f"[OK] Generated P6 CSV: {CSV_PATH} ({len(activities)} activities)")

def strip_accents(text):
    if not text:
        return ""
    import unicodedata
    text = text.replace('đ', 'd').replace('Đ', 'D')
    text = unicodedata.normalize('NFD', text)
    text = ''.join(c for c in text if unicodedata.category(c) != 'Mn')
    return unicodedata.normalize('NFC', text)

def generate_xer(wbs_list, activities):
    now_str = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
    date_start_str = "2026-03-01 08:00"
    date_end_str = "2026-11-30 17:00"

    xer_lines = []
    xer_lines.append("ERMHDR\t8.3\t2026-10-10\tPROJECT\tPROJECT MANAGER\tADMIN\tkaka.Structure")
    
    # Table: PROJECT
    xer_lines.append("%T\tPROJECT")
    xer_lines.append("%F\tproj_id\tacct_id\torig_proj_id\tplan_start_date\tplan_end_date\tproj_short_name\tproj_title")
    clean_p_title = strip_accents("Dự án Nhà máy Hải Phòng 30.000m2 (Kaka BIM-P6 Mapping)")
    xer_lines.append(f"%R\t1\t1\t1\t{date_start_str}\t{date_end_str}\tPROJ-KAKA-FACTORY\t{clean_p_title}")

    # Table: PROJWBS
    xer_lines.append("%T\tPROJWBS")
    xer_lines.append("%F\twbs_id\tproj_id\tparent_wbs_id\twbs_short_name\twbs_name\tseq_num")
    for idx, w in enumerate(wbs_list):
        parent_val = str(w['parent_wbs_id']) if w['parent_wbs_id'] is not None else ""
        clean_w_name = strip_accents(w['wbs_name'])
        xer_lines.append(f"%R\t{w['wbs_id']}\t1\t{parent_val}\t{w['wbs_code']}\t{clean_w_name}\t{idx+1}")

    # Table: TASK
    xer_lines.append("%T\tTASK")
    xer_lines.append("%F\ttask_id\tproj_id\twbs_id\ttask_code\ttask_name\ttarget_drtn_hr_cnt\ttask_type\tstatus_code")
    for a in activities:
        duration_hrs = a['duration'] * 8
        clean_act_name = strip_accents(a['act_name'])
        xer_lines.append(f"%R\t{a['act_id']}\t1\t{a['wbs_id']}\t{a['act_code']}\t{clean_act_name}\t{duration_hrs}\tTT_Task\tTK_NotStart")

    with open(XER_PATH, 'w', encoding='utf-8') as f:
        f.write("\n".join(xer_lines) + "\n")
    print(f"[OK] Generated P6 XER: {XER_PATH} ({len(wbs_list)} WBS nodes, {len(activities)} Tasks)")

if __name__ == '__main__':
    wbs_data, act_data = extract_kaka_structure()
    print(f"Extracted {len(wbs_data)} WBS elements and {len(act_data)} activities from kaka database.")
    generate_csv(wbs_data, act_data)
    generate_xer(wbs_data, act_data)
