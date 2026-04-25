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
├── STS.Domain/            # Modèles et use cases du moteur de jeu ; namespace : Sts.Domain
├── STS.Domain.Content/    # Modèles de contenu (règles, QuickLinks, SiteSettings) ; partagé API ↔ Web ↔ Admin
├── STS.Domain.Tests/      # Tests xUnit ; référence STS.Api pour tester les repositories directement
├── STS.Api/               # ASP.NET Core Minimal API ; JWT auth ; endpoints CRUD ; déploiement Docker
├── STS.Admin/             # Blazor WASM ; interface d'administration (CRUD, filtres, tri)
├── STS.Web/               # Blazor WASM ; frontend public ; rendu Markdown via Markdig
├── STS.Discord/           # BackgroundService Discord.Net ; pattern décorateur sur les use cases
└── STSPlugin/             # Plugin Dalamud ; référence STS.Domain
```

---

## Architecture — règles strictes

Le projet applique la **Clean Architecture**. Ne jamais la violer.

| Couche | Projets | Ce qu'elle contient |
|---|---|---|
| Domain | `STS.Domain`, `STS.Domain.Content` | Interfaces, use cases, modèles métier |
| Infrastructure | `STS.Api` | Implémentations concrètes (repositories JSON, HTTP) |
| Presentation | `STS.Admin`, `STS.Web`, `STSPlugin` | UI uniquement |
| Cross-cutting | `STS.Discord` | Décorateurs autour des use cases |

**Règle d'or :** Les interfaces et implémentations de use cases restent dans `STS.Domain` ou `STS.Domain.Content`. Les implémentations d'infrastructure (fichiers JSON, HTTP) restent dans `STS.Api`. **Un use case = une opération.**

---

## Namespaces

- `STS.Domain` utilise le namespace **`Sts.Domain`** (pas `STS.Domain`)
- `STS.Domain.Content` utilise le namespace **`Sts.Domain.Content`**
- `STS.Admin` utilise les namespaces **`Sts.Admin`** (racine) et **`STS.Admin`** (sous-dossiers — incohérence héritée, ne pas changer)
- Les use cases du plugin utilisent **`STSPlugin.CharacterUseCases`** pour éviter l'ambiguïté avec `Sts.Domain.UseCases`

---

## Conventions de nommage

- Use cases : `ICreatePostUseCase`, `IUpdatePostUseCase`, `IDeletePostUseCase` (un verbe, une opération)
- Repositories : `RulesRepository`, `IJobRepository`
- Fakes de test : `FakeRulesDataSource`, `FakeRulesRepository` (classes écrites à la main, **pas de framework de mock**)

---

## Règles xmldoc — obligatoires sur tous les membres publics

Toute réécriture ou nouveau fichier doit inclure la documentation XML complète.

```csharp
/// <summary>Crée un nouveau post de règles dans la section spécifiée.</summary>
/// <param name="request">Les données du post à créer.</param>
/// <returns>Le post créé avec son identifiant assigné.</returns>
public async Task<RulesPost> ExecuteAsync(CreatePostRequest request) { ... }
```

Utiliser `<inheritdoc/>` sur les implémentations d'interface.

---

## Patterns établis

### Clean Architecture côté Admin et Web — ViewModel + RemoteRepository

Les nouvelles pages de `STS.Admin` et `STS.Web` suivent un pattern strict :

```
Pages/
└── FeatureName/
    ├── FeatureName.razor        ← composant fin, injecte le ViewModel
    └── FeatureNameViewModel.cs  ← toute la logique d'état et les commandes
```

**ViewModel :**
- Injecté via DI (`AddScoped<XxxViewModel>()`)
- Expose `Action? OnStateChanged` — le composant assigne `StateHasChanged`
- Contient `IsLoading`, `Error`, et les champs de formulaire
- Les commandes (`LoadAsync`, `SaveAsync`, etc.) appellent `Notify()` pour déclencher le re-render

**RemoteRepository :**
- Implémente les **interfaces du domain** (`IQuickLinksRepository`, `ISiteSettingsRepository`)
- Parle uniquement à l'API HTTP via `HttpClient`
- Enregistré dans DI à la place des repositories JSON : même interface, implémentation différente

**Use cases :**
- Les **mêmes implémentations** que `STS.Api` sont réutilisées dans `STS.Admin` et `STS.Web`
- Seul le repository enregistré dans le DI change selon le projet

### Interfaces lecture seule (STS.Web)

`STS.Web` n'a accès qu'aux endpoints publics. Les interfaces de repository sont donc séparées :

```csharp
// Lecture seule — utilisée par STS.Web et les use cases en lecture
public interface IQuickLinksReadRepository
{
    Task<IEnumerable<QuickLink>> GetAllAsync();
}

// Complète — étend la lecture seule, utilisée par STS.Api et STS.Admin
public interface IQuickLinksRepository : IQuickLinksReadRepository
{
    Task<QuickLink> AddAsync(CreateQuickLinkParameters parameters);
    Task<QuickLink?> UpdateAsync(Guid id, UpdateQuickLinkParameters parameters);
    Task<bool> DeleteAsync(Guid id);
}
```

Dans `STS.Api` et `STS.Admin`, enregistrer les deux interfaces pour le même repository :

```csharp
builder.Services.AddSingleton<IQuickLinksRepository>(new QuickLinksRepository(...));
builder.Services.AddSingleton<IQuickLinksReadRepository>(sp =>
    sp.GetRequiredService<IQuickLinksRepository>());
```

### Paramètres de use case vs DTOs

Les paramètres de use case (`CreateQuickLinkParameters`, `UpdateQuickLinkParameters`) vivent dans `STS.Domain.Content.UseCases`. Ce ne sont **pas** des DTOs HTTP — les DTOs appartiennent à la couche API/infrastructure. Les paramètres sont des entrées métier, pas des shapes JSON.

### Décorateur Discord

`STS.Discord` enveloppe `ICreatePostUseCase`, `IUpdatePostUseCase`, `IDeletePostUseCase` via le pattern décorateur.

**Important :** `AddDiscordBot()` doit être appelé **après** les enregistrements des use cases dans `Program.cs`.

`DiscordMappingStore` persiste `sectionId → forumChannelId` et `postId → threadId` dans `discord-mappings.json`. Quand `Discord:BotToken` est absent : `NullDiscordPublisher` est injecté automatiquement.

### Thread safety dans l'API

`STS.Api` utilise `ReaderWriterLockSlim` pour les repositories JSON. **Ne jamais appeler une méthode qui acquiert le lock depuis l'intérieur d'un lock déjà détenu.** Utiliser une méthode privée interne sans lock pour les opérations imbriquées.

### Responsabilité des repositories JSON

Les repositories JSON (`QuickLinksRepository`, `SiteSettingsRepository`) gèrent eux-mêmes les opérations find+update/delete en interne. Les use cases ne font **jamais** `GetByIdAsync` suivi d'un `UpdateAsync` — le repository encapsule cette logique. Cela garantit que les `RemoteRepository` HTTP n'ont pas besoin d'un appel supplémentaire.

### Chemins de fichiers JSON

Les chemins sont passés par configuration, cohérents avec `Discord:MappingsFilePath` :

```json
"Data": {
  "QuickLinksFilePath": "data/quick-links.json",
  "SiteSettingsFilePath": "data/site-settings.json"
}
```

En production Docker, surcharger par variables d'environnement avec des chemins absolus :

```yaml
environment:
  - Data__QuickLinksFilePath=/data/quick-links.json
  - Data__SiteSettingsFilePath=/data/site-settings.json
```

Les repositories créent le répertoire parent automatiquement (`Directory.CreateDirectory`) — aucune pré-création manuelle requise.

### Auth dans STS.Admin

`AuthService` stocke le JWT dans localStorage. `ApiClient` injecte automatiquement le header Bearer et redirige sur 401.

### CachedDataSource dans STSPlugin

`CachedDataSource` doit être instancié via DI pour que le fallback et le cache fonctionnent. Trois niveaux : remote → disk → bundled.

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
- `STS.Domain.Tests` référence `STS.Api` directement pour tester les repositories JSON (tests d'intégration fichier)
- Les fakes implémentent les interfaces domain et dupliquent la logique interne du repository pour les tests unitaires
- Lancer les tests : `dotnet test`

---

## Docker & déploiement

- Déploiement sur OVH VPS via Docker ; reverse proxy Caddy (HTTPS Let's Encrypt)
- SSH sur le port **2222**, authentification par clé uniquement
- Les fichiers JSON (`quick-links.json`, `site-settings.json`) sont dans le volume `sts-db` (`/data/`) aux côtés de `sts.db` — ils sont créés automatiquement au premier write

---

## Blazor — pièges connus

- `section` est un mot-clé réservé dans les directives Razor — renommer toute variable portant ce nom.
- `@namespace` obligatoire dans les composants dont le dossier crée une collision de nom avec Blazor (ex. `Pages/Home/Home.razor` nécessite `@namespace STS.Web.Pages.Home`).
- Pour du JS réagissant à des éléments chargés dynamiquement, utiliser un `MutationObserver`.
- Le highlighter Chroma applique la classe `.go` — neutraliser avec `background: transparent !important`.

---

## CSS / Frontend (STS.Web)

- Thème bleu-nuit : `--bg-deep`, `--bg-surface`, `--bg-card`, `--teal`, `--ice`, `--moon`, `--amber`, `--purple`
- Classes utilitaires : `sts-card`, `sts-card-accent`, `sts-card-accent-ice`, `sts-card-accent-amber`, `sts-nav`, `sts-nav-link`, `sts-sidebar`, etc.
- Pas de Bootstrap côté `STS.Web` — CSS custom uniquement
- Valider les changements dans les DevTools avant de committer

---

## Workflow attendu

1. **Toujours demander le fichier actuel avant de patcher.**
2. Poser des questions de clarification avant de coder sur des tâches non triviales.
3. Livraison incrémentale : un fichier à la fois, tester entre chaque étape.
4. Output minimal et ciblé — ne pas régénérer le code environnant si ce n'est pas demandé.