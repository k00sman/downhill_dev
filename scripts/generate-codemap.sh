#!/usr/bin/env bash
set -euo pipefail
REPO=$(git rev-parse --show-toplevel)
dotnet run --project "$REPO/tools/codemap/codemap.csproj" -- "$REPO"
echo "Code map written to docs/codemap.md"
