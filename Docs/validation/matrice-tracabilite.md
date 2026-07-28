# Matrice de traçabilité — `TrialResponseRow` (code) ↔ CSV exporté

> **Rôle** : opposer, champ par champ, ce que le **code émet** (source de vérité) à ce que le **CSV** livre au chercheur.
> **Colonnes pré-remplies** (côté code) : à partir de `Assets/Game/Scripts/Data/FlowDataModels.cs:439-521`, `TrialManager.cs`, `GameManager.cs`. **NE PAS les modifier** sans revérifier le code.
> **Colonnes à remplir par l'équipe** (Axe 1, gate G4) : `CSV ?` et `Statut`.
>
> Légende `CSV ?` : ✅ présente & même nom · 🔁 présente mais **renommée** (préciser) · ❌ **absente** · ⚠️ présente mais type/encodage différent.
> Légende `Statut` : OK · DIVERGENCE (→ reporter dans `journal-divergences.md`) · À VÉRIFIER.
>
> ⚠️ **Sérialisation** (`ApiClient.cs:539` `ToJsonObjectSkippingNullStrings`) : un champ **null** est **omis du JSON** SAUF s'il porte `[IncludeNullInJson]`. Les champs marqués « omis si null » ci-dessous **peuvent disparaître** de certains trials → vérifier que la colonne CSV existe quand même (valeur vide) pour toutes les lignes. Nombres sérialisés en `InvariantCulture` (point décimal), bool en `true`/`false`.

---

## Identification & session

| Champ (code) | Type | Domaine / valeurs | Peuplé par | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-- | :-: | :-: | :-- |
| `participant_id` | string | UUID | `BuildBaseRow` (flow.State ou `Guid.NewGuid`) | non | | |
| `session_template_id` | string | id session | `BuildBaseRow` | non | | |
| `session_name` | string | nom de la session | `SessionConfig.label` via `BuildBaseRow` | omis si null | | |
| `build_version` | string | ex. `0.3.0-flow` | `SessionManager`/`FlowController` | non | | |
| `block_template_id` | string | id template de bloc | `block.block_template_id` (DEC-022) | omis si null | | ⚠️ ajouté au tableau le 28/07/26 (D-014) |
| `block_index` | int | ≥ 1 (1-based) | `BuildBaseRow` | non | | |
| `trial_index` | int | ≥ 1 (1-based) | `BuildBaseRow` | non | | |
| `trial_count` | int | nb trials du bloc | `block.trial_count` | non | | |
| `is_tutorial` | bool | true/false | `block.is_tutorial` | non | | |

## Choix (free) & choix forcés

| Champ (code) | Type | Domaine / valeurs | Peuplé par | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-- | :-: | :-: | :-- |
| `advisor_choice` | string | `human`/`robot`/`none` | `flow.State.advisor_choice` | non | | |
| `valley_choice` | string | `A`/`B`/null | `flow.State.valley_choice` | **oui** | | |
| `advisor_forced` | bool | true/false | block ou flow.State | non | | |
| `advisor_forced_value` | string | `none`/`human`/`robot` | block/flow.State | **oui** | | |
| `distal_forced` | bool | true/false | block/flow.State | non | | |
| `distal_forced_optimal_probability` | float | [0,1] | block/flow.State | non | | |
| `proximal_forced_probability` | float | [0,1] | block/flow.State | non | | |
| `proximal_forced_optimal_probability` | float | [0,1] | block/flow.State | non | | |
| `motor_forced_probability` | float | [0,1] | block/flow.State | non | | |
| `motor_forced_set` | string | `QZD`/`FTH`/`JIL` ⚠️ (ex-`KOM`) | block/flow.State | **oui** | | |

## Config map (paramètres expérimentaux)

> ⚠️ **Précédence** (`BuildBaseRow`) : si `SessionManager.Instance` existe, ses valeurs **priment** sur `FlowController.ActiveMapConfig`. À confirmer (Axe 6) : laquelle pilote réellement le trial.

| Champ (code) | Type | Domaine / valeurs | Peuplé par | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-- | :-: | :-: | :-- |
| `trap_count` | int | ≥ 0 | session/map | non | | |
| `min_distance` | int | ≥ 1 (Manhattan) | session/map | non | | |
| `max_distance` | int | > min | session/map | non | | |
| `min_total_bugs` | int | ex. 20 | session/map | non | | |
| `max_total_bugs` | int | ex. 80 | session/map | non | | |
| `min_green_ratio` | float | [0,1] | session/map | non | | |
| `max_green_ratio` | float | [0,1] | session/map | non | | |
| `gap_min` | float | ≥ 0 | session/map | non | | |
| `gap_max` | float | ≥ 0 | session/map | non | | |
| `fog_probability` | float | [0,1] | session/map | non | | |
| `trial_seed` | long | 0 … 2^31-1 | session.randomizationSeed **ou** flow.CurrentTrialSeed | non | | ⚠️ voir Axe 6 |
| `path_visible_probability` | float | [0,1] | session/map | non | | |
| `suboptimal_path_probability` | float | [0,1] | session/map | non | | |
| `detour_probability` | float | [0,1] | session/map | non | | |
| `motor_advice_visible_probability` | float | [0,1] | session/map | non | | |
| `motor_advice_reliable_probability` | float | [0,1] | session/map | non | | |
| `suboptimal_trap_probability` | float | [0,1] | session/map | non | | |
| `min_suboptimal_traps` | int | ≥ 0 | session/map | non | | |
| `max_suboptimal_traps` | int | ≥ min | session/map | non | | |
| `map_config` | string (JSON) | `{gridWidth,gridHeight,leftCloud{x,y,totalBugs,greenRatio},rightCloud{…}}` | `SetMapConfig`/`SetMapConfigJson` | **oui** (edge) | | |
| `optimal_path_length` | int | ≥ 0 | `SetOptimalPathLength` | non | | |
| `cloud_distance` | int | Manhattan joueur→nuages | `SetCloudDistance` | non | | |
| `show_numerical_feedback` | bool | true/false | `block.show_numerical_feedback` | non | | |

## Advice distal

| Champ (code) | Type | Domaine / valeurs | Peuplé par | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-- | :-: | :-: | :-- |
| `distal_advice_visible_probability` | float | [0,1] | block | non | | |
| `distal_advice_reliable_probability` | float | [0,1] | block | non | | |
| `distal_advice_visible` | bool | true/false | flow.State | non | | |
| `distal_advice_reliable` | bool | true/false | flow.State | non | | |
| `distal_advice_choice` | string | `left`/`right`/null | flow.State | **oui** | | |
| `distal_best_valley` | string | `left`/`right`/null | flow.State | **oui** | | |
| `distal_scan_choice` | string | choix scan/null | flow.State | **oui** | | |
| `distal_scene` | objet JSON | `DistalSceneConfig` (min/max bugs, green ratio, gap) | `block.distal_scene` | non | | |

## Explanations (× distal / motor / proximal)

> Structure identique pour les 3 advices. `display_mode` défaut = `none` (jamais null). Les 4 champs de contenu portent `[IncludeNullInJson]` → **toujours présents** (null si non applicable).

| Champ (code) | Type | Domaine / valeurs | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-: | :-: | :-- |
| `distal_advice_explanation_display_mode` | string | `forced`/`opt-in`/`none` | non | | |
| `distal_advice_explanation_content_variant` | string | `short`/`long`/null | non (`[IncludeNull]`) | | |
| `distal_advice_explanation_text_id` | string | id chercheur/null | non (`[IncludeNull]`) | | |
| `distal_advice_explanation_clicked` | bool? | true/false/null | non (`[IncludeNull]`) | | |
| `distal_advice_explanation_display_duration_ms` | int? | ms/null | non (`[IncludeNull]`) | | |
| `motor_advice_explanation_display_mode` | string | `forced`/`opt-in`/`none` | non | | |
| `motor_advice_explanation_content_variant` | string | `short`/`long`/null | non (`[IncludeNull]`) | | |
| `motor_advice_explanation_text_id` | string | id/null | non (`[IncludeNull]`) | | |
| `motor_advice_explanation_clicked` | bool? | true/false/null | non (`[IncludeNull]`) | | |
| `motor_advice_explanation_display_duration_ms` | int? | ms/null | non (`[IncludeNull]`) | | |
| `proximal_advice_explanation_display_mode` | string | `forced`/`opt-in`/`none` | non | | |
| `proximal_advice_explanation_content_variant` | string | `short`/`long`/null | non (`[IncludeNull]`) | | |
| `proximal_advice_explanation_text_id` | string | id/null | non (`[IncludeNull]`) | | |
| `proximal_advice_explanation_clicked` | bool? | true/false/null | non (`[IncludeNull]`) | | |
| `proximal_advice_explanation_display_duration_ms` | int? | ms/null | non (`[IncludeNull]`) | | |

## Résultats du trial

| Champ (code) | Type | Domaine / valeurs | Peuplé par | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-- | :-: | :-: | :-- |
| `proximal_choice` | string | `left`/`right`/`unknown` | `EndCurrentTrial` | non | | |
| `optimal_path_visible` | bool | true/false | `EndCurrentTrial` | non | | |
| `path_is_suboptimal` | bool | true/false | `EndCurrentTrial` | non | | |
| `choice_correct` | bool | true/false | `EndCurrentTrial` | non | | |
| `true_cloud` | string | `left`/`right`/`none` | `EndCurrentTrial` | non | | |
| `green_bugs_collected` | int | ≥ 0 | `EndCurrentTrial` | non | | |
| `green_bugs_accumulated` | int | ≥ 0 (cumul bloc) | `FlowController.GetAccumulatedScoreAfterTrial` | non | | |
| `traps_hit` | int | ≥ 0 | `EndCurrentTrial` | non | | |
| `steps` | int | ≥ 0 | `EndCurrentTrial` | non | | |
| `overtime_steps` | int | ≥ 0 | `EndCurrentTrial` | non | | |
| `followed_advisor_path` | bool | true/false (chemin **affiché**) | `EndCurrentTrial` | non | | |
| `player_path_log` | string (JSON array) | `[{x,y,t}]` t=ISO UTC | `ToPlayerStepsJson` | non | | |

## Questionnaire & timestamps

| Champ (code) | Type | Domaine / valeurs | Peuplé par | Omis si null | CSV ? | Statut |
| :-- | :-- | :-- | :-- | :-: | :-: | :-- |
| `acceptability_question` | string | réponse (obligatoire) | `SubmitCurrentTrialResponses` | non | | |
| `sens_of_agency_question` | string | réponse (obligatoire) | `SubmitCurrentTrialResponses` | non | | |
| `human_likeness_question` | string | réponse — envoyée en **PATCH** (null au POST) | `QueueHumanLikenessPatchForBlock` | **oui au POST** | | ⚠️ voir Axe 4 |
| `started_at` | string | ISO 8601 UTC (`"o"`) | `StartNewTrial` | non | | |
| `ended_at` | string | ISO 8601 UTC (`"o"`) | `EndCurrentTrial` | non | | |

---

## Synthèse divergences (à compléter après G4)

| # | Champ concerné | Type de divergence | Détail | Sévérité | Action |
| :- | :-- | :-- | :-- | :-- | :-- |
| | | | | | |

> Total champs code = **82** (décompte vérifié le 28/07/26 sur `FlowDataModels.cs:461-543` ;
> l'ancienne valeur de 80 précédait l'ajout de `session_name` et `block_template_id`).
> Reporter chaque ligne `❌`/`🔁`/`⚠️` ici et dans `journal-divergences.md`.

---

## Troisième axe — confrontation au CSV **attendu par le client**

> Ajouté le 28/07/2026. **Cette matrice compare le code au CSV *exporté*. Elle ne comparait
> pas le code au CSV *attendu*** — c'est-à-dire à `Docs/references/CSV_BUGS_Output_V1.xlsx`,
> les 76 colonnes demandées par le chercheur. `Docs/roles/analyse-fonctionnelle.md` prévoyait
> pourtant cette matrice « comportement → colonne CSV V1 » ; elle n'avait jamais été produite.

La confrontation complète **colonne client → champ code** (76 lignes, avec statut
✅ / 🔁 renommé / ⚠️ dérivable / ❌ absent / 🟢 couvert ailleurs) est dans
**[`Docs/project-state/revue-couverture-2026-07-28.md` §2](../project-state/revue-couverture-2026-07-28.md)**.
Elle n'est pas dupliquée ici pour éviter deux sources de vérité divergentes.

**Résultat en un coup d'œil**

| | Nombre |
| :-- | :-: |
| Colonnes attendues par le client | 76 |
| Champs émis par le code | 82 |
| Correspondances de **nom exact** | 10 |
| Renommages à équivalence sémantique | ~16 |
| Colonnes client **absentes** du code | **29** |
| Champs code **absents** du template client (évolution DEC-017/018/019/022, légitime) | ~30 |

**Les 29 absences, par famille**

| Famille | Colonnes | Réf |
| :-- | :-: | :-- |
| `visibility_noise` (feature inexistante) | 7 | F1 / Q-013 |
| Stimulus distal réalisé (`*_valley_*_bugs_nb`) | 5 | **D-009** / Q-DISTAL-1 |
| Niveau moteur (set actif, affiché, donné, choix) | 4 | **D-008** / Q-MOTOR-1 |
| Adhérence au conseil (`*_match_advice`) | 3 | **D-007** |
| Granularité de ligne (`screen_type`, `screen_id`) | 2 | Q-ROW-1 |
| Conseil proximal (cible et chemin affichés) | 2 | — |
| Identification (`username`, `game_session_id`) | 2 | — |
| Bornes de pièges (`min/max_traps_nb`) | 2 | — |
| `Trust in Technology Questionnaire` | 1 | **D-010** / Q-TRUST-1 |
| `proximal_advice_reliability` (paramètre nommé) | 1 | — |

> ⚠️ **Lecture équitable.** Le template CSV V1 date de mars 2026 et précède DEC-017, DEC-018,
> DEC-019 et DEC-022. Une bonne part de l'écart est une **évolution légitime du design**.
> Le problème n'est pas que le code diverge — c'est que **l'écart n'a jamais été acté**,
> ni côté renommages (le chercheur ne sait pas que `final_reward` s'appelle désormais
> `green_bugs_collected`), ni côté absences.

**À faire en Axe 1 / gate G4** : compléter cette confrontation en même temps que les colonnes
`CSV ?` / `Statut` ci-dessus, et faire signer au chercheur la table des renommages — c'est
elle qui rend le CSV lisible sans le code sous les yeux.
