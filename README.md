# Adondeamos API

Backend de **Adondeamos** — app hiperlocal de Nuevo Laredo para guardar lugares vistos en redes y
decidir a dónde ir (solo o en grupo). Web API en ASP.NET Core (.NET 10) sobre PostgreSQL (Neon).

## Requisitos

- SDK de **.NET 10**
- Una base **PostgreSQL en Neon** con el esquema de [`db/001_init_schema.sql`](db/001_init_schema.sql) ya aplicado
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
`Jwt:ExpirationMinutes` y `Cors:AllowedOrigins` (lista de orígenes del frontend).

## Correr

```bash
dotnet run --project src/Adondeamos.Api
```

- Swagger UI: `/swagger`
- Health check: `GET /health`

En Swagger, usa **Authorize** y pega `Bearer <token>` (el token lo devuelven `/auth/register` y
`/auth/login`) para probar los endpoints protegidos.

## Estructura

Solución por capas: `Domain` (entidades/enums), `Application` (DTOs, validación, servicios),
`Infrastructure` (EF Core, repositorios, Google Places, seguridad) y `Api` (controllers + arranque).
Ver [`CLAUDE.md`](CLAUDE.md) para convenciones y detalles.

## Notas de cumplimiento (Google Places)

Lo único que se persiste de Google es el `google_place_id`. Nombre, dirección, horarios, reseñas y
fotos de Google se traen **bajo demanda** y se muestran con atribución; **nunca se guardan ni se
cachean** en la base. Ver detalles en [`CLAUDE.md`](CLAUDE.md).
