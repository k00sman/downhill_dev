#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$repo    = git rev-parse --show-toplevel
$lintDir = Join-Path $repo 'tools/lint'
$proj    = Join-Path $lintDir 'Downhill.Lint.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "'dotnet' not found. Install the .NET SDK 10.x."; exit 1
}

# Harvest the Unity engine managed dir from any generated csproj.
$csproj = Get-ChildItem -Path $repo -Filter '*.csproj' -File | Select-Object -First 1
if (-not $csproj) {
    Write-Error "No Unity-generated .csproj found at repo root. Open the project in Unity once."; exit 1
}
$m = Select-String -Path $csproj.FullName -Pattern '<HintPath>([^<]*UnityEngine/UnityEngine\.dll)</HintPath>' |
     Select-Object -First 1
if (-not $m) { Write-Error "Could not find UnityEngine.dll HintPath in $($csproj.Name)."; exit 1 }
$hint = $m.Matches[0].Groups[1].Value
$managedDir = Split-Path -Parent $hint
if (-not (Test-Path (Join-Path $repo 'Library/ScriptAssemblies'))) {
    Write-Error "Library/ScriptAssemblies missing. Let Unity compile the project first."; exit 1
}

@"
<Project>
  <PropertyGroup>
    <UnityManagedDir>$managedDir</UnityManagedDir>
  </PropertyGroup>
</Project>
"@ | Set-Content -Path (Join-Path $lintDir 'Local.props') -Encoding UTF8

$check = ($args.Count -ge 1 -and $args[0] -eq '--check')

# Phase 1 auto-fix = whitespace + a SCOPED allowlist of safe style fixes.
# `dotnet format style` is run only for these diagnostic IDs — never the whole
# rule set — because some style code-fixes DELETE source (e.g. the IDE0051
# "unused member" fix that once stripped real WIP methods). The allowlist below
# contains only non-destructive fixes. IDE0032 (auto-property) and the UNT*
# Unity perf rules are deliberately excluded; they are reported by Phase 2.
$fixDiags = @('IDE0008','IDE0011','IDE0018','IDE0022','IDE0040','IDE0048','IDE0056','IDE0090')
if ($check) {
    Write-Host "==> Phase 1: format (verify only)"
    dotnet format style $proj --diagnostics $fixDiags --verify-no-changes
    dotnet format whitespace $proj --verify-no-changes
} else {
    Write-Host "==> Phase 1: format (auto-fix)"
    # Some fixes are interdependent — e.g. IDE0008 (explicit type) must apply
    # before IDE0090 ('new()') can on `var x = new T()` — so a single pass does
    # not converge. Iterate style+whitespace until stable (bounded).
    for ($i = 0; $i -lt 5; $i++) {
        dotnet format style $proj --diagnostics $fixDiags
        dotnet format whitespace $proj
        dotnet format style $proj --diagnostics $fixDiags --verify-no-changes 2>$null | Out-Null
        $styleOk = ($LASTEXITCODE -eq 0)
        dotnet format whitespace $proj --verify-no-changes 2>$null | Out-Null
        $wsOk = ($LASTEXITCODE -eq 0)
        if ($styleOk -and $wsOk) { break }
    }
}

Write-Host "==> Phase 2: quality build (report-only; error-severity blocks)"
dotnet build $proj -clp:NoSummary -v quiet

Write-Host "lint: OK"
