import os
import requests


class AdzunaJobProvider:

    BASE_URL = "https://api.adzuna.com/v1/api"

    def __init__(self, app_id, app_key, country="in"):
        self.app_id = app_id
        self.app_key = app_key
        self.country = country

    def search_jobs(
        self,
        keywords,
        location=None,
        page=1,
        results_per_page=10
    ):
        """
        Search jobs using the Adzuna API.
        """

        url = (
            f"{self.BASE_URL}/jobs/"
            f"{self.country}/search/{page}"
        )

        params = {
            "app_id": self.app_id,
            "app_key": self.app_key,
            "results_per_page": results_per_page,
            "what": keywords,
            "content-type": "application/json"
        }

        if location:
            params["where"] = location

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
        Convert provider-specific API data into
        our own job format.
        """

        jobs = []

        for job in data.get("results", []):

            location_data = job.get("location", {})

            company_data = job.get("company", {})

            normalized_job = {
                "id": job.get("id"),
                "title": job.get("title"),
                "company": company_data.get(
                    "display_name"
                ),
                "location": location_data.get(
                    "display_name"
                ),
                "description": job.get(
                    "description"
                ),
                "url": job.get(
                    "redirect_url"
                ),
                "contract_type": job.get(
                    "contract_type"
                ),
                "contract_time": job.get(
                    "contract_time"
                ),
                "salary_min": job.get(
                    "salary_min"
                ),
                "salary_max": job.get(
                    "salary_max"
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
            f"Contract: "
            f"{job['contract_type'] or 'Not specified'}"
        )

        print(
            f"URL: "
            f"{job['url'] or 'Not available'}"
        )

        print("-" * 50)


if __name__ == "__main__":

    app_id = os.getenv("ADZUNA_APP_ID")
    app_key = os.getenv("ADZUNA_APP_KEY")

    if not app_id or not app_key:

        print("Adzuna API credentials are not configured.")

        print(
            "\nSet these environment variables:"
        )

        print("ADZUNA_APP_ID")
        print("ADZUNA_APP_KEY")

        exit()

    provider = AdzunaJobProvider(
        app_id=app_id,
        app_key=app_key
    )

    jobs = provider.search_jobs(
        keywords="Java Backend Developer",
        location="Bangalore",
        results_per_page=10
    )

    print_jobs(jobs)
