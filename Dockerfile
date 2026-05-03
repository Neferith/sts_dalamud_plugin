# ── Build ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les projets nécessaires (tous avant le restore)
COPY STS.Domain/STS.Domain.csproj                         STS.Domain/
COPY STS.Domain.Content/STS.Domain.Content.csproj         STS.Domain.Content/
COPY STS.Domain.Character/STS.Domain.Character.csproj     STS.Domain.Character/
COPY STS.Domain.User/STS.Domain.User.csproj               STS.Domain.User/
COPY STS.Infrastructure/STS.Infrastructure.csproj         STS.Infrastructure/
COPY STS.Discord/STS.Discord.csproj                       STS.Discord/
COPY STS.Api/STS.Api.csproj                               STS.Api/
COPY STS.Admin/STS.Admin.csproj                           STS.Admin/

# Restaurer les dépendances
RUN dotnet restore STS.Api/STS.Api.csproj

# Copier les sources
COPY STS.Domain/           STS.Domain/
COPY STS.Domain.Content/   STS.Domain.Content/
COPY STS.Domain.Character/ STS.Domain.Character/
COPY STS.Domain.User/      STS.Domain.User/
COPY STS.Infrastructure/   STS.Infrastructure/
COPY STS.Discord/          STS.Discord/
COPY STS.Api/              STS.Api/
COPY STS.Admin/            STS.Admin/

# Publier
RUN dotnet publish STS.Api/STS.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

COPY STS.Api/data.json ./data.json

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "STS.Api.dll"]