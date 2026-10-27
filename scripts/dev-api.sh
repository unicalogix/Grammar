#!/usr/bin/env bash
# Run the API on http://127.0.0.1:5147 with local Docker SQL defaults (override via env).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://127.0.0.1:5147}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Server=127.0.0.1,14333;Database=EnUrGrammar;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False}"
export Jwt__Key="${Jwt__Key:-DEV_ONLY_CHANGE_ME_32CHARS_MIN_KEY!!}"
export Cors__AngularOrigin="${Cors__AngularOrigin:-http://127.0.0.1:4317}"
cd "$ROOT/src/EnUrGrammar.Api"
exec dotnet run --no-launch-profile
