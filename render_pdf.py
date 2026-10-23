import os
from weasyprint import HTML

html_path = r'c:\Users\vophi\Downloads\Github\CV\JD-BIM\BIM_Manager_Executive_Dossier_4D_Navisworks.html'
pdf_path = r'c:\Users\vophi\Downloads\Github\CV\JD-BIM\BIM_Manager_Executive_Dossier_4D_Navisworks.pdf'

print('Rendering PDF from HTML...')
HTML(filename=html_path).write_pdf(pdf_path)
print('PDF Rendered successfully!')
print('File size:', os.path.getsize(pdf_path), 'bytes')
