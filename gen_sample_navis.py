import sqlite3, os

xml_path = r'C:\Users\vophi\Downloads\Sample_Navisworks_SearchSets.xml'
csv_path = r'C:\Users\vophi\Downloads\Sample_Navisworks_Timeliner.csv'

conn = sqlite3.connect(r'C:\Users\vophi\Documents\PPMDBSQLite.db')
cur = conn.cursor()
cur.execute('SELECT wbs_short_name, wbs_name FROM projwbs WHERE proj_id = 4517 AND parent_wbs_id IN (SELECT wbs_id FROM projwbs WHERE proj_id = 4517 AND wbs_short_name IN (\'WBS-KC\', \'WBS-KT\'))')
items = cur.fetchall()

xml_lines = [
    '<?xml version="1.0" encoding="UTF-8" ?>',
    '<exchange xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="http://download.autodesk.com/us/navisworks/schemas/nw-exchange-12.0.xsd" units="m">',
    '  <selectionsets>',
    '    <viewfolder name="KAKA 4D BIM — RULE-BASED SEARCH SETS">'
]

csv_lines = ['Activity ID,Activity Name,WBS,Task Type,Synchronization ID,Search Set Name']

for code, name in items:
    clean_name = name.split(' - ')[-1] if ' - ' in name else name
    cat = 'Structural Foundation' if 'Coc' in code or 'Mong' in code else ('Structural Framing' if 'Dam' in code or 'DaKieng' in code else ('Structural Columns' if 'Cot' in code else ('Walls' if 'Tuong' in code or 'Vach' in code else 'Floors')))
    set_name = f'{code} - {clean_name}'
    
    xml_lines.append(f'      <selectionset name="{set_name}">')
    xml_lines.append('        <findspec mode="all" disjoint="0"><conditions>')
    xml_lines.append('          <condition test="contains" flags="10">')
    xml_lines.append('            <category><name internal="LcRevitData_Element">Element</name></category>')
    xml_lines.append('            <property><name internal="LcRevitPropertyElementCategory">Category</name></property>')
    xml_lines.append(f'            <value><data type="wstring">{cat}</data></value>')
    xml_lines.append('          </condition>')
    xml_lines.append('        </conditions></findspec>')
    xml_lines.append('      </selectionset>')
    
    csv_lines.append(f'"{code}","{clean_name}","{code}","Construct","{code}_SYNC","{set_name}"')

xml_lines.append('    </viewfolder>')
xml_lines.append('  </selectionsets>')
xml_lines.append('</exchange>')

with open(xml_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(xml_lines))
with open(csv_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(csv_lines))

print('SAMPLE_GENERATED_SUCCESS:')
print('XML:', xml_path, 'Size:', os.path.getsize(xml_path))
print('CSV:', csv_path, 'Size:', os.path.getsize(csv_path))
