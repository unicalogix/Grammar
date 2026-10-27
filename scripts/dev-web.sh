#!/usr/bin/env bash
# Run Angular on http://127.0.0.1:4317
set -euo pipefail
export NG_CLI_ANALYTICS=false
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export PATH="${HOME}/.npm-global/bin:${PATH}"
cd "$ROOT/src/web"
exec npx ng serve --host 127.0.0.1 --port 4317 --proxy-config proxy.conf.json
