import requests


class FreeHireJobProvider:

    BASE_URL = "https://freehire.me/api/v1"

    def __init__(self, country="IN"):
        self.country = country

    def search_jobs(
        self,
        keywords,
        location=None,
        page=1,
        results_per_page=10
    ):
        """
        Search jobs using the FreeHire API.

        FreeHire provides public job-search endpoints
        without requiring an API key.
        """

        url = f"{self.BASE_URL}/agent/jobs/search"

        offset = (page - 1) * results_per_page

        params = {
            "q": keywords,
            "countries": self.country,
            "limit": min(results_per_page, 100),
            "offset": offset,
            "include_description": "true",
            "description_format": "markdown"
        }

        if location and location.lower() != "india":
            params["cities"] = location

        response = requests.get(
            url,
            params=params,
            timeout=15
        )

        response.raise_for_status()

        data = response.json()

        return self.normalize_jobs(data)

    def normalize_jobs(self, data):
        """
        Convert FreeHire's provider-specific response
        into our application's standard job format.
        """

        jobs = []

        for job in data.get("data", []):

            normalized_job = {
                "id": job.get("public_slug"),

                "title": job.get("title"),

                "company": job.get("company"),

                "location": job.get("location"),

                "description": job.get("description"),

                "url": job.get("url"),

                "contract_type": job.get(
                    "employment_type"
                ),

                "contract_time": None,

                "salary_min": job.get(
                    "salary_min"
                ),

                "salary_max": job.get(
                    "salary_max"
                ),

                "salary_currency": job.get(
                    "salary_currency"
                ),

                "skills": job.get(
                    "skills",
                    []
                ),

                "category": job.get(
                    "category"
                ),

                "source": job.get(
                    "source"
                ),

                "requirements": job.get(
                    "requirements",
                    []
                )
            }

            jobs.append(normalized_job)

        return jobs


def print_jobs(jobs):

    print("\n================================")
    print("          JOB SEARCH")
    print("================================")

    if not jobs:
        print("\nNo jobs found.")
        return

    for index, job in enumerate(jobs, start=1):

        print(f"\n[{index}] {job['title']}")

        print(
            f"Company: "
            f"{job['company'] or 'Not specified'}"
        )

        print(
            f"Location: "
            f"{job['location'] or 'Not specified'}"
        )

        print(
            f"Employment: "
            f"{job['contract_type'] or 'Not specified'}"
        )

        print(
            f"URL: "
            f"{job['url'] or 'Not available'}"
        )

        if job["skills"]:
            print(
                f"Skills: "
                f"{', '.join(job['skills'])}"
            )

        print("-" * 50)


if __name__ == "__main__":

    provider = FreeHireJobProvider()

    jobs = provider.search_jobs(
        keywords="Java Backend Developer",
        location="Bangalore",
        results_per_page=10
    )

    print_jobs(jobs)