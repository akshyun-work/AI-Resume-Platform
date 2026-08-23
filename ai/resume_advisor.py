import re
from pathlib import Path

from pdf_parser import (
    get_pdf_blocks,
    sort_blocks_visually,
    extract_sections,
    build_structured_resume
)


def normalize_text(value):
    """
    Convert different types of resume data into searchable text.
    """

    if value is None:
        return ""

    if isinstance(value, list):
        return " ".join(str(item) for item in value)

    if isinstance(value, dict):
        return " ".join(str(item) for item in value.values())

    return str(value)


def get_resume_text(resume):
    """
    Combine all structured resume information into one text string.
    """

    parts = []

    for key, value in resume.items():
        parts.append(normalize_text(value))

    return " ".join(parts).lower()


def analyze_resume(resume):
    """
    Analyze the resume for strengths, weaknesses,
    and improvement opportunities.
    """

    resume_text = get_resume_text(resume)

    strengths = []
    weaknesses = []
    suggestions = []

    # -----------------------------------------
    # Skills
    # -----------------------------------------

    skills = resume.get("skills", [])

    if isinstance(skills, list) and len(skills) >= 5:
        strengths.append(
            "The resume contains a good range of technical skills."
        )
    elif skills:
        weaknesses.append(
            "The technical skill section could be expanded."
        )
        suggestions.append(
            "Add relevant technical skills supported by "
            "projects, coursework, or experience."
        )
    else:
        weaknesses.append(
            "Technical skills were not clearly detected."
        )
        suggestions.append(
            "Add a clearly labelled technical skills section."
        )

    # -----------------------------------------
    # Projects
    # -----------------------------------------

    projects = resume.get("projects", [])

    project_text = normalize_text(projects)

    if project_text:
        strengths.append(
            "The resume contains project experience."
        )

        # Look for technical/project terminology
        if any(
            word in project_text
            for word in [
                "developed",
                "built",
                "implemented",
                "created",
                "designed"
            ]
        ):
            strengths.append(
                "Project descriptions contain development-related actions."
            )
        else:
            weaknesses.append(
                "Project descriptions could communicate "
                "technical contributions more clearly."
            )

            suggestions.append(
                "Describe what you personally built or implemented "
                "in each project."
            )

    else:
        weaknesses.append(
            "No projects were clearly detected."
        )

        suggestions.append(
            "Add relevant academic or personal projects."
        )

    # -----------------------------------------
    # Experience
    # -----------------------------------------

    experience = resume.get("experience", [])

    experience_text = normalize_text(experience)

    if experience_text:
        strengths.append(
            "The resume contains experience information."
        )

    has_percentage = bool(
        re.search(
            r"\b\d+(?:\.\d+)?\s*%",
        experience_text
    )
)

# Quantities with common impact units
    has_quantity = bool(
        re.search(
            r"\b\d+(?:\.\d+)?\s*"
            r"(?:users?|customers?|clients?|"
            r"projects?|features?|"
            r"applications?|requests?|"
            r"records?|issues?|bugs?|"
            r"contributions?)\b",
            experience_text
        )
    )

    impact_phrases = [
        "increased",
        "decreased",
        "reduced",
        "improved",
        "optimized",
        "accelerated",
        "saved",
        "grew",
        "achieved",
        "ranked",
        "top ",
        "award",
        "won"
    ]

    has_impact_phrase = any(
        phrase in experience_text
        for phrase in impact_phrases
    )

    has_measurable_impact = (
        has_percentage
        or has_quantity
        or has_impact_phrase
    )

    if not has_measurable_impact:
        weaknesses.append(
            "Experience descriptions contain few "
            "measurable or outcome-oriented results."
        )

        suggestions.append(
            "Add measurable outcomes such as percentages, "
            "user counts, performance improvements, "
            "features delivered, rankings, awards, or "
            "other concrete results."
        )

    else:
        weaknesses.append(
            "Professional experience was not clearly detected."
        )

        suggestions.append(
            "If you have relevant internships, contributions, "
            "freelance work, or practical experience, include them."
        )

    # -----------------------------------------
    # Certifications
    # -----------------------------------------

    certifications = resume.get("certifications", [])

    if certifications:
        strengths.append(
            "The resume contains certifications."
        )
    else:
        suggestions.append(
            "Consider adding relevant technical certifications "
            "if they strengthen your target profile."
        )

    # -----------------------------------------
    # Achievements
    # -----------------------------------------

    achievements = resume.get("achievements", [])

    if achievements:
        strengths.append(
            "The resume contains achievements or extracurricular activity."
        )
    else:
        suggestions.append(
            "Add relevant achievements, competitions, "
            "open-source contributions, or leadership activities."
        )

    # -----------------------------------------
    # Contact information
    # -----------------------------------------

    contact_found = any(
        field in resume
        and resume[field]
        for field in [
            "email",
            "phone",
            "contact_information"
        ]
    )

    if contact_found:
        strengths.append(
            "Contact information was detected."
        )
    else:
        weaknesses.append(
            "Complete contact information was not clearly detected."
        )

        suggestions.append(
            "Ensure your email address and phone number "
            "are clearly visible."
        )

    # -----------------------------------------
    # Final cleanup
    # -----------------------------------------

    strengths = list(dict.fromkeys(strengths))
    weaknesses = list(dict.fromkeys(weaknesses))
    suggestions = list(dict.fromkeys(suggestions))

    return {
        "strengths": strengths,
        "weaknesses": weaknesses,
        "suggestions": suggestions
    }


def print_analysis(resume, analysis):

    print("\n================================")
    print("       RESUME ADVISOR")
    print("================================")

    print("\nCandidate:")

    name = resume.get("name", "Unknown")

    print(name)

    print("\n----- STRENGTHS -----")

    if analysis["strengths"]:
        for item in analysis["strengths"]:
            print(f"✓ {item}")
    else:
        print("No major strengths detected.")

    print("\n----- WEAKNESSES -----")

    if analysis["weaknesses"]:
        for item in analysis["weaknesses"]:
            print(f"✗ {item}")
    else:
        print("No major weaknesses detected.")

    print("\n----- IMPROVEMENT SUGGESTIONS -----")

    if analysis["suggestions"]:
        for item in analysis["suggestions"]:
            print(f"→ {item}")
    else:
        print("No immediate suggestions.")


if __name__ == "__main__":

    # -----------------------------------------
    # Locate resume
    # -----------------------------------------

    base_dir = Path(__file__).resolve().parent

    pdf_path = base_dir / "resumes" / "sample.pdf"

    # -----------------------------------------
    # Parse resume
    # -----------------------------------------

    blocks = get_pdf_blocks(pdf_path)

    blocks = sort_blocks_visually(blocks)

    sections = extract_sections(blocks)

    resume = build_structured_resume(
        sections,
        blocks
    )

    # -----------------------------------------
    # Analyze resume
    # -----------------------------------------

    analysis = analyze_resume(resume)

    # -----------------------------------------
    # Display results
    # -----------------------------------------

    print_analysis(
        resume,
        analysis
    )