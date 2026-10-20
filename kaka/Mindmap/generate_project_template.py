import os
import sys
import re
import sqlite3
import zipfile
import shutil

sys.stdout.reconfigure(encoding='utf-8')

DB_PATH = r"c:\Working\kaka\Mindmap\mindmap.db"
BASE_OUT = r"c:\Working\kaka\Mindmap\Project_Template"
XMIND_DIR = r"c:\Working\kaka\Mindmap"

XMIND_FILES = {
    "KIEN_TRUC": os.path.join(XMIND_DIR, "Quy trình làm việc vị trí kiến trúc.xmind"),
    "KET_CAU": os.path.join(XMIND_DIR, "Quy cách tính kết cấu.xmind"),
    "SHOP_REBAR": os.path.join(XMIND_DIR, "SHOP DRAWING REBAR.xmind"),
}

VIETNAMESE_MAP = {
    'a': 'áàảãạăắằẳẵặâấầẩẫậ',
    'A': 'ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬ',
    'd': 'đ', 'D': 'Đ',
    'e': 'éèẻẽẹêếềểễệ', 'E': 'ÉÈẺẼẸÊẾỀỂỄỆ',
    'i': 'íìỉĩị', 'I': 'ÍÌỈĨỊ',
    'o': 'óòỏõọôốồổỗộơớờởỡợ', 'O': 'ÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢ',
    'u': 'úùủũụưứừửữự', 'U': 'ÚÙỦŨỤƯỨỪỬỮỰ',
    'y': 'ýỳỷỹỵ', 'Y': 'ÝỲỶỸỴ'
}

def remove_accents(text):
    for ascii_char, accented_chars in VIETNAMESE_MAP.items():
        for ch in accented_chars:
            text = text.replace(ch, ascii_char)
    return text

def sanitize_folder_name(name):
    # Bo dau tieng Viet de ten folder khong bi loi tren moi he dieu hanh va terminal
    name = remove_accents(name)
    name = re.sub(r'\([^)]*\)', '', name) # bo phan chu thich trong ngoac don
    name = re.sub(r'[\r\n\t]+', ' ', name)
    name = re.sub(r'[\\/*?:"<>|]+', '_', name)
    name = re.sub(r'\s+', '_', name).strip(' ._')
    if len(name) > 50:
        name = name[:50].rstrip(' ._')
    return name

ELEMENT_STANDARD_NAMES = {
    "01.Coc": "01.Coc_Pile",
    "01": "01_Join_Cau_Kien_Theo_Quy_Tac",
    "02.Mong": "02.Mong_Foundation",
    "02.M": "02.Mong_Foundation",
    "03.DaKieng": "03.DaKieng_TieBeam",
    "03": "03.DaKieng_TieBeam",
    "04.Nen": "04.Nen_GroundSlab",
    "04.N": "04.Nen_GroundSlab",
    "05.Cot": "05.Cot_Column",
    "05.C": "05.Cot_Column",
    "06.Vach": "06.Vach_ShearWall",
    "06.V": "06.Vach_ShearWall",
    "07.Dam": "07.Dam_Beam",
    "07.D": "07.Dam_Beam",
    "08.San": "08.San_Slab",
    "08.S": "08.San_Slab",
    "09.CauThang": "09.CauThang_Stair",
    "09.C": "09.CauThang_Stair",
    "10.TamCap": "10.TamCap_Steps",
    "10.Tam": "10.TamCap_Steps",
    "11.Ramp": "11.Ramp",
    "12.Pit": "12.Pit_ThangMay",
    "13.Dock": "13.Dock_Hang",
    "14.CauKienPhu": "14.CauKienPhu_Secondary",
    "14.C": "14.CauKienPhu_Secondary"
}

def load_drawings(conn):
    c = conn.cursor()
    c.execute("SELECT image_id, node_id, module_id, file_name, caption FROM drawings")
    node_drawings = {}
    for img_id, node_id, mod_id, fn, cap in c.fetchall():
        if node_id not in node_drawings:
            node_drawings[node_id] = []
        node_drawings[node_id].append((mod_id, fn, cap))
    return node_drawings

def extract_image_resources():
    extracted = {}
    for mod_id, xf in XMIND_FILES.items():
        if os.path.exists(xf):
            try:
                with zipfile.ZipFile(xf, 'r') as z:
                    for n in z.namelist():
                        if 'resources/' in n and n.endswith('.png'):
                            fn = os.path.basename(n)
                            extracted[(mod_id, fn)] = z.read(n)
            except Exception as e:
                print(f"Lỗi khi đọc file {xf}: {e}")
    return extracted

def format_node_markdown(node, children, node_drawings, target_dir, image_resources):
    node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels = node
    lines = []
    header_title = f"{item_code} - {title}" if item_code and item_code not in title else title
    header_title = header_title.replace('\n', ' - ')
    lines.append(f"# {header_title}\n")
    lines.append(f"- **Phân loại:** `{node_type}`")
    lines.append(f"- **Đường dẫn phân cấp:** `{tree_path.replace(chr(10), ' ')}`")
    if labels:
        lines.append(f"- **Nhãn/Tham số:** `{labels}`")
    lines.append("")
    
    # Bản vẽ đính kèm của node
    if node_id in node_drawings:
        img_dir = os.path.join(target_dir, "Hinh_Anh")
        os.makedirs(img_dir, exist_ok=True)
        lines.append("## 📷 Bản vẽ & Hình ảnh minh họa\n")
        for mod_id, fn, cap in node_drawings[node_id]:
            key = (mod_id, fn)
            if key in image_resources:
                img_path = os.path.join(img_dir, fn)
                with open(img_path, 'wb') as f:
                    f.write(image_resources[key])
                lines.append(f"![{cap}](Hinh_Anh/{fn})")
                lines.append(f"*{cap}*\n")

    if children:
        lines.append("## 📋 Các nội dung / Quy chuẩn kỹ thuật chi tiết\n")
        for ch in children:
            ch_id, ch_parent, ch_code, ch_title, ch_depth, ch_sort, ch_type, ch_path, ch_labels = ch
            prefix = f"### {ch_code} - " if ch_code else "### "
            clean_title = ch_title.replace('\n', '\n> ')
            lines.append(f"{prefix}{clean_title}\n")
            if ch_labels:
                lines.append(f"*Tham số (Parameter):* `{ch_labels}`\n")
            
            # Ảnh của child
            if ch_id in node_drawings:
                img_dir = os.path.join(target_dir, "Hinh_Anh")
                os.makedirs(img_dir, exist_ok=True)
                for mod_id, fn, cap in node_drawings[ch_id]:
                    key = (mod_id, fn)
                    if key in image_resources:
                        img_path = os.path.join(img_dir, fn)
                        with open(img_path, 'wb') as f:
                            f.write(image_resources[key])
                        lines.append(f"![{cap}](Hinh_Anh/{fn})")
                        lines.append(f"*{cap}*\n")

    return "\n".join(lines)

def build_kien_truc(conn, base_dir, node_drawings, image_resources):
    c = conn.cursor()
    mod_dir = os.path.join(base_dir, "01_Kien_Truc")
    os.makedirs(mod_dir, exist_ok=True)

    # 1. Thư mục mẫu Server
    srv_dir = os.path.join(mod_dir, "00_Khoi_Tao_Thu_Muc_Server", "0.1_Architech")
    cad_tmpl = os.path.join(srv_dir, "0.1.1_Cad_Template")
    for sub in ["0.1.1.1.1_Mat_Bang", "0.1.1.1.2_Mat_Dung", "0.1.1.1.3_Mat_Cat", "0.1.1.1.4_Mat_Cat_Thang_Bo"]:
        os.makedirs(os.path.join(cad_tmpl, sub), exist_ok=True)
    
    rvt_dir = os.path.join(srv_dir, "0.1.2_Rvt_File")
    os.makedirs(os.path.join(rvt_dir, "Central_File"), exist_ok=True)
    os.makedirs(os.path.join(rvt_dir, "Local_File"), exist_ok=True)
    os.makedirs(os.path.join(rvt_dir, "Templates_Tham_Chieu"), exist_ok=True)

    boq_dir = os.path.join(srv_dir, "0.1.3_BoQ_Architech")
    os.makedirs(boq_dir, exist_ok=True)

    with open(os.path.join(mod_dir, "00_Khoi_Tao_Thu_Muc_Server", "README.md"), 'w', encoding='utf-8') as f:
        f.write("""# 0. Khởi Tạo Thư Mục Chung (Server Project Workspace)
Cấu trúc cây thư mục làm việc tiêu chuẩn trên Server cho bộ môn Kiến Trúc:

- `0.1_Architech/`
  - `0.1.1_Cad_Template/` : Chứa các file CAD template tách từ bản vẽ gốc
    - `0.1.1.1.1_Mat_Bang/` : Bản vẽ mặt bằng các tầng
    - `0.1.1.1.2_Mat_Dung/` : Bản vẽ mặt đứng
    - `0.1.1.1.3_Mat_Cat/` : Bản vẽ mặt cắt công trình
    - `0.1.1.1.4_Mat_Cat_Thang_Bo/` : Chi tiết thang bộ
  - `0.1.2_Rvt_File/` : Chứa file Revit làm việc
    - Khởi tạo file mới từ Template: `Z:\\BIM_BSV_Project\\0.BSV_TaiLieuThamChieu\\0.Teamplate_Rvt_R20\\ARC`
    - Tạo Central file và các Local file tương ứng
  - `0.1.3_BoQ_Architech/` : Lưu bản sao BoQ của dự án, đổi tên kết thúc bằng `_Arch_Version`
""")

    # 2. 4 Bước quy trình Kiến Trúc
    steps = [
        ("01_Step1_Tiep_Can_Ban_Ve", "Step 1"),
        ("02_Step2_Xu_Ly_Ban_Ve", "Step 2"),
        ("03_Step3_Trien_Khai_Dung_Model_Central", "Step 3"),
        ("04_Step4_Thong_Ke_KL_Nhap_BoQ", "Step 4"),
    ]

    for step_folder, step_code in steps:
        s_dir = os.path.join(mod_dir, step_folder)
        os.makedirs(s_dir, exist_ok=True)

        c.execute("""
            SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
            FROM categories
            WHERE module_id='KIEN_TRUC' AND item_code=?
        """, (step_code,))
        step_node = c.fetchone()
        if not step_node:
            continue

        c.execute("""
            SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
            FROM categories
            WHERE module_id='KIEN_TRUC' AND parent_id=?
            ORDER BY sort_order
        """, (step_node[0],))
        topics = c.fetchall()

        step_content = format_node_markdown(step_node, topics, node_drawings, s_dir, image_resources)
        with open(os.path.join(s_dir, "README.md"), 'w', encoding='utf-8') as f:
            f.write(step_content)

        for top in topics:
            top_title = top[3].replace('\n', ' ')
            top_folder_name = sanitize_folder_name(f"{top[2]}_{top_title}" if top[2] else top_title)
            top_dir = os.path.join(s_dir, top_folder_name)
            os.makedirs(top_dir, exist_ok=True)

            c.execute("""
                SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
                FROM categories
                WHERE module_id='KIEN_TRUC' AND tree_path LIKE ?
                ORDER BY depth, sort_order
            """, (f"{top[7]}%",))
            sub_nodes = c.fetchall()
            children_of_top = [sn for sn in sub_nodes if sn[0] != top[0]]

            top_md = format_node_markdown(top, children_of_top, node_drawings, top_dir, image_resources)
            with open(os.path.join(top_dir, "README.md"), 'w', encoding='utf-8') as f:
                f.write(top_md)

def build_ket_cau(conn, base_dir, node_drawings, image_resources):
    c = conn.cursor()
    mod_dir = os.path.join(base_dir, "02_Ket_Cau")
    os.makedirs(mod_dir, exist_ok=True)

    # 1. 00_Quy_Chuan_BoQ_Va_Bang_Tra
    boq_dir = os.path.join(mod_dir, "00_Quy_Chuan_BoQ_Va_Bang_Tra")
    os.makedirs(boq_dir, exist_ok=True)

    c.execute("""
        SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
        FROM categories
        WHERE module_id='KET_CAU' AND (item_code LIKE '00%' OR item_code IN ('A.TH', 'B', 'C.QUY') OR node_type='TABLE')
        ORDER BY sort_order
    """)
    rules = c.fetchall()
    
    with open(os.path.join(boq_dir, "README.md"), 'w', encoding='utf-8') as f:
        f.write("# 00. Quy Chuẩn Bóc Tách Khối Lượng BoQ Kết Cấu\n\n")
        f.write("Quy ước tính khối lượng với QS và bảng tra tiêu chuẩn cho 17 cấu kiện kết cấu.\n\n")
        f.write("### Thứ tự ưu tiên tính toán & Join cấu kiện:\n")
        f.write("`Sàn > Móng > Cột > Dầm > Vách > Cầu Thang` *(Sử dụng kaka để chạy Join tự động)*\n\n")
        f.write("### Quy trình 7 bước tính khối lượng:\n")
        f.write("1. Gán biến bê tông theo BoQ (chia nhỏ để dễ đối chiếu).\n")
        f.write("2. Chạy Join bê tông theo quy ước (Load thứ tự trong kaka).\n")
        f.write("3. Chạy ván khuôn (kaka), gán biến ván khuôn (Tool BSV_PhanLoaiVanKhuon).\n")
        f.write("4. Kiểm tra trùng lặp (Tool BSV_KiemTraTrungNhau).\n")
        f.write("5. Kiểm tra bảng thống kê với Model 3D và xuất khối lượng ra Excel.\n")
        f.write("6. Link khối lượng vào BoQ (tuyệt đối không tính toán thêm trong bảng).\n")
        f.write("7. Sum tổng khối lượng lót, ván lót, bê tông, ván khuôn khớp số.\n\n")
        
        for r in rules:
            f.write(f"## [{r[2]}] {r[3]}\n")
            if r[8]:
                f.write(f"**Ghi chú/Nhãn:** `{r[8]}`\n")
            f.write(f"**Phân loại:** `{r[6]}`\n\n")

    # 2. 14 Cấu kiện Kết Cấu (Lọc bỏ TABLE)
    c.execute("""
        SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
        FROM categories
        WHERE module_id='KET_CAU' AND depth=1 AND node_type='BRANCH' AND item_code NOT IN ('A.TH', 'B', 'C.QUY')
        ORDER BY sort_order
    """)
    elements = c.fetchall()

    for el in elements:
        code_key = el[2].strip()
        el_folder = ELEMENT_STANDARD_NAMES.get(code_key, sanitize_folder_name(f"{el[2]}_{el[3]}"))
        el_dir = os.path.join(mod_dir, el_folder)
        os.makedirs(el_dir, exist_ok=True)

        c.execute("""
            SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
            FROM categories
            WHERE module_id='KET_CAU' AND tree_path LIKE ?
            ORDER BY depth, sort_order
        """, (f"{el[7]}%",))
        sub_nodes = c.fetchall()
        children = [sn for sn in sub_nodes if sn[0] != el[0]]

        el_md = format_node_markdown(el, children, node_drawings, el_dir, image_resources)
        with open(os.path.join(el_dir, "README.md"), 'w', encoding='utf-8') as f:
            f.write(el_md)

def build_shop_rebar(conn, base_dir, node_drawings, image_resources):
    c = conn.cursor()
    mod_dir = os.path.join(base_dir, "03_Shop_Drawing_Rebar")
    os.makedirs(mod_dir, exist_ok=True)

    # 1. 01_Join_Cau_Kien_Theo_Quy_Tac
    c.execute("""
        SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
        FROM categories
        WHERE module_id='SHOP_REBAR' AND (item_code='01' OR title LIKE '%01. Join%')
    """)
    join_node = c.fetchone()
    if join_node:
        join_dir = os.path.join(mod_dir, "01_Join_Cau_Kien_Theo_Quy_Tac")
        os.makedirs(join_dir, exist_ok=True)
        c.execute("SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels FROM categories WHERE module_id='SHOP_REBAR' AND tree_path LIKE ? ORDER BY depth, sort_order", (f"{join_node[7]}%",))
        children = [sn for sn in c.fetchall() if sn[0] != join_node[0]]
        md = format_node_markdown(join_node, children, node_drawings, join_dir, image_resources)
        with open(os.path.join(join_dir, "README.md"), 'w', encoding='utf-8') as f:
            f.write(md)

    # 2. 13 Cấu kiện Shop Rebar (02.Móng -> 14.Cấu kiện phụ)
    c.execute("""
        SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
        FROM categories
        WHERE module_id='SHOP_REBAR' AND depth=1 AND (item_code != '01' AND title NOT LIKE '%01. Join%')
        ORDER BY sort_order
    """)
    elements = c.fetchall()

    for el in elements:
        code_key = el[2].strip() if el[2] else ""
        if not code_key:
            # Thu lay tu title
            m = re.match(r'^(\d+)', el[3])
            code_key = m.group(1) if m else ""
        
        el_folder = ELEMENT_STANDARD_NAMES.get(code_key, sanitize_folder_name(f"{el[2]}_{el[3]}" if el[2] else el[3]))
        el_dir = os.path.join(mod_dir, el_folder)
        os.makedirs(el_dir, exist_ok=True)

        c.execute("""
            SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
            FROM categories
            WHERE module_id='SHOP_REBAR' AND parent_id=?
            ORDER BY sort_order
        """, (el[0],))
        topics = c.fetchall()

        c.execute("""
            SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
            FROM categories
            WHERE module_id='SHOP_REBAR' AND tree_path LIKE ?
            ORDER BY depth, sort_order
        """, (f"{el[7]}%",))
        all_subs = c.fetchall()
        el_md = format_node_markdown(el, topics, node_drawings, el_dir, image_resources)
        with open(os.path.join(el_dir, "README.md"), 'w', encoding='utf-8') as f:
            f.write(el_md)

        # 5 thư mục con chuẩn: Quy cách vẽ, Đặt tên thép, Xuất bản vẽ, Bảng thống kê, Key plan
        for top in topics:
            top_title = top[3].replace('\n', ' ')
            top_folder = sanitize_folder_name(f"{top[2]}_{top_title}" if top[2] else top_title)
            top_dir = os.path.join(el_dir, top_folder)
            os.makedirs(top_dir, exist_ok=True)

            c.execute("""
                SELECT node_id, parent_id, item_code, title, depth, sort_order, node_type, tree_path, labels
                FROM categories
                WHERE module_id='SHOP_REBAR' AND tree_path LIKE ?
                ORDER BY depth, sort_order
            """, (f"{top[7]}%",))
            top_children = [sn for sn in c.fetchall() if sn[0] != top[0]]
            top_md = format_node_markdown(top, top_children, node_drawings, top_dir, image_resources)
            with open(os.path.join(top_dir, "README.md"), 'w', encoding='utf-8') as f:
                f.write(top_md)

def build_root_index(base_dir):
    idx_file = os.path.join(base_dir, "README.md")
    with open(idx_file, 'w', encoding='utf-8') as f:
        f.write("""# 🏗️ BỘ CÂY THƯ MỤC DỰ ÁN TIÊU CHUẨN (BIM & SHOP DRAWING)
*Trích xuất và chuẩn hóa tự động từ Mindmap BSV / KAKA*

---

## 📂 1. Phân Hệ Kiến Trúc (`01_Kien_Truc/`)
Quy trình BIM kiến trúc 4 bước hoàn chỉnh:
- **`00_Khoi_Tao_Thu_Muc_Server/`**: Cây thư mục máy chủ (Architech > Cad template [Mặt bằng, Mặt đứng, Mặt cắt, Thang bộ], Rvt file [Central/Local/Templates], BoQ Architech).
- **`01_Step1_Tiep_Can_Ban_Ve/`**: Tiếp cận bản vẽ, đọc tổng quan, chỉ mục chung, Schedules Finishing, chi tiết lanh tô bổ trụ giằng tường.
- **`02_Step2_Xu_Ly_Ban_Ve/`**: Tách bản vẽ CAD bằng lệnh BURST, link vào Central file, tạo Grid & Level, xử lý thống kê cửa.
- **`03_Step3_Trien_Khai_Dung_Model_Central/`**: Dựng tường, Set Room toàn dự án, dựng sàn/trần, cửa, lanh tô bổ trụ, hoàn thiện theo Room, chi tiết kết cấu.
- **`04_Step4_Thong_Ke_KL_Nhap_BoQ/`**: Thiết lập khung bảng thống kê, gán parameter, kiểm tra chéo Model & Schedule, xuất BoQ.

---

## 📐 2. Phân Hệ Bóc Tách Khối Lượng Kết Cấu (`02_Ket_Cau/`)
Quy chuẩn bóc tách khối lượng và bảng tra cho 17 cấu kiện:
- **`00_Quy_Chuan_BoQ_Va_Bang_Tra/`**: Thứ tự ưu tiên tính toán & Join (`Sàn > Móng > Cột > Dầm > Vách > Cầu Thang`), thông số Project Parameters, quy trình 7 bước tính khối lượng.
- **14 Thư mục cấu kiện chuẩn:**
  1. `01.Coc_Pile/` (Cọc ly tâm D300, 350, 400, 500, Tool BSV_AutoPile)
  2. `02.Mong_Foundation/` (Móng đơn, móng băng, móng cọc, Tool BSV_AutoFoundation)
  3. `03.DaKieng_TieBeam/` (Đà kiềng rộng x cao, ván khuôn)
  4. `04.Nen_GroundSlab/` (Nền chiều dày H sàn, BTL 50_BTL)
  5. `05.Cot_Column/` (Cột ngắn x dài, Tool BSV_Str_TaoCot_Ver4)
  6. `06.Vach_ShearWall/` (Vách chiều dày, trám lỗ ti)
  7. `07.Dam_Beam/` (Dầm B25, ván khuôn, thép)
  8. `08.San_Slab/` (Sàn hoàn thiện, ván khuôn đáy & thành)
  9. `09.CauThang_Stair/` (Cầu thang, dầm thang, chiếu nghỉ)
  10. `10.TamCap_Steps/` (Tam cấp, đà tam cấp)
  11. `11.Ramp/` (Ramp dốc, vách ramp)
  12. `12.Pit_ThangMay/` (Hố pit thang máy)
  13. `13.Dock_Hang/` (Dock xuất nhập hàng)
  14. `14.CauKienPhu_Secondary/` (Mương, bệ máy, trám lỗ ti)

---

## 🔩 3. Phân Hệ Shop Drawing Cốt Thép (`03_Shop_Drawing_Rebar/`)
Quy chuẩn triển khai bản vẽ Shop Drawing chi tiết cho từng cấu kiện:
- **`01_Join_Cau_Kien_Theo_Quy_Tac/`**: Quy chuẩn liên kết giao cắt cốt thép giữa các cấu kiện.
- **13 Thư mục cấu kiện Shop Rebar**, mỗi cấu kiện bao gồm đầy đủ 5 đề mục tiêu chuẩn:
  - `Quy_cach_ve/`: Sử dụng kaka Rebar / Structure Rebar, quy chuẩn neo, bẻ móc, đoạn nối chồng.
  - `Quy_cach_dat_ten_thep/`: Cấu trúc Partition, BSV_PhanLoai, BSV_Comments.
  - `Xuat_ban_ve/`: View template, mặt bằng, mặt cắt, bố trí Viewport trên Sheet.
  - `Bang_thong_ke/`: Template Schedule thép, bộ lọc Filter, Sorting theo đường kính/hình dạng.
  - `Key_Plan/`: Mặt bằng định vị cấu kiện và đường gióng phạm vi nối cắt thép.
""")

def main():
    print("=== BẮT ĐẦU TẠO CÂY THƯ MỤC PROJECT TEMPLATE TỪ MINDMAP ===")
    conn = sqlite3.connect(DB_PATH)
    
    print("1. Trích xuất bản vẽ và hình ảnh đính kèm từ file .xmind...")
    image_resources = extract_image_resources()
    print(f"   Đã tìm thấy {len(image_resources)} hình ảnh trong các gói XMind.")

    print("2. Đọc danh mục bản vẽ từ database...")
    node_drawings = load_drawings(conn)
    print(f"   Có {len(node_drawings)} nodes có bản vẽ đính kèm.")

    if os.path.exists(BASE_OUT):
        shutil.rmtree(BASE_OUT)
    os.makedirs(BASE_OUT, exist_ok=True)

    print("3. Tạo phân hệ Kiến Trúc (01_Kien_Truc)...")
    build_kien_truc(conn, BASE_OUT, node_drawings, image_resources)

    print("4. Tạo phân hệ Kết Cấu (02_Ket_Cau)...")
    build_ket_cau(conn, BASE_OUT, node_drawings, image_resources)

    print("5. Tạo phân hệ Shop Drawing Rebar (03_Shop_Drawing_Rebar)...")
    build_shop_rebar(conn, BASE_OUT, node_drawings, image_resources)

    print("6. Tạo file tổng hợp README.md...")
    build_root_index(BASE_OUT)

    conn.close()
    print(f"\n✅ HOÀN TẤT! Cây thư mục dự án đã được tạo tại:\n   {BASE_OUT}")

if __name__ == "__main__":
    main()
