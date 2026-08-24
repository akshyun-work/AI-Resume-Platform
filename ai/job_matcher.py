from pathlib import Path

from sentence_transformers import SentenceTransformer
from sklearn.metrics.pairwise import cosine_similarity

from pdf_parser import (
    get_pdf_blocks,
    sort_blocks_visually,
    extract_sections,
    build_structured_resume
)


SKILL_DICTIONARY = [
    "Python",
    "Java",
    "JavaScript",
    "C++",
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
    "OOPS",
    "TensorFlow",
    "PyTorch",
    "Pandas",
    "NumPy",
    "OpenCV",
    "FastAPI",
    "Django",
    "Flask",
    "Microservices"
]

# Local semantic embedding model.
# Loaded once and reused for matching.
EMBEDDING_MODEL = SentenceTransformer(
    "all-MiniLM-L6-v2"
)

def extract_skills(text):
    """
    Find known technical skills inside a piece of text.
    """
    found_skills = []

    text_lower = text.lower()

    for skill in SKILL_DICTIONARY:
        if skill.lower() in text_lower:
            found_skills.append(skill)

    return found_skills


def read_job_description(file_path):
    """
    Read the complete job description.
    """
    with open(file_path, "r", encoding="utf-8") as file:
        return file.read()


def extract_job_sections(job_text):
    """
    Separate the job description into requirements,
    preferred skills, and responsibilities.
    """

    required_text = ""
    preferred_text = ""
    responsibilities_text = ""

    current_section = None

    for line in job_text.splitlines():
        line_clean = line.strip()

        if not line_clean:
            continue

        line_lower = line_clean.lower()

        if "requirements" in line_lower:
            current_section = "required"
            continue

        if "preferred" in line_lower:
            current_section = "preferred"
            continue

        if "responsibilities" in line_lower:
            current_section = "responsibilities"
            continue

        if current_section == "required":
            required_text += " " + line_clean

        elif current_section == "preferred":
            preferred_text += " " + line_clean

        elif current_section == "responsibilities":
            responsibilities_text += " " + line_clean

    return {
        "required": required_text,
        "preferred": preferred_text,
        "responsibilities": responsibilities_text
    }


def analyze_job(job_text):
    """
    Convert raw job description into structured information.
    """

    sections = extract_job_sections(job_text)

    required_skills = extract_skills(sections["required"])
    preferred_skills = extract_skills(sections["preferred"])
    responsibility_skills = extract_skills(
        sections["responsibilities"]
    )

    # Include skills mentioned in responsibilities
    # as additional job-related skills.
    all_job_skills = list(
        dict.fromkeys(
            required_skills
            + preferred_skills
            + responsibility_skills
        )
    )

    return {
        "required_skills": required_skills,
        "preferred_skills": preferred_skills,
        "responsibility_skills": responsibility_skills,
        "all_skills": all_job_skills,

        "required": sections["required"],
        "preferred": sections["preferred"],
        "responsibilities": sections["responsibilities"]
    }


def compare_skills(resume_skills, job_analysis):
    """
    Compare resume skills against job requirements.
    """

    resume_skill_set = {
        skill.lower()
        for skill in resume_skills
    }

    required_skills = job_analysis["required_skills"]
    preferred_skills = job_analysis["preferred_skills"]

    matched_required = []
    missing_required = []

    for skill in required_skills:
        if skill.lower() in resume_skill_set:
            matched_required.append(skill)
        else:
            missing_required.append(skill)

    matched_preferred = []
    missing_preferred = []

    for skill in preferred_skills:
        if skill.lower() in resume_skill_set:
            matched_preferred.append(skill)
        else:
            missing_preferred.append(skill)

    return {
        "matched_required": matched_required,
        "missing_required": missing_required,
        "matched_preferred": matched_preferred,
        "missing_preferred": missing_preferred
    }

def calculate_semantic_similarity(resume, job_analysis):
    """
    Calculate semantic similarity between the resume
    and the complete job requirements.

    Returns a value between 0 and 1.
    """

    resume_parts = []

    for key in [
        "skills",
        "projects",
        "experience",
        "education",
        "certifications"
    ]:
        value = resume.get(key, "")

        if isinstance(value, list):
            resume_parts.extend(
                str(item)
                for item in value
            )
        elif value:
            resume_parts.append(
                str(value)
            )

    resume_text = " ".join(resume_parts)

    job_parts = []

    for key in [
        "required",
        "preferred",
        "responsibilities"
    ]:
        value = job_analysis.get(
            key,
            ""
        )

        if value:
            job_parts.append(value)

    job_text = " ".join(job_parts)

    if not resume_text or not job_text:
        return 0.0

    embeddings = EMBEDDING_MODEL.encode(
        [
            resume_text,
            job_text
        ]
    )

    similarity = cosine_similarity(
        [embeddings[0]],
        [embeddings[1]]
    )[0][0]

    # Convert to a safe 0–1 range.
    return max(
        0.0,
        min(
            1.0,
            float(similarity)
        )
    )

def calculate_match_score(comparison):
    """
    Calculate a simple explainable job-match score.

    Required skills have higher importance than preferred skills.
    """

    required_total = (
        len(comparison["matched_required"])
        + len(comparison["missing_required"])
    )

    preferred_total = (
        len(comparison["matched_preferred"])
        + len(comparison["missing_preferred"])
    )

    required_score = 0
    preferred_score = 0

    if required_total > 0:
        required_score = (
            len(comparison["matched_required"])
            / required_total
        ) * 70

    if preferred_total > 0:
        preferred_score = (
            len(comparison["matched_preferred"])
            / preferred_total
        ) * 30

    return round(required_score + preferred_score)


def generate_recommendations(comparison):
    """
    Generate simple recommendations based on missing skills.
    """

    recommendations = []

    if comparison["missing_required"]:
        recommendations.append(
            "Focus on the missing required skills: "
            + ", ".join(comparison["missing_required"])
        )

    if comparison["missing_preferred"]:
        recommendations.append(
            "Consider learning the preferred skills: "
            + ", ".join(comparison["missing_preferred"])
        )

    if not recommendations:
        recommendations.append(
            "Your resume covers the detected job requirements well."
        )

    return recommendations


def analyze_resume_against_job(resume, job_text):
    """
    Complete resume-to-job analysis.
    """

    job_analysis = analyze_job(job_text)

    comparison = compare_skills(
        resume["skills"],
        job_analysis
    )

    keyword_score = calculate_match_score(comparison)

    semantic_similarity = calculate_semantic_similarity(resume,job_analysis )

    semantic_score = round(semantic_similarity * 100)

    match_score = round((keyword_score * 0.60) + (semantic_score * 0.40))

    recommendations = generate_recommendations(comparison)

    return {
    "job_analysis": job_analysis,
    "comparison": comparison,
    "keyword_score": keyword_score,
    "semantic_similarity": semantic_similarity,
    "semantic_score": semantic_score,
    "match_score": match_score,
    "recommendations": recommendations
}


def print_results(resume, result):
    """
    Display the complete analysis.
    """

    job_analysis = result["job_analysis"]
    comparison = result["comparison"]

    print("\n================================")
    print("       JOB MATCH ANALYSIS")
    print("================================")

    print("\nCandidate:")
    print(resume["name"])

    print("\nRequired Skills:")
    for skill in job_analysis["required_skills"]:
        print(f"- {skill}")

    print("\nPreferred Skills:")
    for skill in job_analysis["preferred_skills"]:
        print(f"- {skill}")

    print("\nMatched Required Skills:")
    for skill in comparison["matched_required"]:
        print(f"✓ {skill}")

    print("\nMissing Required Skills:")
    for skill in comparison["missing_required"]:
        print(f"✗ {skill}")

    print("\nMatched Preferred Skills:")
    for skill in comparison["matched_preferred"]:
        print(f"✓ {skill}")

    print("\nMissing Preferred Skills:")
    for skill in comparison["missing_preferred"]:
        print(f"• {skill}")

    print("\n================================")
    print(
        f"       JOB MATCH SCORE: "
        f"{result['match_score']}/100"
    )
    print("================================")

    print(
        f"       Keyword Score: "
        f"{result['keyword_score']}/100"
    )

    print(
        f"       Semantic Score: "
        f"{result['semantic_score']}/100"
    )

    print("\nRecommendations:")

    for recommendation in result["recommendations"]:
        print(f"- {recommendation}")


if __name__ == "__main__":

    # -----------------------------------------
    # 1. Locate resume
    # -----------------------------------------

    base_dir = Path(__file__).resolve().parent

    pdf_path = base_dir / "resumes" / "sample.pdf"

    # -----------------------------------------
    # 2. Parse resume
    # -----------------------------------------

    blocks = get_pdf_blocks(pdf_path)

    blocks = sort_blocks_visually(blocks)

    sections = extract_sections(blocks)

    resume = build_structured_resume(
        sections,
        blocks
    )

    # -----------------------------------------
    # 3. Read job description
    # -----------------------------------------

    job_path = base_dir / "job_description.txt"

    job_text = read_job_description(job_path)

    # -----------------------------------------
    # 4. Analyze resume against job
    # -----------------------------------------

    result = analyze_resume_against_job(
        resume,
        job_text
    )

    # -----------------------------------------
    # 5. Display result
    # -----------------------------------------

    print_results(
        resume,
        result
    )