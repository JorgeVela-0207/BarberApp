# BarberApp

Aplicación de escritorio y móvil para **barberías, salones y estéticas**: agenda de citas, caja, catálogos y configuración del negocio.

Desarrollada con **.NET MAUI** (Windows MSIX + Android). Datos locales en **SQLite**. Activación por **token de licencia** vinculado al dispositivo.

## Características

- Licencia → registro de administrador → login → panel principal
- Agenda, caja, catálogos (servicios, clientes, empleados)
- Tema claro / oscuro
- Empaquetado Windows (MSIX) para instalación en PC del cliente

## Estructura del repositorio

| Carpeta | Contenido |
|---------|-----------|
| `BarberApp/` | App MAUI (UI y plataforma) |
| `BarberApp.Core/` | Lógica de negocio, SQLite, licencias |
| `BarberApp.Tests/` | Pruebas unitarias |
| `docs/` | Manuales de instalación, QA y soporte |
| `scripts/` | Publicar, empaquetar cliente, licencias, GitHub |

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 con carga de trabajo **.NET MAUI** (Windows)
- Windows 10/11 para compilación y piloto MSIX

## Compilar (Windows)

```powershell
dotnet build BarberApp.slnx -c Release
```

## Scripts útiles

```powershell
# Publicar MSIX Windows
.\scripts\publish-windows.ps1

# Kit para entregar al cliente (USB)
.\scripts\empaquetar-para-cliente.ps1

# Nueva licencia cuando el cliente envía su Device ID
.\scripts\nueva-licencia.ps1

# Subir cambios a GitHub (respeta .gitignore)
.\scripts\subir-a-github.ps1 -Commit "mensaje" -Push
```

## Qué no está en GitHub

Por seguridad, `.gitignore` excluye:

- `dist/` — paquete MSIX listo para el cliente
- `certs/` y `BarberApp.Signing.props` — firma de producción
- `bin/`, `obj/`, `.vs/`

## Documentación

- Instalación en PC del cliente: `docs/INSTALACION_PC_CLIENTE.md`
- Uso diario: `docs/GUIA_CLIENTE.md`
- Checklist QA: `docs/QA_CHECKLIST.md`

## Autor

**Jorge Vela** — [JorgeVela-0207](https://github.com/JorgeVela-0207)

Versión piloto Windows: **1.0.0**
