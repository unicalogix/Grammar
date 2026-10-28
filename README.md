# EN–UR Grammar Practice

English–Urdu grammar practice for school kids and self-learners. Teachers author content; students practice with exercises.

**Stack:** Angular 19 · ASP.NET Core 8 Web API · SQL Server · EF Core · JWT identity

This repo ships **Phase 0–2a**: foundations, content/admin (CRUD + CSV/Excel import + MT stub), and **student practice** (MCQ, arrange words, free-type with normalization).

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) and npm
- Docker (recommended) **or** SQL Server / SQL Express / LocalDB with a reachable instance

---

## Ports (dev)

| Service        | URL / port                          |
|----------------|-------------------------------------|
| Angular UI     | http://127.0.0.1:4317               |
| ASP.NET API    | http://127.0.0.1:5147               |
| SQL Server     | `127.0.0.1,14333` (Docker mapping)  |
| Swagger (dev)  | http://127.0.0.1:5147/swagger       |

---

## 1. Start SQL Server

```bash
docker compose up -d sql
```

Default SA password for this compose file (local only): `Your_strong_Password123`. Change it before any shared/prod use.

**Without Docker:** create a database and set `ConnectionStrings__DefaultConnection` (or user-secrets) to your LocalDB / SQL Express instance, for example:

```text
Server=(localdb)\MSSQLLocalDB;Database=EnUrGrammar;Trusted_Connection=True;TrustServerCertificate=True
```

---

## 2. Configure API secrets (required)

Do **not** commit real secrets. Use user-secrets (recommended on your machine) or environment variables:

```bash
cd src/EnUrGrammar.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,14333;Database=EnUrGrammar;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set "Jwt:Key" "DEV_ONLY_CHANGE_ME_32CHARS_MIN_KEY!!"
```

Or export (see `.env.example`):

```bash
export ConnectionStrings__DefaultConnection='Server=127.0.0.1,14333;Database=EnUrGrammar;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False'
export Jwt__Key='DEV_ONLY_CHANGE_ME_32CHARS_MIN_KEY!!'
```

---

## 3. Run the API

```bash
./scripts/dev-api.sh
# or: cd src/EnUrGrammar.Api && ASPNETCORE_URLS=http://127.0.0.1:5147 dotnet run --no-launch-profile
```

On startup the API:

1. Applies EF Core migrations (including `PracticeSessions` / `Attempts`)
2. Seeds roles `Admin`, `Teacher`, `Student`
3. Seeds sample admin, student, Grade 5, `PAST_SIMPLE` rule, and approved practice sentences (if empty)

### Default seed credentials

| Field    | Value                   | Role    |
|----------|-------------------------|---------|
| Email    | `admin@enur.local`      | Admin   |
| Password | `ChangeMe!Admin123`     |         |
| Email    | `student@enur.local`    | Student |
| Password | `ChangeMe!Student123`   |         |

**Change these passwords before any non-local use.**

Smoke test:

```bash
# login as student
TOKEN=$(curl -s -X POST http://127.0.0.1:5147/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"student@enur.local","password":"ChangeMe!Student123"}' | jq -r .accessToken)

# practice catalog
curl -s http://127.0.0.1:5147/api/practice/catalog -H "Authorization: Bearer $TOKEN"
```

---

## 4. Run Angular

```bash
cd src/web
npm install
npm start
# or: ./scripts/dev-web.sh
```

Open [http://127.0.0.1:4317](http://127.0.0.1:4317). Dev traffic to `/api` is proxied to the API on port `5147`.

### Try student practice (Phase 2a)

1. Sign in as **student@enur.local** / `ChangeMe!Student123` (or admin/teacher to preview)
2. Open **Practice** (home)
3. Select grade(s), optional rules, difficulty, prompt language (EN or UR)
4. **Start practice** — cycle through Multi-choice, Arrange words, and Free type
5. After each attempt, see correct/incorrect and the correct answer
6. Sessions and attempts are stored for later progress (Phase 4)

### Try bulk import (Phase 1)

1. Sign in as admin at http://127.0.0.1:4317/login
2. Open **Import**
3. Choose entity (Grades / Rules / Sentences) → **Download CSV template**
4. Keep or edit the example row (sentences need existing `GradeCode` like `G5` and `RuleCodes` like `PAST_SIMPLE`)
5. Upload → review validation errors → **Confirm commit** (defaults to Draft unless Status=Approved)

---

## Solution layout

```text
EnUrGrammar.sln
docker-compose.yml
src/
  EnUrGrammar.Api/         # ASP.NET Core Web API, EF Core, Identity, JWT
  EnUrGrammar.Contracts/   # Shared request/response DTOs
  web/                     # Angular 19 UI
scripts/
  dev-api.sh
  dev-web.sh
```

---

## Deferred

- Fill-in-the-blanks, arrange with distractors, “which rules?” multi-select, speech — Phase 2b
- Progress dashboards / weak-area coaching / anti-repeat selection — Phase 4
- Real MT provider (stub only; never auto-publishes)
- Bulk export / round-trip catalog edit
- Password reset, lockout, MFA, refresh tokens, audit, rate limits, locked CORS — Phase 3
- Multi-org / class hierarchy; teacher scoped to own content
- Production hosting / CI deploy

See project docs for the full v1 checklist and SQL schema.
