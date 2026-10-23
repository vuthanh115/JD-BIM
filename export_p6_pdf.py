import sqlite3
import datetime
import os
import sys
from weasyprint import HTML

conn = sqlite3.connect(r'C:\Users\vophi\Documents\PPMDBSQLite.db')
cur = conn.cursor()

proj_id = 4517
cur.execute('SELECT proj_short_name, plan_start_date, plan_end_date FROM project WHERE proj_id = ?', (proj_id,))
p_info = cur.fetchone()
proj_code = p_info[0]
proj_start = p_info[1]
proj_end = p_info[2]

cur.execute('''
    SELECT wbs_id, wbs_short_name, wbs_name, parent_wbs_id, anticip_start_date, anticip_end_date
    FROM projwbs
    WHERE proj_id = ?
    ORDER BY wbs_id
''', (proj_id,))
wbs_all = {row[0]: row for row in cur.fetchall()}

cur.execute('''
    SELECT t.task_id, t.wbs_id, t.task_code, t.task_name, t.target_drtn_hr_cnt, t.early_start_date, t.early_end_date, w.wbs_short_name
    FROM task t
    JOIN projwbs w ON t.wbs_id = w.wbs_id
    WHERE t.proj_id = ?
    ORDER BY t.early_start_date, t.task_id
''', (proj_id,))
tasks = cur.fetchall()

dt_start = datetime.datetime.strptime(proj_start, '%Y-%m-%d %H:%M:%S')
dt_end = datetime.datetime.strptime(proj_end, '%Y-%m-%d %H:%M:%S')
total_days = max(1, (dt_end - dt_start).days)

def get_pct(dt_val):
    d = datetime.datetime.strptime(dt_val, '%Y-%m-%d %H:%M:%S')
    diff = (d - dt_start).days
    return max(0.0, min(100.0, (diff / total_days) * 100.0))

months = []
cur_m = dt_start.replace(day=1)
while cur_m <= dt_end:
    months.append(cur_m.strftime('%m/%y'))
    if cur_m.month == 12:
        cur_m = cur_m.replace(year=cur_m.year + 1, month=1)
    else:
        cur_m = cur_m.replace(month=cur_m.month + 1)

month_headers_html = ''.join([f'<th class="m-hdr">{m}</th>' for m in months])

current_wbs = None
rows_html = []

for t_id, w_id, t_code, t_name, hrs, s_date, e_date, w_code in tasks:
    wbs_item = wbs_all.get(w_id)
    wbs_title = f"{w_code} - {wbs_item[2]}" if wbs_item else w_code
    
    if current_wbs != w_id:
        current_wbs = w_id
        w_start = wbs_item[4][:10] if wbs_item and wbs_item[4] else ''
        w_end = wbs_item[5][:10] if wbs_item and wbs_item[5] else ''
        disc_class = 'wbs-kc' if 'KC' in w_code else 'wbs-kt'
        rows_html.append(f'''
            <tr class="wbs-group-row {disc_class}">
                <td colspan="3" class="wbs-title">📁 {wbs_title}</td>
                <td class="text-center font-bold">{w_start}</td>
                <td class="text-center font-bold">{w_end}</td>
                <td colspan="{len(months)}" class="gantt-wbs-cell"></td>
            </tr>
        ''')
    
    days = int(hrs / 8.0) if hrs else 1
    left_pct = get_pct(s_date)
    right_pct = get_pct(e_date)
    width_pct = max(1.2, right_pct - left_pct)
    bar_color = '#e11d48' if 'KC' in t_code else '#0284c7'
    
    short_task_name = t_name.split(' - ')[-1] if ' - ' in t_name else t_name
    s_clean = s_date[:10]
    e_clean = e_date[:10]
    
    rows_html.append(f'''
        <tr class="task-row">
            <td class="t-code">{t_code}</td>
            <td class="t-name" title="{t_name}">{short_task_name}</td>
            <td class="text-center t-dur">{days}d</td>
            <td class="text-center t-date">{s_clean}</td>
            <td class="text-center t-date">{e_clean}</td>
            <td colspan="{len(months)}" class="gantt-cell">
                <div class="gantt-track">
                    <div class="gantt-bar" style="left: {left_pct:.2f}%; width: {width_pct:.2f}%; background-color: {bar_color};"></div>
                </div>
            </td>
        </tr>
    ''')

table_rows_str = '\n'.join(rows_html)

html_content = f'''<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>Tiến Độ Dự Án Primavera P6 - {proj_code}</title>
<style>
    @page {{
        size: A3 landscape;
        margin: 10mm 12mm 10mm 12mm;
        @bottom-right {{
            content: "Trang " counter(page) " / " counter(pages);
            font-size: 8pt;
            color: #64748b;
        }}
        @bottom-left {{
            content: "KAKA BIM Schedule Automation • Xuất từ Primavera P6 Database (PPMDBSQLite.db)";
            font-size: 8pt;
            color: #64748b;
        }}
    }}
    body {{
        font-family: 'Segoe UI', 'DejaVu Sans', Helvetica, Arial, sans-serif;
        font-size: 8.5pt;
        color: #0f172a;
        margin: 0;
        padding: 0;
    }}
    .header {{
        display: flex;
        justify-content: space-between;
        align-items: center;
        border-bottom: 2px solid #0284c7;
        padding-bottom: 8px;
        margin-bottom: 10px;
    }}
    .title-box h1 {{
        margin: 0;
        font-size: 15pt;
        color: #0f172a;
        font-weight: bold;
    }}
    .title-box p {{
        margin: 3px 0 0 0;
        font-size: 9pt;
        color: #475569;
    }}
    .meta-box {{
        text-align: right;
        font-size: 8.5pt;
        color: #334155;
    }}
    .badge {{
        display: inline-block;
        padding: 2px 8px;
        border-radius: 4px;
        font-weight: bold;
        color: white;
        font-size: 8pt;
    }}
    .badge-kc {{ background: #e11d48; }}
    .badge-kt {{ background: #0284c7; }}

    table {{
        width: 100%;
        border-collapse: collapse;
        table-layout: fixed;
    }}
    th {{
        background: #0f172a;
        color: #ffffff;
        font-weight: 600;
        padding: 5px 4px;
        font-size: 8pt;
        border: 1px solid #334155;
        text-align: center;
    }}
    td {{
        padding: 3px 4px;
        border: 1px solid #e2e8f0;
        font-size: 7.5pt;
        vertical-align: middle;
    }}
    .wbs-group-row {{
        background: #f1f5f9;
        font-weight: bold;
        border-top: 1.5px solid #cbd5e1;
    }}
    .wbs-group-row.wbs-kc {{
        background: #fff1f2;
        color: #9f1239;
    }}
    .wbs-group-row.wbs-kt {{
        background: #f0f9ff;
        color: #0369a1;
    }}
    .wbs-title {{
        padding-left: 6px;
        font-size: 8pt;
    }}
    .task-row:nth-child(even) {{
        background: #f8fafc;
    }}
    .t-code {{
        width: 110px;
        font-family: monospace;
        font-size: 7pt;
        color: #475569;
    }}
    .t-name {{
        width: 210px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }}
    .t-dur {{ width: 32px; font-weight: 600; }}
    .t-date {{ width: 64px; font-size: 7pt; }}
    .text-center {{ text-align: center; }}
    .font-bold {{ font-weight: bold; }}

    .m-hdr {{
        width: 48px;
        font-size: 7pt;
    }}
    .gantt-cell {{
        padding: 0;
        position: relative;
        background: #ffffff;
    }}
    .gantt-wbs-cell {{
        padding: 0;
        background: rgba(0,0,0,0.03);
    }}
    .gantt-track {{
        width: 100%;
        height: 12px;
        position: relative;
        background: #fcfcfc;
    }}
    .gantt-bar {{
        position: absolute;
        height: 7px;
        top: 2.5px;
        border-radius: 2px;
        box-shadow: 0 1px 2px rgba(0,0,0,0.15);
    }}
</style>
</head>
<body>

<div class="header">
    <div class="title-box">
        <h1>BẢNG TIẾN ĐỘ THI CÔNG CHI TIẾT (PRIMAVERA P6 GANTT CHART)</h1>
        <p>Dự án: <strong>{proj_code}</strong> • Mô hình BIM: <strong>Kết Cấu & Kiến Trúc (Link)</strong></p>
    </div>
    <div class="meta-box">
        <div><strong>Khởi công:</strong> {proj_start[:10]} | <strong>Hoàn thành:</strong> {proj_end[:10]}</div>
        <div style="margin-top: 4px;">
            <span class="badge badge-kc">KẾT CẤU (12/10/2026 ➔ 30/11/2026)</span>
            <span class="badge badge-kt" style="margin-left: 4px;">KIẾN TRÚC GỐI ĐẦU (16/11/2026 ➔ {proj_end[:10]})</span>
        </div>
    </div>
</div>

<table>
    <thead>
        <tr>
            <th style="width: 110px;">Mã Công Tác (P6)</th>
            <th style="width: 210px;">Nội Dung Hoạt Động (Activity)</th>
            <th style="width: 32px;">T.Lượng</th>
            <th style="width: 64px;">Bắt Đầu</th>
            <th style="width: 64px;">Kết Thúc</th>
            {month_headers_html}
        </tr>
    </thead>
    <tbody>
        {table_rows_str}
    </tbody>
</table>

</body>
</html>
'''

output_pdf = r'C:\Users\vophi\Downloads\261020_Project_P6_Schedule_Gantt.pdf'
HTML(string=html_content).write_pdf(output_pdf)
print('PDF_GENERATED_SUCCESS:', output_pdf)
