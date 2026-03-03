# État d'avancement du projet

> Ce fichier est le résumé condensé de l'état du projet.
> Objectif : un rôle peut comprendre où en est le projet en lisant CE SEUL FICHIER.
> Mis à jour après chaque session qui fait avancer le projet.
> Dernière mise à jour : 2026-03-03

---

## Vue macro

**Avancement global estimé :** ~30-35%
**Phase actuelle :** Forest screen fonctionnel, architecture multi-écran en cours de spécification

---

## Ce qui est TERMINÉ et stable

| Composant | État | Depuis |
|:---|:---|:---|
| LevelRegistry (grille, flags, RNG) | ✅ Stable, documenté TDD v2.4 | 02/03/26 |
| GameManager (cycle round, pénalités, events) | ✅ Stable, documenté | 02/03/26 |
| SessionManager (Singleton, params recherche, CLI) | ✅ Stable, documenté | 02/03/26 |
| FogController (texture masque, révélation) | ✅ Stable, documenté | 17/02/26 |
| GridMover (mouvement joueur, input) | ✅ Stable, documenté | 27/02/26 |
| BugCloudSpawner (placement, ratios, gap) | ✅ Stable, documenté | 02/03/26 |
| PathSpawner (chemins, advisor, probabiliste) | ✅ Stable, documenté | 02/03/26 |
| CorridorWallsGenerator (maze DFS, murs) | ✅ Stable, documenté | 27/02/26 |
| TrapSpawner (pièges, IsFreeForTrap) | ✅ Stable, documenté | 02/03/26 |
| PlayerSpawner (instanciation, RegisterPlayerStart) | ✅ Stable, documenté | 27/02/26 |
| TilesSpawner (grille runtime, originWorld) | ✅ Stable, documenté | 19/02/26 |
| TrialManager (pipeline données, API) | ✅ Stable, documenté | 02/03/26 |
| RoundUI (panneau fin de round) | ✅ Stable, documenté | 02/03/26 |
| Research Parameter Pipeline (CLI → SessionManager → Spawners) | ✅ Stable, documenté | 02/03/26 |
| Step Budget Penalty (dépassement distance Manhattan) | ✅ Stable, documenté | 02/03/26 |
| Seeded RNG (FNV-1a, reproductibilité) | ✅ Stable, documenté | 27/02/26 |
| TDD v2.5 + diagrammes Mermaid architecture | ✅ À jour | 03/03/26 |

---

## Ce qui est EN COURS

| Chantier | État | Livrable | Prochaine étape |
|:---|:---|:---|:---|
| Multi-écran flow | Spec hybride produite (Plan_Implementation_MultiScreen_Flow_v1) | Plan en cours | Produire spec fonc + spec tech séparées avec les templates Rôle 2/3 |
| Système de rôles projet | Migration vers Claude Code | Fichiers en cours de production | Finaliser CLAUDE.md + valider la structure |

---

## Ce qui RESTE à faire (9 gaps identifiés)

### Criticité haute
1. **Multi-screen flow** — FlowController, enchaînement des écrans, SceneManager vs état interne
2. **Advisor system** — Écran advisor, types de conseils, fiabilité configurable, données CSV
3. **Score & feedback** — Cumul inter-trial (?), écran résultat, récompense visible

### Criticité moyenne
4. **Consent screen** — Écran consentement éclairé (IN scope, DEC-004)
5. **Instructions & tutorial** — Écrans d'explication du jeu
6. **Meta-choice** — Écran de confirmation/changement de stratégie (1x/bloc, DEC-001)

### Criticité basse
7. **Mountain UI** — Visualisation progression dans le bloc (DEC-003)
8. **End-of-block summary** — Écran récapitulatif entre blocs
9. **Redirect** — Redirection vers questionnaire externe (DEC-005)

---

## Décisions en attente

| Réf | Sujet | Bloque quoi | Urgence |
|:---|:---|:---|:---|
| DEC-006 | Score cumulé ou remis à zéro entre trials | Score & feedback, Mountain UI | 🟡 Bientôt |

---

## Risques actifs

| Risque | Impact | Statut |
|:---|:---|:---|
| State leaking entre trials dans le flow multi-écran | Données recherche corrompues | À couvrir dans la spec tech |
| WebGL + System.Environment.GetCommandLineArgs | Potentiellement non fonctionnel | À valider avec le dashboard |
| Données perdues si fermeture navigateur avant SendTrials | Trials en mémoire perdus | Connu, pas de solution en place |
