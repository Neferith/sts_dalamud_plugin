# ── Build ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les projets nécessaires
COPY STS.Domain/STS.Domain.csproj         STS.Domain/
COPY STS.Api/STS.Api.csproj               STS.Api/

# Restaurer les dépendances
RUN dotnet restore STS.Api/STS.Api.csproj

# Copier les sources
COPY STS.Domain/   STS.Domain/
COPY STS.Api/      STS.Api/

# Publier
RUN dotnet publish STS.Api/STS.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copier le build
COPY --from=build /app/publish .

# Le data.json est monté via volume en prod (voir docker-compose.yml)
# Ce COPY sert de fallback pour les tests locaux
COPY STS.Api/data.json ./data.json

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "STS.Api.dll"]
