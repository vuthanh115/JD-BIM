import sys
import sqlite3

sys.stdout.reconfigure(encoding='utf-8')
conn = sqlite3.connect(r'C:\Users\vophi\Documents\PPMDBSQLite.db')
cur = conn.cursor()
cur.execute("SELECT proj_id, proj_short_name, plan_start_date, plan_end_date FROM project WHERE proj_short_name = 'PRJ-KAKA-BIM'")
row = cur.fetchone()
print('PRJ-KAKA-BIM:', row)
pid = row[0]
cur.execute('SELECT count(*) FROM task WHERE proj_id = ?', (pid,))
print('Task count:', cur.fetchone()[0])
cur.execute('SELECT count(*) FROM projwbs WHERE proj_id = ?', (pid,))
print('WBS count:', cur.fetchone()[0])
cur.execute('SELECT wbs_id, wbs_short_name, wbs_name FROM projwbs WHERE proj_id = ? LIMIT 5', (pid,))
for w in cur.fetchall():
    print('WBS:', w)
cur.execute('SELECT task_code, task_name, target_drtn_hr_cnt, target_start_date, target_end_date, status_code, total_float_hr_cnt FROM task WHERE proj_id = ? LIMIT 5', (pid,))
for t in cur.fetchall():
    print('Task:', t)
