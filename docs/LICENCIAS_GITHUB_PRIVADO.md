# Licencias en GitHub privado — paso a paso

Objetivo: que **solo BarberApp** (con tu token) lea `licencias.json`, sin exponer tokens al público.

---

## Parte 1 — Crear el repositorio privado

1. Entra a [github.com](https://github.com) → **New repository**.
2. Nombre: `BDLicencias` (o el que prefieras).
3. Marca **Private**.
4. Crea el repo **sin** README (vacío).

---

## Parte 2 — Subir el JSON de licencias

1. En tu PC, copia la plantilla:

```
docs\licencias.ejemplo.json  →  licencias.json
```

2. Edita `licencias.json` con datos reales. Ejemplo:

```json
[
  {
    "id_negocio": "LOCAL001",
    "nombre_local": "Barbería El Estilo",
    "dueno": "Juan Pérez",
    "dispositivo_id": "A1B2C3D4E5F67890",
    "token": "BARBER-2026-0042",
    "estado": "ACTIVO",
    "fecha_activacion": "2026-06-01",
    "fecha_vencimiento": "2027-06-01"
  }
]
```

3. Sube el archivo a la rama **main** del repo privado (GitHub web: **Add file → Upload files**).

La URL que usará la app será:

```
https://raw.githubusercontent.com/TU_USUARIO/BDLicencias/main/licencias.json
```

---

## Parte 3 — Crear token de acceso (PAT)

1. GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**.
2. **Generate new token (classic)**.
3. Nombre: `BarberApp-licencias-read`.
4. Expiración: 90 días o 1 año (renueva antes de que venza).
5. Permiso mínimo: **`repo`** (solo lectura del repo privado; classic no tiene granular read-only fácil — usa fine-grained si prefieres).
6. Genera y **copia el token** (`ghp_...`). No lo compartas con clientes.

---

## Parte 4 — Configurar la app

### 4a. URL en el código

Edita `BarberApp/Models/LicenciaConfig.cs`:

```csharp
public const string Url =
    "https://raw.githubusercontent.com/TU_USUARIO/BDLicencias/main/licencias.json";
```

Reemplaza `TU_USUARIO` por tu usuario real.

### 4b. Token en tu PC de desarrollo (variable de entorno)

**PowerShell (sesión actual):**

```powershell
[Environment]::SetEnvironmentVariable("BARBERAPP_LICENCIA_TOKEN", "ghp_TU_TOKEN_AQUI", "User")
```

Cierra y abre Visual Studio / terminal para que tome el valor.

> La app **no** lleva el token embebido: solo tú (desarrollador) lo usas para probar.  
> Los **clientes** no necesitan el token: ellos solo usan su **token de licencia** de negocio (`BARBER-2026-0042`).

### 4c. Token en builds de Release (opcional)

Si compilas en CI o quieres fijarlo al publicar:

```powershell
$env:BARBERAPP_LICENCIA_TOKEN = "ghp_..."
.\scripts\publish-windows.ps1
```

**Importante:** el token PAT va en **tu máquina o CI**, no en el MSIX que entregas al cliente. La app en el cliente llama a GitHub raw; para repo **privado** el cliente también necesitaría token embebido — **eso no es viable**.

---

## ⚠️ Repo privado + app en cliente: solución correcta

GitHub privado **no funciona** bien si cada cliente descarga el JSON sin credenciales.

Opciones:

| Opción | Cuándo usarla |
|--------|----------------|
| **A. Repo público** solo con `dispositivo_id` + tokens (sin datos sensibles extra) | Piloto rápido |
| **B. Repo privado + proxy/API tuya** (Azure Function, VPS) que valida y devuelve licencias | Producción seria |
| **C. Repo privado + PAT embebido en la app** | ❌ No recomendado (cualquiera extrae el PAT del MSIX) |

### Recomendación para piloto (ahora)

1. Repo **privado** para **tú** editar licencias con seguridad.
2. Publica el JSON en un **endpoint público mínimo** (GitHub Pages, Cloudflare Worker, o repo público **solo** con hashes/tokens, sin datos personales).
3. O mantén repo público `BDLicencias` pero **sin** poner teléfonos/correos — solo ids técnicos.

### Si insistes en privado total hoy

Crea una **Azure Function** (gratis tier) con URL:

```
GET https://tu-api.azurewebsites.net/api/licencias
Header: x-api-key: TU_CLAVE_SECRETA
```

Y cambia `LicenciaConfig.Url` a esa URL. (Sync nube pendiente — misma idea.)

---

## Parte 5 — Flujo operativo diario

Ver también `docs/LICENCIAS_SOPORTE.md`.

1. Cliente te manda **Device ID**.
2. Agregas fila en `licencias.json` → commit → push al repo.
3. Cliente activa con su **token de negocio**.
4. **Renovar:** cambias `fecha_vencimiento` → push.
5. **Cambio de PC:** cambias `dispositivo_id` al nuevo → push.

---

## Parte 6 — Probar que funciona

1. Pon un registro de prueba con tu Device ID actual (pantalla Licencia).
2. Define `BARBERAPP_LICENCIA_TOKEN` si el repo es privado **y** estás probando desde VS en tu PC.
3. Activa con el token de prueba.
4. Si falla “Sin conexión” → revisa URL y PAT.

---

## Checklist rápido

- [ ] Repo privado creado  
- [ ] `licencias.json` subido  
- [ ] PAT creado (solo en tu PC, variable de entorno)  
- [ ] `LicenciaConfig.Url` actualizada  
- [ ] Decidido: repo público mínimo **o** API intermedia para clientes finales  
- [ ] Prueba de activación en máquina limpia  
