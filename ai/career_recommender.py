from pathlib import Path

from pdf_parser import (
    get_pdf_blocks,
    sort_blocks_visually,
    extract_sections,
    build_structured_resume
)


# --------------------------------------------------
# Role Knowledge Base
# --------------------------------------------------

ROLE_PROFILES = {
    "Java Backend Developer": {
        "skills": [
            "Java",
            "Spring Boot",
            "SQL",
            "REST APIs",
            "Git",
            "Docker",
            "Data Structures and Algorithms"
        ]
    },

    "Python Developer": {
        "skills": [
            "Python",
            "SQL",
            "Git",
            "REST APIs",
            "Django",
            "Flask"
        ]
    },

    "Full Stack Developer": {
        "skills": [
            "HTML",
            "CSS",
            "JavaScript",
            "React",
            "Node.js",
            "Express.js",
            "MongoDB",
            "Git",
            "REST APIs"
        ]
    },

    "Frontend Developer": {
        "skills": [
            "HTML",
            "CSS",
            "JavaScript",
            "React",
            "Git"
        ]
    },

    "Data Analyst": {
        "skills": [
            "Python",
            "SQL",
            "Excel",
            "Pandas",
            "NumPy",
            "Data Visualization"
        ]
    },

    "Machine Learning Engineer": {
        "skills": [
            "Python",
            "Machine Learning",
            "Deep Learning",
            "NumPy",
            "Pandas",
            "TensorFlow",
            "PyTorch",
            "SQL"
        ]
    }
}


# --------------------------------------------------
# Extract candidate skills
# --------------------------------------------------

def get_candidate_skills(resume):
    """
    Extract the skills detected by the existing
    resume parser.
    """

    skills = resume.get("skills", [])

    if not isinstance(skills, list):
        return []

    return skills


# --------------------------------------------------
# Compare candidate against role
# --------------------------------------------------

def calculate_role_match(candidate_skills, role_skills):
    """
    Calculate how many role skills are present
    in the candidate resume.
    """

    candidate_skill_set = {
        skill.lower()
        for skill in candidate_skills
    }

    matched = []
    missing = []

    for skill in role_skills:

        if skill.lower() in candidate_skill_set:
            matched.append(skill)
        else:
            missing.append(skill)

    if len(role_skills) == 0:
        score = 0
    else:
        score = round(
            (len(matched) / len(role_skills)) * 100
        )

    return {
        "score": score,
        "matched": matched,
        "missing": missing
    }


# --------------------------------------------------
# Recommend roles
# --------------------------------------------------

def recommend_roles(resume):
    """
    Compare the candidate against every role
    in the role knowledge base.
    """

    candidate_skills = get_candidate_skills(
        resume
    )

    recommendations = []

    for role_name, role_data in ROLE_PROFILES.items():

        result = calculate_role_match(
            candidate_skills,
            role_data["skills"]
        )

        recommendations.append({
            "role": role_name,
            "score": result["score"],
            "matched": result["matched"],
            "missing": result["missing"]
        })

    # Highest match first
    recommendations.sort(
        key=lambda item: item["score"],
        reverse=True
    )

    return recommendations


# --------------------------------------------------
# Display recommendations
# --------------------------------------------------

def print_recommendations(
    resume,
    recommendations
):

    print("\n================================")
    print("      CAREER RECOMMENDATIONS")
    print("================================")

    print(
        f"\nCandidate: "
        f"{resume.get('name', 'Unknown')}"
    )

    print("\nDetected Skills:")

    candidate_skills = get_candidate_skills(
        resume
    )

    for skill in candidate_skills:
        print(f"- {skill}")

    print(
        "\n================================"
    )

    for index, recommendation in enumerate(
        recommendations,
        start=1
    ):

        print(
            f"\n{index}. "
            f"{recommendation['role']}"
        )

        print(
            f"   Match: "
            f"{recommendation['score']}%"
        )

        print("   Matched:")

        if recommendation["matched"]:

            for skill in recommendation["matched"]:
                print(f"   ✓ {skill}")

        else:
            print("   None")

        print("   Missing:")

        if recommendation["missing"]:

            for skill in recommendation["missing"]:
                print(f"   ✗ {skill}")

        else:
            print("   None")


# --------------------------------------------------
# Main
# --------------------------------------------------

if __name__ == "__main__":

    base_dir = Path(__file__).resolve().parent

    pdf_path = (
        base_dir
        / "resumes"
        / "sample.pdf"
    )

    # Parse resume using our existing parser
    blocks = get_pdf_blocks(pdf_path)

    blocks = sort_blocks_visually(blocks)

    sections = extract_sections(blocks)

    resume = build_structured_resume(
        sections,
        blocks
    )

    # Generate recommendations
    recommendations = recommend_roles(
        resume
    )

    # Display results
    print_recommendations(
        resume,
        recommendations
    )