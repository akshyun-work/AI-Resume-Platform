# Member 7 — Resume Analysis Platform Backend

**Owner:** Member 7 — ASP.NET Core Backend Developer  
**Stack:** C# / ASP.NET Core 8 Web API / EF Core / SQL Server / JWT / Swagger / BCrypt

This is **Member 7's part** of the AI Resume Platform. It provides the central server-side foundation for auth, candidate, resume, ATS, jobs, applications, matching and chat. AI/LLM/matching algorithms are owned by other members and integrate via clean contracts.

### What is implemented
- **Auth:** `POST /api/auth/register` + `POST /api/auth/login` (BCrypt, JWT `sub` = CandidateId, 60m expiry)
- **Candidate:** `GET /api/candidates/me`, `PUT /api/candidates/me` (JWT ownership)
- **Resume:** `POST /api/resumes` (PDF 5MB, `%PDF` magic, safe `Guid.pdf`, versioning `IsLatest`), `GET /api/resumes`, `GET /api/resumes/latest`, `GET /api/resumes/{id}`, `GET /api/resumes/{id}/download`, `DELETE /api/resumes/{id}`
- **ATS:** `POST /api/ats`, `GET /api/ats`, `GET /api/ats/{id}`, `GET /api/ats/resume/{id}`, `GET /api/ats/resume/{id}/history` (stores OverallScore/CategorySkills/etc.)
- **Jobs:** `POST /api/jobs` (auth), `PUT /api/jobs/{id}`, `DELETE /api/jobs/{id}`, `GET /api/jobs`/`GET /api/jobs/{id}` (AllowAnonymous, search `?search=&location=&isActive=&skills=&page=&pageSize=`)
- **Applications:** `POST /api/applications` (duplicate 409, inactive 409), `GET /api/applications`, `GET /api/applications/{id}` (ownership)
- **Matching:** `POST /api/matches`, `GET /api/matches`, `GET /api/matches/{id}`, `GET /api/matches/job/{jobId}`, `GET /api/matches/resume/{resumeId}` (no algorithm, persistence only)
- **Chat:** `POST /api/chat/sessions`, `GET /api/chat/sessions`, `GET /api/chat/sessions/{id}`, `DELETE /api/chat/sessions/{id}`, `POST /api/chat/sessions/{id}/messages`, `GET /api/chat/sessions/{id}/messages` (placeholder `IChatCompletionProvider`)

All candidate-owned resources enforce server-side ownership via `User.GetCandidateId()` (`sub`/`NameIdentifier`), never trust client IDs. Centralized error handling (`ApiErrorResponse` with `TraceId`), consistent `ApiResponse<T>` (see `DTOs/Common/ApiResponse.cs`).

### How it integrates
- **Frontend** → `http://localhost:5150/swagger` — copy `Bearer {token}` from `POST /api/auth/login` for all owned routes.
- **Other members' AI services** → `POST /api/ats` (after resume parse), `POST /api/matches` (after matching), `POST /api/chat/sessions/{id}/messages` with `Sender=Assistant` (LLM reply).
- **DB:** `ResumeAnalysisDb` (SQL Server). Migrations: `20260825084736_InitialCreate` + `20260825091803_Phase4_MatchingChat`.

### Exact commands to run (verified `build 0w/0e`, `test 18/18` Release)
```bash
# 1. Restore & build
dotnet restore
dotnet build -c Release

# 2. Test (InMemory, no DB needed)
dotnet test -c Release

# 3. Configure (never commit secrets — use env vars / User Secrets)
# PowerShell:
$env:ConnectionStrings__DefaultConnection="Server=(localdb)\mssqllocaldb;Database=ResumeAnalysisDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
$env:Jwt__Key="YOUR_32+_CHAR_SECRET_FOR_HS256_1234567890"
$env:Jwt__Issuer="ResumeAnalysis.Api"
$env:Jwt__Audience="ResumeAnalysis.Client"
# or: dotnet user-secrets set "Jwt:Key" "YOUR_SECRET"

# 4. Apply migrations (requires SQL Server / LocalDB / docker mssql)
# docker: docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
dotnet ef database update --project backend/src/ResumeAnalysis.Api

# 5. Run
dotnet run --project backend/src/ResumeAnalysis.Api --urls http://localhost:5150
# Verify:
curl http://localhost:5150/health # {"status":"ok"}
curl http://localhost:5150/swagger # or /swagger/v1/swagger.json
```

### Structure
```
backend/
  src/ResumeAnalysis.Api/  (Controllers, Services, DTOs, Entities, Data, Middleware, Configuration, Storage)
  tests/ResumeAnalysis.Tests/ (Auth, CandidateResume, AtsJobsApplications, MatchChat, Hardening — 18 tests)
```

### Branching
This backend was developed on branch `member7-backend` — **do not commit directly to `master`/`main`** as requested. Merge via PR.

### Limitations in this runner
- `Debug` build blocked by App Control — verified via `Release` only.
- `localdb` unavailable here — `dotnet ef database update` needs Docker/LocalDB; integration tests use `UseInMemoryDatabase` and prove model.

### Contacts
- Member 7 — Backend: auth/candidate/resume/ATS/jobs/apps/matching/chat, DTOs, EF, validation, Swagger, tests.
- Other members own: resume parsing, LLM reasoning, facial recognition, job-matching algorithms (see `MEMBER_7_ROLE.md`).
