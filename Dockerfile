FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ ./src/

RUN dotnet restore src/Adondeamos.Api/Adondeamos.Api.csproj

RUN dotnet publish src/Adondeamos.Api/Adondeamos.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Adondeamos.Api.dll"]
