# STSPlugin — Système Très Simple

> Outil de roleplay pour la guilde **La Nouvelle Lune** sur Final Fantasy XIV.

Le **Système Très Simple (STS)** est un système de jets de dés minimaliste conçu pour les events de roleplay. Ce dépôt contient l'ensemble de la suite d'outils associée : plugin Dalamud, API backend, interface d'administration et site joueurs.

---

## Projets

| Projet | Description |
|--------|-------------|
| `STSPlugin` | Plugin Dalamud — jets de dés en jeu, affichage dans le chat |
| `STS.Domain` | Bibliothèque partagée — modèles, use cases, moteur STS |
| `STS.Domain.Content` | Bibliothèque partagée — modèles de contenu (règles, QuickLinks, SiteSettings) |
| `STS.Domain.Tests` | Suite de tests xUnit — couverture du domaine et de l'infrastructure |
| `STS.Infrastructure` | Accès aux données — EF Core, SQLite, migrations |
| `STS.Api` | API ASP.NET Core — données, authentification JWT, CRUD |
| `STS.Admin` | Interface d'administration Blazor WASM (hébergée par l'API) |
| `STS.Web` | Site joueurs Blazor WASM — home Nouvelle Lune, règles, métiers, compétences |

### Architecture des couches

```
UI (STSPlugin / STS.Admin / STS.Web)
    └── Use Cases (STS.Domain.Content — interfaces + implémentations partagées)
            └── Repositories (interfaces en lecture seule ou complètes selon le contexte)
                    └── Infrastructure (STS.Api → JSON ; STS.Admin/Web → HTTP)
                            └── Domain Models (STS.Domain.Content — purs, sans dépendances)
```

**Principe clé :** les implémentations de use cases sont partagées entre `STS.Api`, `STS.Admin` et `STS.Web`. Seul le repository enregistré dans le DI change selon le projet — JSON pour l'API, HTTP pour les clients.

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
https://nlrp.fr          → STS.Web   (site joueurs — Nouvelle Lune)
https://admin.nlrp.fr    → STS.Admin (backoffice)
https://api.nlrp.fr      → STS.Api   (API REST)
```

### Prérequis

- Docker & Docker Compose
- .NET 8 SDK

### Configuration

Créer un fichier `.env` à la racine (ne jamais commiter) :

```env
JWT_SECRET=your_jwt_secret_here
ADMIN_USERNAME=your_admin_username
ADMIN_PASSWORD=your_admin_password
DISCORD_BOT_TOKEN=your_discord_bot_token
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

> ⚠️ **Volumes JSON** — les fichiers `quick-links.json` et `site-settings.json` sont créés automatiquement dans le volume `/data/` au premier appel d'écriture. Aucune action manuelle requise.

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

```bash
cd STS.Infrastructure
dotnet ef migrations add NomExplicatif --context StsDbContext
```

> ⚠️ Ne jamais modifier une migration déjà appliquée en production.

### Production (Docker)

La base de données est persistée dans le volume `/data/` :

```
/data/sts.db
/data/quick-links.json
/data/site-settings.json
```

> **Note :** `QuickLinks` et `SiteSettings` sont actuellement persistés en JSON dans ce volume. La migration vers SQLite est prévue — l'architecture repository facilite ce changement sans toucher aux use cases.

---

## État d'avancement

### ✅ Terminé

- **STS.Domain** — modèles purs, use cases, `StsEngine`, Clean Architecture complète
- **STS.Domain.Content** — modèles de contenu (règles, `QuickLink`, `QuickLinkCategory`, `SiteSettings`), interfaces repository (lecture seule + complètes), use cases partagés
- **STS.Domain.Tests** — suite xUnit + FluentAssertions, fakes handwritten, tests d'intégration JSON pour `QuickLinksRepository`
- **STS.Api** — JWT Bearer auth, CRUD complet (jobs, traits, capacités, actions, règles, QuickLinks, SiteSettings), repositories thread-safe (`ReaderWriterLockSlim`)
- **STS.Admin** — auth JWT, pages CRUD (jobs, traits, capacités, actions, règles, images, Discord), pages home (QuickLinks + SiteSettings) avec architecture ViewModel + RemoteRepository
- **STS.Web** — home Nouvelle Lune (hero, liens rapides par catégorie, chargement depuis l'API), page règles, architecture ViewModel + repositories lecture seule
- **STS.Discord** — pattern décorateur sur les use cases post ; `NullDiscordPublisher` si token absent
- **STSPlugin** — jets de dés en jeu, référence `STS.Domain`, UI ImGui
- **Docker** — build multi-stage, `docker-compose.yml`, Linux containers
- **CI/CD** — pipeline GitHub Actions : tag Git → build → déploiement automatique OVH VPS
- **Déploiement VPS OVH** — Caddy reverse proxy + HTTPS (Let's Encrypt)

### 🔄 En cours / À venir

- **Widget "Dernières mises à jour"** sur la home `STS.Web`
- **Migration SQLite** pour `QuickLinks` et `SiteSettings` (repositories déjà abstraits derrière des interfaces — le changement n'impacte pas les use cases)
- **RemoteJsonDataSource + CachedDataSource** dans le plugin
- **Portrait capture** dans le plugin (GPOSE via `ICondition`)

### 🐛 Bugs connus / Dépréciations intentionnelles

- **GameRandom + Avantage/Désavantage** — contrôles UI désactivés en attendant une refacto
- **Incohérences `data.json`** — certains malus de traits ne sont pas reflétés dans les actions associées

---

## Licence

[GNU Affero General Public License v3.0](LICENSE.md)