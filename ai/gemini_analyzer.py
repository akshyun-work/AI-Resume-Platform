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


def build_prompt(context):
    """
    Build a structured prompt for the job-match explanation.
    """

    return f"""
You are an AI career advisor inside a resume analysis platform.

Analyze ONLY the candidate and job information provided below.

The Python system has already calculated:
- the job match score
- matched required skills
- missing required skills
- matched preferred skills
- missing preferred skills
- matched job-specific skills
- missing job-specific skills

DO NOT change, recalculate, or invent these values.

Candidate analysis data:

{context}

Your task is ONLY to generate the "Why This Match" explanation.

Return ONLY valid JSON.
Do not use Markdown.
Do not use ```json.
Do not add any text before or after the JSON.

Use EXACTLY this structure:

{{
  "match_summary": "A concise explanation of how well the candidate matches this role.",

  "why_you_match": [
    "skill or capability the candidate already has"
  ],

  "what_is_missing": {{
    "required": [
      "missing required skill"
    ],
    "preferred": [
      "missing preferred skill"
    ],
    "job_specific": [
      "missing job-specific skill"
    ]
  }},

  "score_explanation": {{
    "score": 0,
    "explanation": "A concise explanation of why the calculated score is at this level.",
    "factors": [
      "factor affecting the score"
    ]
  }},

  "improvement_actions": [
    "First prioritized improvement action",
    "Second prioritized improvement action",
    "Third prioritized improvement action"
  ]
}}

Rules:

1. "why_you_match" must contain ONLY skills/capabilities that are present in the provided matched data.
2. "what_is_missing.required" must contain ONLY missing_required skills.
3. "what_is_missing.preferred" must contain ONLY missing_preferred skills.
4. "what_is_missing.job_specific" must contain ONLY missing_job_skills.
5. Do not move a skill from one category to another.
6. "score" must exactly equal the Python-calculated job match score.
7. "factors" must explain the actual missing skills or other provided factors affecting the score.
8. Do not claim the candidate has a missing skill.
9. Do not invent AWS, Microservices, CI/CD, OOP, REST APIs, or any other skill unless it exists in the supplied job analysis.
10. "improvement_actions" should contain at most 3 practical actions based on the missing skills.
11. If a category has no missing skills, return an empty array.
12. Keep the explanation concise and professional.
"""


def analyze_with_gemini(context):
    """
    Send the structured analysis to Gemini and return
    validated JSON as a string.
    """

    try:
        client = create_gemini_client()

        prompt = build_prompt(context)

        response = client.models.generate_content(
            model="gemini-3.1-flash-lite",
            contents=prompt
        )

        if not response.text:
            raise RuntimeError(
                "Gemini returned an empty response."
            )

        raw_text = response.text.strip()

        # Remove accidental Markdown fences if Gemini adds them.
        if raw_text.startswith("```json"):
            raw_text = raw_text[7:]

        if raw_text.startswith("```"):
            raw_text = raw_text[3:]

        if raw_text.endswith("```"):
            raw_text = raw_text[:-3]

        raw_text = raw_text.strip()

        # Validate that Gemini actually returned JSON.
        parsed = json.loads(raw_text)

        # Return normalized JSON so ASP.NET receives predictable data.
        return json.dumps(
            parsed,
            ensure_ascii=False
        )

    except json.JSONDecodeError as error:
        raise RuntimeError(
        f"Gemini returned invalid JSON: {error}"
        ) from error

    except ValueError:
        raise

    except Exception as error:
        raise RuntimeError(
        f"Gemini analysis failed: {error}"
    ) from error

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