# STSPlugin — Système Très Simple

> Outil de roleplay pour la guilde **La Nouvelle Lune** sur Final Fantasy XIV.

Le **Système Très Simple (STS)** est un système de jets de dés minimaliste conçu pour les events de roleplay. Ce dépôt contient l'ensemble de la suite d'outils associée : plugin Dalamud, API backend, interface d'administration et site joueurs.

---

## Projets

| Projet | Description |
|--------|-------------|
| `STSPlugin` | Plugin Dalamud — jets de dés en jeu, gestion des fiches personnages |
| `STS.Domain` | Bibliothèque partagée — moteur STS, DataSource, repositories de données de référence |
| `STS.Domain.Content` | Bibliothèque partagée — règles, QuickLinks, SiteSettings |
| `STS.Domain.Character` | Bibliothèque partagée — modèle Character, use cases, ICharacterRepository |
| `STS.Domain.User` | Bibliothèque partagée — modèle User (Admin/Member), auth, IUserRepository |
| `STS.Domain.Tests` | Suite de tests xUnit — couverture du domaine et de l'infrastructure |
| `STS.Infrastructure` | Accès aux données — EF Core, SQLite, migrations |
| `STS.Api` | API ASP.NET Core — auth JWT unifiée, CRUD complet, fiches personnages |
| `STS.Admin` | Interface d'administration Blazor WASM — CRUD contenu + gestion utilisateurs |
| `STS.Web` | Site joueurs Blazor WASM — home, règles, fiches personnages, auth membre |
| `STS.Discord` | Bot Discord — synchronisation des posts de règles |

### Architecture des couches

```
UI (STSPlugin / STS.Admin / STS.Web)
    └── Use Cases (STS.Domain.* — interfaces + implémentations partagées)
            └── Repositories (interfaces partagées dans STS.Domain)
                    └── Infrastructure (STS.Api → JSON/SQLite ; STS.Web → HTTP)
                            └── Domain Models (STS.Domain.* — purs, sans dépendances)
```

**Principe clé :** les implémentations de use cases et repositories sont partagées entre les projets. Seul le source de données (JSON, HTTP, SQLite) change selon le contexte.

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

### Fonctionnalités

- Jets de dés STS (Normal / Avantage / Désavantage)
- Modificateur MJ (+/-3)
- Rerolls selon le rang
- Historique des jets
- Gestion des fiches personnages (création, édition complète, inventaire)
- Mode local (JSON) ou remote (API) configurable depuis les settings
- Cache disque pour le mode remote (fallback offline)

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

### Auth

- Endpoint unique `POST /api/auth/login` — retourne un JWT avec rôle `admin` ou `member`
- Compte admin initialisé depuis `appsettings` au démarrage (`AdminSeedService`)
- Les membres sont créés depuis STS.Admin
- STS.Web : session en mémoire (pas de localStorage pour l'instant)

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
docker build -f Dockerfile -t sts-api:latest .
docker build -f Dockerfile.web -t sts-web:latest .
docker compose up -d
```

---

## Développement local

### STS.Api + STS.Admin

```bash
cd STS.Api
dotnet run
```

API sur `https://localhost:7144` — Swagger sur `https://localhost:7144/swagger`.

> **Note dev local :** créer le dossier `STS.Api/data/` avant le premier démarrage, ou s'assurer que `appsettings.Development.json` pointe vers des chemins relatifs existants (`"data/users.json"`, etc.). Les fichiers JSON sont créés automatiquement au premier write, mais le dossier parent doit exister.

### STS.Web

```bash
cd STS.Web
dotnet run
```

Site sur `https://localhost:7221`.

### Tests

```bash
cd STS.Domain.Tests
dotnet test
```

---

## Base de données

### Fichiers JSON (utilisateurs, personnages, QuickLinks, SiteSettings)

Persistés dans le volume `/data/` — créés automatiquement au premier write.

```
/data/users.json
/data/characters.json
/data/quick-links.json
/data/site-settings.json
```

### SQLite (règles)

```bash
cd STS.Infrastructure
dotnet ef migrations add NomExplicatif --context StsDbContext
```

> ⚠️ Ne jamais modifier une migration déjà appliquée en production.

---

## État d'avancement

### ✅ Terminé

- **STS.Domain** — moteur STS, DataSource partagée (`IDataSource`, `LocalJsonDataSource`, `RemoteJsonDataSource`, `CachedDataSource`), repositories de données de référence partagés (`ITraitRepository`, `IJobRepository`, `IAbilityRepository`, `IActionRepository`)
- **STS.Domain.Character** — modèle `Character` (avec `UserId`), `ICharacterRepository`, use cases CRUD
- **STS.Domain.User** — modèle `User` (enum `UserRole`), `IUserRepository`, `IPasswordHasher`, use cases auth + seed admin
- **STS.Domain.Content** — règles, QuickLinks, SiteSettings
- **STS.Domain.Tests** — suite xUnit + FluentAssertions, fakes handwritten
- **STS.Api** — auth JWT unifiée (admin/member), CRUD personnages, CRUD utilisateurs, seed admin au démarrage, `UserRepository` + `CharacterRepository` JSON thread-safe
- **STS.Admin** — auth JWT, pages CRUD (jobs, traits, capacités, actions, règles, images, Discord, **utilisateurs**)
- **STS.Web** — auth membre (login modal, `AuthService` avec parsing JWT), fiches personnages (liste, détail, création, édition complète), `HttpDataSource` avec pre-load, repositories partagés
- **STSPlugin** — fiches personnages async (`LocalCharacterRepository`), use cases async, pattern cache UI pour le render thread ImGui, mode local/remote configurable
- **STS.Discord** — pattern décorateur sur les use cases post
- **Docker + CI/CD** — pipeline GitHub Actions, déploiement OVH VPS, Caddy HTTPS

### 🔄 En cours / À venir

- **Cookie/localStorage** — persister la session après refresh dans STS.Web
- **Modération officier** — certains champs de fiche verrouillés (certifications)
- **RemoteCharacterRepository** dans le plugin — synchronisation fiche locale ↔ API
- **Migration SQLite** — QuickLinks, SiteSettings, users et characters
- **Widget "Dernières mises à jour"** sur la home STS.Web
- **Portrait capture** dans le plugin (GPOSE via `ICondition`)

### 🐛 Bugs connus

- **GameRandom + Avantage/Désavantage** — contrôles UI désactivés en attendant refacto
- **Bouton Roll plugin** — bypass `StartRoll()` → toujours en Internal RNG
- **Session STS.Web** — perdue au refresh (pas de localStorage)

---

## Licence

[GNU Affero General Public License v3.0](LICENSE.md)