# ── Build container ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restauramos capas por proyecto para cachear mejor
COPY Adondeamos.slnx ./
COPY src/Adondeamos.Domain/Adondeamos.Domain.csproj src/Adondeamos.Domain/
COPY src/Adondeamos.Application/Adondeamos.Application.csproj src/Adondeamos.Application/
COPY src/Adondeamos.Infrastructure/Adondeamos.Infrastructure.csproj src/Adondeamos.Infrastructure/
COPY src/Adondeamos.Api/Adondeamos.Api.csproj src/Adondeamos.Api/

RUN dotnet restore

COPY . .
RUN dotnet publish src/Adondeamos.Api -c Release -o /out --no-restore

# ── Runtime container ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .

# Render inyecta la variable PORT automáticamente.
ENV ASPNETCORE_URLS=http://+:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Adondeamos.Api.dll"]
