# STS — Roadmap

## 🐛 Bugs connus

### Plugin Dalamud
- [ ] Les boutons de roll bypassent `StartRoll()`/`StartReroll()` → toujours en mode Internal RNG
- [ ] Advantage/Disadvantage non supporté en mode GameRandom (second jeu de dés jamais sourcé depuis le jeu)

### STS.Web
- [ ] Session perdue au refresh de page (pas de localStorage/cookie)

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
- [ ] `RemoteCharacterRepository` — synchronisation fiches locales ↔ API
- [ ] Auth du plugin vers l'API (token joueur)
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