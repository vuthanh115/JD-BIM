#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
========================================================================================
BIM MANAGER EXECUTIVE PORTFOLIO - AUTOMATED PDF GENERATOR ENGINE
========================================================================================
Mục đích: Tự động biên dịch 'BIM_Manager_Portfolio_Print.html' thành file PDF in chuẩn A4
          chất lượng cao 'BIM_Manager_Executive_Portfolio.pdf' chỉ với 1 lệnh hoặc 1 click.
Sử dụng: Microsoft Edge / Google Chrome Headless Engine có sẵn trên Windows.
========================================================================================
"""

import os
import sys
import subprocess
import time
import shutil

# Đảm bảo mã hóa console UTF-8 trên Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
        sys.stderr.reconfigure(encoding='utf-8', errors='replace')
    except Exception:
        pass

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
HTML_PATH = os.path.join(BASE_DIR, "BIM_Manager_Portfolio_Print.html")
OUTPUT_PDF = os.path.join(BASE_DIR, "BIM_Manager_Executive_Portfolio.pdf")

# Danh sách đường dẫn trình duyệt có khả năng in Headless PDF
BROWSER_CANDIDATES = [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
]

def find_browser():
    for path in BROWSER_CANDIDATES:
        if os.path.exists(path):
            return path
    
    # Kiểm tra trong PATH
    for name in ["msedge", "chrome", "brave"]:
        found = shutil.which(name)
        if found:
            return found
            
    return None

def generate_pdf():
    print("=" * 80)
    print("🚀 BỘ SINH BÁO CÁO PDF PORTFOLIO TỰ ĐỘNG - BIM MANAGER EXECUTIVE")
    print("=" * 80)
    
    if not os.path.exists(HTML_PATH):
        print(f"❌ LỖI: Không tìm thấy file mẫu HTML tại:\n   {HTML_PATH}")
        sys.exit(1)
        
    browser_exe = find_browser()
    if not browser_exe:
        print("❌ LỖI: Không tìm thấy Microsoft Edge hoặc Google Chrome trên máy tính.")
        print("   Vui lòng mở file 'BIM_Manager_Portfolio_Print.html' bằng trình duyệt và bấm Ctrl + P để in thủ công.")
        sys.exit(1)
        
    print(f"🔍 Trình duyệt thực thi: {browser_exe}")
    print(f"📄 Nguồn HTML:           {HTML_PATH}")
    print(f"🎯 File đích PDF:        {OUTPUT_PDF}")
    print("-" * 80)
    print("⏳ Đang render bản in A4 và đóng gói PDF chất lượng cao (Vector Graphics)...")
    
    # Tạo thư mục tạm user profile cho headless
    temp_profile = os.path.join(os.environ.get("TEMP", r"C:\Temp"), "edge_pdf_render_profile")
    os.makedirs(temp_profile, exist_ok=True)
    
    file_url = f"file:///{HTML_PATH.replace(os.sep, '/')}"
    
    cmd = [
        browser_exe,
        "--headless",
        "--disable-gpu",
        "--run-all-compositor-stages-before-draw",
        f"--user-data-dir={temp_profile}",
        "--no-pdf-header-footer",
        f"--print-to-pdf={OUTPUT_PDF}",
        file_url
    ]
    
    start_time = time.time()
    try:
        proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=45)
        elapsed = time.time() - start_time
    except Exception as e:
        print(f"❌ LỖI khi khởi chạy trình duyệt: {e}")
        sys.exit(1)
        
    if os.path.exists(OUTPUT_PDF) and os.path.getsize(OUTPUT_PDF) > 5000:
        pdf_size_kb = os.path.getsize(OUTPUT_PDF) / 1024
        print(f"✅ XUẤT PDF THÀNH CÔNG trong {elapsed:.2f} giây!")
        print(f"📦 Kích thước file:      {pdf_size_kb:.1f} KB")
        print(f"📂 Đường dẫn file:       {OUTPUT_PDF}")
        print("=" * 80)
        print("💡 Mẹo: Bạn có thể in trực tiếp bản PDF này ra khổ giấy A4 để mang đi phỏng vấn.")
        return OUTPUT_PDF
    else:
        print("❌ Quá trình xuất PDF không tạo ra file hợp lệ.")
        if proc.stderr:
            print(f"Chi tiết lỗi: {proc.stderr.decode('utf-8', errors='ignore')}")
        sys.exit(1)

if __name__ == "__main__":
    generate_pdf()
