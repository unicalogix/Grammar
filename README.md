# EN–UR Grammar Practice

English–Urdu grammar practice for school kids and self-learners. Teachers author content; students practice with exercises.

**Stack:** Angular 19 · ASP.NET Core 8 Web API · SQL Server · EF Core · JWT identity

This repo currently ships **Phase 0 foundations** plus **Phase 1 content/admin**: grades/rules/sentences CRUD, Grade↔Rule and Sentence↔Rule links, CSV/Excel import (templates → validate → commit), and an MT suggestion stub that never auto-publishes. Practice exercises come later.

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

1. Applies EF Core migrations
2. Seeds roles `Admin`, `Teacher`, `Student`
3. Seeds sample admin, Grade 5, and `PAST_SIMPLE` rule (if empty)

### Default seed credentials

| Field    | Value                 |
|----------|-----------------------|
| Email    | `admin@enur.local`    |
| Password | `ChangeMe!Admin123`   |
| Role     | Admin                 |

**Change this password before any non-local use.**

Smoke test:

```bash
# login
TOKEN=$(curl -s -X POST http://127.0.0.1:5147/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@enur.local","password":"ChangeMe!Admin123"}' | jq -r .accessToken)

# protected ping
curl -s http://127.0.0.1:5147/api/ping -H "Authorization: Bearer $TOKEN"
```

---

## 4. Run Angular

```bash
cd src/web
npm install
npm start
# or: ./scripts/dev-web.sh
```

Open [http://127.0.0.1:4317](http://127.0.0.1:4317). Sign in with the seed admin. Dev traffic to `/api` is proxied to the API on port `5147`.

### Try bulk import (Phase 1)

1. Sign in as admin at http://127.0.0.1:4317/login
2. Open **Import**
3. Choose entity (Grades / Rules / Sentences) → **Download CSV template**
4. Keep or edit the example row (sentences need existing `GradeCode` like `G5` and `RuleCodes` like `PAST_SIMPLE`)
5. Upload → review validation errors → **Confirm commit** (defaults to Draft unless Status=Approved)
6. Or via curl:

```bash
TOKEN=$(curl -s -X POST http://127.0.0.1:5147/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@enur.local","password":"ChangeMe!Admin123"}' | jq -r .accessToken)

curl -s -OJ -H "Authorization: Bearer $TOKEN" \
  'http://127.0.0.1:5147/api/import/templates/sentences?format=csv'

curl -s -H "Authorization: Bearer $TOKEN" \
  -F entityKind=sentences -F file=@enur-sentences-template.csv \
  http://127.0.0.1:5147/api/import/batches

# then POST /api/import/batches/{id}/commit when status=Validated
```

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

- Practice exercises (MCQ, arrange, free-type) — Phase 2a
- Real MT provider (stub only; never auto-publishes)
- Bulk export / round-trip catalog edit
- Password reset, lockout, MFA, refresh tokens, audit, rate limits, locked CORS
- Multi-org / class hierarchy; teacher scoped to own content
- Production hosting / CI deploy

See project docs for the full v1 checklist and SQL schema.
