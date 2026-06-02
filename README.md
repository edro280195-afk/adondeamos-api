# Adondeamos API

Backend de **Adondeamos** — app hiperlocal de Nuevo Laredo para guardar lugares vistos en redes y
decidir a dónde ir (solo o en grupo). Web API en ASP.NET Core (.NET 10) sobre PostgreSQL (Neon).
El cliente V1 será una app móvil nativa en Flutter.

## Requisitos

- SDK de **.NET 10**
- Una base **PostgreSQL en Neon** con los cuatro scripts SQL aplicados **en orden**:
  1. [`db/001_init_schema.sql`](db/001_init_schema.sql) — esquema base
  2. [`db/002_group_invitations.sql`](db/002_group_invitations.sql) — invitaciones a grupos
  3. [`db/003_add_username_to_users.sql`](db/003_add_username_to_users.sql) — columna `username`
  4. [`db/004_add_email_confirmation.sql`](db/004_add_email_confirmation.sql) — confirmación de email
- Una **API key de Google Places (New)** con Places API habilitada (solo para `/places/search` y `/places/resolve`)

## Configuración (secretos)

Los secretos **no van en el repo**. En desarrollo se usan *user-secrets*; en producción, variables de entorno.

### Desarrollo (user-secrets) — modo autoconfirm (sin correo real)

```bash
cd src/Adondeamos.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>.neon.tech;Database=<db>;Username=<user>;Password=<pwd>;SSL Mode=Require;Channel Binding=Require"
dotnet user-secrets set "Jwt:Secret" "<clave-aleatoria-de-al-menos-32-caracteres>"
dotnet user-secrets set "GooglePlaces:ApiKey" "<tu-api-key-de-google>"
```

Con `Auth:AutoConfirmEmail=true` (el default en `appsettings.json`), el email queda confirmado al
registrar. El link de confirmación aparece en el **log de la consola** con nivel Warning para poder
copiarlo fácilmente:

```
[DEV] Correo de confirmación para Eduardo <e@example.com>. Link: http://localhost:5172/auth/confirm-email?token=...
```

### Desarrollo — con SMTP real (opcional)

```bash
dotnet user-secrets set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Username" "tu@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "<app-password>"
dotnet user-secrets set "Email:Smtp:FromAddress" "Adondeamos <tu@gmail.com>"
# Opcional: requerir confirmación antes de hacer login
dotnet user-secrets set "Auth:AutoConfirmEmail" "false"
dotnet user-secrets set "Auth:RequireConfirmedEmailToLogin" "true"
```

### Producción (variables de entorno)

El doble guion bajo (`__`) separa secciones en .NET:

```
ConnectionStrings__Default=Host=...;...;SSL Mode=Require;Channel Binding=Require
Jwt__Secret=<clave-aleatoria>
GooglePlaces__ApiKey=<api-key>
Email__Smtp__Host=smtp.proveedor.com
Email__Smtp__Port=587
Email__Smtp__Username=...
Email__Smtp__Password=...
Email__Smtp__FromAddress=Adondeamos <noreply@adondeamos.app>
Auth__AutoConfirmEmail=false
Auth__RequireConfirmedEmailToLogin=true
App__ConfirmEmailUrlBase=https://api.adondeamos.app
```

## Correr

```bash
dotnet run --project src/Adondeamos.Api
```

- Swagger UI: `/swagger`
- Health check: `GET /health`

En Swagger, usa **Authorize** y pega `Bearer <token>` (el token lo devuelven `/auth/register` y
`/auth/login`) para probar los endpoints protegidos.

## Autenticación y confirmación de email

El registro requiere `name`, `username`, `email` y `password`. El **login usa `username`** (no email).

```json
// POST /auth/register
{ "name": "Eduardo", "username": "eduardo_rdz", "email": "e@example.com", "password": "segura123" }

// POST /auth/login
{ "username": "eduardo_rdz", "password": "segura123" }

// POST /auth/confirm-email  (token recibido por email o en el log en modo dev)
{ "token": "<token-del-link>" }

// POST /auth/resend-confirmation  (siempre responde 200, anti-enumeración)
{ "email": "e@example.com" }
```

**Biometría:** es responsabilidad del cliente Flutter (guarda el JWT cifrado y lo desbloquea con
biometría local). El servidor no la conoce ni la valida.

## Alcance V1

| Módulo | Endpoints principales |
|---|---|
| Auth | `POST /auth/register`, `/auth/login`, `/auth/confirm-email`, `/auth/resend-confirmation`, `GET /me`, `PATCH /me` |
| Groups | `POST /groups`, `GET /groups`, `GET /groups/{id}` |
| Invitaciones | `POST /groups/{id}/invitations`, `GET /me/invitations`, `POST /invitations/{id}/accept\|reject` |
| Places | `GET /places/search?q=`, `POST /places/resolve`, `POST /places` |
| Saves | `POST /saves`, `GET /saves`, `PATCH /saves/{id}`, `DELETE /saves/{id}` |
| Lists | `POST /lists`, `GET /lists`, `GET /lists/{id}`, `POST /lists/{id}/items`, `DELETE /lists/{id}/items/{saveId}` |
| Decide/match | `POST /decisions`, `POST /decisions/{id}/options`, `POST /decisions/{id}/options/{optionId}/votes`, `GET /decisions/{id}` |

**Pendiente (próximo paso):** recuperación de contraseña ("olvidé mi contraseña").  
**Fuera de alcance (aún):** capa social Fase 2, recomendador Fase 3.

## Smoke test V1

Con el API corriendo (y los cuatro scripts SQL aplicados en Neon):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-v1.ps1 -BaseUrl http://localhost:5172
```

El script prueba el flujo completo: registro (con email_confirmed por AutoConfirmEmail) → login →
grupos → invitar/aceptar/rechazar → lugares propios → guardados → listas → decisión grupal con
votos y match persistido. No toca Google Places para evitar costos.

## Estructura

Solución por capas: `Domain` (entidades/enums), `Application` (DTOs, validación, servicios),
`Infrastructure` (EF Core, repositorios, Google Places, email Dev/SMTP, seguridad) y `Api`
(controllers + arranque). Ver [`CLAUDE.md`](CLAUDE.md) para convenciones y detalles de mapeo.

## Notas de cumplimiento (Google Places)

Lo único que se persiste de Google es el `google_place_id`. Nombre, dirección, horarios, reseñas y
fotos de Google se traen **bajo demanda** con field masks y se devuelven en `ResolvePlaceResponse.google`
para mostrarse con atribución; **nunca se guardan ni se cachean** en la base.
