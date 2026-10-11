import sys
import os
import sqlite3
import datetime

def sync_to_p6_db(db_path, proj_code="PRJ-KAKA-BIM", proj_title="Project A - Kaka BIM P6 Integration"):
    if not os.path.exists(db_path):
        print(f"ERROR: Database file not found: {db_path}", file=sys.stderr)
        return False

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    try:
        # 1. Clean existing project
        cur.execute("SELECT proj_id FROM project WHERE proj_short_name = ?", (proj_code,))
        existing = cur.fetchall()
        for (pid,) in existing:
            cur.execute("DELETE FROM taskpred WHERE proj_id = ?", (pid,))
            cur.execute("DELETE FROM task WHERE proj_id = ?", (pid,))
            cur.execute("DELETE FROM projwbs WHERE proj_id = ?", (pid,))
            cur.execute("DELETE FROM project WHERE proj_id = ?", (pid,))

        # 2. Allocate IDs
        cur.execute("SELECT MAX(proj_id) FROM project")
        new_proj_id = (cur.fetchone()[0] or 100) + 1

        cur.execute("SELECT MAX(wbs_id) FROM projwbs")
        wbs_counter = (cur.fetchone()[0] or 1000) + 1

        cur.execute("SELECT MAX(task_id) FROM task")
        task_counter = (cur.fetchone()[0] or 10000) + 1

        cur.execute("SELECT MAX(task_pred_id) FROM taskpred")
        pred_counter = (cur.fetchone()[0] or 100000) + 1

        now_str = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        # 3. Insert PROJECT
        cur.execute("""
            INSERT INTO project (
                proj_id, proj_short_name, project_flag,
                clndr_id, def_complete_pct_type, plan_start_date, plan_end_date,
                scd_end_date, add_date, last_recalc_date, def_duration_type,
                def_qty_type, def_rate_type, def_task_type, add_by_name,
                create_date, create_user, update_date, update_user
            ) VALUES (
                ?, ?, 'Y',
                178, 'CP_Drtn', '2026-10-12 08:00:00', '2027-04-15 17:00:00',
                '2027-04-15 17:00:00', ?, ?, 'DT_FixedDUR2',
                'QT_Hour', 'COST_PER_QTY', 'TT_Task', 'admin',
                ?, 'admin', ?, 'admin'
            )
        """, (new_proj_id, proj_code, now_str, now_str, now_str, now_str))

        # 4. Insert Root WBS
        root_wbs_id = wbs_counter
        wbs_counter += 1
        cur.execute("""
            INSERT INTO projwbs (
                wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag,
                status_code, wbs_short_name, wbs_name, parent_wbs_id,
                create_date, create_user, update_date, update_user
            ) VALUES (?, ?, 540, 1, 1.0, 'Y', 'Y', 'WS_Open', ?, ?, 3063, ?, 'admin', ?, 'admin')
        """, (root_wbs_id, new_proj_id, proj_code, f"{proj_title} (Project Root)", now_str, now_str))

        # 5. Insert Sub WBS: KC
        kc_wbs_id = wbs_counter
        wbs_counter += 1
        cur.execute("""
            INSERT INTO projwbs (
                wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag,
                status_code, wbs_short_name, wbs_name, parent_wbs_id,
                create_date, create_user, update_date, update_user
            ) VALUES (?, ?, 540, 2, 1.0, 'N', 'Y', 'WS_Open', 'WBS-KC', 'Phân Hệ Kết Cấu Kaka (Structural Works)', ?, ?, 'admin', ?, 'admin')
        """, (kc_wbs_id, new_proj_id, root_wbs_id, now_str, now_str))

        components = [
            ("01.Coc", "01Coc", "Công tác Cọc (Piles & Pile Caps)", 10),
            ("02.Mong", "02Mong", "Công tác Móng (Foundations)", 12),
            ("03.DaKieng", "03DaKieng", "Công tác Đà kiềng (Tie Beams)", 8),
            ("04.Nen", "04Nen", "Công tác Nền & Lớp lót (Slab on Grade)", 15),
            ("05.Cot", "05Cot", "Công tác Cột kết cấu (Columns)", 10),
            ("06.Vach", "06Vach", "Công tác Vách thang máy (Shear Walls)", 8),
            ("07.Dam", "07Dam", "Công tác Dầm kết cấu (Framing Beams)", 9),
            ("08.San", "08San", "Công tác Sàn kết cấu (Structural Slabs)", 12),
            ("09.CauThang", "09CauThang", "Công tác Cầu thang (Stairs)", 6),
            ("10.TamCap", "10TamCap", "Công tác Tam cấp (Entrance Steps)", 4),
            ("11.Ramp", "11Ramp", "Công tác Ramp dốc (Loading Ramps)", 5),
            ("12.Pit", "12Pit", "Công tác Hố Pit (Elevator Pits)", 4),
            ("13.Dock", "13Dock", "Công tác Sàn Dock (Loading Docks)", 5),
            ("14.CauKienPhu", "14CauKienPhu", "Công tác Cấu kiện phụ (Secondary Works)", 4),
        ]

        task_types = [
            ("Thi công cốt thép & đệm lót", 0.35),
            ("Lắp dựng ván khuôn & gông giằng", 0.25),
            ("Đổ bê tông & nghiệm thu", 0.20),
            ("Bảo dưỡng & hoàn thiện bề mặt", 0.20),
        ]

        all_task_ids = []
        wbs_task_map = {}

        seq = 10
        for comp_code, comp_short, comp_name, comp_days in components:
            seq += 1
            comp_wbs_id = wbs_counter
            wbs_counter += 1

            cur.execute("""
                INSERT INTO projwbs (
                    wbs_id, proj_id, obs_id, seq_num, est_wt, proj_node_flag, sum_data_flag,
                    status_code, wbs_short_name, wbs_name, parent_wbs_id,
                    create_date, create_user, update_date, update_user
                ) VALUES (?, ?, 540, ?, 1.0, 'N', 'Y', 'WS_Open', ?, ?, ?, ?, 'admin', ?, 'admin')
            """, (comp_wbs_id, new_proj_id, seq, f"WBS-KC.{comp_code}", f"{comp_code} - {comp_name}", kc_wbs_id, now_str, now_str))

            wbs_task_map[comp_code] = []

            for suffix, ratio in task_types:
                t_dur_days = max(1, int(comp_days * ratio))
                t_dur_hrs = t_dur_days * 8.0

                current_task_id = task_counter
                task_counter += 1
                all_task_ids.append(current_task_id)
                wbs_task_map[comp_code].append(current_task_id)

                act_code = f"ACT-KC-{comp_short}-{current_task_id}"
                act_name = f"{comp_name} - {suffix}"

                cur.execute("""
                    INSERT INTO task (
                        task_id, proj_id, wbs_id, clndr_id, phys_complete_pct,
                        rev_fdbk_flag, est_wt, lock_plan_flag, auto_compute_act_flag,
                        complete_pct_type, task_type, duration_type, status_code,
                        task_code, task_name, total_float_hr_cnt, free_float_hr_cnt,
                        remain_drtn_hr_cnt, remain_work_qty, target_work_qty,
                        target_drtn_hr_cnt, target_equip_qty, late_start_date, late_end_date,
                        early_start_date, early_end_date, restart_date, reend_date,
                        target_start_date, target_end_date, rem_late_start_date, rem_late_end_date,
                        driving_path_flag, create_date, update_date, create_user, update_user
                    ) VALUES (
                        ?, ?, ?, 178, 0.0,
                        'N', 1.0, 'N', 'Y',
                        'CP_Drtn', 'TT_Task', 'DT_FixedDUR2', 'TK_NotStart',
                        ?, ?, 0.0, 0.0,
                        ?, ?, ?,
                        ?, 0.0, '2026-10-12 08:00:00', '2027-04-15 17:00:00',
                        '2026-10-12 08:00:00', '2027-04-15 17:00:00', '2026-10-12 08:00:00', '2027-04-15 17:00:00',
                        '2026-10-12 08:00:00', '2027-04-15 17:00:00', '2026-10-12 08:00:00', '2027-04-15 17:00:00',
                        'Y', ?, ?, 'admin', 'admin'
                    )
                """, (
                    current_task_id, new_proj_id, comp_wbs_id,
                    act_code, act_name,
                    t_dur_hrs, t_dur_hrs, t_dur_hrs,
                    t_dur_hrs,
                    now_str, now_str
                ))

        # 6. Insert Logic Relationships (TASKPRED)
        # Sequential FS links
        for i in range(1, len(all_task_ids)):
            pred_counter += 1
            cur.execute("""
                INSERT INTO taskpred (
                    task_pred_id, task_id, pred_task_id, proj_id, pred_proj_id,
                    pred_type, lag_hr_cnt, create_date, create_user, update_date, update_user
                ) VALUES (?, ?, ?, ?, ?, 'PR_FS', 0.0, ?, 'admin', ?, 'admin')
            """, (pred_counter, all_task_ids[i], all_task_ids[i - 1], new_proj_id, new_proj_id, now_str, now_str))

        conn.commit()
        print(f"SUCCESS: Synced Project {proj_code} (ID: {new_proj_id}) with {len(components)+2} WBS and {len(all_task_ids)} Tasks into {db_path}")
        return True

    except Exception as e:
        conn.rollback()
        print(f"ERROR: {e}", file=sys.stderr)
        return False
    finally:
        conn.close()

if __name__ == "__main__":
    db = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\vophi\Documents\PPMDBSQLite.db"
    code = sys.argv[2] if len(sys.argv) > 2 else "PRJ-KAKA-BIM"
    title = sys.argv[3] if len(sys.argv) > 3 else "Project A - Toàn Bộ Dự Án Kaka (Điển hình)"
    res = sync_to_p6_db(db, code, title)
    sys.exit(0 if res else 1)
