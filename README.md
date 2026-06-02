# Adondeamos API

Backend de **Adondeamos** — app hiperlocal de Nuevo Laredo para guardar lugares vistos en redes y
decidir a dónde ir (solo o en grupo). Web API en ASP.NET Core (.NET 10) sobre PostgreSQL (Neon).
El cliente V1 será una app móvil nativa en Flutter.

## Requisitos

- SDK de **.NET 10**
- Una base **PostgreSQL en Neon** con [`db/001_init_schema.sql`](db/001_init_schema.sql) y
  [`db/002_group_invitations.sql`](db/002_group_invitations.sql) ya aplicados, en ese orden
- Una **API key de Google Places (New)** con Places API habilitada

## Configuración (secretos)

Los secretos **no van en el repo**. En desarrollo se usan *user-secrets*; en producción, variables de entorno.

### Desarrollo (user-secrets)

```bash
cd src/Adondeamos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>.neon.tech;Database=<db>;Username=<user>;Password=<pwd>;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Jwt:Secret" "<una-clave-larga-y-aleatoria-de-al-menos-32-caracteres>"
dotnet user-secrets set "GooglePlaces:ApiKey" "<tu-api-key-de-google>"
```

### Producción (variables de entorno)

El doble guion bajo (`__`) separa secciones en .NET:

```
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
Jwt__Secret=<clave-larga-y-aleatoria>
GooglePlaces__ApiKey=<api-key>
```

Otros ajustes con valor por defecto (se pueden sobrescribir): `Jwt:Issuer`, `Jwt:Audience`,
`Jwt:ExpirationMinutes` y `Cors:AllowedOrigins`. La app Flutter nativa no depende de CORS, pero se
mantiene la configuración para Swagger, pruebas locales o una futura versión web/admin.

## Correr

```bash
dotnet run --project src/Adondeamos.Api
```

- Swagger UI: `/swagger`
- Health check: `GET /health`

En Swagger, usa **Authorize** y pega `Bearer <token>` (el token lo devuelven `/auth/register` y
`/auth/login`) para probar los endpoints protegidos.

## Alcance V1

El backend V1 cubre:

- Auth: registro, login y perfil (`/auth/register`, `/auth/login`, `/me`).
- Groups: grupos con invitaciones confirmadas (`/groups`, `/groups/{id}/invitations`,
  `/me/invitations`, `/invitations/{id}/accept|reject`).
- Places: búsqueda/resolución con Google Places y lugares propios (`/places/search`,
  `/places/resolve`, `/places`).
- Saves: guardados por usuario, filtros, edición y borrado (`/saves`).
- Lists: listas personales o de grupo con elementos (`/lists`).
- Decide/match: sesiones en solitario o grupo, opciones, votos y match persistido (`/decisions`).

La capa social pública (reseñas, fotos, follows, badges e interacciones) existe en el esquema como
gancho de Fase 2, pero todavía no tiene endpoints. El recomendador inteligente queda para Fase 3.

## Smoke test V1

Con el API corriendo localmente:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-v1.ps1 -BaseUrl http://localhost:5172
```

El script crea usuarios smoke con correos únicos, crea un grupo, envía y acepta una invitación, crea
un lugar propio, guarda el lugar para ambos usuarios, crea una decisión grupal, vota con ambos y
verifica que el match quede persistido. No usa Google Places para evitar costo y dependencias
externas durante el smoke.

## Estructura

Solución por capas: `Domain` (entidades/enums), `Application` (DTOs, validación, servicios),
`Infrastructure` (EF Core, repositorios, Google Places, seguridad) y `Api` (controllers + arranque).
Ver [`CLAUDE.md`](CLAUDE.md) para convenciones y detalles.

## Notas de cumplimiento (Google Places)

Lo único que se persiste de Google es el `google_place_id`. Nombre, dirección, horarios, reseñas y
fotos de Google se traen **bajo demanda** y se muestran con atribución; **nunca se guardan ni se
cachean** en la base. Ver detalles en [`CLAUDE.md`](CLAUDE.md).
