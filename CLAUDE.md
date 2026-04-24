# CLAUDE.md — Système Très Simple (STS)

Bienvenue dans le projet STS. Ce fichier documente l'architecture, les conventions et les pièges connus pour que tu puisses travailler efficacement sans régressions.

---

## Vue d'ensemble

STS est un écosystème full-stack autour d'un système de roleplay tabulaire Final Fantasy XIV pour une guilde. Il comprend un plugin Dalamud, une API backend, deux frontends Blazor et une intégration Discord.

**Stack tech :** C# / .NET 8 partout. Dalamud + ImGui pour le plugin, ASP.NET Core Minimal API, Blazor WASM, xUnit + FluentAssertions, Docker + Caddy sur OVH VPS, Discord.Net.

---

## Structure de la solution

```
STS.sln
├── STS.Domain/            # Modèles et use cases du moteur de jeu ; namespace : Sts.Domain
├── STS.Domain.Content/    # Modèles de contenu (RulesSection, RulesPost) ; net8.0 ; partagé API ↔ Web
├── STS.Domain.Tests/      # 106 tests xUnit ; référence STS.Api pour tester RulesRepository directement
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
| Infrastructure | `STS.Api` | Implémentations concrètes (repositories, data sources) |
| Presentation | `STS.Admin`, `STS.Web`, `STSPlugin` | UI uniquement |
| Cross-cutting | `STS.Discord` | Décorateurs autour des use cases |

**Règle d'or :** Les interfaces et implémentations de use cases restent dans `STS.Domain` ou `STS.Domain.Content`. Les implémentations d'infrastructure (fichiers JSON, HTTP) restent dans `STS.Api`. **Un use case = une opération.**

---

## Namespaces

- `STS.Domain` utilise le namespace **`Sts.Domain`** (pas `STS.Domain`)
- Les use cases du plugin utilisent **`STSPlugin.CharacterUseCases`** pour éviter l'ambiguïté avec `Sts.Domain.UseCases`

---

## Conventions de nommage

- Use cases : `ICreatePostUseCase`, `IUpdatePostUseCase`, `IDeletePostUseCase` (un verbe, une opération)
- Repositories : `RulesRepository`, `IJobRepository`
- Services : `RulesService` (orchestration dans l'API, pas dans le domaine)
- Fakes de test : `FakeRulesDataSource`, `FakeRulesRepository` (classes écrites à la main, **pas de framework de mock**)

---

## Règles xmldoc — obligatoires sur tous les membres publics

Toute réécriture ou nouveau fichier doit inclure la documentation XML complète. La qualité ne doit pas régresser.

```csharp
/// <summary>
/// Crée un nouveau post de règles dans la section spécifiée.
/// </summary>
/// <param name="request">Les données du post à créer.</param>
/// <returns>Le post créé avec son identifiant assigné.</returns>
public async Task<RulesPost> ExecuteAsync(CreatePostRequest request) { ... }
```

Utiliser `<inheritdoc/>` sur les implémentations d'interface.

---

## Patterns établis

### Décorateur Discord

`STS.Discord` enveloppe `ICreatePostUseCase`, `IUpdatePostUseCase`, `IDeletePostUseCase` via le pattern décorateur. Les endpoints de `STS.Api` ne sont pas modifiés.

**Important :** `AddDiscordBot()` doit être appelé **après** les enregistrements des use cases dans `Program.cs` (dépendance d'ordre du décorateur).

`DiscordMappingStore` persiste `sectionId → forumChannelId` et `postId → threadId` dans `discord-mappings.json`.

Quand `Discord:BotToken` est absent de la config : `NullDiscordPublisher` est injecté automatiquement.

### Thread safety dans l'API

`STS.Api` utilise `ReaderWriterLockSlim` pour la concurrence sur les repositories. **Ne jamais appeler une méthode qui acquiert le lock depuis l'intérieur d'un lock déjà détenu.** Utiliser une méthode interne privée sans lock pour les opérations imbriquées (sinon : deadlock).

### CachedDataSource dans STSPlugin

`CachedDataSource` doit être instancié via DI (pas `new RemoteJsonDataSource()` directement) pour que le fallback et le cache fonctionnent. Trois niveaux de fallback : remote → disk → bundled. La méthode `Load()` est idempotente.

### Auth dans STS.Admin

`AuthService` stocke le JWT dans localStorage. `ApiClient` injecte automatiquement le header Bearer et redirige sur 401.

---

## Pièges ImGui connus (STSPlugin)

- **`BeginTabBar` + drag-and-drop = crash.** Remplacer par un toggle `_activeTab` (int) géré manuellement.
- Deux bugs connus documentés, à corriger plus tard :
  1. Le bouton Roll contourne `plugin.StartRoll()`.
  2. Avantage/Désavantage non supporté correctement en mode GameRandom.

---

## Tests

- Framework : **xUnit + FluentAssertions**
- **Pas de framework de mock** — utiliser des fakes écrits à la main (`FakeRulesDataSource`, etc.)
- `STS.Domain.Tests` référence `STS.Api` directement pour tester `RulesRepository`
- Lancer les tests : `dotnet test`

---

## Docker & déploiement

- Déploiement sur OVH VPS via Docker ; reverse proxy Caddy (HTTPS Let's Encrypt)
- **Attention :** un volume Docker monté sur un fichier inexistant crée un répertoire. Pré-créer `rules.json` avec `[]` avant le premier démarrage.
- SSH sur le port **2222**, authentification par clé uniquement

---

## Blazor — pièges connus

- `section` est un mot-clé réservé dans les directives Razor — renommer toute variable portant ce nom.
- Pour du JS qui doit réagir à des éléments chargés dynamiquement, utiliser un `MutationObserver` (le DOM est prêt après le rendu Blazor, pas immédiatement).
- Le highlighter de syntaxe Chroma applique la classe `.go` dans les blocs de code — neutraliser avec `background: transparent !important`.

---

## CSS / Frontend

- Utiliser les custom properties CSS existantes : `--bg-surface`, `--border`, `--text-primary`, `--teal`, etc.
- Valider les changements CSS dans les DevTools du navigateur avant de committer.

---

## Workflow attendu

1. **Toujours demander le fichier actuel avant de patcher.** Travailler sur le fichier fourni, pas sur une version générée précédemment.
2. Poser des questions de clarification avant de coder sur des tâches non triviales.
3. Livraison incrémentale : modifier un fichier à la fois, tester entre chaque étape.
4. Output minimal et ciblé — ne pas régénérer le code environnant si ce n'est pas demandé.
