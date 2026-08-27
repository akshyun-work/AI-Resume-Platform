import os
from google import genai


api_key = os.getenv("GEMINI_API_KEY")

if not api_key:
    print("Gemini API key is missing.")
    exit()

client = genai.Client(
    api_key=api_key
)

response = client.models.generate_content(
    model="gemini-3.6-flash",
    contents="Reply with exactly: Gemini connection successful."
)

print("\n===== GEMINI TEST =====")
print(response.text)