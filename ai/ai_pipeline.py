from pathlib import Path

from gemini_analyzer import run_pipeline


def main():
    print("\n================================")
    print("       AI RESUME PLATFORM")
    print("================================")

    print("\nEnter resume PDF path.")

    pdf_path = input("PDF path: ").strip()

    if not pdf_path:
        print("\nPDF path cannot be empty.")
        return

    pdf_path = Path(pdf_path)

    if not pdf_path.exists():
        print("\nPDF file was not found.")
        return

    if pdf_path.suffix.lower() != ".pdf":
        print("\nPlease provide a PDF file.")
        return

    print("\nEnter the job you want to analyze.")

    job_name = input("Job name: ").strip()
    location = input("Location: ").strip()

    if not job_name:
        print("\nJob name cannot be empty.")
        return

    if not location:
        print("\nLocation cannot be empty.")
        return

    run_pipeline(
        pdf_path=pdf_path,
        job_mode="search",
        job_name=job_name,
        location=location
    )


if __name__ == "__main__":
    main()