# Exclusivo ERP

Sistema de inventario y punto de venta migrado de ASP.NET MVC 5 / .NET Framework 4.8 a **.NET 8 + PostgreSQL + EF Core**, dockerizado y listo para CI/CD con GitHub Actions, GHCR y Railway.

## Arquitectura

| Capa | Tecnología |
| --- | --- |
| Web | ASP.NET Core MVC 8 (Razor, cookies, autorización por roles) |
| Datos | EF Core 8 + Npgsql (PostgreSQL) |
| Contenedor | Dockerfile multi-stage Linux (`sdk:8.0` → `aspnet:8.0`) |
| CI | GitHub Actions → GHCR (`master`) |
| Deploy | Railway (usa la variable `PORT`) |

## Ejecutar en local

Requiere PostgreSQL en `localhost:5432` (usuario `postgres`, contraseña `postgres`, base `exclusivo`) o definir `DATABASE_URL`.

```bash
dotnet restore Exclusivo.sln
dotnet build Exclusivo.sln
dotnet run --project src/Exclusivo.Web
```

La base se crea y siembra sola al arrancar (`DbInitializer`). Usuarios de prueba:

| Usuario | Contraseña | Rol |
| --- | --- | --- |
| admin | Admin123* | Administrador |
| gerente | Gerente123* | Gerente |
| vendedor | Vendedor123* | Vendedor |
| inventario | Inventario123* | Inventario |

El rol `Cajero` fue eliminado. En bases existentes, los usuarios con ese rol se
convierten automáticamente a `Vendedor` al iniciar la aplicación.

## Docker

```bash
docker build -t exclusivo-erp .
docker run -p 3000:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=exclusivo;Username=postgres;Password=postgres" \
  exclusivo-erp
```

Abre <http://localhost:3000>.

> En Linux, para acceder al PostgreSQL del host usa `host.docker.internal` con `--add-host=host.docker.internal:host-gateway` o una red de contenedores.

## CI/CD (guía del docente)

1. El workflow `.github/workflows/docker-ci.yml` se dispara con `push` a **`master`**.
2. Construye la imagen y la publica en GHCR con `GITHUB_TOKEN` y `packages: write`.
3. Tags generados: `:latest` y `:sha-<commit>`.
4. Railway despliega desde el repo usando el `Dockerfile` de la raíz y asigna `PORT`.

La app lee `PORT` en `Program.cs`, por lo que responde en el puerto que Railway indique (el Dockerfile expone 8080 por defecto).

### Checklist

- [x] `Dockerfile` en la raíz (Linux, multi-stage)
- [x] `.dockerignore` para excluir binarios y secretos
- [x] Workflow en `.github/workflows/docker-ci.yml` con `branches: [master]`
- [x] Login en GHCR con `secrets.GITHUB_TOKEN` y `permissions: packages: write`
- [x] Paso de minúsculas para el nombre de la imagen
- [x] `railway.json` con builder Dockerfile
- [x] Uso de `PORT` en el código
- [ ] Push a `master` (requiere tu cuenta de GitHub)
- [ ] Workflow en verde y paquete en GHCR
- [ ] Deploy activo en Railway con dominio público
- [ ] PR pequeño → merge → nueva versión desplegada

## Estructura

```
Exclusivo.sln
Dockerfile
.github/workflows/docker-ci.yml
railway.json
src/Exclusivo.Web/
  Controllers/         Account, Home, Products, Inventory, Sales, Suppliers,
                       Warehouses, Users, Reports
  Data/                AppDbContext, DbInitializer
  Models/              Entidades y ViewModels
  Services/            Auth, PasswordHasher, Audit, Inventory, Sales
  Views/               Razor
tests/Exclusivo.Tests/
```

## Migración desde el proyecto legacy

El código original (`.NET Framework 4.8`, MVC 5, SQL Server, stored procedures, Dockerfile Windows) se conserva en las carpetas `ExcProy`, `CapaDatos`, `CapaEntidades`, etc. La nueva solución `Exclusivo.sln` convive con él y no modifica el legacy.

Cambios clave respecto al legacy:
- Autenticación SHA-256 sin salt → PBKDF2 con salt de 16 bytes y 100 000 iteraciones.
- Autorización solo visual → atributos `[Authorize(Roles = ...)]` en servidor.
- `CD_Inventario.Listar()` filtraba `estado = 0` (bug corregido).
- Stock y movimientos se actualizan en transacciones atómicas.
- Anulación de ventas requiere motivo y restaura el inventario.
- Auditoría de operaciones sensibles.
