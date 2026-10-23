path = r'c:\Users\vophi\Downloads\Github\CV\JD-BIM\BIM_Manager_Executive_Dossier_4D_Navisworks.html'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Update @page CSS
old_page_css = '''    @page {
      size: A4 portrait;
      margin: 8mm 10mm 8mm 10mm;
    }'''

new_page_css = '''    @page {
      size: A4 portrait;
      margin: 0;
    }'''

assert old_page_css in text, 'old_page_css not found'
text = text.replace(old_page_css, new_page_css)

# 2. Update .page CSS
old_page_class = '''.page {
      width: 210mm;
      height: 297mm;
      max-height: 297mm;
      padding: 10mm 12mm 10mm 12mm;
      position: relative;
      background: #fff;
      page-break-after: always;
      page-break-inside: avoid;
      display: flex;
      flex-direction: column;
      box-sizing: border-box;
      overflow: hidden;
    }'''

new_page_class = '''.page {
      width: 210mm;
      height: 297mm;
      max-height: 297mm;
      padding: 10mm 12mm 10mm 12mm;
      position: relative;
      background: #fff;
      page-break-after: always;
      page-break-inside: avoid;
      display: flex;
      flex-direction: column;
      box-sizing: border-box;
      overflow: hidden;
    }'''
# keep .page dimensions exact

# 3. Update @media print
old_media_print = '''    /* Print specific optimization */
    @media print {
      body {
        background: #fff;
      }
      .no-print {
        display: none !important;
      }
      .document-wrapper {
        box-shadow: none;
        max-width: none;
        width: 100%;
      }
      .page {
        padding: 0;
        height: 280mm;
        max-height: 280mm;
      }
    }'''

new_media_print = '''    /* Print specific optimization */
    @media print {
      body {
        background: #fff;
        margin: 0;
        padding: 0;
      }
      .no-print {
        display: none !important;
      }
      .document-wrapper {
        box-shadow: none;
        max-width: none;
        width: 100%;
        margin: 0;
        padding: 0;
      }
      .page {
        width: 210mm;
        height: 297mm;
        max-height: 297mm;
        padding: 10mm 12mm 10mm 12mm !important;
        margin: 0 auto;
        page-break-after: always;
        page-break-inside: avoid;
        box-sizing: border-box;
      }
    }'''

assert old_media_print in text, 'old_media_print not found'
text = text.replace(old_media_print, new_media_print)

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print('FIXED_PRINT_CSS_SUCCESS')
