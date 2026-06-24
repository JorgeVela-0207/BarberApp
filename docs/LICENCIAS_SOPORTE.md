# BarberApp — Gestión de licencias (soporte interno)

## Archivo de licencias

Las licencias viven en un JSON (repositorio **privado** recomendado):

- URL actual: ver `BarberApp/Models/LicenciaConfig.cs`
- Para repo privado en GitHub: crea un **Personal Access Token (read)** y define la variable de entorno `BARBERAPP_LICENCIA_TOKEN` en el equipo de desarrollo (no la subas al repo).

### Ejemplo de registro

```json
{
  "id_negocio": "LOCAL001",
  "nombre_local": "Barbería El Estilo",
  "dueno": "Juan Pérez",
  "dispositivo_id": "A1B2C3D4E5F67890",
  "token": "TOKEN-UNICO-CLIENTE-001",
  "estado": "ACTIVO",
  "fecha_activacion": "2026-06-01",
  "fecha_vencimiento": "2027-06-01"
}
```

| Campo | Descripción |
|-------|-------------|
| `dispositivo_id` | Device ID que muestra la app (16 caracteres hex) |
| `token` | Clave que entregas al cliente para activar |
| `estado` | `ACTIVO` o suspendido |
| `fecha_vencimiento` | Tras esta fecha la app bloquea el acceso |

---

## Alta de cliente nuevo

1. Cliente instala BarberApp y te envía el **Device ID**.
2. Genera un **token** único (ej. `BARBER-2026-0042`).
3. Agrega el registro al JSON con `dispositivo_id`, fechas y `estado: ACTIVO`.
4. Sube el JSON (commit al repo privado o actualiza tu API).
5. Entrega el **token** al cliente por canal seguro (WhatsApp, correo).

---

## Renovación

1. Actualiza `fecha_vencimiento` en el JSON del cliente.
2. Cliente pulsa **Verificar pago / renovación** o entra a Ajustes → **Verificar / Renovar licencia**.

---

## Migración (cambio de PC)

1. Cliente instala en el **nuevo** PC y copia el **nuevo Device ID**.
2. En el JSON, cambia `dispositivo_id` al nuevo valor (mismo `token`).
3. Cliente activa de nuevo con el **mismo token** en la pantalla de licencia.

> El token sigue igual; solo cambia el `dispositivo_id` en el servidor.

---

## Suspender servicio

Cambia `estado` a `SUSPENDIDO` o vence la fecha. La app mostrará pantalla de bloqueo.

---

## Seguridad

- **No uses repo público** con tokens en texto plano.
- Rota tokens si hay filtración.
- Un token = un negocio / instalación activa.
