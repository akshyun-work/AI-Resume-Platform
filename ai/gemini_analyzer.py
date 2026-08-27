import os

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
    Build the instruction sent to Gemini.
    """

    return f"""
You are an AI career advisor inside a resume
analysis platform.

You must analyze ONLY the information provided
below.

Do not invent:
- skills
- experience
- projects
- qualifications
- achievements
- certifications

The Python system has already calculated the
job match score and skill matches. Do not change
those scores.

Candidate analysis data:

{context}

Provide the following:

1. Resume strengths
2. Resume weaknesses
3. Explanation of the current job match
4. Most important missing skills
5. Three prioritized improvement actions
6. Career direction based on the available
   evidence

Keep the response practical and concise.

Clearly distinguish between:
- skills the candidate already has
- skills that are missing
- skills that should be learned

Do not claim that learning a skill means the
candidate already possesses it.
"""


def analyze_with_gemini(context):
    """
    Send the structured analysis to Gemini.
    """

    try:
        client = create_gemini_client()

        prompt = build_prompt(context)

        response = client.models.generate_content(
            model="gemini-3.6-flash",
            contents=prompt
        )

        if not response.text:
            raise RuntimeError(
                "Gemini returned an empty response."
            )

        return response.text

    except ValueError:
        # Preserve configuration errors such as
        # missing GEMINI_API_KEY.
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