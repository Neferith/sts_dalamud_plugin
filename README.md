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
| `STS.Api` | API ASP.NET Core — données, authentification JWT, CRUD |
| `STS.Admin` | Interface d'administration Blazor WASM (hébergée par l'API) |
| `STS.Web` | Site joueurs Blazor WASM — règles, métiers, compétences |

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
- .NET 8 SDK (pour STS.Api / STS.Admin)
- .NET 10 SDK (pour STS.Web)

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

---

## Licence

[GNU Affero General Public License v3.0](LICENSE.md)