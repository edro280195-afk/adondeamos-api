# Adondeamos API

Backend de **Adondeamos** — app hiperlocal de Nuevo Laredo para guardar lugares vistos en redes y
decidir a dónde ir (solo o en grupo). Web API en ASP.NET Core (.NET 10) sobre PostgreSQL (Neon).
El cliente V1 será una app móvil nativa en Flutter.

## Requisitos

- SDK de **.NET 10**
- Una base **PostgreSQL en Neon** con los tres scripts SQL aplicados **en orden**:
  1. [`db/001_init_schema.sql`](db/001_init_schema.sql) — esquema base
  2. [`db/002_group_invitations.sql`](db/002_group_invitations.sql) — invitaciones a grupos
  3. [`db/003_add_username_to_users.sql`](db/003_add_username_to_users.sql) — columna `username` en usuarios
- Una **API key de Google Places (New)** con Places API habilitada (solo para `/places/search` y `/places/resolve`)

## Configuración (secretos)

Los secretos **no van en el repo**. En desarrollo se usan *user-secrets*; en producción, variables de entorno.

### Desarrollo (user-secrets)

```bash
cd src/Adondeamos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>.neon.tech;Database=<db>;Username=<user>;Password=<pwd>;SSL Mode=Require;Channel Binding=Require"
dotnet user-secrets set "Jwt:Secret" "<clave-aleatoria-de-al-menos-32-caracteres>"
dotnet user-secrets set "GooglePlaces:ApiKey" "<tu-api-key-de-google>"
```

### Producción (variables de entorno)

El doble guion bajo (`__`) separa secciones en .NET:

```
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Channel Binding=Require
Jwt__Secret=<clave-aleatoria>
GooglePlaces__ApiKey=<api-key>
```

Otros ajustes con valor por defecto (se pueden sobrescribir): `Jwt:Issuer`, `Jwt:Audience`,
`Jwt:ExpirationMinutes` (default: 10080 min = 7 días) y `Cors:AllowedOrigins`.

## Correr

```bash
dotnet run --project src/Adondeamos.Api
```

- Swagger UI: `/swagger`
- Health check: `GET /health`

En Swagger, usa **Authorize** y pega `Bearer <token>` (el token lo devuelven `/auth/register` y
`/auth/login`) para probar los endpoints protegidos.

## Autenticación

El registro requiere `name`, `username`, `email` y `password`. El **login usa `username`** (no email).
El `username` solo puede contener letras, números, puntos y guiones bajos (min 3, max 50 caracteres).

```json
// POST /auth/register
{ "name": "Eduardo", "username": "eduardo_rdz", "email": "e@example.com", "password": "segura123" }

// POST /auth/login
{ "username": "eduardo_rdz", "password": "segura123" }
```

## Alcance V1

| Módulo | Endpoints principales |
|---|---|
| Auth | `POST /auth/register`, `POST /auth/login`, `GET /me`, `PATCH /me` |
| Groups | `POST /groups`, `GET /groups`, `GET /groups/{id}` |
| Invitaciones | `POST /groups/{id}/invitations`, `GET /me/invitations`, `POST /invitations/{id}/accept\|reject` |
| Places | `GET /places/search?q=`, `POST /places/resolve`, `POST /places` |
| Saves | `POST /saves`, `GET /saves`, `PATCH /saves/{id}`, `DELETE /saves/{id}` |
| Lists | `POST /lists`, `GET /lists`, `GET /lists/{id}`, `POST /lists/{id}/items`, `DELETE /lists/{id}/items/{saveId}` |
| Decide/match | `POST /decisions`, `POST /decisions/{id}/options`, `POST /decisions/{id}/options/{optionId}/votes`, `GET /decisions/{id}` |

La capa social pública (reseñas, fotos, follows, badges e interacciones) existe en el esquema como
gancho de Fase 2, pero todavía no tiene endpoints. El recomendador inteligente queda para Fase 3.

## Smoke test V1

Con el API corriendo (y los tres scripts SQL aplicados en Neon):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-v1.ps1 -BaseUrl http://localhost:5172
```

El script crea usuarios con `username` único por corrida, prueba el flujo completo: registro →
login → grupos → invitar/aceptar/rechazar → lugares propios → guardados → listas → decisión grupal
con votos y match persistido. No toca Google Places para evitar costos.

## Estructura

Solución por capas: `Domain` (entidades/enums), `Application` (DTOs, validación, servicios),
`Infrastructure` (EF Core, repositorios, Google Places, seguridad) y `Api` (controllers + arranque).
Ver [`CLAUDE.md`](CLAUDE.md) para convenciones, decisiones técnicas y detalles de mapeo.

## Notas de cumplimiento (Google Places)

Lo único que se persiste de Google es el `google_place_id`. Nombre, dirección, horarios, reseñas y
fotos de Google se traen **bajo demanda** con field masks y se devuelven en `ResolvePlaceResponse.google`
para mostrarse con atribución; **nunca se guardan ni se cachean** en la base.
