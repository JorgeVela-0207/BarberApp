# Genera un token y bloque JSON para agregar a licencias.json
param(
    [string]$IdNegocio = "LOCAL001",
    [string]$NombreLocal = "Mi Barbería",
    [string]$Dueno = "Nombre Dueño",
    [string]$DeviceId = "PEGAR_DEVICE_ID",
    [int]$MesesValidez = 12
)

$random = -join ((65..90) + (48..57) | Get-Random -Count 6 | ForEach-Object { [char]$_ })
$token = "BARBER-$(Get-Date -Format yyyy)-$IdNegocio-$random"
$activacion = Get-Date -Format "yyyy-MM-dd"
$vencimiento = (Get-Date).AddMonths($MesesValidez).ToString("yyyy-MM-dd")

$entry = [ordered]@{
    id_negocio = $IdNegocio
    nombre_local = $NombreLocal
    dueno = $Dueno
    dispositivo_id = $DeviceId
    token = $token
    estado = "ACTIVO"
    fecha_activacion = $activacion
    fecha_vencimiento = $vencimiento
}

Write-Host ""
Write-Host "Token para el cliente: $token" -ForegroundColor Green
Write-Host ""
Write-Host "Pega esto en licencias.json (dentro del array [ ]):"
Write-Host ""
$entry | ConvertTo-Json | Write-Host -ForegroundColor Cyan
