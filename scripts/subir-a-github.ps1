# Prepara el repo Git y sube SOLO lo permitido (.gitignore).
# Uso:
#   .\scripts\subir-a-github.ps1                    # revisar qué se subiria
#   .\scripts\subir-a-github.ps1 -Commit "mensaje"  # commit local
#   .\scripts\subir-a-github.ps1 -Commit "mensaje" -Push  # commit + push

param(
    [string]$Commit = "",
    [switch]$Push,
    [string]$RemoteUrl = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$ProhibidosEnStage = @(
    @{ Path = "BarberApp\BarberApp.Signing.props"; Exact = $true },
    @{ Path = "\certs\"; Exact = $false },
    @{ Path = "\dist\"; Exact = $false },
    @{ Path = "\bin\"; Exact = $false },
    @{ Path = "\obj\"; Exact = $false },
    @{ Path = "\.vs\"; Exact = $false },
    @{ Path = ".env"; Exact = $false }
)

function Test-Git {
    git --version | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Git no esta instalado. Instala Git for Windows." }
}

function Ensure-GitIgnore {
    $gi = Join-Path $Root ".gitignore"
    if (-not (Test-Path $gi)) {
        throw "Falta .gitignore en la raiz del proyecto."
    }
}

function Init-RepoIfNeeded {
    if (-not (Test-Path (Join-Path $Root ".git"))) {
        Write-Host "Inicializando repositorio Git..." -ForegroundColor Cyan
        git init | Out-Null
        git branch -M main 2>$null
    }
}

function Get-StagedPaths {
    git diff --cached --name-only 2>$null
}

function Test-ForbiddenStaged {
    $staged = Get-StagedPaths
    $bad = @()
    foreach ($p in $staged) {
        foreach ($rule in $ProhibidosEnStage) {
            $match = if ($rule.Exact) {
                ($p -replace '/', '\') -eq ($rule.Path -replace '/', '\')
            } else {
                ($p -replace '/', '\') -like "*$($rule.Path)*"
            }
            if ($match) { $bad += $p; break }
        }
    }
    return $bad
}

Write-Host "== BarberApp - preparar subida a GitHub ==" -ForegroundColor Cyan
Write-Host "Carpeta: $Root`n"

Test-Git
Ensure-GitIgnore
Init-RepoIfNeeded

# Respetar .gitignore: solo agrega lo permitido
git add -A

$staged = Get-StagedPaths
if (-not $staged) {
    Write-Host "No hay cambios para subir (todo ignorado o ya commiteado)." -ForegroundColor Yellow
    git status
    exit 0
}

$forbidden = Test-ForbiddenStaged
if ($forbidden.Count -gt 0) {
    Write-Host "ERROR: Archivos prohibidos en staging (revisa .gitignore):" -ForegroundColor Red
    $forbidden | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    git reset
    exit 1
}

Write-Host "Archivos que SE SUBIRIAN ($($staged.Count)):" -ForegroundColor Green
$staged | ForEach-Object { Write-Host "  + $_" }

Write-Host ""
Write-Host "Ignorados automaticamente (ejemplos):" -ForegroundColor DarkGray
@("bin/", "obj/", "dist/", "certs/", ".vs/", "BarberApp.Signing.props") | ForEach-Object {
    Write-Host "  - $_" -ForegroundColor DarkGray
}

if ([string]::IsNullOrWhiteSpace($Commit)) {
    Write-Host ""
    Write-Host "Revisión lista. Para commit:" -ForegroundColor Yellow
    Write-Host '  .\scripts\subir-a-github.ps1 -Commit "BarberApp v1.0 piloto"'
    Write-Host ""
    Write-Host "Para commit + push (despues de crear repo en GitHub):" -ForegroundColor Yellow
    Write-Host '  .\scripts\subir-a-github.ps1 -Commit "BarberApp v1.0" -Push -RemoteUrl "https://github.com/TU_USUARIO/BarberApp.git"'
    git status -s
    exit 0
}

git commit -m $Commit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Commit creado." -ForegroundColor Green

if (-not $Push) { exit 0 }

$remotes = git remote 2>$null
if (-not $remotes -and [string]::IsNullOrWhiteSpace($RemoteUrl)) {
    Write-Host "Falta remote. Crea repo en GitHub y ejecuta:" -ForegroundColor Yellow
    Write-Host '  git remote add origin https://github.com/TU_USUARIO/BarberApp.git'
    Write-Host '  .\scripts\subir-a-github.ps1 -Commit "..." -Push -RemoteUrl "https://github.com/TU_USUARIO/BarberApp.git"'
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($RemoteUrl)) {
    if ($remotes -contains "origin") {
        git remote set-url origin $RemoteUrl
    } else {
        git remote add origin $RemoteUrl
    }
}

Write-Host "Subiendo a origin/main..." -ForegroundColor Cyan
git push -u origin main
if ($LASTEXITCODE -eq 0) {
    Write-Host "Listo. Codigo en GitHub." -ForegroundColor Green
}
