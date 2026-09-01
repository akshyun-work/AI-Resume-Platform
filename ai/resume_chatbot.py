import os

from google import genai
from ats_analyzer import calculate_ats_score

from pdf_parser import (
    get_pdf_blocks,
    sort_blocks_visually,
    extract_sections,
    build_structured_resume
)

from career_recommender import recommend_roles
def create_chatbot_client():
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

def run_chatbot(context):
    """
    Run an interactive chatbot session with
    conversation memory.
    """

    conversation = []

    print("\n================================")
    print("       RESUME CHATBOT")
    print("================================")

    print("\nType 'exit' to end the chatbot.")

    while True:

        question = input(
            "\nYou: "
        ).strip()

        if question.lower() in {
            "exit",
            "quit"
        }:
            print("\nChatbot session ended.")
            break

        if not question:
            continue

        answer = ask_chatbot(
            context,
            question,
            conversation
        )

        print("\nAssistant:")
        print(answer)

        conversation.append({
            "role": "user",
            "message": question
        })

        conversation.append({
            "role": "assistant",
            "message": answer
        })

def build_chatbot_prompt(
    context,
    user_question,
    conversation
):
    """
    Build the prompt for the resume chatbot,
    including previous conversation history.
    """

    conversation_text = ""

    for message in conversation:

        role = message["role"]
        content = message["message"]

        if role == "user":
            conversation_text += (
                f"\nUser: {content}"
            )

        elif role == "assistant":
            conversation_text += (
                f"\nAssistant: {content}"
            )

    return f"""
You are the AI resume assistant inside an
AI Resume Platform.

You must answer the user's question using ONLY
the candidate information provided below and
the conversation history.

Do not invent:
- skills
- experience
- projects
- qualifications
- certifications
- achievements
- job requirements

If the information required to answer the
question is not present in the context or
conversation, clearly say that the information
is not available.

Important rules:

1. Do not change or recalculate the ATS score.
2. Do not change or recalculate the job match score.
3. Clearly distinguish between existing skills
   and missing skills.
4. Do not claim that a recommended skill is
   already possessed by the candidate.
5. Give practical answers relevant to the
   candidate's resume and career situation.
6. Keep answers concise but useful.
7. Use previous conversation turns to understand
   references such as "that", "those", "it",
   "which one", and similar follow-up questions.
8. Do not treat information from previous
   conversation as new resume facts unless that
   information already exists in the candidate
   context.

Candidate Context:

{context}

Previous Conversation:

{conversation_text}

Current User Question:

{user_question}

Answer the current question directly.
"""

def ask_chatbot(
    context,
    user_question,
    conversation
):
    """
    Send the current question together with
    previous conversation history to Gemini.
    """

    client = create_chatbot_client()

    prompt = build_chatbot_prompt(
        context,
        user_question,
        conversation
    )

    response = client.models.generate_content(
        model="gemini-3.1-flash-lite",
        contents=prompt
    )

    answer_parts = []

    for part in response.candidates[0].content.parts:

        if part.text:
            answer_parts.append(part.text)

    return "".join(answer_parts)

def run_api_chatbot(
    pdf_path,
    user_question,
    conversation=None
):
    """
    API-friendly resume chatbot.

    Builds the resume context directly from the
    candidate's uploaded PDF and answers one question.
    """

    if not pdf_path:
        raise ValueError("PDF path is required.")

    if not user_question or not user_question.strip():
        raise ValueError("User question is required.")

    if conversation is None:
        conversation = []

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
    # Calculate ATS
    # -----------------------------------------

    ats_result = calculate_ats_score(
        resume
    )

    # -----------------------------------------
    # Career recommendations
    # -----------------------------------------

    career_recommendations = recommend_roles(
        resume
    )

    # -----------------------------------------
    # Build chatbot context
    # -----------------------------------------

    context = build_chat_context(
        resume=resume,
        ats_result=ats_result,
        career_recommendations=career_recommendations
    )

    # -----------------------------------------
    # Ask chatbot
    # -----------------------------------------

    answer = ask_chatbot(
        context,
        user_question.strip(),
        conversation
    )

    return {
        "answer": answer,

        "conversation": conversation + [
            {
                "role": "user",
                "message": user_question.strip()
            },
            {
                "role": "assistant",
                "message": answer
            }
        ]
    }
def build_chat_context(
    resume,
    ats_result=None,
    job=None,
    job_analysis=None,
    comparison=None,
    match_score=None,
    career_recommendations=None,
    gemini_analysis=None
):
    """
    Build the structured context available to
    the resume chatbot.

    The chatbot will use this context to answer
    questions about the candidate, resume, job,
    matching results, and career recommendations.
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
            ),

            "achievements": resume.get(
                "achievements",
                []
            )
        },

        "ats": {
            "score": (
                ats_result.get("score")
                if ats_result
                else None
            ),

            "breakdown": (
                ats_result.get("breakdown", {})
                if ats_result
                else {}
            )
        },

        "job": {
            "title": (
                job.get("title")
                if job
                else None
            ),

            "company": (
                job.get("company")
                if job
                else None
            ),

            "location": (
                job.get("location")
                if job
                else None
            ),

            "description": (
                job.get("description")
                if job
                else None
            ),

            "url": (
                job.get("url")
                if job
                else None
            )
        },

        "job_analysis": {
            "required_skills": (
                job_analysis.get(
                    "required_skills",
                    []
                )
                if job_analysis
                else []
            ),

            "preferred_skills": (
                job_analysis.get(
                    "preferred_skills",
                    []
                )
                if job_analysis
                else []
            ),

            "job_skills": (
                job_analysis.get(
                    "job_skills",
                    []
                )
                if job_analysis
                else []
            )
        },

        "job_match": {
            "score": match_score,

            "matched_required": (
                comparison.get(
                    "matched_required",
                    []
                )
                if comparison
                else []
            ),

            "missing_required": (
                comparison.get(
                    "missing_required",
                    []
                )
                if comparison
                else []
            ),

            "matched_preferred": (
                comparison.get(
                    "matched_preferred",
                    []
                )
                if comparison
                else []
            ),

            "missing_preferred": (
                comparison.get(
                    "missing_preferred",
                    []
                )
                if comparison
                else []
            ),

             "matched_job_skills": (
                comparison.get(
                    "matched_job_skills",
                    []
                )
                if comparison
                else []
            ),

            "missing_job_skills": (
                comparison.get(
                    "missing_job_skills",
                    []
                )
                if comparison
                else []
            )
        },

        "career_recommendations": (
            career_recommendations[:5]
            if career_recommendations
            else []
        ),

        "gemini_analysis": (
            gemini_analysis
            if gemini_analysis
            else None
        )
    }

if __name__ == "__main__":

    sample_resume = {
        "name": "Test Candidate",

        "skills": [
            "Python",
            "Java",
            "SQL"
        ],

        "projects": [
            "Resume Platform"
        ],

        "experience": [],

        "education": [
            "B.Tech Computer Science"
        ],

        "certifications": [],

        "achievements": []
    }

    sample_ats = {
        "score": 72,

        "breakdown": {
            "contact_information": 10,
            "resume_sections": 15,
            "technical_skills": 20,
            "projects": 10,
            "experience": 7,
            "certifications": 4,
            "achievements": 6
        }
    }

    context = build_chat_context(
        resume=sample_resume,
        ats_result=sample_ats,
        match_score=65
    )

    run_chatbot(context)