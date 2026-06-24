# Genera certificado válido para firmar MSIX de BarberApp.
# Ejecutar UNA VEZ:  .\scripts\create-msix-cert.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$CertDir = Join-Path $Root "certs"
$CerPath = Join-Path $CertDir "BarberApp-MSIX.cer"
$PropsPath = Join-Path $Root "BarberApp\BarberApp.Signing.props"
$Publisher = "CN=Jorge Vela BarberApp"

New-Item -ItemType Directory -Path $CertDir -Force | Out-Null

Write-Host "Eliminando certificados BarberApp anteriores..." -ForegroundColor Cyan
Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $Publisher -or $_.FriendlyName -eq "BarberApp MSIX Signing" } |
    ForEach-Object { Remove-Item $_.PSPath -Force }

Write-Host "Creando certificado de firma de código..." -ForegroundColor Cyan
$cert = New-SelfSignedCertificate `
    -Subject $Publisher `
    -Type CodeSigningCert `
    -KeyUsage DigitalSignature `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -KeyExportPolicy Exportable `
    -FriendlyName "BarberApp MSIX Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5)

Export-Certificate -Cert $cert -FilePath $CerPath -Force | Out-Null

$props = @"
<Project>
  <PropertyGroup>
    <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
    <PackageCertificateThumbprint>$($cert.Thumbprint)</PackageCertificateThumbprint>
  </PropertyGroup>
</Project>
"@

Set-Content -Path $PropsPath -Value $props -Encoding UTF8

Write-Host ""
Write-Host "Listo." -ForegroundColor Green
Write-Host "  Publisher  : $Publisher (debe coincidir con Package.appxmanifest)"
Write-Host "  Thumbprint : $($cert.Thumbprint)"
Write-Host "  Cert (.cer): $CerPath"
Write-Host "  Props      : $PropsPath"
Write-Host ""
Write-Host "Siguiente: .\scripts\publish-windows.ps1"
