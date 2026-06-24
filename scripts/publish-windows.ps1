# Publicar BarberApp para Windows (Release)
# Ejecutar desde PowerShell: .\scripts\publish-windows.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "BarberApp\BarberApp.csproj"
$Dist = Join-Path $Root "dist\windows"
$Framework = "net9.0-windows10.0.19041.0"

Write-Host "== Tests ==" -ForegroundColor Cyan
dotnet test (Join-Path $Root "BarberApp.Tests\BarberApp.Tests.csproj") -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== Publish Release (MSIX) ==" -ForegroundColor Cyan
$signingProps = Join-Path $Root "BarberApp\BarberApp.Signing.props"
if (Test-Path $signingProps) {
    Write-Host "Firma MSIX: ACTIVADA (BarberApp.Signing.props)" -ForegroundColor Green
} else {
    Write-Host "Firma MSIX: desactivada. Ejecuta scripts\create-msix-cert.ps1 primero." -ForegroundColor Yellow
}

dotnet publish $Project -c Release -f $Framework
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path $Dist) { Remove-Item $Dist -Recurse -Force }
New-Item -ItemType Directory -Path $Dist | Out-Null

$releaseRoot = Join-Path $Root "BarberApp\bin\Release\$Framework\win10-x64"
$msix = Get-ChildItem -Path $releaseRoot -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($msix) {
    Copy-Item $msix.FullName (Join-Path $Dist $msix.Name)
    Write-Host "MSIX copiado: $(Join-Path $Dist $msix.Name)" -ForegroundColor Green
}

$publishDir = Join-Path $releaseRoot "publish"
if (Test-Path $publishDir) {
    Copy-Item $publishDir (Join-Path $Dist "BarberApp-portable") -Recurse
    Write-Host "Portable: $(Join-Path $Dist 'BarberApp-portable')" -ForegroundColor Green
}

Write-Host ""
Write-Host "Entrega al cliente:" -ForegroundColor Green
Write-Host "  1. MSIX en dist\windows\ (instalar con doble clic)"
Write-Host "  2. O carpeta BarberApp-portable con BarberApp.exe"
Write-Host ""
Write-Host "MSIX sin firma: puede requerir 'Modo desarrollador' en Windows."
Write-Host "Cierra Visual Studio y BarberApp antes de instalar."
