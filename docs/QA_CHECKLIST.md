# BarberApp — Checklist QA manual (Release)

Usar antes de entregar a cada cliente. Marcar ✅ al completar.

## Build

- [ ] `dotnet test` — todos los tests pasan
- [ ] Build Release Windows OK (`scripts/publish-windows.ps1`)
- [ ] Instalar MSIX o carpeta publicada en PC **sin Visual Studio**

## Licencia

- [ ] Pantalla de activación muestra Device ID estable (reabrir app → mismo ID)
- [ ] Activación con token válido + internet → registro admin
- [ ] Token inválido → mensaje claro
- [ ] Token válido pero Device ID distinto → mensaje de migración
- [ ] Licencia vencida → pantalla de bloqueo
- [ ] Renovar fecha en JSON → verificación desbloquea

## Registro y login

- [ ] Crear admin con todos los campos
- [ ] Login correcto → Dashboard
- [ ] Login incorrecto → error
- [ ] Salir → vuelve a login
- [ ] Timeout de sesión (opcional: bajar a 1 min en Ajustes)

## Operación

- [ ] Catálogos: servicio, cliente, barbero
- [ ] Agenda: crear cita, editar, cancelar
- [ ] Caja: cobrar cita (3 métodos de pago)
- [ ] Dashboard: stats y gráfica semanal
- [ ] Reporte CSV/PDF desde Caja

## Respaldo

- [ ] Exportar JSON desde Ajustes
- [ ] Restaurar en misma máquina (modo fusionar)
- [ ] Respaldo automático tras entrar al Dashboard

## Tema y UI

- [ ] Tema claro / oscuro en Dashboard, Agenda, Caja, Catálogos
- [ ] Login, Licencia, Registro legibles en ambos temas

## Notificaciones

- [ ] Android 13+: app solicita permiso de notificaciones
- [ ] Cita dentro de 1 h → notificación (app abierta o en segundo plano reciente)
- [ ] Windows: toast o alerta al recordatorio

## Ajustes (admin)

- [ ] Cambiar datos del negocio
- [ ] Copiar Device ID
- [ ] Cambiar contraseña admin

---

**Resultado:** ___ Aprobado / ___ Con observaciones

**Probado en:** Windows ___ / Android ___  
**Versión:** ___  
**Fecha:** ___
