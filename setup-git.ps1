# setup-git.ps1
# Run this script once from X:\Dev\unity-prefab-pooling to initialise the
# local Git repository and push it to GitHub as jardaczk/unity-prefab-pooling.
#
# Prerequisites:
#   - Git installed and on PATH
#   - GitHub CLI (gh) installed and authenticated  OR  a personal access token
#     configured in your Git credential manager

Set-Location $PSScriptRoot

Write-Host "==> Initialising Git repository..." -ForegroundColor Cyan
git init
git checkout -b main

Write-Host "==> Staging all files..." -ForegroundColor Cyan
git add .
git commit -m "chore: initial package release v1.0.0"

Write-Host "==> Creating GitHub repository and pushing..." -ForegroundColor Cyan
# Requires GitHub CLI: https://cli.github.com/
gh repo create jardaczk/unity-prefab-pooling --public --source=. --remote=origin --push

Write-Host "==> Tagging v1.0.0..." -ForegroundColor Cyan
git tag v1.0.0
git push origin v1.0.0

Write-Host ""
Write-Host "Done! Install the package in Unity via:" -ForegroundColor Green
Write-Host "  https://github.com/jardaczk/unity-prefab-pooling.git#v1.0.0" -ForegroundColor Yellow
