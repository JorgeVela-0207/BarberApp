# BarberApp — Instalación en PC del cliente

**Versión:** 1.0.0  
**Plataforma:** Windows 10 o superior  
**Uso:** Piloto, prueba y operación diaria del local  

---

## A. Antes de ir al cliente (tú — soporte)

### 1. Preparar el kit en USB

En tu PC de desarrollo, ejecuta:

```powershell
cd C:\Users\jv869\source\repos\BarberApp
.\scripts\empaquetar-para-cliente.ps1
```

Copia **toda** la carpeta `dist\cliente\` a un USB. Debe contener:

| Archivo | Descripción |
|---------|-------------|
| `BarberApp_1.0.0.0_x64.msix` | Instalador de la aplicación |
| `BarberApp-MSIX.cer` | Certificado de confianza |
| `install-msix-cert-cliente.ps1` | Script para instalar el certificado |
| `LEEME_INSTALAR.txt` | Resumen rápido |
| `GUIA_CLIENTE.md` | Manual de uso diario |
| `INSTALACION_PC_CLIENTE.md` | Este documento |

### 2. Crear la licencia del cliente en GitHub

1. En la PC del cliente (después de instalar) copiarán el **Device ID**, o puedes instalar tú primero y copiarlo.
2. Generar token y JSON:

```powershell
.\scripts\nueva-licencia.ps1 `
  -IdNegocio "BARBER-002" `
  -NombreLocal "Nombre del local" `
  -Dueno "Nombre del dueño" `
  -DeviceId "DEVICE_ID_16_CARACTERES"
```

3. Agregar el bloque JSON a:  
   `https://github.com/JorgeVela-0207/BDLicencias/blob/main/licencias.json`
4. Guardar (commit) y esperar ~1 minuto.
5. Anotar el **token** para entregárselo al cliente (WhatsApp).

### 3. Llevar al cliente

- [ ] USB con carpeta `cliente`
- [ ] Token de licencia anotado
- [ ] Este documento impreso o en PDF (opcional)

---

## B. Instalación en la PC del cliente (15–20 min)

> Requiere **Windows 10** (1809+) o **Windows 11**.  
> Cuenta con permisos de **Administrador** en el PC.

### Paso 1 — Copiar archivos

1. Inserta el USB.
2. Copia la carpeta `cliente` al escritorio, por ejemplo:  
   `C:\Users\Public\Desktop\BarberApp`

### Paso 2 — Instalar certificado (obligatorio)

1. Clic derecho en **Inicio** → **Terminal (Administrador)** o **PowerShell (Administrador)**.
2. Ejecuta:

```powershell
cd C:\Users\Public\Desktop\BarberApp
Set-ExecutionPolicy -Scope Process Bypass -Force
.\install-msix-cert-cliente.ps1
```

3. Debe aparecer: **"Certificado instalado..."**
4. Cierra la ventana de administrador.

**Si no haces este paso**, Windows puede bloquear la instalación o pedir Modo desarrollador.

### Paso 3 — Instalar BarberApp

1. Doble clic en **`BarberApp_1.0.0.0_x64.msix`**
2. Clic en **Instalar**
3. Al terminar, abre **BarberApp** desde el menú Inicio de Windows.

### Paso 4 — Activar licencia

1. Se abre la pantalla **Activación de licencia**.
2. Copia el **Device ID** (16 caracteres) y envíalo a soporte si aún no está en GitHub.
3. Soporte confirma el **token** (ej. `TOKEN-2026-X9Y2`).
4. Pega el token → **Activar licencia** (necesitas **internet**).
5. Si es correcto, pasa a crear admin o login.

| Mensaje | Qué hacer |
|---------|-----------|
| Sin conexión | Conectar Wi‑Fi o cable |
| Token no válido | Revisar token con soporte |
| Equipo no registrado | Enviar Device ID a soporte para actualizar GitHub |

### Paso 5 — Crear cuenta administrador (solo primera vez)

1. **Nombre del negocio**
2. **Nombre del admin**
3. **Usuario** (sin espacios)
4. **Contraseña** + confirmación
5. Clic en crear → irá al **Login**

### Paso 6 — Primer acceso

1. Inicia sesión con usuario y contraseña del admin.
2. Verás el **Dashboard**.

---

## C. Configuración inicial del local (30 min)

Hazlo con el dueño o encargado del local.

### 1. Datos del negocio

**Dashboard → ⚙ Ajustes → Mi negocio**

- Nombre del local  
- Dueño / responsable  
- Teléfono  
- Horario apertura / cierre  
- **Guardar datos**

### 2. Catálogos (obligatorio antes de citas)

**Dashboard → Catálogos**

| Pestaña | Qué cargar |
|---------|------------|
| **Servicios** | Nombre, precio, duración (ej. Fade $150, 30 min) |
| **Clientes** | Nombre, teléfono (para WhatsApp) |
| **Barberos** | Personal con usuario y contraseña (solo admin) |

### 3. Prueba completa (checklist piloto)

- [ ] Crear **1 cita** en Agenda (cliente + servicio + barbero + hora)
- [ ] Ir a **Caja** y **cobrar** esa cita (efectivo / tarjeta / transferencia)
- [ ] Ver total en Dashboard y en Caja
- [ ] **Ajustes → Exportar respaldo JSON** y guardar en USB o nube

Si todo eso funciona, el piloto está **operativo**.

### 4. Primer respaldo (importante)

**Ajustes → Respaldo de datos → Exportar respaldo JSON**

- Guardar en USB, Google Drive o correo.
- Repetir **al menos 1 vez por semana**.

---

## D. Uso diario en el local

| Momento | Acción |
|---------|--------|
| **Abrir el día** | Login → revisar Dashboard (citas pendientes, ingresos) |
| **Agendar** | Agenda → nueva cita |
| **Cliente sin cita** | Caja → **+ Cobro walk-in** |
| **Cobrar cita** | Caja → cobrar pendientes del día |
| **Cierre** | Revisar totales en Caja; exportar reporte CSV/PDF si hace falta |
| **Salir** | Dashboard → **Salir** (cierra sesión) |

**Roles**

- **Administrador:** todo + Ajustes + catálogos completos.
- **Barbero:** sus citas, clientes y cobros asignados.

---

## E. Soporte

| | |
|---|---|
| **WhatsApp** | +52 868 363 8812 |
| **Correo** | Jorgeluisvelatovar@gmail.com |
| **Horario** | Lun–Sáb 9:00 – 18:00 |

Al contactar soporte, ten listo:

- **Device ID** (Ajustes → Licencia, o pantalla de activación)
- Nombre del negocio
- Qué estabas haciendo cuando falló

---

## F. Problemas frecuentes

| Problema | Solución |
|----------|----------|
| No instala el MSIX | Ejecutar `install-msix-cert-cliente.ps1` como **Administrador** |
| Pide token otra vez | Internet OK → pegar token → Activar; si persiste, contactar soporte |
| Token inválido | Verificar Device ID en GitHub |
| Cambio de PC | Nuevo Device ID → soporte actualiza licencia → mismo token |
| Licencia vencida | Ajustes → Verificar / Renovar licencia (con internet) |
| Se borraron datos | Restaurar desde **Exportar respaldo JSON** (Ajustes) |
| App lenta | Cerrar otras apps; reiniciar PC |

---

## G. Renovación y licencia

- La app avisa **5 días antes** del vencimiento.
- Soporte renueva la fecha en GitHub.
- Cliente: **Ajustes → Verificar / Renovar licencia**.

---

## H. Checklist final — piloto en marcha

**Soporte (tú)**

- [ ] Kit USB generado con `empaquetar-para-cliente.ps1`
- [ ] Licencia en GitHub con Device ID correcto
- [ ] Token entregado al cliente
- [ ] Instalación cert + MSIX completada
- [ ] Prueba cita + cobro + backup OK

**Cliente**

- [ ] Sabe iniciar sesión y salir
- [ ] Tiene servicios y clientes cargados
- [ ] Sabe exportar respaldo semanal
- [ ] Tiene contacto de soporte guardado

---

**BarberApp v1.0.0** — Salones, barberías y estéticas  
Documento para instalación en PC — piloto y uso productivo
