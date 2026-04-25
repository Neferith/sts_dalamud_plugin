# STS — Roadmap

## 🐛 Bugs connus sur le plugin dalamud
- [ ] Les boutons de roll bypassent `StartRoll()`/`StartReroll()` → toujours en mode Internal RNG
- [ ] Advantage/Disadvantage non supporté en mode GameRandom (second jeu de dés jamais sourcé depuis le jeu)

## 🗃️ Données
- [ ] Corrections des incohérences dans `data.json` (surfacées via l'admin UI)
- [ ] Migration des données de référence (jobs / traits / capacités) vers SQLite
- [ ] Migration des données de home (personnages, fiches) vers SQLite

## 🐳 Infrastructure
- [✅] Configuration Docker (API + Admin + Discord bot)
- [✅] Déploiement sur VPS OVH + nom de domaine

## 🔌 Plugin Dalamud
- [✅] `RemoteJsonDataSource` + `CachedDataSource` — consommation de l'API plutôt que le JSON local
- [ ] Auth du plugin vers l'API (token joueur ou token de guilde)
- [ ] 🖼️ Portrait capture — détection GPOSE via `ICondition` (déféré)

## 👤 Authentification & Utilisateurs
- [ ] Connexion joueur sur le site (roles : admin / member / npc / …)

## 📋 Fiches personnages
- [ ] Création de fiches persos sur le site (équivalent du plugin Dalamud)
- [ ] Synchronisation des fiches persos → Discord
- [ ] Synchronisation des fiches persos → Plugin Dalamud

## 📡 Synchronisation Discord
- [ ] Synchronisation des données de référence (jobs / traits / capacités) → Discord

## 🎲 Gameplay (à affiner)
- [ ] Historique des jets par personnage / session
- [ ] Notion de session active avec GM désigné

## 🔔 Temps réel (à étudier)
- [ ] Notification Plugin ← Discord/Site lors d'un jet distant

## 🧪 Technique transverse
- [ ] Tests unitaires / intégration pour les nouvelles couches SQLite
- [ ] Tests pour la couche auth
- [ ] Mise à jour du `CLAUDE.md` à chaque évolution majeure
