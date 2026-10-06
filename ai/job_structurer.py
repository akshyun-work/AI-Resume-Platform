import os
import json
import re
from pathlib import Path
from typing import Any, Dict, List, Optional
from google import genai
try:
    from dotenv import load_dotenv
    load_dotenv()
    # Search for .env in parent directories if not found
    for p in [Path.cwd(), Path(__file__).resolve().parent, Path(__file__).resolve().parent.parent, Path(__file__).resolve().parent.parent / "backend" / "src" / "ResumeAnalysis.Api"]:
        env_file = p / ".env"
        if env_file.exists():
            load_dotenv(env_file)
except ImportError:
    pass


# ============================================================
# Canonical Skill Normalization Dictionary & Alias Mapping
# ============================================================

SKILL_ALIASES = {
    # Frontend
    "reactjs": "React",
    "react.js": "React",
    "react js": "React",
    "react": "React",
    "nextjs": "Next.js",
    "next.js": "Next.js",
    "next js": "Next.js",
    "vuejs": "Vue.js",
    "vue.js": "Vue.js",
    "vue": "Vue.js",
    "angularjs": "Angular",
    "angular": "Angular",
    "typescript": "TypeScript",
    "ts": "TypeScript",
    "javascript": "JavaScript",
    "js": "JavaScript",
    "html5": "HTML5",
    "html": "HTML",
    "css3": "CSS3",
    "css": "CSS",
    "tailwind": "Tailwind CSS",
    "tailwindcss": "Tailwind CSS",
    "bootstrap": "Bootstrap",
    "redux": "Redux",

    # Backend
    "python": "Python",
    "python3": "Python",
    "java": "Java",
    "c#": "C#",
    "csharp": "C#",
    ".net": ".NET",
    ".net core": ".NET Core",
    "dotnet": ".NET",
    "asp.net": "ASP.NET Core",
    "asp.net core": "ASP.NET Core",
    "spring boot": "Spring Boot",
    "springboot": "Spring Boot",
    "nodejs": "Node.js",
    "node.js": "Node.js",
    "node": "Node.js",
    "express": "Express.js",
    "express.js": "Express.js",
    "expressjs": "Express.js",
    "fastapi": "FastAPI",
    "django": "Django",
    "flask": "Flask",
    "golang": "Go",
    "go": "Go",
    "rust": "Rust",
    "c++": "C++",
    "cpp": "C++",

    # Databases
    "sql": "SQL",
    "mysql": "MySQL",
    "postgres": "PostgreSQL",
    "postgresql": "PostgreSQL",
    "sqlite": "SQLite",
    "mongodb": "MongoDB",
    "mongo": "MongoDB",
    "redis": "Redis",
    "elasticsearch": "Elasticsearch",
    "cassandra": "Cassandra",
    "dynamodb": "DynamoDB",

    # Cloud & DevOps
    "docker": "Docker",
    "kubernetes": "Kubernetes",
    "k8s": "Kubernetes",
    "aws": "AWS",
    "amazon web services": "AWS",
    "azure": "Azure",
    "microsoft azure": "Azure",
    "gcp": "Google Cloud",
    "google cloud platform": "Google Cloud",
    "google cloud": "Google Cloud",
    "ci/cd": "CI/CD",
    "git": "Git",
    "github": "GitHub",
    "gitlab": "GitLab",
    "terraform": "Terraform",
    "linux": "Linux",

    # Architecture & AI/ML
    "rest": "REST APIs",
    "restful": "REST APIs",
    "rest api": "REST APIs",
    "rest apis": "REST APIs",
    "graphql": "GraphQL",
    "microservices": "Microservices",
    "machine learning": "Machine Learning",
    "ml": "Machine Learning",
    "deep learning": "Deep Learning",
    "artificial intelligence": "Artificial Intelligence",
    "ai": "Artificial Intelligence",
    "nlp": "NLP",
    "pytorch": "PyTorch",
    "tensorflow": "TensorFlow",
    "pandas": "Pandas",
    "numpy": "NumPy",
    "data structures": "Data Structures & Algorithms",
    "algorithms": "Data Structures & Algorithms",
    "dsa": "Data Structures & Algorithms",
    "oops": "Object-Oriented Programming (OOP)",
    "oop": "Object-Oriented Programming (OOP)",
}


def normalize_skill_name(skill: str) -> str:
    """Normalize skill name using canonical dictionary."""
    if not skill or not isinstance(skill, str):
        return ""
    clean = skill.strip()
    lower = clean.lower()
    return SKILL_ALIASES.get(lower, clean)


def deduplicate_and_normalize_skills(skills: List[str]) -> List[str]:
    """Deduplicate and normalize a list of skill names preserving casing."""
    seen = set()
    result = []
    for s in skills:
        norm = normalize_skill_name(s)
        if norm and norm.lower() not in seen:
            seen.add(norm.lower())
            result.append(norm)
    return result


def create_gemini_client():
    """Create Google Gemini Client using GEMINI_API_KEY environment variable."""
    api_key = os.getenv("GEMINI_API_KEY")
    if not api_key:
        return None
    return genai.Client(api_key=api_key)


def clean_empty_values(data: Any) -> Any:
    """
    Recursively remove None, empty strings, and empty collections.
    Returns None if the resulting structure is empty.
    """
    if isinstance(data, dict):
        cleaned = {}
        for k, v in data.items():
            cleaned_v = clean_empty_values(v)
            if cleaned_v is not None:
                cleaned[k] = cleaned_v
        return cleaned if cleaned else None
    elif isinstance(data, list):
        cleaned_list = []
        for item in data:
            cleaned_item = clean_empty_values(item)
            if cleaned_item is not None:
                cleaned_list.append(cleaned_item)
        return cleaned_list if cleaned_list else None
    elif isinstance(data, str):
        s = data.strip()
        return s if s else None
    return data


# ============================================================
# Gemini JD Structuring Prompt
# ============================================================

STRUCTURING_PROMPT = """
You are an expert Job Description Analysis & Structuring Engine.
Your task is to transform the following raw, unstructured, or flyer-style Job Description into a clean, canonical JSON object.

### STRICT RULES:
1. ONLY return a JSON object conforming to the schema below.
2. DO NOT output keys if there is no meaningful content for them. DO NOT return empty arrays [], empty strings "", or null fields.
3. Crucial Content Categorization:
   - "aboutCompany": Company mission, product domain, industry, company overview.
   - "teamAndRole": Day-to-day team context, reporting line, cross-functional collaboration.
   - "whyJoinUs": Career growth, engineering challenges, mentorship, leadership opportunities, high-impact mission (Value propositions for why a candidate should join).
   - "cultureAndMindset": Cultural traits, values, attitude, and work philosophy (e.g. "High ownership with real technical impact", "Passion for building reliable systems", "Curious and self-driven").
   - "responsibilities": Concrete, actionable day-to-day duties and technical scope.
   - "requiredSkills": Essential hard technical skills and tools (e.g. "C#", "React", "PostgreSQL", "Docker"). Keep individual items short (1-3 words).
   - "preferredSkills": Nice-to-have, bonus, or desired technical skills (e.g. "Kubernetes", "Redis", "FastAPI"). Keep individual items short.
   - "qualifications": Experience levels (e.g. "3+ years of experience"), education/degrees, certifications.
   - "benefits": Tangible compensation and perks ONLY (e.g. "Health, Dental & Vision insurance", "401(k) matching", "Remote office stipend", "Unlimited PTO"). DO NOT put career growth or culture here.
   - "howToApply": Application contact details (email, URL link, specific instructions, or deadline).
   - "additionalSections": Array of custom sections that carry useful structure but don't fit the above (e.g. "Interview Process", "Security Clearance", "Equal Opportunity Notice") with "title" and either "items" (list) or "content" (string).

### JSON SCHEMA:
{
  "aboutCompany": string,
  "teamAndRole": string,
  "whyJoinUs": [string],
  "cultureAndMindset": [string],
  "responsibilities": [string],
  "requiredSkills": [string],
  "preferredSkills": [string],
  "qualifications": [string],
  "benefits": [string],
  "howToApply": {
    "email": string,
    "url": string,
    "instructions": string,
    "deadline": string
  },
  "additionalSections": [
    {
      "title": string,
      "items": [string],
      "content": string
    }
  ]
}

### RAW JOB DESCRIPTION:
"""


def fallback_structure_jd(raw_text: str) -> Dict[str, Any]:
    """
    Heuristic rule-based fallback when Gemini API is unavailable.
    """
    lines = [line.strip() for line in raw_text.splitlines() if line.strip()]
    
    responsibilities = []
    qualifications = []
    benefits = []
    about_lines = []
    
    current_section = "about"
    for line in lines:
        lower = line.lower()
        if any(h in lower for h in ["responsibilities", "what you'll do", "key duties", "the role"]):
            current_section = "responsibilities"
            continue
        elif any(h in lower for h in ["requirements", "qualifications", "what you bring", "must have", "skills"]):
            current_section = "qualifications"
            continue
        elif any(h in lower for h in ["benefits", "perks", "what we offer", "compensation"]):
            current_section = "benefits"
            continue
            
        clean_item = re.sub(r"^[\*\-\•\d\.\)]+\s*", "", line)
        if current_section == "responsibilities":
            responsibilities.append(clean_item)
        elif current_section == "qualifications":
            qualifications.append(clean_item)
        elif current_section == "benefits":
            benefits.append(clean_item)
        else:
            about_lines.append(line)

    result: Dict[str, Any] = {}
    if about_lines:
        result["aboutCompany"] = " ".join(about_lines[:3])
    if responsibilities:
        result["responsibilities"] = responsibilities
    if qualifications:
        result["qualifications"] = qualifications
    if benefits:
        result["benefits"] = benefits

    return result


def structure_job_description(raw_jd_text: str) -> Dict[str, Any]:
    """
    Task A: Pure Job Description Structuring & Ingestion.
    Transforms raw text into Canonical Structured JSON via Gemini.
    """
    if not raw_jd_text or not raw_jd_text.strip():
        return {}

    raw_clean = raw_jd_text.strip()

    try:
        client = create_gemini_client()
        if not client:
            return fallback_structure_jd(raw_clean)

        prompt = STRUCTURING_PROMPT + "\n" + raw_clean

        response = client.models.generate_content(
            model="gemini-3.1-flash-lite",
            contents=prompt,
            config={
                "response_mime_type": "application/json"
            }
        )

        response_text = response.text.strip()
        parsed = json.loads(response_text)

        # Normalize and deduplicate skills
        if "requiredSkills" in parsed and isinstance(parsed["requiredSkills"], list):
            parsed["requiredSkills"] = deduplicate_and_normalize_skills(parsed["requiredSkills"])
            
        if "preferredSkills" in parsed and isinstance(parsed["preferredSkills"], list):
            pref_norm = deduplicate_and_normalize_skills(parsed["preferredSkills"])
            req_set = set(s.lower() for s in parsed.get("requiredSkills", []))
            # Remove any skills from preferred if they are already required
            parsed["preferredSkills"] = [s for s in pref_norm if s.lower() not in req_set]

        # Clean all empty fields
        cleaned = clean_empty_values(parsed)
        return cleaned if isinstance(cleaned, dict) else {}

    except Exception as ex:
        # Fallback cleanly on error
        fb = fallback_structure_jd(raw_clean)
        return fb


def run_structurer_api(raw_jd_text: str) -> Dict[str, Any]:
    """Entry point for .NET / Python AI integration."""
    return structure_job_description(raw_jd_text)


if __name__ == "__main__":
    sample_jd = """
    We're building AI-powered tools, mobile applications, and travel technology at NextGen Labs.
    High ownership with real technical impact. Passion for building reliable, secure, and scalable products.
    
    What You'll Do:
    - Collaborate with Product, AI, Mobile, and Growth teams.
    - Design, build, and maintain high-performance microservices using ASP.NET Core and Go.
    - Implement real-time data pipelines and semantic search integrations.
    
    Must Haves:
    - 3+ years experience with C# and .NET Core.
    - Hands-on experience with PostgreSQL, Docker, and RESTful APIs.
    - Bachelor's degree in Computer Science or equivalent practical experience.
    
    Bonus Points:
    - Familiarity with Kubernetes, Redis, and FastAPI.
    - Experience in vector databases or GenAI applications.
    
    Why Join NextGen:
    - Huge opportunity to lead distributed systems architecture from ground up.
    - Transparent culture with fast execution and direct leadership access.
    
    Perks & Benefits:
    - 100% covered health, vision, and dental insurance.
    - 401k matching up to 5% and annual learning stipend of $2,000.
    
    How to Apply:
    Please send your resume and portfolio to careers@nextgenlabs.ai with subject 'Backend Engineer'.
    """
    structured = structure_job_description(sample_jd)
    print(json.dumps(structured, indent=2))
