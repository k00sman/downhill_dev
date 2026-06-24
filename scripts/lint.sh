#!/usr/bin/env bash
set -euo pipefail

REPO=$(git rev-parse --show-toplevel)
LINT_DIR="$REPO/tools/lint"
PROJ="$LINT_DIR/Downhill.Lint.csproj"

command -v dotnet >/dev/null 2>&1 || {
  echo "error: 'dotnet' not found. Install the .NET SDK 10.x." >&2; exit 1; }

# --- Harvest the one machine-specific path: the Unity engine managed dir. ---
CSPROJ=$(ls "$REPO"/*.csproj 2>/dev/null | head -1 || true)
if [ -z "${CSPROJ:-}" ]; then
  echo "error: no Unity-generated .csproj found at repo root." >&2
  echo "       Open the project in Unity once to generate them." >&2
  exit 1
fi
HINT=$(grep -m1 -oE '<HintPath>[^<]*UnityEngine/UnityEngine\.dll</HintPath>' "$CSPROJ" \
        | sed -E 's#</?HintPath>##g' || true)
if [ -z "${HINT:-}" ]; then
  echo "error: could not find the UnityEngine.dll HintPath in $CSPROJ." >&2
  exit 1
fi
MANAGED_DIR=$(dirname "$HINT")
if [ ! -d "$REPO/Library/ScriptAssemblies" ]; then
  echo "error: Library/ScriptAssemblies missing. Let Unity compile the project first." >&2
  exit 1
fi

printf '<Project>\n  <PropertyGroup>\n    <UnityManagedDir>%s</UnityManagedDir>\n  </PropertyGroup>\n</Project>\n' \
  "$MANAGED_DIR" > "$LINT_DIR/Local.props"

CHECK=0
[ "${1:-}" = "--check" ] && CHECK=1

# Phase 1 auto-fix = whitespace + a SCOPED allowlist of safe style fixes.
# `dotnet format style` is run only for these diagnostic IDs — never the whole
# rule set — because some style code-fixes DELETE source (e.g. the IDE0051
# "unused member" fix that once stripped real WIP methods). The allowlist below
# contains only non-destructive fixes. IDE0032 (auto-property) and the UNT*
# Unity perf rules are deliberately excluded; they are reported by Phase 2.
FIX_DIAGS="IDE0008 IDE0011 IDE0018 IDE0022 IDE0040 IDE0048 IDE0056 IDE0090"
if [ "$CHECK" = "1" ]; then
  echo "==> Phase 1: format (verify only)"
  dotnet format style "$PROJ" --diagnostics $FIX_DIAGS --verify-no-changes
  dotnet format whitespace "$PROJ" --verify-no-changes
else
  echo "==> Phase 1: format (auto-fix)"
  # Some fixes are interdependent — e.g. IDE0008 (explicit type) must apply
  # before IDE0090 ('new()') can on `var x = new T()` — so a single pass does
  # not converge. Iterate style+whitespace until stable (bounded).
  for _ in 1 2 3 4 5; do
    dotnet format style "$PROJ" --diagnostics $FIX_DIAGS
    dotnet format whitespace "$PROJ"
    if dotnet format style "$PROJ" --diagnostics $FIX_DIAGS --verify-no-changes >/dev/null 2>&1 \
       && dotnet format whitespace "$PROJ" --verify-no-changes >/dev/null 2>&1; then
      break
    fi
  done
fi

echo "==> Phase 2: quality build (report-only; error-severity blocks)"
dotnet build "$PROJ" -clp:NoSummary -v quiet

echo "lint: OK"
