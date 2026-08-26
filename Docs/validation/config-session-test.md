# Config des sessions de test — couverture des conditions

> **But** : définir la/les session(s) Supabase à créer pour que les tests exercent **toutes** les conditions expérimentales au moins une fois (Axe 3).
> **Principe** : plutôt qu'une session « réaliste », construire des sessions **de couverture** où chaque bloc isole une manipulation, plus au moins une session « réaliste » de bout-en-bout.

---

## 1. Matrice de couverture (chaque case doit être observée ≥ 1× dans le CSV)

| Manipulation                                                                                         | Niveaux à couvrir                                                                   | Bloc(s) dédié(s) |          Observé ✅           |
| :--------------------------------------------------------------------------------------------------- | :---------------------------------------------------------------------------------- | :--------------- | :---------------------------: |
| Advisor (méta)                                                                                       | `human`, `robot`, `none`                                                            |                  |                               |
| Advisor forced                                                                                       | forced=false ; forced=true × {human, robot, none}                                   |                  |                               |
| Distal forced                                                                                        | false ; true (optimal / non-optimal)                                                |                  |                               |
| Proximal forced                                                                                      | proba 0 ; proba 1 (optimal / non-optimal)                                           |                  |                               |
| Motor forced                                                                                         | proba 0 ; proba 1 × set {QZD, FTH, JIL} ⚠️ `JIL` remplace `KOM` (commit `7b3a9e86`) |                  |                               |
| Distal advice visible                                                                                | false ; true                                                                        |                  |                               |
| Distal advice reliable                                                                               | false ; true                                                                        |                  |                               |
| Motor advice visible                                                                                 | proba 0 ; 1                                                                         |                  |                               |
| Motor advice reliable                                                                                | proba 0 ; 1                                                                         |                  |                               |
| Explanation display_mode (×3 advices)                                                                | `none`, `forced`, `opt-in`                                                          |                  |                               |
| Explanation content_variant                                                                          | `short`, `long`                                                                     |                  |                               |
| Explanation opt-in clic                                                                              | cliqué / non cliqué (⇒ `_clicked`, `_display_duration_ms`)                          |                  |                               |
| **Explanation `display_probability`** (proximal, motor **uniquement** — le distal n'a pas ce tirage) | 0 ; 1                                                                               |                  | ⚠️ ajouté le 28/07/26 (D-013) |
| Path visible                                                                                         | proba 0 ; 1                                                                         |                  |                               |
| Suboptimal path                                                                                      | proba 0 ; 1                                                                         |                  |                               |
| Detour                                                                                               | proba 0 ; 1                                                                         |                  |                               |
| Fog                                                                                                  | proba 0 ; 1                                                                         |                  |                               |
| Suboptimal traps                                                                                     | proba 0 ; 1 (min/max)                                                               |                  |                               |
| `show_numerical_feedback`                                                                            | true ; false                                                                        |                  |                               |
| Tutorial                                                                                             | is_tutorial=true (⇒ **aucune ligne** dans le CSV)                                   |                  |                               |

> Astuce : mettre les probabilités à **0 ou 1** dans les blocs de couverture rend le résultat **déterministe** et donc vérifiable. Garder les valeurs intermédiaires pour la session réaliste.

---

## 2. Sessions à créer

### Session A — « couverture déterministe »

- Un bloc par manipulation isolée, probabilités forcées à 0/1, seed fixe.
- Objectif : valider la présence et l'exactitude de **chaque** colonne (Axes 1–3).

### Session B — « réaliste »

- Paramètres proches de l'expé réelle (probabilités intermédiaires, nb de trials cible — cf. Q-012), plusieurs blocs.
- Objectif : bout-en-bout (Axe 5), volumétrie, échappement CSV.

### Session C — « repro »

- = Session A avec **le même seed**, rejouée 2×.
- Objectif : reproductibilité (Axe 6).

---

## 3. Paramètres à renseigner par bloc (BlockConfig)

Champs pilotant les colonnes (source : `BlockConfig` lu par `TrialManager.BuildBaseRow` / `ApplyForcedChoiceState`) :

`advisor_forced`, `advisor_forced_value`, `distal_forced`, `distal_forced_optimal_probability`,
`proximal_forced_probability`, `proximal_forced_optimal_probability`,
`motor_forced_probability`, `motor_forced_set`,
`distal_advice_visible_probability`, `distal_advice_reliable_probability`, `show_numerical_feedback`,
`distal_scene`, `trial_count`, `is_tutorial`, `block_order`, `is_order_locked`, `config_fingerprint`,

- paramètres map (`trap_count`, `min/max_distance`, `min/max_total_bugs`, `min/max_green_ratio`, `gap_min/max`, `fog_probability`, `path_visible_probability`, `suboptimal_path_probability`, `detour_probability`, `motor_advice_visible/reliable_probability`, `suboptimal_trap_probability`, `min/max_suboptimal_traps`)
- config explanations (display_mode / content_variant / corpus text_id **et `display_probability`** par advice).

> ⚠️ **`display_probability`** (`AdviceExplanationConfig`, `FlowDataModels.cs:286`) est un
> tirage **par trial** décidant si l'explanation proximale ou motrice s'affiche. Il **contredit
> TR7** de `specs/explanations-short-long/spec-fonc.md:196` (« granularité blockwise ») et
> n'était documenté **nulle part** avant le 28/07/26 (D-013). Le mettre à **0 ou 1** dans les
> blocs de couverture, sans quoi la présence des colonnes `*_advice_explanation_*` devient
> non déterministe. Le distal n'a pas ce tirage : son explanation est résolue au niveau bloc.

> ⚠️ Vérifier la **précédence** `SessionManager` vs `FlowController.ActiveMapConfig` (Axe 6) : si `SessionManager.Instance` est présent au runtime, il peut écraser la config du bloc. S'assurer que la config Supabase est bien celle appliquée.

---

## 4. Lancement

- Éditeur : renseigner `_editorSessionId` (fallback) ou passer `sessionId=<id>` en argument.
- Build/WebGL : `sessionId=<id>` via argument / URL.
- Consigner le `sessionId` et le `trial_seed` utilisés pour chaque run dans `protocole-sessions-test.md`.
