# Licencias — decisión y configuración (piloto)

## Decisión recomendada para ti (sucursal única, 1–20 clientes)

| Opción | Veredicto |
|--------|-----------|
| **Repo público `BDLicencias`** con JSON mínimo | ✅ **Usar ahora** |
| Repo privado + PAT en la app | ❌ Inseguro (el token se extrae del MSIX) |
| API privada (Azure/Cloudflare) | ⏳ Cuando tengas 20+ clientes |

### Por qué repo público mínimo

- La app del cliente **no lleva contraseñas** de GitHub.
- Cada licencia lleva **token largo + Device ID**; sin el equipo del cliente el token no sirve en otro PC (validación por dispositivo).
- **No pongas** teléfonos, correos ni datos personales en el JSON — solo nombre del local y ids técnicos.

Cuando crezcas, migras a una API propia sin cambiar la app (solo cambias `LicenciaConfig.Url`).

---

## Estructura del repo en GitHub

**Nombre:** `BDLicencias`  
**Visibilidad:** **Public** (piloto) o Private si luego usas API  
**Archivo único en raíz:** `licencias.json`

### Contenido mínimo seguro

```json
[
  {
    "id_negocio": "LOCAL001",
    "nombre_local": "Barbería El Estilo",
    "dueno": "Juan Pérez",
    "dispositivo_id": "A1B2C3D4E5F67890",
    "token": "BARBER-2026-0001-X7K9M2",
    "estado": "ACTIVO",
    "fecha_activacion": "2026-06-19",
    "fecha_vencimiento": "2027-06-19"
  }
]
```

Copia la plantilla desde: `docs/licencias.plantilla.json`

---

## Pasos en GitHub (primera vez)

### 1. Crear repositorio

1. [github.com/new](https://github.com/new)
2. Nombre: **BDLicencias**
3. **Public** → Create repository

### 2. Subir licencias.json

1. **Add file → Upload files**
2. Sube `licencias.json` (editado con tu primer cliente)
3. Commit to **main**

### 3. Verificar URL

Debe quedar accesible en el navegador:

```
https://raw.githubusercontent.com/TU_USUARIO/BDLicencias/main/licencias.json
```

(Sustituye `TU_USUARIO` por tu usuario GitHub, ej. `JorgeVela-0207`.)

### 4. Actualizar la app (una vez)

En `BarberApp/Models/LicenciaConfig.cs`:

```csharp
public const string Url =
    "https://raw.githubusercontent.com/JorgeVela-0207/BDLicencias/main/licencias.json";
```

Recompila y publica MSIX.

---

## Alta de un cliente nuevo (cada venta)

1. Cliente instala BarberApp → te envía **Device ID** (16 caracteres).
2. Generas token único: `BARBER-2026-XXXX-` + 6 caracteres aleatorios.
3. Agregas objeto al array en `licencias.json`.
4. **Commit + push** en GitHub (tarda ~1 min en propagarse).
5. Envías **solo el token** al cliente por WhatsApp.
6. Cliente activa en la app.

### Renovar

Cambia `fecha_vencimiento` → push. Cliente: **Ajustes → Verificar / Renovar licencia**.

### Cambio de PC

Cliente envía **nuevo Device ID** → cambias `dispositivo_id` en su registro → push → mismo token.

---

## Si más adelante quieres repo privado

Necesitas **API intermedia** (no PAT en la app). Opciones:

- Azure Function (~gratis tier)
- Cloudflare Worker
- VPS con endpoint `GET /licencias`

La app ya envía petición GET a `LicenciaConfig.Url`; solo cambias la URL.

---

## Checklist licencias

- [ ] Repo `BDLicencias` creado en GitHub  
- [ ] `licencias.json` subido con al menos 1 cliente de prueba  
- [ ] URL verificada en navegador  
- [ ] `LicenciaConfig.cs` con tu URL real  
- [ ] MSIX recompilado y probado activación  

Guía operativa diaria: `docs/LICENCIAS_SOPORTE.md`
