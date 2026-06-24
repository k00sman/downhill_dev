#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
$repo = git rev-parse --show-toplevel
dotnet run --project "$repo/tools/codemap/codemap.csproj" -- $repo
Write-Host "Code map written to docs/codemap.md"
