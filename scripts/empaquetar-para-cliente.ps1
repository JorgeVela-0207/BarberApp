# Publicar MSIX + certificado listos para entregar al cliente
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$ClienteDir = Join-Path $Root "dist\cliente"
$SoporteFile = Join-Path $Root "docs\SOPORTE.txt"

Write-Host "== Publicar MSIX ==" -ForegroundColor Cyan
& (Join-Path $Root "scripts\publish-windows.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path $ClienteDir) { Remove-Item $ClienteDir -Recurse -Force }
New-Item -ItemType Directory -Path $ClienteDir | Out-Null

$msix = Get-ChildItem (Join-Path $Root "dist\windows") -Filter "*.msix" -ErrorAction SilentlyContinue | Select-Object -First 1
$cer = Join-Path $Root "certs\BarberApp-MSIX.cer"

if ($msix) { Copy-Item $msix.FullName $ClienteDir }
if (Test-Path $cer) { Copy-Item $cer $ClienteDir }

# Guia con datos de soporte desde docs/SOPORTE.txt
$guia = Get-Content (Join-Path $Root "docs\GUIA_CLIENTE.md") -Raw -Encoding UTF8
$wa = "+52 (completar en docs/SOPORTE.txt)"
$mail = "(completar en docs/SOPORTE.txt)"
$horario = "Lun-Sab 9:00-18:00"
if (Test-Path $SoporteFile) {
    Get-Content $SoporteFile | ForEach-Object {
        if ($_ -match '^WhatsApp=(.+)$') { $wa = $matches[1].Trim() }
        if ($_ -match '^Correo=(.+)$') { $mail = $matches[1].Trim(); if ($mail -eq '') { $mail = "(completar en docs/SOPORTE.txt)" } }
        if ($_ -match '^Horario=(.+)$') { $horario = $matches[1].Trim() }
    }
}
$guia = $guia -replace '\| \*\*WhatsApp\*\* \| \+52 __ ___ ____ ____ \|', "| **WhatsApp** | $wa |"
$guia = $guia -replace '\| \*\*Correo\*\* \| soporte@_____________\.com \|', "| **Correo** | $mail |"
$guia = $guia -replace '\| \*\*Horario\*\* \| Lun–Sáb 9:00 – 18:00 \|', "| **Horario** | $horario |"
$guia = $guia -replace '> \*\(El proveedor del software debe completar.*\)', ''
Set-Content (Join-Path $ClienteDir "GUIA_CLIENTE.md") $guia.TrimEnd() -Encoding UTF8

Copy-Item (Join-Path $Root "scripts\install-msix-cert-cliente.ps1") $ClienteDir
Copy-Item (Join-Path $Root "docs\INSTALAR_EN_CLIENTE.md") $ClienteDir
Copy-Item (Join-Path $Root "docs\INSTALACION_PC_CLIENTE.md") $ClienteDir

@'
========================================
  BarberApp v1.0 — INSTALACION (Windows)
========================================

PASO 1 — Certificado (Administrador)
  PowerShell como admin en esta carpeta:
  .\install-msix-cert-cliente.ps1

PASO 2 — Instalar
  Doble clic: BarberApp_1.0.0.0_x64.msix

PASO 3 — Licencia
  Copiar Device ID -> enviar a soporte -> pegar TOKEN -> Activar

Ver GUIA_CLIENTE.md para uso diario.
'@ | Set-Content (Join-Path $ClienteDir "LEEME_INSTALAR.txt") -Encoding UTF8

Write-Host ""
Write-Host "Kit cliente listo en:" -ForegroundColor Green
Write-Host "  $ClienteDir"
Write-Host ""
if ($wa -match 'completar') {
    Write-Host "AVISO: Edita docs/SOPORTE.txt con tu WhatsApp y correo, luego vuelve a ejecutar este script." -ForegroundColor Yellow
}
Write-Host "Contenido:"
Get-ChildItem $ClienteDir | ForEach-Object { Write-Host "  - $($_.Name)" }
