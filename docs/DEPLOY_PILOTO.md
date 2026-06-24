# Checklist deploy piloto — BarberApp

Marca ✅ al completar. Orden recomendado.

---

## A. Licencias (GitHub)

- [ ] Leí `docs/PASO_A_PASO_LICENCIAS.md` y elegí **repo público mínimo**
- [ ] Creé repo `BDLicencias` en GitHub
- [ ] Subí `licencias.json` (desde `docs/licencias.plantilla.json`)
- [ ] Puse mi **Device ID** de prueba en `dispositivo_id`
- [ ] Generé token único (ej. `BARBER-2026-0001-X7K9M2`)
- [ ] Verifiqué URL raw en el navegador
- [ ] Actualicé `LicenciaConfig.cs` con mi URL
- [ ] Activé licencia en la app con ese token

---

## B. Empaquetado Windows

- [ ] Ejecuté `.\scripts\create-msix-cert.ps1` (una vez)
- [ ] Ejecuté `.\scripts\publish-windows.ps1`
- [ ] MSIX en `dist\windows\`
- [ ] Probé instalar `.cer` + MSIX en **mi** PC

---

## C. Instalación cliente

- [ ] Ejecuté `.\scripts\empaquetar-para-cliente.ps1`
- [ ] Entregué carpeta `dist\cliente\` (MSIX + CER + guía)
- [ ] Seguí `docs/INSTALAR_EN_CLIENTE.md` en PC del cliente
- [ ] Cliente activó con su token
- [ ] Primer respaldo JSON exportado

---

## D. Documentación

- [ ] Completé contacto en `docs/GUIA_CLIENTE.md` (WhatsApp / correo)
- [ ] Imprimí o envié guía al cliente

---

## E. QA

- [ ] Recorrí `docs/QA_CHECKLIST.md` en PC limpia (sin Visual Studio)
- [ ] Login, cita, cobro, reporte PDF, backup OK

---

## F. Pendiente (no bloquea piloto)

- [ ] Sync nube
- [ ] Microsoft Store
- [ ] API privada de licencias (cuando escales)

**Fecha piloto:** ___________  
**Cliente #1:** ___________
