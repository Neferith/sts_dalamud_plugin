# CLAUDE.md — Système Très Simple (STS)

Bienvenue dans le projet STS. Ce fichier documente l'architecture, les conventions et les pièges connus pour que tu puisses travailler efficacement sans régressions.

---

## Vue d'ensemble

STS est un écosystème full-stack autour d'un système de roleplay tabulaire Final Fantasy XIV pour la guilde **Nouvelle Lune**. Il comprend un plugin Dalamud, une API backend, deux frontends Blazor et une intégration Discord.

**Stack tech :** C# / .NET 8 partout. Dalamud + ImGui pour le plugin, ASP.NET Core Minimal API, Blazor WASM, xUnit + FluentAssertions, Docker + Caddy sur OVH VPS, Discord.Net.

---

## Structure de la solution

```
STS.sln
├── STS.Domain/              # Moteur de jeu, DataSource/DataModel, repositories partagés ; namespace : Sts.Domain
├── STS.Domain.Content/      # Modèles de contenu (règles, QuickLinks, SiteSettings) ; partagé API ↔ Web ↔ Admin
├── STS.Domain.Character/    # Modèle Character, ICharacterRepository, use cases character ; namespace : Sts.Domain.Character
├── STS.Domain.User/         # Modèle User (roles : Admin/Member), IUserRepository, use cases auth ; namespace : Sts.Domain.User
├── STS.Domain.Tests/        # Tests xUnit ; référence STS.Api pour tester les repositories directement
├── STS.Api/                 # ASP.NET Core Minimal API ; JWT auth ; endpoints CRUD ; déploiement Docker
├── STS.Admin/               # Blazor WASM ; interface d'administration (CRUD, filtres, tri, gestion utilisateurs)
├── STS.Web/                 # Blazor WASM ; frontend public ; fiches personnages ; auth membre
├── STS.Discord/             # BackgroundService Discord.Net ; pattern décorateur sur les use cases
└── STSPlugin/               # Plugin Dalamud ; référence STS.Domain + STS.Domain.Character
```

---

## Architecture — règles strictes

Le projet applique la **Clean Architecture**. Ne jamais la violer.

| Couche | Projets | Ce qu'elle contient |
|---|---|---|
| Domain | `STS.Domain`, `STS.Domain.Content`, `STS.Domain.Character`, `STS.Domain.User` | Interfaces, use cases, modèles métier |
| Infrastructure | `STS.Api` | Implémentations concrètes (repositories JSON, HTTP) |
| Presentation | `STS.Admin`, `STS.Web`, `STSPlugin` | UI uniquement |
| Cross-cutting | `STS.Discord` | Décorateurs autour des use cases |

**Règle d'or :** Les interfaces et implémentations de use cases restent dans le domain. Les implémentations d'infrastructure restent dans `STS.Api`. **Un use case = une opération.**

### Dépendances entre projets domain

```
STS.Domain.Character  →  STS.Domain        (RankKey, Rank, RollAction…)
STS.Domain.User       →  (aucune)          User est autonome
STS.Api               →  STS.Domain.Character + STS.Domain.User
STSPlugin             →  STS.Domain + STS.Domain.Character
STS.Web               →  STS.Domain.Character + STS.Domain.User
```

`Character.UserId` est un `Guid?` nu — pas de référence directe à `User`, pas de dépendance circulaire.

---

## Namespaces

- `STS.Domain` → **`Sts.Domain`**
- `STS.Domain.Content` → **`Sts.Domain.Content`**
- `STS.Domain.Character` → **`Sts.Domain.Character`**
- `STS.Domain.User` → **`Sts.Domain.User`**
- `STS.Domain` repositories partagés → **`Sts.Domain.Repositories`**
- `STS.Domain` datasource → **`Sts.Domain.DataSource`**
- `STS.Admin` → `Sts.Admin` (racine) et `STS.Admin` (sous-dossiers — incohérence héritée, ne pas changer)
- Use cases plugin → **`STSPlugin.CharacterUseCases`**

---

## Conventions de nommage

- Use cases : `ICreateCharacterUseCase`, `IAuthenticateUserUseCase` (un verbe, une opération)
- Repositories : `ICharacterRepository`, `IUserRepository`, `ITraitRepository`
- Fakes de test : `FakeRulesDataSource`, `FakeRulesRepository` (**pas de framework de mock**)
- DTOs API : `UserDto`, `CreateUserRequest`, `LoginRequest` — dans la couche API, pas dans le domain

---

## Règles xmldoc — obligatoires sur tous les membres publics

```csharp
/// <summary>Authentifie un utilisateur par son nom et son code d'accès.</summary>
/// <param name="username">Nom d'utilisateur saisi.</param>
/// <param name="plainCode">Code d'accès en clair (non haché).</param>
/// <returns>L'utilisateur authentifié, ou null si les identifiants sont incorrects.</returns>
public async Task<User?> ExecuteAsync(string username, string plainCode) { ... }
```

Utiliser `<inheritdoc/>` sur les implémentations d'interface.

---

## Patterns établis

### DataSource partagée (IDataSource)

`IDataSource` est dans `Sts.Domain.DataSource`. Deux méthodes :
- `Load()` — sync, retourne depuis le cache mémoire
- `LoadAsync()` — async, charge la source et remplit le cache

**Implémentations :**
- `LocalJsonDataSource` — lit `data.json` sur disque (plugin mode local)
- `RemoteJsonDataSource` — GET HTTP sync avec timeout (plugin mode remote)
- `CachedDataSource` — décorateur remote → cache disque → local (plugin)
- `HttpDataSource` — GET `/api/data` async avec cache mémoire (STS.Web)

**Dans STS.Web** : `HttpDataSource.LoadAsync()` est appelé au démarrage avant `host.RunAsync()`. Les repositories singletons appellent `Load()` de façon synchrone ensuite.

### Repositories de données de référence

`ITraitRepository`, `IJobRepository`, `IAbilityRepository`, `IActionRepository` sont dans `Sts.Domain.Repositories`. Les implémentations `Default*Repository` sont aussi dans `STS.Domain` — elles prennent `IDataSource` et sont partagées entre plugin et web.

**Constructeur pré-charge :** les Default*Repository chargent dans leur constructeur via `dataSource.Load()`. C'est possible car `Load()` est synchrone et le cache est déjà rempli (pre-load async au démarrage côté web, sync côté plugin).

### Auth JWT unifiée (admin + member)

Un seul endpoint `POST /api/auth/login`. Le JWT contient :
- `ClaimTypes.NameIdentifier` = `userId` (Guid)
- `ClaimTypes.Name` = `username`
- `ClaimTypes.Role` = `"admin"` ou `"member"` (minuscules)

Policies ASP.NET Core : `"admin"` et `"member"` déclarées dans `AddAuthorization`.

`ISeedAdminUseCase` est exécuté au démarrage via `AdminSeedService` (`IHostedService`) — crée le compte admin depuis `appsettings` s'il n'existe pas.

**STS.Web :** `AuthService` parse le JWT côté client (base64 decode du payload) pour extraire `UserId`, `Username`, `Role`. Pas de localStorage pour l'instant — session en mémoire uniquement.

### Fiches personnages — accès et limites

- Lecture : tout utilisateur authentifié voit toutes les fiches
- Création : membre → 1 fiche max ; admin → 8 fiches max
- Édition/suppression : propriétaire uniquement (vérifié via `UserId` dans le JWT)
- `Character.UserId` est `Guid?` — null pour les fiches créées localement dans le plugin (rétrocompatibilité)

### ViewModel pattern (STS.Admin et STS.Web)

```
Pages/
└── FeatureName/
    ├── FeatureName.razor        ← composant fin, injecte le ViewModel
    └── FeatureNameViewModel.cs  ← toute la logique d'état et les commandes
```

**ViewModel :**
- `Action? OnStateChanged` — le composant assigne `StateHasChanged`
- `IsLoading`, `IsSaving`, `Error`, `Success` — état standard
- Commandes async appellent `Notify()` pour déclencher le re-render
- Enregistré en `AddScoped<XxxViewModel>()` dans Program.cs

### CharacterApiService (STS.Web)

Wrapper HTTP direct pour les fiches personnages — pas d'implémentation de `ICharacterRepository` (corps HTTP incompatibles entre POST et PUT). Méthodes : `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.

### Use cases plugin — sync vs async

- `GetActiveCharacterUseCase` et `SetActiveCharacterUseCase` restent **synchrones** (appelés depuis le render thread ImGui). Ils utilisent `.GetAwaiter().GetResult()` sur les méthodes async du repository.
- Tous les autres use cases character sont **async**.
- Les mutations dans les fenêtres ImGui utilisent `_ = Task.Run(() => useCase.ExecuteAsync(...))`.
- Le cache UI (`_characters`, `_activeCharacter`) est mis à jour via `TriggerRefresh()` après chaque mutation.

### Décorateur Discord

`AddDiscordBot()` doit être appelé **après** les enregistrements des use cases dans `Program.cs`.

### Thread safety dans l'API

`UserRepository` et `CharacterRepository` utilisent `SemaphoreSlim(1,1)`. **Ne jamais appeler une méthode qui acquiert le lock depuis une méthode qui le détient déjà** — risque de deadlock. Utiliser une méthode privée interne sans lock pour les opérations imbriquées.

Pattern correct :
```csharp
public async Task SaveAsync(Character character)
{
    await _lock.WaitAsync();
    try   { await SaveInternalAsync(character); }   // interne, sans lock
    finally { _lock.Release(); }
}

private async Task SaveInternalAsync(Character character) { ... } // pas de lock ici
```

### Chemins de fichiers JSON (STS.Api)

```json
"Data": {
  "UsersFilePath": "/data/users.json",
  "CharactersFilePath": "/data/characters.json",
  "QuickLinksFilePath": "/data/quick-links.json",
  "SiteSettingsFilePath": "/data/site-settings.json"
}
```

Variables d'environnement Docker : `Data__UsersFilePath=/data/users.json`, etc.

---

## Pièges ImGui connus (STSPlugin)

- **`BeginTabBar` + drag-and-drop = crash.** Remplacer par un toggle `_activeTab` (int) géré manuellement.
- Deux bugs connus à corriger :
  1. Le bouton Roll contourne `plugin.StartRoll()`.
  2. Avantage/Désavantage non supporté correctement en mode GameRandom.

---

## Tests

- Framework : **xUnit + FluentAssertions**
- **Pas de framework de mock** — fakes écrits à la main
- `STS.Domain.Tests` référence `STS.Api` directement pour tester les repositories JSON
- Lancer les tests : `dotnet test`

---

## Docker & déploiement

- Déploiement sur OVH VPS via Docker ; reverse proxy Caddy (HTTPS Let's Encrypt)
- SSH sur le port **2222**, authentification par clé uniquement
- Volume `sts-db` (`/data/`) : `sts.db`, `users.json`, `characters.json`, `quick-links.json`, `site-settings.json`
- Les fichiers JSON sont créés automatiquement au premier write

---

## Blazor — pièges connus

- `section` est un mot-clé réservé dans les directives Razor — renommer toute variable portant ce nom.
- `@namespace` obligatoire dans les composants dont le dossier crée une collision de nom avec Blazor.
- Pour du JS réagissant à des éléments chargés dynamiquement, utiliser un `MutationObserver`.
- En Blazor WASM, `Scoped` se comporte comme `Singleton` — ne pas enregistrer en `Singleton` un service qui dépend de `HttpClient` (enregistré en `Scoped`).
- **`_Imports.razor` dans STS.Web** — centraliser les `@using` récurrents :
  ```razor
  @using Sts.Domain
  @using Sts.Domain.Character
  @using Sts.Domain.User
  @using STS.Web.ViewModels
  @using STS.Web.Services
  ```

## BCrypt

Le package NuGet s'appelle **`BCrypt.Net-Next`** (pas `BCrypt.Net`). L'appel se fait via le chemin complet pour éviter l'ambiguïté entre le namespace et la classe :

```csharp
// ✅ Correct
BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor);
BCrypt.Net.BCrypt.Verify(plaintext, hash);

// ✅ Aussi valide — alias
using BC = BCrypt.Net.BCrypt;
BC.HashPassword(plaintext, workFactor);

// ❌ Éviter — crée une ambiguïté
using BCrypt.Net;
BCrypt.HashPassword(...); // erreur de compilation
```

---

## CSS / Frontend (STS.Web)

- Thème bleu-nuit : `--bg-deep`, `--bg-surface`, `--bg-card`, `--teal`, `--ice`, `--moon`, `--amber`, `--purple`
- Classes utilitaires : `sts-card`, `sts-card-accent`, `sts-nav`, `sts-btn`, `sts-btn-primary`, `sts-btn-ghost`, `sts-input`, `sts-label`, `sts-field`, `sts-modal`, `sts-modal-backdrop`, `sts-rank-btn`, `sts-section-title`, `sts-back-link`
- Pas de Bootstrap côté `STS.Web` — CSS custom uniquement
- Valider les changements dans les DevTools avant de committer

---

## Workflow attendu

1. **Toujours demander le fichier actuel avant de patcher.**
2. Poser des questions de clarification avant de coder sur des tâches non triviales.
3. Livraison incrémentale : un fichier à la fois, tester entre chaque étape.
4. Output minimal et ciblé — ne pas régénérer le code environnant si ce n'est pas demandé.