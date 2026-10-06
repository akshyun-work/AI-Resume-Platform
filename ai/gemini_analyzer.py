import os
import json
from ats_analyzer import calculate_ats_score
from pathlib import Path

from google import genai

from pdf_parser import (
    get_pdf_blocks,
    sort_blocks_visually,
    extract_sections,
    build_structured_resume
)

from job_api import FreeHireJobProvider

from job_matcher import (
    analyze_job,
    compare_skills,
    calculate_match_score,
    calculate_semantic_similarity
)

from career_recommender import (
    recommend_roles
)

from resume_chatbot import (
    build_chat_context,
    run_chatbot
)

def create_gemini_client():
    """
    Create Gemini client using the API key
    stored in the environment variable.
    """

    api_key = os.getenv("GEMINI_API_KEY")

    if not api_key:
        raise ValueError(
            "GEMINI_API_KEY is not configured."
        )

    return genai.Client(
        api_key=api_key
    )


def build_candidate_context(
    resume,
    job,
    job_analysis,
    comparison,
    keyword_score,
    semantic_score,
    match_score,
    career_recommendations
):
    """
    Convert our existing Python analysis into
    structured information for Gemini.
    """

    return {
        "candidate": {
            "name": resume.get(
                "name",
                "Unknown"
            )
        },

        "resume": {
            "skills": resume.get(
                "skills",
                []
            ),

            "projects": resume.get(
                "projects",
                []
            ),

            "experience": resume.get(
                "experience",
                []
            ),

            "education": resume.get(
                "education",
                []
            ),

            "certifications": resume.get(
                "certifications",
                []
            )
        },

                "job": {
            "title": (
                job.get(
                    "title",
                    "User-provided job description"
                )
                if job
                else "User-provided job description"
            ),

            "company": (
                job.get(
                    "company",
                    "Not specified"
                )
                if job
                else "Not specified"
            ),

            "location": (
                job.get(
                    "location",
                    "Not specified"
                )
                if job
                else "Not specified"
            ),

            "url": (
                job.get(
                    "url",
                    ""
                )
                if job
                else ""
            ),

            "required_skills":
                job_analysis.get(
                    "required_skills",
                    []
                ),

            "preferred_skills":
                job_analysis.get(
                    "preferred_skills",
                    []
                ),

            "job_skills":
                job_analysis.get(
                    "job_skills",
                    []
                )
        },

        "job_match": {
             "score": match_score,

            "keyword_score": keyword_score,

            "semantic_score": semantic_score,

            "matched_required":
                comparison.get(
                    "matched_required",
                    []
                ),

            "missing_required":
                comparison.get(
                    "missing_required",
                    []
                ),

            "matched_preferred":
                comparison.get(
                    "matched_preferred",
                    []
                ),

            "missing_preferred":
                comparison.get(
                    "missing_preferred",
                    []
                ),
            "matched_job_skills":
                comparison.get(
                    "matched_job_skills",
                    []
                ),

            "missing_job_skills":
                comparison.get(
                    "missing_job_skills",
                    []
                )
        },

        "career_recommendations": [
            {
                "role": item["role"],
                "score": item["score"],
                "missing": item["missing"]
            }

            for item in career_recommendations[:5]
        ]
    }


def generate_fallback_match_analysis(context):
    """
    Generate deterministic structured JSON fallback containing all 6 Why This Match subcategories.
    """
    job_match = context.get("job_match", {})
    candidate_name = context.get("candidate", {}).get("name", "Candidate")
    job = context.get("job") or {}
    job_title = job.get("title", "this position")
    matched_req = job_match.get("matched_required", []) or []
    matched_pref = job_match.get("matched_preferred", []) or []
    matched_job = job_match.get("matched_job_skills", []) or []
    missing_req = job_match.get("missing_required", []) or []
    missing_pref = job_match.get("missing_preferred", []) or []
    missing_job = job_match.get("missing_job_skills", []) or []
    score = job_match.get("score", 0)

    # 1. Resume Strengths
    resume_strengths = []
    if matched_req:
        resume_strengths.append({
            "title": "Strong Technical Foundation",
            "description": f"You possess core required skills including {', '.join(str(s) for s in matched_req)}."
        })
    if matched_pref or matched_job:
        resume_strengths.append({
            "title": "Relevant Project & Domain Skills",
            "description": f"Your profile demonstrates verified experience in {', '.join(str(s) for s in (matched_pref + matched_job)[:4])}."
        })
    if not resume_strengths:
        resume_strengths.append({
            "title": "Transferable Background",
            "description": "Demonstrates transferable engineering background and analytical foundations."
        })

    # 2. Resume Weaknesses
    resume_weaknesses = []
    if missing_req:
        resume_weaknesses.append({
            "title": "Missing Core Required Competencies",
            "description": f"Your resume lacks explicit mention of {', '.join(str(s) for s in missing_req)}—critical for this position."
        })
    if missing_pref:
        resume_weaknesses.append({
            "title": "Lack of Preferred Tools & Architecture Exposure",
            "description": f"Currently missing preferred experience in {', '.join(str(s) for s in missing_pref)}."
        })
    if missing_job:
        resume_weaknesses.append({
            "title": "Job-Specific Skill Gaps",
            "description": f"No direct evidence for job-specific expectations: {', '.join(str(s) for s in missing_job)}."
        })
    if not resume_weaknesses:
        resume_weaknesses.append({
            "title": "Minor Content Optimization",
            "description": "No major technical weaknesses detected for this role."
        })

    # 3. Explanation of Job Match
    factors = []
    if missing_req:
        factors.append(f"Impacted by absence of required skills: {', '.join(str(s) for s in missing_req[:3])}")
    if missing_pref:
        factors.append(f"Missing preferred skills: {', '.join(str(s) for s in missing_pref[:3])}")
    if not factors:
        factors.append("Candidate strongly satisfies the role criteria.")

    summary = f"{candidate_name} matches {score}% of the core criteria for {job_title}."
    analysis_text = f"You meet key required areas ({', '.join(str(s) for s in (matched_req or ['foundational areas']))}). The score of {score}/100 is adjusted based on missing required & preferred items."

    # 5. Improvement Actions
    improvement_actions = []
    all_missing = missing_req + missing_pref + missing_job
    for i, skill in enumerate(all_missing[:3], 1):
        improvement_actions.append({
            "title": f"Develop and Document {skill}",
            "action": f"Build or highlight a project specifically implementing and testing {skill}, then explicitly list it on your resume."
        })
    if not improvement_actions:
        improvement_actions.append({
            "title": "Quantify Resume Impact",
            "action": "Add quantifiable metrics and leadership outcomes to your existing project descriptions."
        })

    # 6. Career Direction
    career_recs = context.get("career_recommendations", [])
    alt_role = career_recs[0]["role"] if career_recs else "Specialized Engineering"
    career_direction = {
        "immediate_target": f"{job_title} (requires bridging the gap in {', '.join(str(s) for s in all_missing[:2]) if all_missing else 'specialized workflows'}).",
        "stronger_alignment": f"Your current skill set also shows high alignment for {alt_role} roles given your existing projects and strengths."
    }

    return {
        "resume_strengths": resume_strengths,
        "resume_weaknesses": resume_weaknesses,
        "explanation_of_job_match": {
            "score": score,
            "summary": summary,
            "analysis": analysis_text,
            "factors": factors
        },
        "most_important_missing_skills": {
            "required": missing_req,
            "preferred": missing_pref,
            "job_specific": missing_job
        },
        "prioritized_improvement_actions": improvement_actions,
        "career_direction": career_direction,

        # Backward compatibility aliases
        "match_summary": summary,
        "why_you_match": [s.get("description", str(s)) if isinstance(s, dict) else str(s) for s in resume_strengths],
        "what_is_missing": {
            "required": missing_req,
            "preferred": missing_pref,
            "job_specific": missing_job
        },
        "score_explanation": {
            "score": score,
            "explanation": analysis_text,
            "factors": factors
        },
        "improvement_actions": [f"{a['title']}: {a['action']}" if isinstance(a, dict) else str(a) for a in improvement_actions]
    }


def build_prompt(context):
    """
    Build a structured prompt for the 6-part "Why This Match" career evaluation.
    """
    context_str = json.dumps(context, indent=2, default=str)

    return f"""
You are an expert AI career advisor and hiring evaluator inside a resume intelligence platform.

Analyze ONLY the candidate and job information provided below.

Candidate and calculated match data:
{context_str}

Your task is to generate the comprehensive 6-part "Why This Match" assessment.

Return ONLY valid JSON.
Do not use Markdown.
Do not use ```json.
Do not add any text before or after the JSON.

Use EXACTLY this structure:

{{
  "resume_strengths": [
    {{
      "title": "Strength Category/Title (e.g., Strong Technical Foundation)",
      "description": "Specific explanation grounded in the candidate's matched skills and projects."
    }}
  ],

  "resume_weaknesses": [
    {{
      "title": "Weakness Category/Title (e.g., Missing Core Backend Competencies)",
      "description": "Specific explanation of what skills or principles are missing for this role."
    }}
  ],

  "explanation_of_job_match": {{
    "score": 0,
    "summary": "Concise summary of candidate suitability for this position.",
    "analysis": "Detailed explanation of why the calculated score is at this level based on required vs preferred skills.",
    "factors": [
      "Key factor affecting the score"
    ]
  }},

  "most_important_missing_skills": {{
    "required": [
      "Missing required skill"
    ],
    "preferred": [
      "Missing preferred skill"
    ],
    "job_specific": [
      "Missing job-specific skill"
    ]
  }},

  "prioritized_improvement_actions": [
    {{
      "title": "Action Title (e.g., Develop and Document REST APIs)",
      "action": "Practical, step-by-step guidance on what to build or update on the resume."
    }}
  ],

  "career_direction": {{
    "immediate_target": "Target role and what is needed to qualify.",
    "stronger_alignment": "Alternative or adjacent role where candidate profile has highest potential given their background."
  }}
}}

Rules:
1. "resume_strengths" must reference verified matched skills/projects from the provided data.
2. "resume_weaknesses" must accurately describe missing requirements without hallucinating unrelated technologies.
3. "explanation_of_job_match.score" must exactly equal the Python-calculated job match score.
4. "most_important_missing_skills" must categorize ONLY the supplied missing skills.
5. "prioritized_improvement_actions" must contain 2-3 concrete, actionable recommendations.
6. "career_direction" must advise on the immediate target and any higher-potential career pathways based on their projects/experience.
7. Keep tone professional, constructive, and empowering.
"""


def analyze_with_gemini(context):
    """
    Send the structured analysis to Gemini and return
    validated JSON object or fallback structure.
    """
    fallback = generate_fallback_match_analysis(context)

    try:
        client = create_gemini_client()

        prompt = build_prompt(context)

        response = client.models.generate_content(
            model="gemini-3.1-flash-lite",
            contents=prompt,
            config={
                "response_mime_type": "application/json"
            }
        )

        if not response.text:
            return fallback

        raw_text = response.text.strip()

        # Remove accidental Markdown fences if Gemini adds them.
        if raw_text.startswith("```json"):
            raw_text = raw_text[7:]

        if raw_text.startswith("```"):
            raw_text = raw_text[3:]

        if raw_text.endswith("```"):
            raw_text = raw_text[:-3]

        raw_text = raw_text.strip()

        parsed = json.loads(raw_text)
        if isinstance(parsed, dict) and any(k in parsed for k in ("resume_strengths", "explanation_of_job_match", "match_summary", "career_direction")):
            if "match_summary" not in parsed and isinstance(parsed.get("explanation_of_job_match"), dict):
                parsed["match_summary"] = parsed["explanation_of_job_match"].get("summary", "")
            if "why_you_match" not in parsed and isinstance(parsed.get("resume_strengths"), list):
                parsed["why_you_match"] = [
                    s.get("description", str(s)) if isinstance(s, dict) else str(s)
                    for s in parsed["resume_strengths"]
                ]
            return parsed

        return fallback

    except Exception as error:
        # Gracefully fall back to deterministic structured response
        return fallback

def print_gemini_analysis(
    resume,
    match_score,
    analysis
):

    print("\n================================")
    print("       GEMINI AI ANALYSIS")
    print("================================")

    print(
        f"\nCandidate: "
        f"{resume.get('name', 'Unknown')}"
    )

    print(
        f"Job Match Score: "
        f"{match_score}/100"
    )

    print("\n--------------------------------")
    print(analysis)
    print("--------------------------------")


def select_job(jobs):
    """
    Display available jobs and allow the user
    to select one.
    """

    print("\n================================")
    print("       AVAILABLE JOBS")
    print("================================")

    if not jobs:
        raise RuntimeError(
            "No jobs were found."
        )

    for index, job in enumerate(jobs, start=1):

        print(
            f"\n[{index}] "
            f"{job.get('title', 'Unknown')}"
        )

        print(
            f"Company: "
            f"{job.get('company') or 'Not specified'}"
        )

        print(
            f"Location: "
            f"{job.get('location') or 'Not specified'}"
        )

        print(
            f"Employment: "
            f"{job.get('contract_type') or 'Not specified'}"
        )

        print(
            f"URL: "
            f"{job.get('url') or 'Not available'}"
        )

        print("-" * 50)

    while True:

        choice = input(
            f"\nSelect a job (1-{len(jobs)}): "
        )

        try:
            selected_index = int(choice)

        except ValueError:
            print(
                "Please enter a valid number."
            )
            continue

        if 1 <= selected_index <= len(jobs):
            return jobs[selected_index - 1]

        print(
            f"Please enter a number between "
            f"1 and {len(jobs)}."
        )

def run_pipeline( 
    pdf_path,
    job_mode="search",
    job_description=None,
    job_name=None,
    location=None):
    # -----------------------------------------
    # Locate files
    # -----------------------------------------


    # -----------------------------------------
    # Parse resume
    # -----------------------------------------

    try:
        blocks = get_pdf_blocks(
        pdf_path
        )

        blocks = sort_blocks_visually(
        blocks
        )

        sections = extract_sections(
        blocks
        )

        resume = build_structured_resume(
        sections,
        blocks
        )

    except FileNotFoundError as error:
        raise RuntimeError(
        f"Resume PDF was not found: {pdf_path}"
        ) from error

    except Exception as error:
        raise RuntimeError(
        f"Resume processing failed: {error}"
        ) from error

    # -----------------------------------------
    # Calculate ATS score
    # -----------------------------------------

    ats_result = calculate_ats_score(
    resume
    )
    # -----------------------------------------
    # Obtain job input
    # -----------------------------------------

    job = None

    if job_mode == "description":

        if not job_description or not job_description.strip():
            raise ValueError(
                "A job description is required "
                "when job_mode='description'."
            )

        job_text = job_description.strip()

        try:
            job_analysis = analyze_job(
            job_text
        )

        except Exception as error:
            raise RuntimeError(
                f"Job analysis failed: {error}"
            ) from error

    elif job_mode == "search":

        if not job_name or not job_name.strip():
            raise ValueError(
                "A job name is required "
                "when job_mode='search'."
            )

        if not location or not location.strip():
            raise ValueError(
                "A location is required "
                "when job_mode='search'."
            )
        job_provider = FreeHireJobProvider()

        try:
            jobs = job_provider.search_jobs(
            keywords=job_name.strip(),
            location=location.strip(),
            results_per_page=10
            )

        except Exception as error:
            raise RuntimeError(
                f"Job search failed: {error}"
            )from error

        if not jobs:
            raise RuntimeError(
            "No jobs were found for the "
            "specified job name and location."
            )

        job = select_job(jobs)

        job_text = job.get(
            "description",
            ""
        )

        if not job_text.strip():
            raise RuntimeError(
                "The selected job does not contain "
                "a usable description."
            )

        print("\n================================")
        print("       SELECTED JOB")
        print("================================")

        print(
            f"Title: {job.get('title', 'Unknown')}"
        )

        print(
            f"Company: {job.get('company', 'Unknown')}"
        )

        print(
            f"Location: {job.get('location', 'Unknown')}"
        )

        print(
            f"URL: {job.get('url', 'Not available')}"
        )
        try:
            job_analysis = analyze_job(
                job_text,
                job_data=job
        )

        except Exception as error:
            raise RuntimeError(
                f"Job analysis failed: {error}"
            ) from error

    else:

        raise ValueError(
            "Invalid job_mode. Use "
            "'description' or 'search'."
        )
     

    # -----------------------------------------
    # Compare resume with job
    # -----------------------------------------

    comparison = compare_skills(resume["skills"],job_analysis)

    keyword_score = calculate_match_score(comparison)

    semantic_similarity = calculate_semantic_similarity(resume,job_analysis)

    semantic_score = round(semantic_similarity * 100)

    match_score = round((keyword_score * 0.60)+ (semantic_score * 0.40))

    # -----------------------------------------
    # Career recommendations
    # -----------------------------------------

    career_recommendations = recommend_roles(
        resume
    )

    # -----------------------------------------
    # Build LLM context
    # -----------------------------------------

    context = build_candidate_context(
        resume,
        job,
        job_analysis,
        comparison,
        keyword_score,
        semantic_score,
        match_score,
        career_recommendations
    )

    # -----------------------------------------
    # Gemini analysis
    # -----------------------------------------

    analysis = analyze_with_gemini(
        context
    )

    # -----------------------------------------
    # Display
    # -----------------------------------------

    print("\n================================")
    print("          ATS SCORE")
    print("================================")

    print(
        f"ATS Score: "
        f"{ats_result['score']}/100"
    )

    print("\nATS Breakdown:")

    for category, points in ats_result["breakdown"].items():
        print(f"- {category}: {points}")

    print_gemini_analysis(
    resume,
    match_score,
    analysis
    )

# -----------------------------------------
# Start resume chatbot
# -----------------------------------------

    chat_context = build_chat_context(
    resume=resume,
    ats_result=ats_result,
    job=job,
    job_analysis=job_analysis,
    comparison=comparison,
    match_score=match_score,
    career_recommendations=career_recommendations,
    gemini_analysis=analysis
)

    try:
        run_chatbot(
            chat_context
        )

    except Exception as error:
        raise RuntimeError(
            f"Resume chatbot failed: {error}"
        ) from error

    return {
    "resume": resume,
    "job": job,
    "job_analysis": job_analysis,
    "comparison": comparison,
    "match_score": match_score,
    "career_recommendations": career_recommendations,
    "gemini_analysis": analysis,
    "chat_context": chat_context
}

def run_api_pipeline(pdf_path, job_description=None, job_data=None):
    """
    Non-interactive AI pipeline for ASP.NET integration.

    Unlike run_pipeline(), this function:
    - does not search for jobs
    - does not ask for user input
    - does not start the chatbot
    - returns structured analysis data
    """

    # -----------------------------------------
    # Parse resume
    # -----------------------------------------

    try:
        blocks = get_pdf_blocks(pdf_path)
        blocks = sort_blocks_visually(blocks)
        sections = extract_sections(blocks)
        resume = build_structured_resume(sections, blocks)

    except FileNotFoundError as error:
        raise RuntimeError(
            f"Resume PDF was not found: {pdf_path}"
        ) from error

    except Exception as error:
        raise RuntimeError(
            f"Resume processing failed: {error}"
        ) from error

    # -----------------------------------------
    # ATS score
    # -----------------------------------------

    ats_result = calculate_ats_score(resume)

    # -----------------------------------------
    # Initialize job analysis
    # -----------------------------------------

    job_analysis = None
    comparison = None
    keyword_score = 0
    semantic_score = 0
    match_score = 0

    # -----------------------------------------
    # Analyze supplied job description
    # -----------------------------------------

    if job_description and job_description.strip():

        try:
            job_analysis = analyze_job(
                job_description.strip(),
                job_data=job_data
            )

        except Exception as error:
            raise RuntimeError(
                f"Job analysis failed: {error}"
            ) from error

        # -----------------------------------------
        # Compare resume with job
        # -----------------------------------------

        comparison = compare_skills(
            resume["skills"],
            job_analysis
        )

        keyword_score = calculate_match_score(comparison)

        semantic_similarity = calculate_semantic_similarity(
            resume,
            job_analysis
        )

        semantic_score = round(
            semantic_similarity * 100
        )

        match_score = round(
            (keyword_score * 0.60)
            + (semantic_score * 0.40)
        )

        # Build structured job-specific ATS fields from the existing
        # comparison result. These are only populated when a job is
        # supplied; a resume-only ATS analysis keeps them null.
        matched_required = comparison.get("matched_required", []) or []
        matched_preferred = comparison.get("matched_preferred", []) or []
        matched_job_skills = comparison.get("matched_job_skills", []) or []
        missing_required = comparison.get("missing_required", []) or []
        missing_preferred = comparison.get("missing_preferred", []) or []
        missing_job_skills = comparison.get("missing_job_skills", []) or []

        keywords_identified = list(dict.fromkeys(
            matched_required + matched_preferred + matched_job_skills
        ))

        missing_keywords = list(dict.fromkeys(
            missing_required + missing_preferred + missing_job_skills
        ))

        missing_skills = list(dict.fromkeys(
            missing_required + missing_preferred
        ))

        issues = []
        if missing_required:
            issues.append(
                "Missing required skills: "
                + ", ".join(str(skill) for skill in missing_required)
            )
        if missing_preferred:
            issues.append(
                "Missing preferred skills: "
                + ", ".join(str(skill) for skill in missing_preferred)
            )
        if missing_job_skills:
            issues.append(
                "Missing job-specific skills: "
                + ", ".join(str(skill) for skill in missing_job_skills)
            )

        recommendations = [
            f"Develop {skill} to improve alignment with the selected job."
            for skill in missing_skills
        ]

        ats_result.update({
            "keywords_identified": keywords_identified,
            "missing_keywords": missing_keywords,
            "missing_skills": missing_skills,
            "issues": issues,
            "recommendations": recommendations
        })

    # -----------------------------------------
    # Career recommendations
    # -----------------------------------------

    career_recommendations = recommend_roles(
        resume
    )

    # -----------------------------------------
    # Gemini context
    # -----------------------------------------

    if job_description and job_description.strip():

        context = build_candidate_context(
            resume,
            job_data,
            job_analysis,
            comparison,
            keyword_score,
            semantic_score,
            match_score,
            career_recommendations
        )

    else:

        context = {
            "candidate": {
                "name": resume.get(
                    "name",
                    "Unknown"
                )
            },

            "resume": {
                "skills": resume.get("skills", []),
                "projects": resume.get("projects", []),
                "experience": resume.get("experience", []),
                "education": resume.get("education", []),
                "certifications": resume.get(
                    "certifications",
                    []
                )
            },

            "ats": ats_result,

            "career_recommendations": [
                {
                    "role": item["role"],
                    "score": item["score"],
                    "missing": item["missing"]
                }
                for item in career_recommendations[:5]
            ]
        }

    # -----------------------------------------
    # Gemini analysis
    # -----------------------------------------

    analysis = analyze_with_gemini(
        context
    )

    # -----------------------------------------
    # Return structured result
    # -----------------------------------------

    return {
        "resume": resume,
        "ats_result": ats_result,
        "job": job_data,
        "job_analysis": job_analysis,
        "comparison": comparison,
        "keyword_score": keyword_score,
        "semantic_score": semantic_score,
        "match_score": match_score,
        "career_recommendations": career_recommendations,
        "gemini_analysis": analysis
    }

if __name__ == "__main__":
    base_dir = Path(__file__).resolve().parent

    run_pipeline(
        pdf_path=base_dir / "resumes" / "sample.pdf",
        job_mode="search",
        job_name="Java Backend Developer",
        location="Bangalore"
    )