# État d'avancement du projet

> Ce fichier est le résumé condensé de l'état du projet.
> Objectif : un rôle peut comprendre où en est le projet en lisant CE SEUL FICHIER.
> Mis à jour après chaque session qui fait avancer le projet.
> Dernière mise à jour : 2026-03-16

---

## Vue macro

**Avancement global estimé :** ~40%
**Phase actuelle :** Architecture multi-écran finalisée (spec tech + data model + responsabilités systèmes). Prêt pour implémentation.

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
| **Spec tech multi-écran flow** | ✅ Finalisée | 16/03/26 |
| **Data model Supabase** | ✅ Finalisé (table plate `trial_responses`) | 16/03/26 |
| **Matrice responsabilités systèmes** | ✅ Documentée dans spec-tech | 16/03/26 |
| **Décisions DEC-001 à DEC-015** | ✅ Enregistrées | 16/03/26 |

---

## Ce qui est EN COURS

| Chantier | État | Livrable | Prochaine étape |
|:---|:---|:---|:---|
| Multi-écran flow | Spec tech finalisée, architecture validée | `Docs/specs/multi-screen-flow/spec-tech.md` | Implémenter FlowController + ApiClient |

---

## Ce qui RESTE à faire

### Criticité haute
1. **FlowController** — Singleton DDOL, porte config + state, orchestre transitions (spec prête)
2. **ApiClient** — Singleton DDOL, HTTP unique : fetch config, POST trial, PATCH questionnaire (spec prête)
3. **FadeTransition** — Composant DDOL pour fade noir (DEC-002)
4. **SessionManager refonte** — Façade locale, lit depuis FlowController.ActiveMapConfig (DEC-009)
5. **TrialManager refonte** — Assemble TrialResponseRow, passe à ApiClient (plus de HTTP propre)
6. **Scènes UI légères** — Welcome, Consent, Advisor, Distal, Questionnaire, End (pattern commun documenté)

### Criticité moyenne
7. **Advisor system** — Écran advisor, types de conseils, fiabilité configurable
8. **Distal screen** — UI choix de vallée + preview floue (rendu à définir)
9. **Questionnaire in-game** — 2-3 questions par bloc, PATCH sur dernier trial (DEC-013)
10. **Tutorial** — BlockConfig spécial `is_tutorial=true`, config hardcodée (DEC-014)

### Criticité basse
11. **Mountain UI** — Visualisation progression dans le bloc (DEC-003)
12. **End-of-block summary** — Écran récapitulatif entre blocs
13. **Redirect** — Redirection vers questionnaire externe (DEC-005)

---

## Décisions en attente

| Réf | Sujet | Bloque quoi | Urgence |
|:---|:---|:---|:---|
| — | Preview floue distal screen | Rendu: image statique vs dynamique Unity | 🟢 Peut attendre le design |

---

## Risques actifs

| Risque | Impact | Statut |
|:---|:---|:---|
| State leaking entre trials dans le flow multi-écran | Données recherche corrompues | ✅ Couvert par spec tech (DDOL vs scene-scoped) |
| WebGL + System.Environment.GetCommandLineArgs | Potentiellement non fonctionnel | ✅ Résolu par DEC-009 (URL param + API) |
| Données perdues si fermeture navigateur avant SendTrials | Trials en mémoire perdus | ⚠️ Atténué : 1 POST par trial (pas de batch) |
