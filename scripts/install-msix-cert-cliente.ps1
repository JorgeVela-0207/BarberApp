# Instala el certificado BarberApp en la PC del CLIENTE (ejecutar como Administrador).
# Uso: clic derecho PowerShell -> Ejecutar como administrador
#      cd ruta\BarberApp
#      .\scripts\install-msix-cert-cliente.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$CerPath = Join-Path $Root "certs\BarberApp-MSIX.cer"

if (-not (Test-Path $CerPath)) {
    Write-Host "No se encuentra: $CerPath" -ForegroundColor Red
    Write-Host "Copia BarberApp-MSIX.cer junto con el MSIX al cliente."
    exit 1
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Ejecuta este script como Administrador." -ForegroundColor Red
    exit 1
}

Import-Certificate -FilePath $CerPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
Import-Certificate -FilePath $CerPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null

Write-Host "Certificado instalado. Ya puedes instalar BarberApp.msix sin Modo desarrollador." -ForegroundColor Green
