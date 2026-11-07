# One-time per-clone Git setup for the Unity project in game/.
# Registers UnityYAMLMerge as the merge driver used by .gitattributes (merge=unityyamlmerge)
# and makes sure Git LFS hooks are installed. Run from anywhere inside the repo:
#   powershell -ExecutionPolicy Bypass -File tools/setup-git.ps1 [-EditorVersion 6000.6.7f1]
param(
    [string]$EditorVersion = "6000.6.7f1"
)
$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel).Trim()
if (-not $repoRoot) { throw "Not inside a git repository." }

$candidates = @(
    "C:\Program Files\Unity\Hub\Editor\$EditorVersion\Editor\Data\Tools\UnityYAMLMerge.exe",
    "$env:ProgramFiles\Unity\Hub\Editor\$EditorVersion\Editor\Data\Tools\UnityYAMLMerge.exe"
)
$merge = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $merge) { throw "UnityYAMLMerge.exe not found for Unity $EditorVersion. Install that editor via Unity Hub or pass -EditorVersion." }
$mergeFwd = $merge.Replace('\', '/')

git -C $repoRoot config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git -C $repoRoot config merge.unityyamlmerge.driver "'$mergeFwd' merge -h -p --force %O %B %A %A"
git -C $repoRoot config merge.unityyamlmerge.recursive binary

git lfs version | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Git LFS is not installed. Install it from https://git-lfs.com and re-run." }
git -C $repoRoot lfs install --local | Out-Null

Write-Host "Configured merge.unityyamlmerge.driver -> $mergeFwd"
Write-Host "Git LFS hooks installed for $repoRoot"
