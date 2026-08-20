import fitz
import re


SECTION_ALIASES = {
    "education": [
        "education",
        "academic background",
        "academic qualifications"
    ],
    "skills": [
        "skills",
        "technical skills",
        "core skills",
        "technical expertise"
    ],
    "certifications": [
        "certifications",
        "certificates",
        "certifications & courses"
    ],
    "experience": [
        "experience",
        "work experience",
        "professional experience",
        "internship",
        "internships"
    ],
    "projects": [
        "projects",
        "academic projects",
        "personal projects",
        "project experience"
    ],
    "languages": [
        "languages",
        "language"
    ],
    "achievements": [
        "achievements",
        "extra-curricular",
        "extra-curricular and achievements"
    ]
}

SKILL_DICTIONARY = [
    "Python",
    "Java",
    "JavaScript",
    "C++",
    "C",
    "C#",
    "SQL",
    "HTML",
    "CSS",
    "React",
    "Next.js",
    "Node.js",
    "Express.js",
    "MongoDB",
    "MySQL",
    "PostgreSQL",
    "Spring Boot",
    "ASP.NET Core",
    "Entity Framework",
    "REST APIs",
    "Git",
    "GitHub",
    "Docker",
    "AWS",
    "Azure",
    "Machine Learning",
    "Deep Learning",
    "Artificial Intelligence",
    "NLP",
    "Data Structures and Algorithms",
    "Object Oriented Programming",
    "OOPS",
    "TensorFlow",
    "PyTorch",
    "Pandas",
    "NumPy",
    "OpenCV",
    "FastAPI",
    "Django",
    "Flask"
]

def extract_skills(skill_text):
    found_skills = []

    text = skill_text.lower()

    for skill in SKILL_DICTIONARY:
        skill_lower = skill.lower()

        if skill_lower in text:
            found_skills.append(skill)

    return found_skills

def extract_email(text):
    pattern = r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"

    match = re.search(pattern, text)

    if match:
        return match.group(0)

    return None


def extract_phone(text):
    pattern = r"(?:\+91[\s-]?)?[6-9]\d{9}"

    match = re.search(pattern, text)

    if match:
        return match.group(0)

    return None


def extract_name(blocks):
    if not blocks:
        return None

    # The first meaningful block is usually the candidate's name.
    first_text = blocks[0]["text"]

    # Ignore the block if it looks like contact information.
    if "@" in first_text or re.search(r"\d{7,}", first_text):
        return None

    return first_text.strip()

def extract_email(text):
    pattern = r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"

    match = re.search(pattern, text)

    if match:
        return match.group(0)

    return None


def extract_phone(text):
    pattern = r"(?:\+91[\s-]?)?[6-9]\d{9}"

    match = re.search(pattern, text)

    if match:
        return match.group(0)

    return None


def extract_name(blocks):
    if not blocks:
        return None

    first_text = blocks[0]["text"]

    if "@" in first_text or re.search(r"\d{7,}", first_text):
        return None

    return first_text.strip()
def build_structured_resume(sections, blocks):
    resume = {
        "name": None,
        "email": None,
        "phone": None,
        "education": [],
        "skills": [],
        "certifications": [],
        "experience": [],
        "projects": [],
        "achievements": [],
        "languages": []
    }

    for section, content in sections.items():
        resume[section] = content

    full_text = " ".join(
        block["text"] for block in blocks
    )

    resume["name"] = extract_name(blocks)
    resume["email"] = extract_email(full_text)
    resume["phone"] = extract_phone(full_text)

    skill_text = " ".join(resume["skills"])
    resume["skills"] = extract_skills(skill_text)

    return resume

def normalize_heading(text):
    text = text.lower().strip()
    text = re.sub(r"[^a-zA-Z&\s-]", "", text)
    text = re.sub(r"\s+", " ", text)
    return text


def detect_section(text):
    normalized = normalize_heading(text)

    for section, aliases in SECTION_ALIASES.items():
        for alias in aliases:
            if normalized == alias:
                return section

    return None


def get_pdf_blocks(pdf_path):
    document = fitz.open(pdf_path)

    blocks = []

    for page_number, page in enumerate(document, start=1):
        for block in page.get_text("blocks"):
            x0, y0, x1, y1, text, *_ = block

            text = text.strip()

            if not text:
                continue

            blocks.append({
                "page": page_number,
                "x0": x0,
                "y0": y0,
                "x1": x1,
                "y1": y1,
                "text": text
            })

    document.close()

    return blocks


def sort_blocks_visually(blocks):
    return sorted(
        blocks,
        key=lambda block: (
            block["page"],
            block["y0"],
            block["x0"]
        )
    )


def extract_sections(blocks):
    sections = {}
    current_section = None

    for block in blocks:
        section = detect_section(block["text"])

        if section:
            current_section = section

            if current_section not in sections:
                sections[current_section] = []

            continue

        if current_section:
            sections[current_section].append(block["text"])

    return sections

if __name__ == "__main__":
    pdf_path = "resumes/sample.pdf"

    blocks = get_pdf_blocks(pdf_path)
    blocks = sort_blocks_visually(blocks)

    sections = extract_sections(blocks)

    resume = build_structured_resume(sections, blocks)

    print("\n===== STRUCTURED RESUME =====")

for section, content in resume.items():
    print(f"\n{section.upper()}:")

    if isinstance(content, list):
        for item in content:
            print(f"- {item}")
    else:
        print(content)