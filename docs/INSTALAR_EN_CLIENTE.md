# BarberApp — Kit de instalación para el cliente (Windows)

Entrega esta carpeta al cliente con **3 archivos**:

| Archivo | Descripción |
|---------|-------------|
| `BarberApp_1.0.0.0_x64.msix` | La aplicación |
| `BarberApp-MSIX.cer` | Certificado de confianza |
| `GUIA_CLIENTE.pdf` o `.md` | Guía de uso (opcional, imprimir `docs/GUIA_CLIENTE.md`) |

Genera el kit con:

```powershell
cd C:\Users\jv869\source\repos\BarberApp
.\scripts\empaquetar-para-cliente.ps1
```

La carpeta `dist\cliente\` quedará lista para copiar a USB.

---

## Paso 1 — Instalar certificado (Administrador)

1. Copia `BarberApp-MSIX.cer` al PC del cliente.
2. Clic derecho en **Inicio** → **Terminal (Administrador)** o **PowerShell (Administrador)**.
3. Ejecuta (ajusta la ruta):

```powershell
cd C:\Users\Cliente\Desktop\BarberApp
Import-Certificate -FilePath .\BarberApp-MSIX.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
Import-Certificate -FilePath .\BarberApp-MSIX.cer -CertStoreLocation Cert:\LocalMachine\Root
```

O usa el script incluido:

```powershell
.\install-msix-cert-cliente.ps1
```

4. Cierra la ventana.

> **Sin este paso**, Windows pedirá Modo desarrollador o bloqueará la instalación.

---

## Paso 2 — Instalar BarberApp

1. Doble clic en **`BarberApp_1.0.0.0_x64.msix`**
2. **Instalar**
3. Abre **BarberApp** desde el menú Inicio

---

## Paso 3 — Activar licencia (tú le das el token)

1. Cliente copia el **Device ID** de la pantalla y te lo envía (WhatsApp).
2. Tú lo registras en `licencias.json` en GitHub (ver `docs/PASO_A_PASO_LICENCIAS.md`).
3. Entregas al cliente su **token** (ej. `BARBER-2026-0001`).
4. Cliente pega token → **Activar licencia** (con internet).
5. Crea cuenta **administrador** (solo la primera vez).
6. **Ajustes → Exportar respaldo JSON** (primer respaldo).

---

## Paso 4 — Verificación rápida (5 min)

- [ ] Login admin OK  
- [ ] Alta de 1 servicio y 1 cliente en Catálogos  
- [ ] 1 cita en Agenda  
- [ ] Cobro en Caja  
- [ ] Exportar respaldo JSON  

Checklist completo: `docs/QA_CHECKLIST.md`

---

## Problemas frecuentes

| Problema | Solución |
|----------|----------|
| No instala el MSIX | Instalar `.cer` como administrador |
| Pide token otra vez | Re-activar con el mismo token; revisar internet |
| Token no válido | Verificar `dispositivo_id` y `token` en GitHub |
| App no abre | Reinstalar MSIX; Windows 10 1809 o superior |

---

## Soporte

Ver contacto en `docs/GUIA_CLIENTE.md` sección 6.
