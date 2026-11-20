# SESSION MAP

> Ce fichier est maintenu par l'IA et mis à jour à chaque étape.
> Il représente l'état en temps réel de la session en cours.

---

## ✅ Aucune session en cours

**Dernière session close :** Retours pilote de Mark du 18/11 — sliders SoA / HL (DEC-053) et sets moteur multiples (DEC-054) — clôturée le 2026-11-20.

---

## Session close — Retours pilote de Mark : sliders SoA / HL + sets moteur multiples (DEC-053, DEC-054)

**Objectif :** traiter les deux points du mail de Mark du 18/11/26 — (1) sense of agency et human-likeness sur un slider 1-7 ancré, (2) plusieurs sets moteur actifs sur un même trial, réglables au dashboard.
**Démarrée le :** 2026-11-20 · **Clôturée le :** 2026-11-20 (code + docs ; câblage Unity, migration, build et déploiement à faire)

```
[██████░░░░] 4/6 tâches complétées
```

| # | Symbole | Tâche | État | Livré dans |
|---|---------|-------|------|------------|
| 1 | 🤖 | Lot 1 : `TrialQuestionsUI` — 4 questions sur le slider, ancres par question, radio supprimés | ✅ Fait | `TrialQuestionsUI.cs` |
| 2 | 🤖 | Lot 2 : spec (amendements MotorAdvice), DTO, `SanitizeBlockConfig`, `SessionManager`, `DrawMotorAdvice` (RNG `:multiset`), `ActiveSets`, écho CSV | ✅ Fait | `specs/MotorAdvice/*`, `FlowDataModels.cs`, `FlowController.cs`, `SessionManager.cs`, `TrialDrawResolver.cs`, `MotorAdviceController.cs`, `TrialManager.cs` |
| 3 | 🤖 | Backend : migration `20261120120000`, Zod, mapper, dashboard Motor Choice + astuce, export 117 → 119, docs (contrat, CSV, dashboard) | ✅ Fait | `backend-bugs` |
| 4 | 🤖 | Docs BUGS : DEC-053/054, Q-MOTOR-2, avancement, dictionnaire, matrice, config de test ; `BuildInfo.Version` 1.9.0 | ✅ Fait | `Docs/**`, `BuildInfo.cs`, `readme.md` |
| 5 | 👤 | Unity : câbler `_labelMin` / `_labelMax` (ProximalScene), supprimer `AnswerRow` (prefab `QuestionPanel`), tester en Play Mode | ⬜ À faire | — |
| 6 | 👤 | Appliquer la migration, déployer le backend, build WebGL 1.9.0, tester un export (trial mono-set, 2 sets, 3 sets, trial forcé) | ⬜ À faire | — |

---

## Session close — Motor Advice : export complet + log des touches (DEC-050)

**Objectif :** répondre aux 3 points du mail de Mark du 04/10/26 — set actif, set conseillé et log des touches dans le CSV.
**Démarrée le :** 2026-10-06 · **Clôturée le :** 2026-10-06 (code + docs ; build et déploiement à faire)

```
[████████░░] 4/5 tâches complétées
```

| # | Symbole | Tâche | État | Livré dans |
|---|---------|-------|------|------------|
| 1 | 🤖 | `motor_active_set` / `motor_advised_set` dans `TrialResponseRow` + `ApplyMotorAdviceState` | ✅ Fait | `FlowDataModels.cs`, `TrialManager.cs` |
| 2 | 🤖 | `player_press_log` : `PlayerKeyPress`, `ResolveMotorKey`, `ReportPressedKeys` → `OnKeyPressed` → `RecordKeyPress`, `ToKeyPressesJson` | ✅ Fait | `Data/PlayerKeyPress.cs`, `MotorAdviceController.cs`, `GridMover.cs`, `GameManager.cs`, `TrialManager.cs`, `FlowSerializationUtility.cs` |
| 3 | 🤖 | Backend : migration `20261006120000`, schéma Zod, ingestion, export CSV 113 → 116, docs | ✅ Fait | `backend-bugs` |
| 4 | 🤖 | Docs BUGS : TDD, spec fonc, dictionnaire, matrice, journal, DEC-050, Q-MOTOR-1, avancement ; `BuildInfo.Version` 1.5.0 | ✅ Fait | `Docs/**`, `BuildInfo.cs` |
| 5 | 👤 | Déployer backend + migration, recompiler, build WebGL 1.5.0, tester un export (trial libre, trial trompeur, log de touches) | ⬜ À faire | — |

---

## Session close — Motor Advice

**Objectif :** Tirer un set de touches par essai et afficher un advice probabiliste pour guider le joueur.
**Démarrée le :** 2026-03-12 · **Clôturée le :** 2026-07-28

```
[██████████] 5/5 tâches complétées
```

| # | Symbole | Tâche | État | Livré dans |
|---|---------|-------|------|------------|
| 1 | 🤖 | Params motor advice dans SessionManager + parsing CLI | ✅ Fait | `SessionManager.cs` (`motorAdviceVisibleProbability`, `motorAdviceReliableProbability`) |
| 2 | 🤖 | MotorAdviceController (tirage set + mapping input) | ✅ Fait | `Assets/Game/Scripts/Controllers/MotorAdviceController.cs` |
| 3 | 🤖 | Adapter GridMover pour lire le set actif | ✅ Fait | `GridMover.cs:113` — aucun fallback flèches |
| 4 | 👤 | Créer UI motor advice + hook scène | ✅ Fait | `Assets/Game/Scripts/UI/MotorAdviceUI.cs` |
| 5 | 👁️ | Validation manuelle (3 cas de proba) | ✅ Fait | — |

**Légende états :** ⬜ À faire · 🔄 En cours · ✅ Fait · ⏸️ En attente · ❌ Bloqué

### Écarts constatés à la clôture

- Les sets sont **ZQSD / TFGH / IJKL** (et non QZD/FTH/KOM comme dans `free-forced-choices/spec-fonc.md`) — le set OKLM a été remplacé par IJKL au commit `7b3a9e86`. Valeurs API : `QZD` / `FTH` / `JIL`. **MàJ 09/10/26 (DEC-051)** : sets renommés **WASD / TFGH / IJKL**, valeurs API `AWD` / `FTH` / `JIL` (nomenclature QWERTY).
- `MotorAdviceController` a été placé dans `Controllers/` (comme le prévoyait `multi-screen-flow/spec-tech.md`) et non dans `Systems/` (comme le prévoyait `MotorAdvice/spec-tech.md:183`).
- **Reste ouvert :** Q-002 (colonnes CSV motor advice, toujours `TBD` dans `MotorAdvice/spec-fonc.md:81-84`) et Q-004 (format d'affichage imposé du set).

---

## Contexte à transmettre à la prochaine session

- L'état réel du projet est décrit dans `Docs/project-state/revue-completude-2026-07-28.md` et `Docs/project-state/avancement.md`.
- Prochain chantier prioritaire : **Lot A — fiabilisation du pipeline de données** (cf. revue §7).
