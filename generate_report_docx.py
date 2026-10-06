import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn
from pathlib import Path

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{fill_hex}"/>')
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = parse_xml(f'<w:tcMar {nsdecls("w")}><w:top w:w="{top}" w:type="dxa"/><w:bottom w:w="{bottom}" w:type="dxa"/><w:left w:w="{left}" w:type="dxa"/><w:right w:w="{right}" w:type="dxa"/></w:tcMar>')
    tcPr.append(tcMar)

def set_table_borders(table, color="CCCCCC", sz="4", val="single"):
    tblPr = table._tbl.tblPr
    borders = parse_xml(f'''
        <w:tblBorders {nsdecls("w")}>
            <w:top w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:bottom w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:insideH w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:insideV w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:left w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
            <w:right w:val="{val}" w:sz="{sz}" w:space="0" w:color="{color}"/>
        </w:tblBorders>
    ''')
    tblPr.append(borders)

def build_report():
    doc = docx.Document()

    # Set Standard Page Margins (1 inch)
    for section in doc.sections:
        section.top_margin = Inches(1)
        section.bottom_margin = Inches(1)
        section.left_margin = Inches(1)
        section.right_margin = Inches(1)

    # Styles
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Times New Roman'
    normal_style.font.size = Pt(12)
    normal_style.font.color.rgb = RGBColor(0, 0, 0)

    # ==========================================
    # PAGE 1: TITLE / COVER PAGE
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(36)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("INTERNSHIP REPORT\n")
    run.font.size = Pt(22)
    run.font.bold = True

    p2 = doc.add_paragraph()
    p2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p2.paragraph_format.space_after = Pt(14)
    run = p2.add_run("A report submitted in partial fulfillment of the requirements for the award of degree of\n")
    run.font.size = Pt(12)
    run.font.italic = True

    run = p2.add_run("BACHELOR OF TECHNOLOGY\n")
    run.font.size = Pt(14)
    run.font.bold = True

    run = p2.add_run("in\n")
    run.font.size = Pt(12)

    run = p2.add_run("COMPUTER SCIENCE AND ENGINEERING\n(ARTIFICIAL INTELLIGENCE AND MACHINE LEARNING)\n")
    run.font.size = Pt(14)
    run.font.bold = True

    p3 = doc.add_paragraph()
    p3.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p3.paragraph_format.space_before = Pt(18)
    p3.paragraph_format.space_after = Pt(6)
    run = p3.add_run("by\n")
    run.font.size = Pt(12)
    run = p3.add_run("Yash Vashisth  (23BAI10717)\n")
    run.font.size = Pt(14)
    run.font.bold = True

    p4 = doc.add_paragraph()
    p4.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p4.paragraph_format.space_before = Pt(18)
    p4.paragraph_format.space_after = Pt(18)
    run = p4.add_run("Under the supervision of\n")
    run.font.size = Pt(12)
    run = p4.add_run("Pratibha Reddy, MP Online, [Remote]\n")
    run.font.size = Pt(13)
    run.font.bold = True
    run = p4.add_run("(Duration: 20th May, 2026 to 31st August, 2026)\n")
    run.font.size = Pt(11)
    run.font.italic = True

    p5 = doc.add_paragraph()
    p5.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p5.paragraph_format.space_before = Pt(18)
    p5.paragraph_format.space_after = Pt(24)
    run = p5.add_run("Project: AI-Resume-Platform – AI-Powered Resume Intelligence, ATS Scoring & Semantic Job Matching System\n")
    run.font.size = Pt(13)
    run.font.bold = True

    p6 = doc.add_paragraph()
    p6.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p6.paragraph_format.space_before = Pt(24)
    run = p6.add_run("School of Computing Science Engineering and Artificial Intelligence (SCAI)\n")
    run.font.size = Pt(13)
    run.font.bold = True
    run = p6.add_run("VIT BHOPAL UNIVERSITY\n2023 – 2027\n")
    run.font.size = Pt(14)
    run.font.bold = True

    doc.add_page_break()

    # ==========================================
    # PAGE 2: CERTIFICATE
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(36)
    p.paragraph_format.space_after = Pt(24)
    run = p.add_run("CERTIFICATE")
    run.font.size = Pt(18)
    run.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.3
    p.paragraph_format.space_after = Pt(48)
    p.add_run(
        "This is to certify that the “Internship Report” submitted by Yash Vashisth (Regd. No.: 23BAI10717) "
        "is work done by him and submitted during the academic year, in partial fulfillment of the requirements for "
        "the award of the degree of BACHELOR OF TECHNOLOGY in COMPUTER SCIENCE AND ENGINEERING "
        "(ARTIFICIAL INTELLIGENCE AND MACHINE LEARNING), at MP Online, [Remote]."
    )

    t = doc.add_table(rows=1, cols=2)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    for cell in t.rows[0].cells:
        cell.width = Inches(3.2)

    cell_left = t.rows[0].cells[0]
    p_l = cell_left.paragraphs[0]
    r = p_l.add_run("College Internship Guide\n\n\n\nDr. Pradeep Kumar Mishra\nSenior Assistant Professor")
    r.font.bold = True

    cell_right = t.rows[0].cells[1]
    p_r = cell_right.paragraphs[0]
    p_r.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    r = p_r.add_run("Program Chair\n\n\n\nDr. Pradeep Kumar Mishra\nSenior Assistant Professor")
    r.font.bold = True

    doc.add_page_break()

    # ==========================================
    # PAGE 3: CERTIFICATION PLACEHOLDER
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(36)
    p.paragraph_format.space_after = Pt(24)
    run = p.add_run("CERTIFICATION")
    run.font.size = Pt(18)
    run.font.bold = True

    p_box = doc.add_paragraph()
    p_box.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_box.paragraph_format.space_before = Pt(72)
    p_box.paragraph_format.space_after = Pt(72)
    r = p_box.add_run("[ --- ATTACH / PASTE MP ONLINE CERTIFICATE OF COMPLETION IMAGE HERE --- ]")
    r.font.size = Pt(14)
    r.font.bold = True
    r.font.color.rgb = RGBColor(128, 128, 128)

    doc.add_page_break()

    # ==========================================
    # PAGE 4: ACKNOWLEDGEMENT
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("ACKNOWLEDGEMENT")
    run.font.size = Pt(18)
    run.font.bold = True

    ack_paras = [
        "First, I would like to thank the Director of MP Online for giving me the opportunity to do an internship within the organization.",
        "I would also like to thank all the people who worked along with me at MP Online; their patience and openness created an enjoyable working environment.",
        "It is indeed with a great sense of pleasure and immense gratitude that I acknowledge the help of these individuals.",
        "I am highly indebted to Dean Dr. Pon Harshavardhanan for the facilities provided to accomplish this internship.",
        "I would like to thank my Program Chair Dr. Pradeep Kumar Mishra for his constructive criticism throughout my internship.",
        "I would like to thank Pratibha Reddy Mam for the support and advice that helped me get and complete the internship in the above-mentioned organization.",
        "I would also like to thank my project teammates for their cooperation and teamwork during the development of AI-Resume-Platform.",
        "I am extremely grateful to my department staff members and friends who helped me in the successful completion of this internship."
    ]

    for text in ack_paras:
        p = doc.add_paragraph()
        p.paragraph_format.line_spacing = 1.2
        p.paragraph_format.space_after = Pt(10)
        p.add_run(text)

    p_sig = doc.add_paragraph()
    p_sig.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p_sig.paragraph_format.space_before = Pt(24)
    r = p_sig.add_run("Yash Vashisth (23BAI10717)")
    r.font.bold = True

    doc.add_page_break()

    # ==========================================
    # PAGE 5: ABSTRACT
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("ABSTRACT")
    run.font.size = Pt(18)
    run.font.bold = True

    abstract_texts = [
        "In modern talent acquisition and recruitment, matching candidate profiles to relevant job descriptions at scale is a critical challenge. Traditional Applicant Tracking Systems (ATS) rely primarily on exact string matches and keyword density, frequently rejecting qualified candidates due to formatting inconsistencies, synonymous terminologies, or multi-column layout parsing errors. Furthermore, job seekers lack objective, real-time feedback on how their resumes align with industry job specifications.",
        "AI-Resume-Platform is an end-to-end, intelligent recruitment and career analytics platform engineered to resolve these limitations. The platform incorporates a high-performance document parsing pipeline using PyMuPDF (fitz) with coordinate-based visual bounding box sorting to extract multi-column PDF resumes accurately into standardized section taxonomies.",
        "The core intelligence engine leverages SentenceTransformers (all-MiniLM-L6-v2) and Scikit-Learn cosine similarity to perform dense semantic vector matching between candidate profiles and job requirements, effectively capturing conceptual similarities beyond literal keyword occurrences. To provide actionable guidance, the platform integrates the Google Gemini Generative AI API using structured context grounding, generating quantified ATS health scores, skill gap analyses, and hallucination-resistant resume improvement recommendations.",
        "The application is developed using an enterprise backend in ASP.NET Core (.NET 8) and Python FastAPI, utilizes Microsoft SQL Server managed through Entity Framework Core as its database, features biometric face authentication via PyTorch and FAISS, and provides a modern single-page frontend in React. Source code is maintained on GitHub. This report describes the problem, the architecture, the technologies, the implementation and the learning outcomes of the internship project completed at MP Online."
    ]

    for t in abstract_texts:
        p = doc.add_paragraph()
        p.paragraph_format.line_spacing = 1.25
        p.paragraph_format.space_after = Pt(12)
        p.add_run(t)

    doc.add_page_break()

    # ==========================================
    # PAGE 6: METHODOLOGY
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("METHODOLOGY")
    run.font.size = Pt(18)
    run.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(12)
    p.add_run("The project aims to modernize candidate evaluation and job matching by combining layout-aware document extraction, semantic deep learning embeddings, and generative AI guidance. The work was carried out in phases:")

    steps = [
        ("1. Requirement study: ", "understanding how resume parsing fails in traditional ATS, analyzing multi-column layout structures, and formalizing semantic matching requirements."),
        ("2. Initial project structure and database design: ", "creating the repository layout separating Python AI services, .NET backend APIs, and designing the SQL Server relational schema."),
        ("3. System design: ", "architecting microservice interactions between the React UI, ASP.NET Core business layer, Python NLP inference engine, and the Google Gemini API."),
        ("4. Implementation: ", "building the document ingestion pipeline in PyMuPDF, developing the SentenceTransformer embedding matcher, and implementing the heuristic ATS scoring engine."),
        ("5. AI and LLM integration: ", "orchestrating context-grounded prompts with the Google Gemini API for personalized gap analyses and interactive career coaching."),
        ("6. Biometric authentication & testing: ", "implementing face login with MTCNN and FAISS in FastAPI, integrating backend endpoints, and performing comprehensive verification.")
    ]

    for title, desc in steps:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.25)
        p.paragraph_format.space_after = Pt(8)
        r1 = p.add_run(title)
        r1.font.bold = True
        p.add_run(desc)

    doc.add_page_break()

    # ==========================================
    # PAGE 7: ORGANIZATION INFORMATION
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("ORGANIZATION INFORMATION")
    run.font.size = Pt(18)
    run.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(10)
    p.add_run(
        "MPOnline Limited is a joint venture between the Government of Madhya Pradesh and Tata Consultancy Services (TCS), "
        "established in 2006 and headquartered in Bhopal, Madhya Pradesh. It provides digital and e-Governance services such as "
        "online applications, examinations, recruitment, admissions, and bill payments."
    )

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(14)
    p.add_run(
        "MPOnline also offers technology training and internship programs in areas including AI/ML, software engineering, "
        "and digital literacy."
    )

    # Table
    tbl = doc.add_table(rows=7, cols=2)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(tbl)

    tbl_data = [
        ("Item", "Details"),
        ("Organization", "MP Online"),
        ("Location", "Bhopal, Madhya Pradesh"),
        ("Internship title / domain", "Artificial Intelligence & Machine Learning"),
        ("Supervisor", "Pratibha Reddy"),
        ("Duration", "20/05/26 to 31/08/26"),
        ("Project undertaken", "AI-Resume-Platform – AI-Powered Resume Intelligence, ATS Scoring & Semantic Job Matching System")
    ]

    for i, (k, v) in enumerate(tbl_data):
        row = tbl.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        c0.width, c1.width = Inches(2.2), Inches(4.3)
        p0, p1 = c0.paragraphs[0], c1.paragraphs[0]
        set_cell_margins(c0, 80, 80, 100, 100)
        set_cell_margins(c1, 80, 80, 100, 100)

        if i == 0:
            set_cell_background(c0, "D9E2F3")
            set_cell_background(c1, "D9E2F3")
            r0 = p0.add_run(k)
            r0.font.bold = True
            r1 = p1.add_run(v)
            r1.font.bold = True
        else:
            r0 = p0.add_run(k)
            r0.font.bold = True
            p1.add_run(v)

    p_sub = doc.add_paragraph()
    p_sub.paragraph_format.space_before = Pt(16)
    p_sub.paragraph_format.space_after = Pt(6)
    r = p_sub.add_run("Benefits to the Company / Institution through this Report")
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(10)
    p.add_run(
        "The AI-Resume-Platform prototype shows how an organization can eliminate manual resume screening fatigue and transition "
        "to objective, semantic talent evaluation. Because document structure extraction, semantic matching, and gap feedback are "
        "automated with AI, recruiters spend less time on repetitive manual filtering and more time engaging top-tier talent. "
        "The report documents the architecture, algorithms, and system design for institutional knowledge reuse."
    )

    p_sub2 = doc.add_paragraph()
    p_sub2.paragraph_format.space_before = Pt(10)
    p_sub2.paragraph_format.space_after = Pt(6)
    r = p_sub2.add_run("Learning Objectives / Internship Objectives")
    r.font.bold = True

    objs = [
        "To understand modern recruitment intelligence concepts including ATS scoring, semantic similarity, and skill extraction.",
        "To design and build layout-aware PDF document parsing pipelines using PyMuPDF (fitz).",
        "To implement dense vector semantic search using SentenceTransformers (all-MiniLM-L6-v2) and Scikit-Learn.",
        "To integrate a large language model API (Google Gemini) using context grounding to prevent hallucinations.",
        "To design scalable REST APIs with ASP.NET Core (.NET 8) and Python FastAPI.",
        "To work in a professional team using Git and GitHub for version control."
    ]

    for obj in objs:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(4)
        p.add_run("• " + obj)

    doc.add_page_break()

    # ==========================================
    # PAGES 8 & 9: WEEKLY OVERVIEW TABLES
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("WEEKLY OVERVIEW OF INTERNSHIP ACTIVITIES")
    run.font.size = Pt(18)
    run.font.bold = True

    def add_week_table(week_title, rows_data):
        tbl = doc.add_table(rows=len(rows_data)+1, cols=3)
        tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
        set_table_borders(tbl)

        # Header
        h_row = tbl.rows[0]
        h0, h1, h2 = h_row.cells[0], h_row.cells[1], h_row.cells[2]
        h0.width, h1.width, h2.width = Inches(1.3), Inches(1.3), Inches(3.9)
        set_cell_background(h0, "D9E2F3")
        set_cell_background(h1, "D9E2F3")
        set_cell_background(h2, "D9E2F3")

        p0, p1, p2 = h0.paragraphs[0], h1.paragraphs[0], h2.paragraphs[0]
        r0 = p0.add_run(week_title)
        r0.font.bold = True
        r1 = p1.add_run("DAY")
        r1.font.bold = True
        r2 = p2.add_run("NAME OF THE TOPIC / MODULE COMPLETED")
        r2.font.bold = True

        for idx, (dt, day, topic) in enumerate(rows_data):
            row = tbl.rows[idx+1]
            c0, c1, c2 = row.cells[0], row.cells[1], row.cells[2]
            c0.width, c1.width, c2.width = Inches(1.3), Inches(1.3), Inches(3.9)
            set_cell_margins(c0, 60, 60, 80, 80)
            set_cell_margins(c1, 60, 60, 80, 80)
            set_cell_margins(c2, 60, 60, 80, 80)
            c0.paragraphs[0].add_run(dt)
            c1.paragraphs[0].add_run(day)
            c2.paragraphs[0].add_run(topic)

        p_spacer = doc.add_paragraph()
        p_spacer.paragraph_format.space_before = Pt(8)

    w1_data = [
        ("20/05/26", "Monday", "Internship and Project Orientation"),
        ("21/05/26", "Tuesday", "Introduction to AI-Resume-Platform Scope"),
        ("22/05/26", "Wednesday", "Project Requirements and Functional Modules"),
        ("23/05/26", "Thursday", "Python AI & Development Environment Setup"),
        ("24/05/26", "Friday", "Repository Setup & Architecture Baseline"),
        ("25/05/26", "Saturday", "-")
    ]
    add_week_table("1st WEEK\nDATE", w1_data)

    w2_data = [
        ("27/05/26", "Monday", "AI-Resume-Platform System Architecture"),
        ("28/05/26", "Tuesday", "PDF Layout Analysis & PyMuPDF (Fitz) Exploration"),
        ("29/05/26", "Wednesday", "Visual Coordinate Block Sorting (y0, x0) Implementation"),
        ("30/05/26", "Thursday", "Section Taxonomy & Regex Alias Mapping"),
        ("31/05/26", "Friday", "Document Ingestion & Structured Resume JSON Generation"),
        ("01/06/26", "Saturday", "-")
    ]
    add_week_table("2nd WEEK\nDATE", w2_data)

    doc.add_page_break()

    w3_data = [
        ("03/06/26", "Monday", "Introduction to Semantic Search & Vector Embeddings"),
        ("04/06/26", "Tuesday", "SentenceTransformers (all-MiniLM-L6-v2) Integration"),
        ("05/06/26", "Wednesday", "Cosine Similarity Mathematics & Metric Formulation"),
        ("06/06/26", "Thursday", "Skill Dictionary Architecture & Keyword Extraction"),
        ("07/06/26", "Friday", "Hybrid Scoring Algorithm (Dense Semantic + Sparse Keyword)"),
        ("08/06/26", "Saturday", "-")
    ]
    add_week_table("3rd WEEK\nDATE", w3_data)

    w4_data = [
        ("10/06/26", "Monday", "ATS Scoring Rules & Section Completeness Heuristics"),
        ("11/06/26", "Tuesday", "Google Gemini API Client Integration"),
        ("12/06/26", "Wednesday", "Context Grounding & Prompt Orchestration"),
        ("13/06/26", "Thursday", "Automated Resume Gap Analysis & Recommendation Engine"),
        ("14/06/26", "Friday", "Conversational AI Career Advisor (Chatbot) Integration"),
        ("15/06/26", "Saturday", "-")
    ]
    add_week_table("4th WEEK\nDATE", w4_data)

    doc.add_page_break()

    # ==========================================
    # PAGE 10: INDEX / TABLE OF CONTENTS
    # ==========================================
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(24)
    p.paragraph_format.space_after = Pt(18)
    run = p.add_run("INDEX")
    run.font.size = Pt(18)
    run.font.bold = True

    index_tbl = doc.add_table(rows=33, cols=3)
    index_tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(index_tbl)

    index_data = [
        ("S.No.", "Contents", "Page"),
        ("1", "Introduction to AI-Resume-Platform", "1"),
        ("2", "Overview of AI Resume Analysis & ATS Systems", "1"),
        ("", "2.1 Evolution of Applicant Tracking Systems", "1"),
        ("", "2.2 Definition & Mathematical Principles", "1"),
        ("", "2.3 Resume Intelligence Lifecycle", "2"),
        ("3", "Problem Statement and Objectives", "2"),
        ("", "3.1 Problem statement", "2"),
        ("", "3.2 Objectives", "2"),
        ("4", "System Architecture", "3"),
        ("5", "User Roles and Modules", "3"),
        ("", "5.1 User roles", "3"),
        ("", "5.2 Main modules", "4"),
        ("6", "Technologies Used", "4"),
        ("", "6.1 Python & FastAPI", "4"),
        ("", "6.2 SentenceTransformers (all-MiniLM-L6-v2)", "4"),
        ("", "6.3 Google Gemini API", "4"),
        ("", "6.4 PyMuPDF (fitz)", "4"),
        ("", "6.5 Scikit-Learn & FAISS", "5"),
        ("", "6.6 ASP.NET Core (.NET 8) & React", "5"),
        ("", "6.7 SQL Server & EF Core", "5"),
        ("", "6.8 Git and GitHub", "5"),
        ("7", "AI & Machine Learning Engine", "5"),
        ("8", "Generative AI & Context Grounding", "6"),
        ("9", "Advantages and Disadvantages", "6"),
        ("", "9.1 Advantages", "6"),
        ("", "9.2 Disadvantages", "6"),
        ("10", "Requirements", "7"),
        ("", "10.1 Software requirements", "7"),
        ("", "10.2 Hardware requirements", "7"),
        ("11", "Project Implementation", "7"),
        ("", "11.1 Repository structure", "7"),
        ("", "11.2 Setup & Execution Guide", "8")
    ]

    for idx, (sno, title, pg) in enumerate(index_data):
        row = index_tbl.rows[idx]
        c0, c1, c2 = row.cells[0], row.cells[1], row.cells[2]
        c0.width, c1.width, c2.width = Inches(1.0), Inches(4.5), Inches(1.0)
        set_cell_margins(c0, 40, 40, 60, 60)
        set_cell_margins(c1, 40, 40, 60, 60)
        set_cell_margins(c2, 40, 40, 60, 60)
        if idx == 0:
            set_cell_background(c0, "D9E2F3")
            set_cell_background(c1, "D9E2F3")
            set_cell_background(c2, "D9E2F3")
            r0 = c0.paragraphs[0].add_run(sno)
            r0.font.bold = True
            r1 = c1.paragraphs[0].add_run(title)
            r1.font.bold = True
            r2 = c2.paragraphs[0].add_run(pg)
            r2.font.bold = True
        else:
            c0.paragraphs[0].add_run(sno)
            c1.paragraphs[0].add_run(title)
            c2.paragraphs[0].add_run(pg)

    doc.add_page_break()

    # ==========================================
    # SECTION 1 & 2: INTRODUCTION & OVERVIEW
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("1. INTRODUCTION TO AI-RESUME-PLATFORM")
    r.font.size = Pt(14)
    r.font.bold = True

    intro_texts = [
        "Modern talent acquisition runs on technology. Organizations receive hundreds of resumes for every job opening, creating severe bottlenecks for human recruiters. Applicant Tracking Systems (ATS) were created to automate resume filtering; however, legacy ATS tools rely on simplistic, exact string keyword searches. Qualified applicants are routinely rejected because they used synonyms, formatting styles, or two-column resume templates that legacy parsers cannot read.",
        "AI-Resume-Platform is our answer to this problem: a modern, intelligent recruitment platform that combines layout-aware PDF extraction, dense semantic vector embeddings, heuristic ATS health scoring, and generative AI career advisory. The source code is organized as a modular microservices solution on GitHub."
    ]

    for t in intro_texts:
        p = doc.add_paragraph()
        p.paragraph_format.line_spacing = 1.2
        p.paragraph_format.space_after = Pt(8)
        p.add_run(t)

    # Info table
    tbl = doc.add_table(rows=6, cols=2)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(tbl)

    t_data = [
        ("Item", "Details"),
        ("Project name", "AI-Resume-Platform"),
        ("Type", "AI-Powered Resume Intelligence, ATS Scoring & Semantic Job Matching System"),
        ("Repository", "AI-Resume-Platform (Git / GitHub)"),
        ("Technology", "Python, SentenceTransformers, Google Gemini API, PyMuPDF, Scikit-Learn, ASP.NET Core 8, React, SQL Server"),
        ("Team & Role", "Team: AI-Resume-Platform Project Team\nMy Role: Python AI/ML & NLP Engineer – AI Matching & Analytics Module\nResponsible for layout-aware PDF parsing (PyMuPDF), semantic vector matching (SentenceTransformers), ATS scoring algorithms, and Gemini context-grounded prompt orchestration.")
    ]

    for i, (k, v) in enumerate(t_data):
        row = tbl.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        c0.width, c1.width = Inches(2.0), Inches(4.5)
        set_cell_margins(c0, 60, 60, 80, 80)
        set_cell_margins(c1, 60, 60, 80, 80)
        if i == 0:
            set_cell_background(c0, "D9E2F3")
            set_cell_background(c1, "D9E2F3")
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            r1 = c1.paragraphs[0].add_run(v)
            r1.font.bold = True
        else:
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            c1.paragraphs[0].add_run(v)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("2. OVERVIEW OF AI RESUME ANALYSIS & ATS SYSTEMS")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("2.1 Evolution of Applicant Tracking Systems\n")
    r.font.bold = True
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(6)
    p.add_run(
        "Early recruitment software in the 1990s and 2000s acted primarily as digital databases using Boolean queries (e.g., 'Java' AND 'Spring'). "
        "These keyword scanners lacked semantic understanding, failing to recognize that 'PostgreSQL' and 'Relational Database' represent related competencies. "
        "The modern AI era uses Transformer neural networks and continuous vector spaces to evaluate conceptual match rather than pure keyword matching."
    )

    p = doc.add_paragraph()
    r = p.add_run("2.2 Definition & Mathematical Principles\n")
    r.font.bold = True
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(6)
    p.add_run(
        "Semantic matching projects text into a continuous d-dimensional vector space. Using SentenceTransformers (all-MiniLM-L6-v2), "
        "a resume and job description are transformed into dense embeddings u and v (d = 384). The semantic relevance is computed using Cosine Similarity: "
        "CosineSimilarity(u, v) = (u . v) / (||u|| * ||v||)."
    )

    p = doc.add_paragraph()
    r = p.add_run("2.3 Resume Intelligence Lifecycle\n")
    r.font.bold = True
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(8)
    p.add_run(
        "The complete lifecycle within AI-Resume-Platform follows 6 stages: (1) Ingestion & layout coordinate sorting, (2) Section taxonomy parsing, "
        "(3) Dense embedding generation, (4) Semantic similarity & keyword overlap scoring, (5) Heuristic ATS health evaluation, and (6) Generative AI context-grounded coaching."
    )

    doc.add_page_break()

    # ==========================================
    # SECTION 3 & 4: PROBLEMS, OBJECTIVES & ARCHITECTURE
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("3. PROBLEM STATEMENT AND OBJECTIVES")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("3.1 Problem Statement")
    r.font.bold = True
    p.paragraph_format.space_after = Pt(4)

    prob_items = [
        "Multi-column PDF resumes are corrupted by standard parsers, merging unrelated columns into scrambled sentences.",
        "Exact keyword filtering rejects skilled candidates who use different synonyms or technical phrasing.",
        "Candidates have no visibility into ATS scoring criteria, making resume optimization difficult.",
        "Uncontrolled LLMs hallucinate candidate skills if prompts are not strictly grounded in extracted factual context."
    ]
    for item in prob_items:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + item)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(8)
    r = p.add_run("3.2 Objectives")
    r.font.bold = True
    p.paragraph_format.space_after = Pt(4)

    obj_items = [
        "Engineer a layout-aware PDF ingestion pipeline using PyMuPDF coordinate sorting (y0, x0).",
        "Implement dense semantic vector matching with SentenceTransformers (all-MiniLM-L6-v2) and Scikit-Learn.",
        "Build a multi-dimensional ATS scoring engine evaluating section completeness, formatting, and skill density.",
        "Orchestrate Google Gemini Generative AI prompts via context grounding for hallucination-free career advice.",
        "Deliver a secure full-stack platform with biometric face login, ASP.NET Core APIs, and a React frontend."
    ]
    for item in obj_items:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + item)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("4. SYSTEM ARCHITECTURE")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(10)
    p.add_run(
        "AI-Resume-Platform is constructed using a decoupled multi-tier architecture connecting the React frontend, "
        "an ASP.NET Core business logic backend, a Microsoft SQL Server database, a Python FastAPI biometric service, "
        "and the Python AI/ML matching & Google Gemini LLM pipeline."
    )

    arch_img_path = Path(r"C:\Users\VISHAL\.gemini\antigravity\brain\85b1685c-5292-4f8d-a707-b01309ddeb33\.user_uploaded\media_1790345061946.jpg")
    if arch_img_path.exists():
        p_img = doc.add_paragraph()
        p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_img.paragraph_format.space_before = Pt(12)
        p_img.paragraph_format.space_after = Pt(6)
        doc.add_picture(str(arch_img_path), width=Inches(6.2))
        p_cap = doc.add_paragraph()
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.paragraph_format.space_after = Pt(12)
        r_cap = p_cap.add_run("Figure 4.1: Architecture of AI-Resume-Platform")
        r_cap.font.size = Pt(10.5)
        r_cap.font.italic = True
        r_cap.font.bold = True
    else:
        p_box = doc.add_paragraph()
        p_box.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_box.paragraph_format.space_before = Pt(24)
        p_box.paragraph_format.space_after = Pt(24)
        r = p_box.add_run("[ --- INSERT SYSTEM ARCHITECTURE DIAGRAM (FIGURE 4.1) HERE --- ]")
        r.font.size = Pt(12)
        r.font.bold = True
        r.font.color.rgb = RGBColor(128, 128, 128)

    arch_bullets = [
        "Users / Candidates: Access the system via any modern web browser.",
        "React Frontend (SPA): Provides interactive dashboards, PDF upload, ATS scorecards, and AI chat.",
        "ASP.NET Core 8 Web API: Handles authentication, business logic, resume management, and database operations.",
        "SQL Server Database: Stores candidates, uploaded resume metadata, match results, and ATS scores.",
        "Python AI/ML Pipeline: Executes PyMuPDF layout parsing, SentenceTransformers embeddings, and Gemini context grounding.",
        "Python FastAPI Biometric Service: Executes MTCNN face detection and InceptionResnetV1 embeddings with FAISS search."
    ]
    for b in arch_bullets:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + b)

    doc.add_page_break()

    # ==========================================
    # SECTION 5 & 6: MODULES & TECHNOLOGIES
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("5. USER ROLES AND MODULES")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("5.1 User Roles")
    r.font.bold = True

    tbl_roles = doc.add_table(rows=3, cols=2)
    tbl_roles.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(tbl_roles)

    roles_data = [
        ("Role", "Purpose in the System"),
        ("Candidate", "Uploads resumes, views ATS scorecards, inspects job match percentages, explores missing skills, and interacts with the AI career advisor."),
        ("Recruiter / Admin", "Publishes job specifications, searches candidates by semantic similarity, reviews candidate ATS ratings, and manages platform accounts.")
    ]

    for i, (k, v) in enumerate(roles_data):
        row = tbl_roles.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        c0.width, c1.width = Inches(2.0), Inches(4.5)
        set_cell_margins(c0, 60, 60, 80, 80)
        set_cell_margins(c1, 60, 60, 80, 80)
        if i == 0:
            set_cell_background(c0, "D9E2F3")
            set_cell_background(c1, "D9E2F3")
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            r1 = c1.paragraphs[0].add_run(v)
            r1.font.bold = True
        else:
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            c1.paragraphs[0].add_run(v)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    r = p.add_run("5.2 Main Modules")
    r.font.bold = True

    mods = [
        "Document Ingestion & Section Parsing: Layout-aware text extraction with PyMuPDF and regex section classification.",
        "Semantic Vector Matching: SentenceTransformer dense embedding generation and cosine similarity calculation.",
        "ATS Health Analysis: Heuristic evaluation of section completeness, formatting quality, and keyword density.",
        "Generative AI Career Advisor: Google Gemini prompt integration for personalized feedback and chat advisory.",
        "Biometric Authentication: MTCNN face detection and FAISS nearest-neighbor verification."
    ]
    for m in mods:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + m)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("6. TECHNOLOGIES USED")
    r.font.size = Pt(14)
    r.font.bold = True

    techs = [
        ("6.1 Python & FastAPI: ", "Core language for ML pipelines, NLP models, and microservice APIs."),
        ("6.2 SentenceTransformers (all-MiniLM-L6-v2): ", "Pre-trained deep learning transformer model generating 384-dimensional dense semantic embeddings."),
        ("6.3 Google Gemini Generative AI API: ", "State-of-the-art multimodal LLM used with context grounding for candidate critique."),
        ("6.4 PyMuPDF (fitz): ", "High-speed PDF parser extracting spatial bounding box coordinates for layout reconstruction."),
        ("6.5 Scikit-Learn & FAISS: ", "Mathematical computation of cosine similarity and vector search indexing."),
        ("6.6 ASP.NET Core (.NET 8) & React: ", "Modern backend REST API and responsive single-page web application."),
        ("6.7 SQL Server & Entity Framework Core: ", "Relational database and ORM managing user and analysis records."),
        ("6.8 Git and GitHub: ", "Distributed version control and codebase collaboration.")
    ]

    for title, desc in techs:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(4)
        r = p.add_run(title)
        r.font.bold = True
        p.add_run(desc)

    doc.add_page_break()

    # ==========================================
    # SECTION 7, 8, 9: AI PIPELINE, GENAI, PROS/CONS
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("7. AI & MACHINE LEARNING PIPELINE")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(6)
    p.add_run(
        "The AI pipeline solves two core NLP problems: (1) Document Layout Extraction and (2) Dense Semantic Matching. "
        "PyMuPDF extracts blocks with coordinates (x0, y0, x1, y1) which are sorted vertically within horizontal columns. "
        "Sections are categorized using alias dictionaries across education, skills, experience, and projects."
    )

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(8)
    p.add_run(
        "For matching, the system implements a Hybrid Scoring Formula combining dense semantic vector similarity and exact keyword overlap:"
    )

    p_eq = doc.add_paragraph()
    p_eq.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_eq = p_eq.add_run("Final Match Score = 0.65 * Semantic_Score + 0.35 * Keyword_Score")
    r_eq.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("8. GENERATIVE AI & CONTEXT GROUNDING")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(6)
    p.add_run(
        "To prevent large language models from hallucinating credentials, AI-Resume-Platform uses Context Grounding. "
        "The Python engine packages extracted candidate skills, computed match scores, and missing requirements into a structured JSON context payload. "
        "This verified payload is injected into Google Gemini with explicit system constraints, ensuring 100% factual accuracy."
    )

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("9. ADVANTAGES AND DISADVANTAGES")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("9.1 Advantages")
    r.font.bold = True
    advs = [
        "Semantic matching recognizes synonymously equivalent technical skills without requiring exact keyword phrasing.",
        "Visual coordinate sorting prevents text corruption in multi-column PDF resumes.",
        "Context-grounded LLM advice provides candidates with transparent, actionable resume feedback.",
        "Decoupled microservices architecture enables independent scaling of AI and web services."
    ]
    for a in advs:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + a)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(6)
    r = p.add_run("9.2 Disadvantages")
    r.font.bold = True
    disadvs = [
        "Generative AI features require an active internet connection and a valid Google Gemini API key.",
        "Scanned image-only PDFs require an upstream OCR step before text block processing.",
        "Deep learning transformer inference requires sufficient local CPU/RAM resources."
    ]
    for d in disadvs:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + d)

    doc.add_page_break()

    # ==========================================
    # SECTION 10 & 11: REQUIREMENTS & IMPLEMENTATION
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("10. REQUIREMENTS")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("10.1 Software Requirements")
    r.font.bold = True

    tbl_sw = doc.add_table(rows=7, cols=2)
    tbl_sw.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(tbl_sw)

    sw_data = [
        ("Software", "Use"),
        ("Python 3.10+", "AI/ML pipelines, SentenceTransformers, PyMuPDF, FastAPI"),
        (".NET 8.0 SDK", "Backend REST API development and execution"),
        ("SQL Server / LocalDB", "Relational database for users, resumes, and scores"),
        ("Google Gemini API Key", "Generative AI resume critique and career guidance"),
        ("Node.js & npm", "React frontend development and build tooling"),
        ("Git & GitHub", "Version control and repository management")
    ]

    for i, (k, v) in enumerate(sw_data):
        row = tbl_sw.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        c0.width, c1.width = Inches(2.2), Inches(4.3)
        set_cell_margins(c0, 60, 60, 80, 80)
        set_cell_margins(c1, 60, 60, 80, 80)
        if i == 0:
            set_cell_background(c0, "D9E2F3")
            set_cell_background(c1, "D9E2F3")
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            r1 = c1.paragraphs[0].add_run(v)
            r1.font.bold = True
        else:
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            c1.paragraphs[0].add_run(v)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(10)
    r = p.add_run("10.2 Hardware Requirements")
    r.font.bold = True
    hw = [
        "Computer with at least 8 GB RAM (16 GB recommended for deep learning inference).",
        "At least 10 GB free SSD storage for Python virtual environments, models, and databases.",
        "Multi-core modern processor (Intel i5 / AMD Ryzen 5 or higher)."
    ]
    for h in hw:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + h)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("11. PROJECT IMPLEMENTATION")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("11.1 Repository Structure")
    r.font.bold = True

    tbl_repo = doc.add_table(rows=5, cols=2)
    tbl_repo.alignment = WD_TABLE_ALIGNMENT.CENTER
    set_table_borders(tbl_repo)

    repo_data = [
        ("Folder / File", "Purpose"),
        ("ai/", "Python AI/ML pipeline (pdf_parser.py, job_matcher.py, ats_analyzer.py, gemini_analyzer.py)"),
        ("FaceRecognitionService/", "FastAPI biometric facial recognition service with PyTorch and FAISS"),
        ("backend/", "ASP.NET Core (.NET 8) Web API business logic, controllers, EF Core models"),
        ("frontend/", "React (TypeScript, Tailwind CSS, Vite) single-page application")
    ]

    for i, (k, v) in enumerate(repo_data):
        row = tbl_repo.rows[i]
        c0, c1 = row.cells[0], row.cells[1]
        c0.width, c1.width = Inches(2.2), Inches(4.3)
        set_cell_margins(c0, 60, 60, 80, 80)
        set_cell_margins(c1, 60, 60, 80, 80)
        if i == 0:
            set_cell_background(c0, "D9E2F3")
            set_cell_background(c1, "D9E2F3")
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            r1 = c1.paragraphs[0].add_run(v)
            r1.font.bold = True
        else:
            r0 = c0.paragraphs[0].add_run(k)
            r0.font.bold = True
            c1.paragraphs[0].add_run(v)

    doc.add_page_break()

    # ==========================================
    # SECTION 12, 13, 14: DB, CODE, OUTPUT
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("12. DATABASE DESIGN")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(8)
    p.add_run(
        "The relational database is implemented in SQL Server using Entity Framework Core. "
        "It maintains relational integrity across Candidates, Resumes, MatchResults, AtsAnalyses, and Jobs."
    )

    p_box = doc.add_paragraph()
    p_box.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_box.paragraph_format.space_before = Pt(24)
    p_box.paragraph_format.space_after = Pt(24)
    r = p_box.add_run("[ --- INSERT DATABASE ENTITY-RELATIONSHIP (ER) DIAGRAM HERE --- ]")
    r.font.size = Pt(12)
    r.font.bold = True
    r.font.color.rgb = RGBColor(128, 128, 128)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("13. SOURCE CODE HIGHLIGHTS")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(6)
    p.add_run("Important code snippets from the Python AI/ML pipeline are shown below:")

    code_snips = [
        ("PDF Coordinate Sorting (pdf_parser.py):", 
         "def sort_blocks_visually(blocks):\n"
         "    return sorted(blocks, key=lambda b: (round(b[1] / 20) * 20, b[0]))"),
        ("SentenceTransformers Semantic Matching (job_matcher.py):", 
         "EMBEDDING_MODEL = SentenceTransformer('all-MiniLM-L6-v2')\n"
         "def calculate_semantic_similarity(resume_text, job_text):\n"
         "    embeddings = EMBEDDING_MODEL.encode([resume_text, job_text])\n"
         "    sim = cosine_similarity([embeddings[0]], [embeddings[1]])[0][0]\n"
         "    return float(sim * 100)"),
        ("Google Gemini Context Grounding (gemini_analyzer.py):",
         "def generate_grounded_analysis(candidate_context, client):\n"
         "    prompt = f'Analyze verified profile against job:\\n{json.dumps(candidate_context)}'\n"
         "    response = client.models.generate_content(model='gemini-2.5-flash', contents=prompt)\n"
         "    return response.text")
    ]

    for title, code in code_snips:
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(6)
        p.paragraph_format.space_after = Pt(2)
        r = p.add_run(title)
        r.font.bold = True

        p_c = doc.add_paragraph()
        p_c.paragraph_format.left_indent = Inches(0.25)
        p_c.paragraph_format.space_after = Pt(6)
        r_c = p_c.add_run(code)
        r_c.font.name = 'Consolas'
        r_c.font.size = Pt(9.5)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(14)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("14. OUTPUT")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(10)
    p.add_run("The placeholders below indicate where application screenshots should be attached:")

    screens = [
        "[ --- INSERT SCREENSHOT 1: CANDIDATE DASHBOARD & RESUME OVERVIEW --- ]",
        "[ --- INSERT SCREENSHOT 2: ATS SCORECARD & GAP ANALYSIS SCREEN --- ]",
        "[ --- INSERT SCREENSHOT 3: SEMANTIC JOB MATCHING RECOMMENDATIONS --- ]",
        "[ --- INSERT SCREENSHOT 4: INTERACTIVE AI CAREER ADVISOR CHATBOT --- ]"
    ]
    for s in screens:
        p_box = doc.add_paragraph()
        p_box.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_box.paragraph_format.space_before = Pt(12)
        p_box.paragraph_format.space_after = Pt(12)
        r = p_box.add_run(s)
        r.font.size = Pt(11)
        r.font.bold = True
        r.font.color.rgb = RGBColor(128, 128, 128)

    doc.add_page_break()

    # ==========================================
    # SECTION 15 & 16: CONCLUSION & BIBLIOGRAPHY
    # ==========================================
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(12)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("15. CONCLUSION AND FUTURE SCOPE")
    r.font.size = Pt(14)
    r.font.bold = True

    p = doc.add_paragraph()
    r = p.add_run("15.1 Conclusion\n")
    r.font.bold = True
    p.paragraph_format.line_spacing = 1.2
    p.paragraph_format.space_after = Pt(8)
    p.add_run(
        "During the internship at MP Online, we successfully designed, developed, and validated AI-Resume-Platform. "
        "The project demonstrated practical mastery of Natural Language Processing, Transformer-based dense embeddings (SentenceTransformers), "
        "document layout reconstruction with PyMuPDF, context-grounded Generative AI integration with Google Gemini, full-stack .NET and React engineering, "
        "and collaborative software development using Git and GitHub."
    )

    p = doc.add_paragraph()
    r = p.add_run("15.2 Future Scope\n")
    r.font.bold = True
    p.paragraph_format.space_after = Pt(4)

    f_items = [
        "Add an OCR pre-processing module for scanned, image-only resumes.",
        "Implement an automated AI resume rewriter tailored to specific job descriptions.",
        "Support multilingual embedding models for international recruitment evaluation.",
        "Deploy the platform as containerized microservices across cloud Kubernetes clusters."
    ]
    for item in f_items:
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.space_after = Pt(3)
        p.add_run("• " + item)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(18)
    p.paragraph_format.space_after = Pt(6)
    r = p.add_run("16. BIBLIOGRAPHY AND REFERENCES")
    r.font.size = Pt(14)
    r.font.bold = True

    refs = [
        "AI-Resume-Platform Project Repository, GitHub – https://github.com/YashVashisth/AI-Resume-Platform",
        "SentenceTransformers Documentation, SBERT – https://www.sbert.net",
        "Google AI for Developers, Gemini API Documentation – https://ai.google.dev",
        "PyMuPDF Documentation, Artifex – https://pymupdf.readthedocs.io",
        "Scikit-Learn Documentation, Pairwise Metrics – https://scikit-learn.org",
        "Microsoft, ASP.NET Core & Entity Framework Core Documentation – https://learn.microsoft.com/aspnet/core",
        "FAISS Documentation, Meta AI Research – https://github.com/facebookresearch/faiss"
    ]

    for idx, ref in enumerate(refs, 1):
        p = doc.add_paragraph()
        p.paragraph_format.left_indent = Inches(0.25)
        p.paragraph_format.space_after = Pt(4)
        p.add_run(f"{idx}. {ref}")

    out_path = Path(r"c:\Users\VISHAL\Downloads\AI-Resume-Platform\repo\AI-Resume-Platform\Internship_Report_Yash_Vashisth_23BAI10717.docx")
    doc.save(str(out_path))
    print(f"Successfully generated docx report at: {out_path}")

if __name__ == "__main__":
    build_report()
