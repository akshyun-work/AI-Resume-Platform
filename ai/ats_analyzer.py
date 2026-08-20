from pdf_parser import (
    get_pdf_blocks,
    sort_blocks_visually,
    extract_sections,
    build_structured_resume
)

def calculate_ats_score(resume):
    score = 0

    breakdown = {}

    # 1. Contact Information - 10 points
    contact_score = 0

    if resume.get("name"):
        contact_score += 3

    if resume.get("email"):
        contact_score += 4

    if resume.get("phone"):
        contact_score += 3

    breakdown["contact_information"] = contact_score
    score += contact_score

    # 2. Resume Sections - 15 points
    section_score = 0

    important_sections = [
        "education",
        "skills",
        "experience",
        "projects",
        "certifications"
    ]

    for section in important_sections:
        if resume.get(section):
            section_score += 3

    breakdown["resume_sections"] = section_score
    score += section_score

    # 3. Technical Skills - 25 points
    skills = resume.get("skills", [])

    if len(skills) >= 10:
        skill_score = 25
    elif len(skills) >= 7:
        skill_score = 20
    elif len(skills) >= 5:
        skill_score = 15
    elif len(skills) >= 3:
        skill_score = 10
    elif len(skills) > 0:
        skill_score = 5
    else:
        skill_score = 0

    breakdown["technical_skills"] = skill_score
    score += skill_score

    # 4. Projects - 15 points
    projects = resume.get("projects", [])

    if len(projects) >= 3:
        project_score = 15
    elif len(projects) == 2:
        project_score = 10
    elif len(projects) == 1:
        project_score = 5
    else:
        project_score = 0

    breakdown["projects"] = project_score
    score += project_score

    # 5. Experience - 15 points
    experience = resume.get("experience", [])

    if len(experience) >= 3:
        experience_score = 15
    elif len(experience) == 2:
        experience_score = 10
    elif len(experience) == 1:
        experience_score = 7
    else:
        experience_score = 0

    breakdown["experience"] = experience_score
    score += experience_score

    # 6. Certifications - 10 points
    certifications = resume.get("certifications", [])

    if len(certifications) >= 3:
        certification_score = 10
    elif len(certifications) == 2:
        certification_score = 7
    elif len(certifications) == 1:
        certification_score = 4
    else:
        certification_score = 0

    breakdown["certifications"] = certification_score
    score += certification_score

    # 7. Achievements - 10 points
    achievements = resume.get("achievements", [])

    if len(achievements) >= 3:
        achievement_score = 10
    elif len(achievements) == 2:
        achievement_score = 7
    elif len(achievements) == 1:
        achievement_score = 4
    else:
        achievement_score = 0

    breakdown["achievements"] = achievement_score
    score += achievement_score

    return {
        "score": score,
        "breakdown": breakdown
    }

if __name__ == "__main__":
    pdf_path = "resumes/sample.pdf"

    # Step 1: Extract PDF blocks
    blocks = get_pdf_blocks(pdf_path)

    # Step 2: Sort blocks according to visual position
    blocks = sort_blocks_visually(blocks)

    # Step 3: Detect resume sections
    sections = extract_sections(blocks)

    # Step 4: Build structured resume
    resume = build_structured_resume(sections, blocks)

    # Step 5: Calculate ATS score
    result = calculate_ats_score(resume)

    print("\n===== RESUME ANALYSIS =====")

    print(f"\nName: {resume['name']}")
    print(f"Email: {resume['email']}")
    print(f"Phone: {resume['phone']}")

    print("\nSkills:")
    for skill in resume["skills"]:
        print(f"- {skill}")

    print("\n===== ATS RESULT =====")
    print(f"ATS Score: {result['score']}/100")

    print("\nBreakdown:")

    for category, points in result["breakdown"].items():
        print(f"{category}: {points}")