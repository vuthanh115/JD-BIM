import sys
import sqlite3

sys.stdout.reconfigure(encoding='utf-8')
db_path = r'C:\Users\vophi\Documents\PPMDBSQLite.db'
conn = sqlite3.connect(db_path)
cur = conn.cursor()

cur.execute("SELECT proj_id, proj_short_name, plan_start_date, plan_end_date FROM project")
projects = cur.fetchall()
print(f"Total projects in PPMDBSQLite: {len(projects)}")
for p in projects[-10:]:
    print("Project:", p)

# Check tasks
cur.execute("SELECT count(*) FROM task")
print("Total tasks:", cur.fetchone()[0])

cur.execute("SELECT task_id, task_code, task_name, target_drtn_hr_cnt, target_start_date, target_end_date FROM task LIMIT 5")
for t in cur.fetchall():
    print("Sample task:", t)
