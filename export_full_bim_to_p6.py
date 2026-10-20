# -*- coding: utf-8 -*-
"""
========================================================================================
FULL MULTI-DISCIPLINE BIM (STRUCTURAL + ARCHITECTURAL + MEP) TO PRIMAVERA P6 ENGINE
========================================================================================
Source: C:\\Working\\kaka\\Mindmap\\mindmap.db & BSV BIM Standards
Standards: Oracle Primavera P6 EPPM / Encore Group CPM Standard
Outputs:
  1. KAKA_P6_Full_BIM_Schedule.csv (P6 Spreadsheet Import Format)
  2. KAKA_P6_Full_BIM_Schedule.xer (Native Primavera P6 XER Exchange Format)
========================================================================================
"""

import sqlite3
import csv
import os
import sys
import datetime
import unicodedata

DB_PATH = r"kaka\Mindmap\mindmap.db"
if not os.path.exists(DB_PATH):
    DB_PATH = r"C:\Working\kaka\Mindmap\mindmap.db"

OUTPUT_DIR = r"c:\Users\thanhvp\Documents\GitHub\JD BIM"
CSV_PATH = os.path.join(OUTPUT_DIR, "KAKA_P6_Full_BIM_Schedule.csv")
XER_PATH = os.path.join(OUTPUT_DIR, "KAKA_P6_Full_BIM_Schedule.xer")

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
    lines = [line.strip() for line in text.strip().split('\n') if line.strip()]
    return " - ".join(lines)

def strip_accents(s):
    if not s:
        return ""
    s_norm = unicodedata.normalize('NFKD', s)
    return "".join(c for c in s_norm if not unicodedata.combining(c))

def build_full_bim_schedule():
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()

    wbs_list = []
    activities = []

    # 1. Root WBS: PROJ-FACTORY
    wbs_list.append({
        'wbs_id': 100,
        'parent_wbs_id': None,
        'wbs_code': 'PROJ-FACTORY',
        'wbs_name': 'Dự án Nhà máy Công nghiệp Hải Phòng 30.000m2'
    })

    # =========================================================================
    # PHÂN HỆ 1: KẾT CẤU (WBS-KC)
    # =========================================================================
    wbs_list.append({
        'wbs_id': 110,
        'parent_wbs_id': 100,
        'wbs_code': 'WBS-KC',
        'wbs_name': 'Phân hệ 1: Kết cấu Kaka (Structural Engineering Works)'
    })

    wbs_counter = 120
    act_counter = 1000

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
            c.execute("""
                SELECT c3.node_id, c3.title, c3.labels
                FROM categories c3
                WHERE c3.parent_id = ?
                ORDER BY c3.node_id
            """, (l2_id,))
            l3_rows = c.fetchall()

            if l3_rows:
                for l3_id, l3_title, l3_labels in l3_rows:
                    cleaned_l3_title = clean_text(l3_title)
                    act_counter += 10
                    act_code = f"ACT-KC-{comp_code[:2]}-{act_counter}"
                    act_name = f"{cleaned_l2_title}: {cleaned_l3_title}"
                    
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

                    activities.append({
                        'act_id': act_counter,
                        'act_code': act_code,
                        'act_name': act_name[:120],
                        'wbs_id': comp_wbs_id,
                        'wbs_code': wbs_code,
                        'duration': dur,
                        'resource': res,
                        'cost_account': cost_acc,
                        'notes': f"Module: KET_CAU; Component: {comp_code}"
                    })

    # =========================================================================
    # PHÂN HỆ 2: KIẾN TRÚC (WBS-KT)
    # =========================================================================
    kt_root_id = wbs_counter
    wbs_counter += 1
    wbs_list.append({
        'wbs_id': kt_root_id,
        'parent_wbs_id': 100,
        'wbs_code': 'WBS-KT',
        'wbs_name': 'Phân hệ 2: Kiến trúc & Hoàn thiện (Architectural Works)'
    })

    kt_subs = [
        ('WBS-KT.01.Tuong', 'Công tác Tường xây (Block & Gạch tuynel)', [
            ('Xây tường ngoài bao che BSV_ExWall (chống thấm)', 12, 'RES-MASON (Đội thợ nề xây tường)', 'CA-KT-WALL'),
            ('Xây tường trong ngăn chia phòng BSV_InWall', 10, 'RES-MASON (Đội thợ nề xây tường)', 'CA-KT-WALL'),
            ('Đổ bê tông bổ trụ BSV_BoTru', 5, 'RES-CONC (Bê tông B20 bổ trụ)', 'CA-KT-CONC'),
            ('Lắp dựng cốp pha & đổ bê tông lanh tô giằng tường', 6, 'RES-MASON (Thợ cốp pha lanh tô)', 'CA-KT-CONC')
        ]),
        ('WBS-KT.02.Trat', 'Công tác Trát tường & Hoàn thiện thô', [
            ('Trát tường ngoài dày 20mm trộn phụ gia chống thấm', 10, 'RES-PLASTER (Đội thợ trát)', 'CA-KT-PLASTER'),
            ('Trát tường trong dày 15mm mác M75', 12, 'RES-PLASTER (Đội thợ trát)', 'CA-KT-PLASTER'),
            ('Trát cạnh cửa & đắp Sill dốc cửa sổ', 4, 'RES-PLASTER (Đội thợ trát)', 'CA-KT-PLASTER')
        ]),
        ('WBS-KT.03.SanTran', 'Công tác Hoàn thiện Sàn & Trần (Room Finish)', [
            ('Chống thấm sàn khu vệ sinh & mái (Màng Sika/Bitum)', 6, 'RES-WATERPROOF (Chống thấm chuyên nghiệp)', 'CA-KT-WATERPROOF'),
            ('Cán vữa tạo dốc sàn vệ sinh & hành lang', 5, 'RES-MASON (Đội cán vữa)', 'CA-KT-FLOOR'),
            ('Ốp lát gạch ceramic/granite sàn & chân tường', 10, 'RES-TILER (Đội thợ ốp lát)', 'CA-KT-FLOOR'),
            ('Lắp đặt khung xương & đóng tấm trần thạch cao chống ẩm', 8, 'RES-CEILING (Đội trần thạch cao)', 'CA-KT-CEILING')
        ]),
        ('WBS-KT.04.Cua', 'Công tác Cửa đi, Cửa sổ & Vách kính', [
            ('Lắp đặt khung bao cửa đi nhôm kính & cửa thép chống cháy', 6, 'RES-DOOR (Đội lắp đặt cửa)', 'CA-KT-DOOR'),
            ('Lắp cánh cửa, phụ kiện khóa tay gạt & chèn keo Foam', 5, 'RES-DOOR (Đội lắp đặt cửa)', 'CA-KT-DOOR'),
            ('Lắp đặt hệ vách kính mặt dựng Curtain Wall', 12, 'RES-FACADE (Nhà thầu nhôm kính)', 'CA-KT-FACADE')
        ]),
        ('WBS-KT.05.Son', 'Công tác Sơn bả & Vệ sinh công nghiệp', [
            ('Bả matic 2 lớp & xả nhám phẳng tường trần', 8, 'RES-PAINTER (Đội sơn bả)', 'CA-KT-PAINT'),
            ('Sơn lót kháng kiềm 1 lớp & sơn phủ màu 2 lớp', 6, 'RES-PAINTER (Đội sơn bả)', 'CA-KT-PAINT'),
            ('Vệ sinh công nghiệp & bàn giao khối kiến trúc', 4, 'RES-CLEAN (Đội vệ sinh)', 'CA-KT-CLEAN')
        ])
    ]

    for sub_code, sub_name, sub_acts in kt_subs:
        sub_wbs_id = wbs_counter
        wbs_counter += 1
        wbs_list.append({
            'wbs_id': sub_wbs_id,
            'parent_wbs_id': kt_root_id,
            'wbs_code': sub_code,
            'wbs_name': sub_name
        })

        for a_title, a_dur, a_res, a_ca in sub_acts:
            act_counter += 10
            act_code = f"ACT-KT-{act_counter}"
            activities.append({
                'act_id': act_counter,
                'act_code': act_code,
                'act_name': a_title,
                'wbs_id': sub_wbs_id,
                'wbs_code': sub_code,
                'duration': a_dur,
                'resource': a_res,
                'cost_account': a_ca,
                'notes': f"Module: KIEN_TRUC; Sub: {sub_code}"
            })

    # =========================================================================
    # PHÂN HỆ 3: CƠ ĐIỆN (WBS-MEP)
    # =========================================================================
    mep_root_id = wbs_counter
    wbs_counter += 1
    wbs_list.append({
        'wbs_id': mep_root_id,
        'parent_wbs_id': 100,
        'wbs_code': 'WBS-MEP',
        'wbs_name': 'Phân hệ 3: Cơ điện & PCCC (MEP & Fire Protection Works)'
    })

    mep_subs = [
        ('WBS-MEP.01.AmKetCau', 'Công tác Đặt Sleeves & Ống âm kết cấu', [
            ('Đặt Sleeves ống dầm sàn theo bản vẽ SOD trước khi đổ BT', 5, 'RES-MEP (Đội thợ điện nước)', 'CA-MEP-SLEEVES'),
            ('Đi ống luồn dây điện Conduit âm sàn kết cấu', 6, 'RES-ELEC (Đội thợ điện)', 'CA-MEP-CONDUIT')
        ]),
        ('WBS-MEP.02.HVAC', 'Hệ thống Thông gió & Điều hòa không khí (HVAC)', [
            ('Lắp đặt giá treo Unistrut & ống gió tôn mạ kẽm (Ductwork)', 12, 'RES-HVAC (Đội thợ ống gió)', 'CA-MEP-HVAC'),
            ('Lắp đặt đường ống nước lạnh Chilled Water & bọc cách nhiệt', 10, 'RES-HVAC (Thợ hàn ống áp lực)', 'CA-MEP-CHILLER'),
            ('Lắp đặt dàn nóng/lạnh VRV, AHU & quạt thông gió nhà xưởng', 8, 'RES-HVAC (Kỹ thuật viên cơ điện)', 'CA-MEP-EQUIP'),
            ('Cân bằng lưu lượng gió và nước (TAB testing)', 5, 'RES-COMMISSION (Chuyên gia TAB)', 'CA-MEP-TAB')
        ]),
        ('WBS-MEP.03.Elec', 'Hệ thống Điện Động lực, Trạm Biến áp & ELV', [
            ('Lắp đặt thang máng cáp điện (Cable Tray / Ladder)', 10, 'RES-ELEC (Thợ máng cáp)', 'CA-MEP-TRAY'),
            ('Kéo rải cáp nguồn chính trung thế & hạ thế Cu/XLPE/PVC', 8, 'RES-ELEC (Đội kéo cáp)', 'CA-MEP-CABLE'),
            ('Lắp đặt máy biến áp, máy phát điện dự phòng & tủ tổng MSB', 6, 'RES-ELEC (Kỹ sư điện chính)', 'CA-MEP-SUBSTATION'),
            ('Lắp đặt tủ tầng DB, đèn chiếu sáng Led Highbay & công tắc', 10, 'RES-ELEC (Thợ điện chiếu sáng)', 'CA-MEP-LIGHT'),
            ('Kéo rải cáp mạng LAN, camera CCTV, âm thanh PA & Access Control', 8, 'RES-ELV (Kỹ thuật viên điện nhẹ)', 'CA-MEP-ELV')
        ]),
        ('WBS-MEP.04.Plumb', 'Hệ thống Cấp Thoát Nước & Xử lý Nước thải (Plumbing)', [
            ('Lắp đặt trục đứng Riser cấp nước PPR & thoát nước uPVC', 8, 'RES-PLUMB (Thợ cấp thoát nước)', 'CA-MEP-PIPE'),
            ('Lắp đặt hệ thống phễu thu nước mưa mái Siphonic nhà xưởng', 7, 'RES-PLUMB (Thợ cấp thoát nước)', 'CA-MEP-STORM'),
            ('Lắp cụm bơm cấp nước biến tần & thiết bị vệ sinh (Lavabo, WC)', 6, 'RES-PLUMB (Thợ lắp thiết bị)', 'CA-MEP-PLUMB'),
            ('Thử áp lực thủy tĩnh đường ống 1.5x (Hydrostatic test)', 3, 'RES-PLUMB (Đội thử áp lực)', 'CA-MEP-TEST')
        ]),
        ('WBS-MEP.05.FP', 'Hệ thống Phòng cháy chữa cháy (Fire Protection)', [
            ('Lắp đặt đường ống chữa cháy Sprinkler thép đen SCH40', 12, 'RES-FP (Thợ hàn & cùm rãnh PCCC)', 'CA-MEP-FP'),
            ('Lắp đặt cụm máy bơm chữa cháy cứu hỏa chuẩn NFPA 20', 6, 'RES-FP (Kỹ sư lắp bơm PCCC)', 'CA-MEP-PUMP'),
            ('Kéo dây & lắp đặt đầu báo khói, còi đèn báo cháy địa chỉ', 8, 'RES-FP (Thợ báo cháy)', 'CA-MEP-ALARM'),
            ('Chạy thử liên động PCCC & kiểm định nghiệm thu Công an PCCC', 5, 'RES-COMMISSION (Chuyên gia PCCC)', 'CA-MEP-INSPECT')
        ]),
        ('WBS-MEP.06.TC', 'Chạy thử liên động toàn hệ thống (Commissioning)', [
            ('Đóng điện trạm biến áp & hòa đồng bộ máy phát điện', 3, 'RES-COMMISSION (Chuyên gia đóng điện)', 'CA-MEP-COMM'),
            ('Chạy thử tải liên động toàn nhà xưởng & bàn giao O&M', 5, 'RES-COMMISSION (Ban nghiệm thu T&C)', 'CA-MEP-COMM')
        ])
    ]

    for sub_code, sub_name, sub_acts in mep_subs:
        sub_wbs_id = wbs_counter
        wbs_counter += 1
        wbs_list.append({
            'wbs_id': sub_wbs_id,
            'parent_wbs_id': mep_root_id,
            'wbs_code': sub_code,
            'wbs_name': sub_name
        })

        for a_title, a_dur, a_res, a_ca in sub_acts:
            act_counter += 10
            act_code = f"ACT-MEP-{act_counter}"
            activities.append({
                'act_id': act_counter,
                'act_code': act_code,
                'act_name': a_title,
                'wbs_id': sub_wbs_id,
                'wbs_code': sub_code,
                'duration': a_dur,
                'resource': a_res,
                'cost_account': a_ca,
                'notes': f"Module: MEP; Sub: {sub_code}"
            })

    print(f"Generated {len(wbs_list)} WBS nodes and {len(activities)} Activities across 3 Disciplines (KC + KT + MEP)!")
    return wbs_list, activities

def generate_csv(wbs_list, activities):
    with open(CSV_PATH, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow([
            "Activity ID", 
            "Activity Name", 
            "WBS Code", 
            "Original Duration (Days)", 
            "Primary Resource", 
            "Cost Account", 
            "Notes / Module Reference"
        ])
        for act in activities:
            writer.writerow([
                act['act_code'],
                act['act_name'],
                act['wbs_code'],
                act['duration'],
                act['resource'],
                act['cost_account'],
                act['notes']
            ])
    print(f"Exported CSV: {CSV_PATH}")

def generate_xer(wbs_list, activities):
    date_start_str = "2026-03-01 08:00"
    date_end_str = "2026-11-30 17:00"

    xer_lines = []
    xer_lines.append("ERMHDR\t8.3\t2026-10-20\tPROJECT\tPROJECT MANAGER\tADMIN\tkaka.FullBIM")
    
    # Table: PROJECT
    xer_lines.append("%T\tPROJECT")
    xer_lines.append("%F\tproj_id\tacct_id\torig_proj_id\tplan_start_date\tplan_end_date\tproj_short_name\tproj_title")
    clean_p_title = strip_accents("Dự án Nhà máy Hải Phòng 30.000m2 (Full Multi-Discipline BIM: KC + KT + MEP)")
    xer_lines.append(f"%R\t1\t1\t1\t{date_start_str}\t{date_end_str}\tPROJ-FULL-BIM\t{clean_p_title}")

    # Table: PROJWBS
    xer_lines.append("%T\tPROJWBS")
    xer_lines.append("%F\twbs_id\tproj_id\tparent_wbs_id\twbs_short_name\twbs_name\tseq_num")
    for idx, w in enumerate(wbs_list):
        parent_val = str(w['parent_wbs_id']) if w['parent_wbs_id'] is not None else ""
        clean_w_name = strip_accents(w['wbs_name'])
        xer_lines.append(f"%R\t{w['wbs_id']}\t1\t{parent_val}\t{w['wbs_code']}\t{clean_w_name}\t{idx+1}")

    # Table: TASK
    xer_lines.append("%T\tTASK")
    xer_lines.append("%F\ttask_id\tproj_id\twbs_id\ttask_code\ttask_name\ttarget_drtn_hr_cnt\ttask_type")
    for act in activities:
        drtn_hr = act['duration'] * 8.0
        clean_task_name = strip_accents(act['act_name'])
        xer_lines.append(f"%R\t{act['act_id']}\t1\t{act['wbs_id']}\t{act['act_code']}\t{clean_task_name}\t{drtn_hr}\tTT_Task")

    # Table: TASKPRED (Logic relationships)
    xer_lines.append("%T\tTASKPRED")
    xer_lines.append("%F\ttask_pred_id\ttask_id\tpred_task_id\tpred_type\tlag_hr_cnt")
    pred_counter = 1
    for i in range(1, len(activities)):
        prev_act = activities[i-1]
        curr_act = activities[i]
        xer_lines.append(f"%R\t{pred_counter}\t{curr_act['act_id']}\t{prev_act['act_id']}\tPR_FS\t0.0")
        pred_counter += 1

    with open(XER_PATH, 'w', encoding='utf-8') as f:
        f.write("\n".join(xer_lines))
    print(f"Exported Native P6 XER: {XER_PATH}")

if __name__ == '__main__':
    wbs, acts = build_full_bim_schedule()
    generate_csv(wbs, acts)
    generate_xer(wbs, acts)
    print("Multi-discipline BIM integration to Primavera P6 successfully finished!")
