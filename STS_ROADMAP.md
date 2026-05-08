# STS — Roadmap

## 🐛 Bugs connus

### Plugin Dalamud
- [ ] Les boutons de roll bypassent `StartRoll()`/`StartReroll()` → toujours en mode Internal RNG
- [ ] Advantage/Disadvantage non supporté en mode GameRandom (second jeu de dés jamais sourcé depuis le jeu)

### STS.Web
- [ ] Session perdue au refresh de page (pas de localStorage/cookie)

---

## 🎯 V1 — Features en attente

Objectif : fiche personnage complète et utilisable en event.

- [- [x] `RemoteJsonDataSource` + `CachedDataSource` — consommation de l'API
] **Image de personnage** — upload depuis STS.Web et STS.Admin, affichée sur la fiche
- [ ] **Stats visibles** — rang, palier, rerolls, réputation, avoirs de compagnie
- [ ] **Capacités par rang** — liste rang 1, rang 2, rang 3 avec descriptions
- [ ] **Traits** — liste des traits équipés avec descriptions (déjà partiellement présent)
- [ ] **Possessions uniques** — liste dédiée (distincte de l'inventaire général)
- [ ] **Export depuis STS.Admin** — PDF, Markdown, JSON par fiche
- [ ] **Amélioration visuelle STS.Web** — rendu fiche plus riche, mise en page soignée
- [ ] **Session persistante STS.Web** — localStorage pour le JWT
- [ ] **Vue fiches dans STS.Admin** — liste de toutes les fiches + modération (certifications, rang)

---

## 🗃️ Données
- [ ] Corrections des incohérences dans `data.json` (surfacées via l'admin UI)
- [ ] Migration `users.json` → SQLite
- [ ] Migration `characters.json` → SQLite
- [ ] Migration `quick-links.json` + `site-settings.json` → SQLite
- [ ] Migration des données de référence (jobs / traits / capacités) → SQLite

---

## 🐳 Infrastructure
- [x] Configuration Docker (API + Admin + Discord bot)
- [x] Déploiement sur VPS OVH + nom de domaine
- [ ] Environnement de staging / review

---

## 🔌 Plugin Dalamud
- [x] `RemoteJsonDataSource` + `CachedDataSource` — consommation de l'API
- [x] `LocalCharacterRepository` async — fiches personnages locales
- [x] Use cases character async — pattern cache UI pour le render thread ImGui
- [x] `RemoteCharacterRepository` — synchronisation fiches depuis l'API, filtre sur `UserId`
- [x] Auth du plugin vers l'API (`ILoginUseCase`, `ILogoutUseCase`, `IGetTokenUseCase`, `AuthState`)
- [ ] 🖼️ Portrait capture — détection GPOSE via `ICondition` (déféré)

---

## 👤 Authentification & Utilisateurs
- [x] Auth unifiée admin/member (`POST /api/auth/login`, JWT avec rôle)
- [x] Seed admin au démarrage depuis `appsettings`
- [x] Gestion des utilisateurs dans STS.Admin (créer, reset mot de passe, supprimer)
- [x] Login modal dans STS.Web (`AuthService` avec parsing JWT)
- [ ] Cookie/localStorage — persister le JWT + username après refresh (à minima `localStorage.setItem("sts_token", token)` dans `AuthService.LoginAsync`)
- [ ] Modération officier — rôle intermédiaire entre admin et member

---

## 📋 Fiches personnages
- [x] Modèle `Character` avec `UserId` (rétrocompat plugin)
- [x] API CRUD personnages avec contrôle d'accès (propriétaire / admin)
- [x] Liste et détail des fiches sur STS.Web (données de référence résolues)
- [x] Création de fiche sur STS.Web (1 max pour member, 8 pour admin)
- [x] Édition complète sur STS.Web (traits, compétences, certifications, inventaire)
- [ ] Synchronisation des fiches persos → Discord
- [ ] Synchronisation des fiches persos → Plugin Dalamud (RemoteCharacterRepository)
- [ ] Vue fiches dans STS.Admin — liste de toutes les fiches + modération (ajout/retrait de certifications, modification du rang par un officier)

---

## 📡 Synchronisation Discord
- [ ] Synchronisation des données de référence (jobs / traits / capacités) → Discord

---

## 🎲 Gameplay (à affiner)
- [ ] Historique des jets par personnage / session
- [ ] Notion de session active avec GM désigné

---

## 🔔 Temps réel (à étudier)
- [ ] Notification Plugin ← Discord/Site lors d'un jet distant

---

## 🧪 Technique transverse
- [ ] Tests pour la couche auth (User, PlayerRepository)
- [ ] Tests pour CharacterRepository
- [ ] Tests pour HttpDataSource
- [ ] Mise à jour du `CLAUDE.md` à chaque évolution majeure