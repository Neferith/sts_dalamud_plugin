# STSPlugin — Système Très Simple

> Outil de roleplay pour la guilde **La Nouvelle Lune** sur Final Fantasy XIV.

Le **Système Très Simple (STS)** est un système de jets de dés minimaliste conçu pour les events de roleplay. Ce dépôt contient l'ensemble de la suite d'outils associée : plugin Dalamud, API backend, interface d'administration et site joueurs.

---

## Projets

| Projet | Description |
|--------|-------------|
| `STSPlugin` | Plugin Dalamud — jets de dés en jeu, affichage dans le chat |
| `STS.Domain` | Bibliothèque partagée — modèles, use cases, moteur STS |
| `STS.Domain.Content` | Bibliothèque partagée — modèles de contenu (règles) |
| `STS.Domain.Tests` | Suite de tests xUnit — couverture du domaine et de l'infrastructure |
| `STS.Infrastructure` | Accès aux données — EF Core, SQLite, migrations |
| `STS.Api` | API ASP.NET Core — données, authentification JWT, CRUD |
| `STS.Admin` | Interface d'administration Blazor WASM (hébergée par l'API) |
| `STS.Web` | Site joueurs Blazor WASM — règles, métiers, compétences |

### Architecture des couches

```
UI (STSPlugin / STS.Admin / STS.Web)
    └── Use Cases (STS.Domain — interfaces + implémentations)
            └── Repositories (STS.Domain — interfaces)
                    └── Infrastructure (STS.Infrastructure — EF Core, JSON)
                            └── Domain Models (STS.Domain — purs, sans dépendances)
```

---

## Plugin Dalamud (STSPlugin)

### Prérequis

- Final Fantasy XIV avec [XIVLauncher](https://goatcorp.github.io/) et Dalamud installés
- .NET 8 SDK

### Installation

1. Ajouter l'URL du repo custom dans XIVLauncher :
   ```
   https://raw.githubusercontent.com/Neferith/sts_dalamud_plugin/master/repo.json
   ```
2. Chercher **STSPlugin** dans le gestionnaire de plugins et l'installer.

### Commandes

| Commande | Description |
|----------|-------------|
| `/sts` | Ouvre la fenêtre principale |
| `/sts roll` | Lance un jet de dés |
| `/sts config` | Ouvre la configuration |

---

## Stack web

### Architecture

```
https://nlrp.fr          → STS.Web   (site joueurs)
https://admin.nlrp.fr    → STS.Admin (backoffice)
https://api.nlrp.fr      → STS.Api   (API REST)
```

### Prérequis

- Docker & Docker Compose
- .NET 8 SDK (pour `STS.Api`, `STS.Admin`, `STS.Infrastructure`)
- .NET 10 SDK (pour `STS.Web`)

### Configuration

Créer un fichier `.env` à la racine (ne jamais commiter) :

```env
JWT_SECRET=your_jwt_secret_here
ADMIN_USERNAME=your_admin_username
ADMIN_PASSWORD=your_admin_password
```

Un `.env.example` est fourni comme référence.

### Build & déploiement

```bash
# Builder les images
docker build -f Dockerfile -t sts-api:latest .
docker build -f Dockerfile.web -t sts-web:latest .

# Démarrer
docker compose up -d
```

> ⚠️ **Volume JSON** — si `rules.json` n'existe pas encore sur le host, le créer manuellement avant le premier `docker compose up` :
> ```bash
> echo "[]" > /data/rules.json
> ```
> Sans cette étape, Docker crée un répertoire à cet emplacement au lieu d'un fichier.

---

## Développement local

### STS.Api + STS.Admin

```bash
cd STS.Api
dotnet run
```

L'API est accessible sur `https://localhost:7144`.  
La Swagger UI est disponible sur `https://localhost:7144/swagger`.

### STS.Web

```bash
cd STS.Web
dotnet run
```

Le site est accessible sur `https://localhost:7221`.

### Tests

```bash
cd STS.Domain.Tests
dotnet test
```

---

## Base de données

### SQLite (développement local)

La base de données SQLite est créée automatiquement au premier démarrage de `STS.Api` via les migrations EF Core. Le fichier `sts.db` est généré dans `STS.Api/` — il est exclu du dépôt (`.gitignore`).

### Migrations

Les fichiers de migration dans `STS.Infrastructure/Migrations/` sont versionnés et doivent être commités. Ils sont appliqués automatiquement au démarrage de l'API (`db.Database.Migrate()`).

**Créer une migration après modification du schéma :**

```bash
cd STS.Infrastructure
dotnet ef migrations add NomExplicatif --context StsDbContext
```

**Appliquer manuellement (optionnel) :**

```bash
dotnet ef database update --context StsDbContext
```

> ⚠️ Ne jamais modifier une migration déjà appliquée en production. Toujours créer une nouvelle migration.

### Production (Docker)

En production, la base de données est persistée dans le volume `/data/` monté via `docker-compose.yml` et survit aux redéploiements :

```
/data/sts.db
```

### Passer à PostgreSQL

L'architecture isole l'accès aux données derrière `IRulesDataSource` dans `STS.Infrastructure`. Pour migrer :

1. Remplacer `Microsoft.EntityFrameworkCore.Sqlite` par `Npgsql.EntityFrameworkCore.PostgreSQL` dans `STS.Infrastructure.csproj`
2. Changer `UseSqlite(...)` par `UseNpgsql(...)` dans `Program.cs`
3. Générer une nouvelle migration initiale
4. Mettre à jour la connection string dans `docker-compose.yml`

---

## État d'avancement

### ✅ Terminé

- **STS.Domain** — modèles purs, use cases (un par opération), `StsEngine`, architecture Clean Architecture complète
- **STS.Domain.Tests** — suite xUnit avec FluentAssertions, fakes handwrittens (`FakeRulesDataSource`, `FakeRulesRepository`)
- **STS.Api** — JWT Bearer auth, CRUD complet (jobs, traits, capacités, actions, règles), services thread-safe (`SemaphoreSlim`), `RulesSection` / `RulesPost`
- **STS.Admin** — auth JWT, `ApiClient` avec injection Bearer, quatre pages CRUD (jobs, traits, capacités, actions), page de gestion des règles
- **STSPlugin** — jets de dés en jeu, référence `STS.Domain`, UI ImGui
- **Docker** — build multi-stage, `docker-compose.yml`, Linux containers
- **CI/CD** — pipeline GitHub Actions : un tag Git déclenche le build et le déploiement automatique sur le VPS OVH
- **Déploiement VPS OVH** — Nginx reverse proxy + HTTPS (Let's Encrypt), `nlrp.fr` / `admin.nlrp.fr` / `api.nlrp.fr` en production

### 🔄 En cours / À venir
- **STS.Web** — site joueurs Blazor WASM (règles, métiers, compétences) — consomme `STS.Api`
- **RemoteJsonDataSource** — pattern `CachedDataSource` dans le plugin pour fetcher `data.json` depuis l'API
- **Migration base de données** — couche infrastructure déjà architecturée pour ça

### 🐛 Bugs connus / Dépréciations intentionnelles

- **GameRandom + Avantage/Désavantage** — les contrôles UI sont désactivés quand `RollSource == GameRandom` (palliation court-terme en attendant une refacto)
- **Incohérences `data.json`** — certains malus de traits (ex. "Spécialiste à distance") ne sont pas reflétés dans les entrées d'actions associées

---

## Licence

[GNU Affero General Public License v3.0](LICENSE.md)