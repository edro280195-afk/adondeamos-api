FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos solo los .csproj para cachear la capa de restore
COPY src/Adondeamos.Domain/Adondeamos.Domain.csproj src/Adondeamos.Domain/
COPY src/Adondeamos.Application/Adondeamos.Application.csproj src/Adondeamos.Application/
COPY src/Adondeamos.Infrastructure/Adondeamos.Infrastructure.csproj src/Adondeamos.Infrastructure/
COPY src/Adondeamos.Api/Adondeamos.Api.csproj src/Adondeamos.Api/

# Restauramos el proyecto principal (las referencias entre proyectos jalan automático)
RUN dotnet restore src/Adondeamos.Api/Adondeamos.Api.csproj

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish src/Adondeamos.Api/Adondeamos.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Adondeamos.Api.dll"]
