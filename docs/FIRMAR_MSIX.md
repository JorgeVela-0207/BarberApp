# Firmar MSIX — guía paso a paso

Instalar BarberApp **sin activar Modo desarrollador** en Windows requiere un MSIX **firmado** y que el cliente **confíe en tu certificado**.

---

## Parte A — En tu PC (desarrollo) — una sola vez

### Paso 1: Crear el certificado

1. Abre **PowerShell** (no hace falta administrador).
2. Ve a la carpeta del proyecto:

```powershell
cd C:\Users\jv869\source\repos\BarberApp
```

3. Ejecuta:

```powershell
.\scripts\create-msix-cert.ps1
```

Verás algo como:

- **Thumbprint** del certificado  
- Archivo `certs\BarberApp-MSIX.cer`  
- Archivo `BarberApp\BarberApp.Signing.props` (activa la firma automáticamente)

> Si Windows bloquea scripts:  
> `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

### Paso 2: Publicar MSIX firmado

```powershell
.\scripts\publish-windows.ps1
```

El MSIX firmado queda en `dist\windows\`.

### Paso 3: Probar en tu PC

1. Instala el certificado en **tu** máquina (como admin):

```powershell
.\scripts\install-msix-cert-cliente.ps1
```

2. Doble clic en el `.msix` de `dist\windows\`.

Si instala sin pedir Modo desarrollador, el paquete está bien firmado para sideload.

---

## Parte B — En la PC del cliente (cada instalación nueva)

Entrega **dos archivos**:

| Archivo | Para qué |
|---------|----------|
| `BarberApp_1.0.0.0_x64.msix` | La aplicación |
| `BarberApp-MSIX.cer` | Certificado de confianza |

### Paso 1: Instalar certificado (administrador)

1. Copia `certs\BarberApp-MSIX.cer` al cliente (USB, correo, etc.).
2. En el cliente: PowerShell **como administrador**:

```powershell
cd C:\ruta\donde\copiaste\los\archivos
Import-Certificate -FilePath .\BarberApp-MSIX.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
Import-Certificate -FilePath .\BarberApp-MSIX.cer -CertStoreLocation Cert:\LocalMachine\Root
```

O usa el script `install-msix-cert-cliente.ps1` si copiaste toda la carpeta `scripts`.

### Paso 2: Instalar BarberApp

Doble clic en el `.msix` → **Instalar**.

### Paso 3: Activar licencia

Sigue `docs/GUIA_CLIENTE.md`.

---

## Qué hace el `.csproj` por dentro

El proyecto importa `BarberApp.Signing.props` (generado por el script):

```xml
<AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
<PackageCertificateThumbprint>TU_THUMBPRINT</PackageCertificateThumbprint>
```

Ese archivo **no se sube a Git** (está en `.gitignore`).

---

## Producción “seria” (más adelante)

El certificado **autofirmado** sirve para pilotos y clientes que tú instalas.

Para muchos clientes o Microsoft Store conviene un certificado comercial (DigiCert, Sectigo, etc.) con el mismo flujo de thumbprint en `BarberApp.Signing.props`.

---

## Problemas frecuentes

| Error | Solución |
|-------|----------|
| “No se puede verificar el publicador” | Instala `.cer` en TrustedPeople + Root |
| Modo desarrollador pedido | MSIX no firmado → vuelve a publicar con `Signing.props` |
| Certificado expirado | Vuelve a ejecutar `create-msix-cert.ps1` y republica |
