# Prepara el repo Git y sube SOLO lo permitido (.gitignore).
# Uso:
#   .\scripts\subir-a-github.ps1
#   .\scripts\subir-a-github.ps1 -Commit "mensaje"
#   .\scripts\subir-a-github.ps1 -Push -RemoteUrl "https://github.com/JorgeVela-0207/BarberApp.git"
#   .\scripts\subir-a-github.ps1 -Commit "mensaje" -Push -RemoteUrl "https://github.com/JorgeVela-0207/BarberApp.git"

param(
    [string]$Commit = "",
    [switch]$Push,
    [string]$RemoteUrl = "https://github.com/JorgeVela-0207/BarberApp.git"
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
    if ($LASTEXITCODE -ne 0) { throw "Git no esta instalado." }
}

function Init-RepoIfNeeded {
    if (-not (Test-Path (Join-Path $Root ".git"))) {
        Write-Host "Inicializando Git..." -ForegroundColor Cyan
        git init | Out-Null
        git branch -M main 2>$null
    }
}

function Test-ForbiddenStaged {
    param([string[]]$Paths)
    $bad = @()
    foreach ($p in $Paths) {
        foreach ($rule in $ProhibidosEnStage) {
            $norm = $p -replace '/', '\'
            $match = if ($rule.Exact) { $norm -eq ($rule.Path -replace '/', '\') }
                     else { $norm -like "*$($rule.Path)*" }
            if ($match) { $bad += $p; break }
        }
    }
    return $bad
}

function Invoke-GitPush {
    param([string]$Url)
    if ([string]::IsNullOrWhiteSpace($Url)) {
        Write-Host "Indica -RemoteUrl https://github.com/JorgeVela-0207/BarberApp.git" -ForegroundColor Yellow
        exit 1
    }
    $remotes = @(git remote 2>$null)
    if ($remotes -contains "origin") { git remote set-url origin $Url }
    else { git remote add origin $Url }

    Write-Host "Subiendo a $Url ..." -ForegroundColor Cyan
    git push -u origin main
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Listo: https://github.com/JorgeVela-0207/BarberApp" -ForegroundColor Green
    } else {
        Write-Host "Si dice 'Repository not found', crea el repo vacio en GitHub primero." -ForegroundColor Yellow
        exit $LASTEXITCODE
    }
}

Write-Host "== BarberApp -> GitHub ==" -ForegroundColor Cyan
Test-Git
Init-RepoIfNeeded

git add -A
$staged = @(git diff --cached --name-only 2>$null)

if ($staged.Count -gt 0) {
    $forbidden = Test-ForbiddenStaged $staged
    if ($forbidden.Count -gt 0) {
        Write-Host "ERROR - archivos prohibidos:" -ForegroundColor Red
        $forbidden | ForEach-Object { Write-Host "  $_" }
        git reset
        exit 1
    }
    Write-Host "Archivos a commitear ($($staged.Count)):" -ForegroundColor Green
    $staged | Select-Object -First 15 | ForEach-Object { Write-Host "  + $_" }
    if ($staged.Count -gt 15) { Write-Host "  ... y $($staged.Count - 15) mas" }
}

if ($staged.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($Commit)) {
    git commit -m $Commit
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Commit creado." -ForegroundColor Green
} elseif ($staged.Count -eq 0 -and -not [string]::IsNullOrWhiteSpace($Commit)) {
    Write-Host "Nada nuevo que commitear (ya esta guardado)." -ForegroundColor Yellow
}

if ($Push) {
    Invoke-GitPush $RemoteUrl
} elseif ($staged.Count -eq 0) {
    Write-Host "Working tree clean." -ForegroundColor DarkGray
    git status -sb
    Write-Host ""
    Write-Host "Para subir a GitHub (crea repo BarberApp vacio antes):" -ForegroundColor Yellow
    Write-Host '  .\scripts\subir-a-github.ps1 -Push'
} else {
    Write-Host ""
    Write-Host "Para commit:" -ForegroundColor Yellow
    Write-Host '  .\scripts\subir-a-github.ps1 -Commit "BarberApp v1.0" -Push'
}
