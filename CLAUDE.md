# Adondeamos API — Guía del proyecto

Backend (Web API en ASP.NET Core, .NET 10) de **Adondeamos**: app hiperlocal de Nuevo Laredo,
Tamaulipas, para guardar lugares que la gente ve en redes (TikTok, Instagram, Facebook, WhatsApp,
Google Maps) y decidir a dónde ir, solo o en pareja/grupo.

## Convenciones (síguelas siempre)

- Responder en **español**.
- **Identificadores** (clases, métodos, variables, tablas, columnas) en **inglés**; **comentarios en español**.
- Código **completo y production-ready**: nada de TODOs, stubs ni "// implementar aquí".
- **Commits en español**, naturales (ej. "agrega autenticación con JWT y registro de usuarios").
- DTOs de entrada/salida: **no se exponen las entidades** directamente.
- Si algo es ambiguo, preguntar antes de implementar.

## Decisiones técnicas (fijas)

- **ASP.NET Core, .NET 10 LTS.**
- **PostgreSQL en Neon.** El esquema YA está aplicado; la fuente de verdad son los archivos `db/`.
  **NO se recrea ni se altera el esquema** sin un nuevo archivo SQL versionado.
- **EF Core mapeado al esquema existente. NO se usan migraciones de EF Core.** Las entidades se
  escriben a mano para calzar exacto con las tablas. El mapeo a `snake_case` lo aplica
  `UseSnakeCaseNamingConvention` (paquete `EFCore.NamingConventions`).
- Los **enums** del esquema (`place_origin`, `social_network`, etc.) se mapean como enums nativos de
  PostgreSQL: `MapEnum<T>` en el data source y `HasPostgresEnum<T>` en el `DbContext`. Los nombres de
  los miembros C# se traducen a las etiquetas SQL con `NpgsqlSnakeCaseNameTranslator`.
- **IDs y timestamps** los genera la base (`gen_random_uuid()`, `now()` y triggers `set_updated_at`);
  EF los marca como generados por el almacén y los lee de vuelta.
- **Autenticación con JWT** (register/login por **username**, no por email). Contraseñas con **BCrypt**.
  Login social queda para después.
- **Swagger/OpenAPI** habilitado. **CORS** configurado para pruebas locales, Swagger, Flutter web
  opcional o una futura consola admin. El cliente V1 principal será **Flutter nativo**.
- **Health check** en `GET /health` (Render lo necesita; evita cold starts). Es liveness simple, no toca la base.
- Errores centralizados con **ProblemDetails** y códigos correctos (400/401/403/404/409/502).
- **Arquitectura por capas.**

## Reglas CRÍTICAS de Google Places (cumplimiento legal)

- Se usa **Places API (New)**. La búsqueda usa **Autocomplete** (sesiones gratis) y los detalles se
  traen **bajo demanda**, solo cuando el usuario selecciona un lugar.
- **Lo ÚNICO que se persiste de Google es el `google_place_id`.** Nunca se guarda ni se cachea nombre,
  dirección, horarios, reseñas ni fotos de Google.
- Los datos de Google se muestran con su atribución; nunca se usan para armar un dataset propio.
- Se usan **field masks** para pedir solo los campos necesarios (control de costo).
- La **API key** va en configuración (user-secrets en dev, variables de entorno en prod). NO se hardcodea.

## Estructura de la solución

```
Adondeamos.slnx
db/
  001_init_schema.sql          # Esquema base (aplicado en Neon)
  002_group_invitations.sql    # Tabla group_invitations + enum invitation_status
  003_add_username_to_users.sql# Columna username en users (índice único lower(username))
scripts/
  smoke-v1.ps1                 # Smoke test end-to-end del flujo V1 completo
src/
  Adondeamos.Domain/           # Entidades y enums (calcan el esquema). Sin dependencias.
    Entities/  Enums/
  Adondeamos.Application/      # DTOs, validadores, servicios, interfaces (repos, JWT, hasher, Google).
    Abstractions/  Common/  DTOs/  Services/  Validators/
  Adondeamos.Infrastructure/   # DbContext, repositorios, JWT, hasher, cliente de Google Places.
    Persistence/  Repositories/  Security/  Google/
  Adondeamos.Api/              # Controllers, middleware, Program.cs, configuración.
    Controllers/  Extensions/  Middleware/
```

## Modelo de datos relevante

### Tabla `users` (después de db/001 + db/003)

| columna | tipo | notas |
|---|---|---|
| `id` | uuid PK | `gen_random_uuid()` |
| `username` | text UNIQUE | login principal; `lower(username)` único |
| `name` | text | nombre visible |
| `email` | text UNIQUE | registro/contacto; no es el campo de login |
| `password_hash` | text | BCrypt; nulo si solo login social |
| `avatar_url` | text | URL de la foto de perfil |
| `created_at` / `updated_at` | timestamptz | trigger automático |

### Enums nativos (PostgreSQL)

`place_origin`, `social_network`, `save_status`, `content_visibility`, `group_role`,
`invitation_status` — todos mapeados con `NpgsqlSnakeCaseNameTranslator`.

## Cómo corre el acceso a datos

- Los servicios (capa Application) dependen de **interfaces de repositorio** y de `IUnitOfWork`
  (definidas en Application). La implementación (EF Core) vive en Infrastructure.
- Cada repositorio expone métodos concretos (no se filtra `IQueryable`).
- Los controllers extraen el `userId` del JWT y lo pasan a los servicios; la **autorización por
  pertenencia** (ownership) se verifica en cada operación.

## Alcance V1 (lo que SÍ se construye)

Auth (`/auth/register`, `/auth/login`, `/me`), Groups (con invitaciones: el invitado debe aceptar —
`/groups/{id}/invitations`, `/me/invitations`, `/invitations/{id}/accept|reject`), Places
(search/resolve/own), Saves, Lists, Decide/match (`/decisions`). V1 se considera completo cuando
compila y pasa el smoke test `scripts/smoke-v1.ps1` contra Neon.

## Fuera de alcance (NO construir aún)

- Fase 2 (capa social): reviews, photos, follows, badges, user_badges, interactions. Las tablas
  existen, pero **no se les hacen endpoints todavía**.
- Fase 3: recomendador inteligente (clima, hábitos, embeddings).

## Cómo correr

Ver [`README.md`](README.md). En corto: configurar `ConnectionStrings:Default` (Neon),
`Jwt:Secret` y `GooglePlaces:ApiKey` por user-secrets, y `dotnet run --project src/Adondeamos.Api`.
Para validar el ida-y-vuelta completo sin usar Google Places:
`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-v1.ps1`.
