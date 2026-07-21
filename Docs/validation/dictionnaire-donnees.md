# Dictionnaire de données (codebook) — table `trial_responses`

> **Public** : chercheurs. **But** : permettre l'analyse en autonomie.
> **Granularité** : 1 ligne = 1 trial **non-tutorial** (les trials `is_tutorial=true` ne sont pas envoyés — DEC-014).
> **Clé** : (`participant_id`, `block_index`, `trial_index`).
> **Aligné sur le code** (`TrialResponseRow`). Encodage : nombres à point décimal, booléens `true`/`false`, timestamps ISO 8601 UTC.
> **Statut** : à valider avec le chercheur (gate G5).

---

## Identification & session

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `participant_id` | string | UUID | Identifiant participant (généré côté client). Sert de clé de regroupement. |
| `session_template_id` | string | id | Modèle de session (config chargée depuis l'API). |
| `session_name` | string | texte | Nom de la session, recopié depuis la clé `label` de la configuration. |
| `build_version` | string | ex. `0.3.0-flow` | Version du build ayant produit la donnée. |
| `block_index` | int | ≥ 1 | Numéro du bloc dans la session (1-based). |
| `trial_index` | int | ≥ 1 | Numéro du trial dans le bloc (1-based). |
| `trial_count` | int | ≥ 1 | Nombre total de trials prévus dans le bloc. |
| `is_tutorial` | bool | — | Toujours `false` dans l'export (les tutorials ne sont pas envoyés). |

## Choix du participant & choix forcés (design free/forced)

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `advisor_choice` | string | `human`/`robot`/`none` | Type d'advisor (méta-choix, 1×/bloc). |
| `valley_choice` | string | `A`/`B`/∅ | Choix de vallée (distal). Vide si non applicable. |
| `advisor_forced` | bool | — | Le méta-choix advisor était-il imposé ? |
| `advisor_forced_value` | string | `none`/`human`/`robot`/∅ | Valeur imposée si `advisor_forced`. |
| `distal_forced` | bool | — | Le choix distal était-il imposé ? |
| `distal_forced_optimal_probability` | float | [0,1] | Probabilité que l'imposition distale soit optimale. |
| `proximal_forced_probability` | float | [0,1] | Probabilité d'imposer le choix proximal (par trial). |
| `proximal_forced_optimal_probability` | float | [0,1] | Probabilité que l'imposition proximale soit optimale. |
| `motor_forced_probability` | float | [0,1] | Probabilité d'imposer le set moteur (par trial). |
| `motor_forced_set` | string | `QZD`/`FTH`/`KOM`/∅ | Set de touches imposé si applicable. |

## Paramètres expérimentaux de la map

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `trap_count` | int | ≥ 0 | Nombre de pièges sur la map. |
| `min_distance` / `max_distance` | int | — | Bornes de distance Manhattan entre les deux nuages. |
| `min_total_bugs` / `max_total_bugs` | int | — | Bornes du nombre total de bugs par nuage. |
| `min_green_ratio` / `max_green_ratio` | float | [0,1] | Bornes du ratio de bugs verts. |
| `gap_min` / `gap_max` | float | ≥ 0 | Bornes de l'écart de ratio vert entre les deux nuages. |
| `fog_probability` | float | [0,1] | Probabilité de brouillard de guerre. |
| `trial_seed` | long | 0…2³¹−1 | Seed de génération. ⚠️ voir §Limites (repro). |
| `path_visible_probability` | float | [0,1] | Probabilité que le chemin advisor soit visible. |
| `suboptimal_path_probability` | float | [0,1] | Probabilité d'afficher un chemin suboptimal. |
| `detour_probability` | float | [0,1] | Probabilité d'un détour. |
| `motor_advice_visible_probability` | float | [0,1] | Probabilité que le conseil moteur soit visible. |
| `motor_advice_reliable_probability` | float | [0,1] | Probabilité que le conseil moteur soit fiable. |
| `suboptimal_trap_probability` | float | [0,1] | Probabilité de pièges sur chemin suboptimal. |
| `min_suboptimal_traps` / `max_suboptimal_traps` | int | ≥ 0 | Bornes du nombre de pièges suboptimaux. |
| `map_config` | JSON (string) | — | `{gridWidth, gridHeight, leftCloud{x,y,totalBugs,greenRatio}, rightCloud{…}}`. À parser pour la géométrie exacte. |
| `optimal_path_length` | int | ≥ 0 | Longueur (cases) du chemin optimal. |
| `cloud_distance` | int | ≥ 0 | Distance Manhattan joueur → nuages (= budget de pas). |
| `show_numerical_feedback` | bool | — | Le feedback numérique était-il affiché ? |

## Advice distal

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `distal_advice_visible_probability` | float | [0,1] | Probabilité de visibilité (paramètre). |
| `distal_advice_reliable_probability` | float | [0,1] | Probabilité de fiabilité (paramètre). |
| `distal_advice_visible` | bool | — | Le conseil distal a-t-il été **réellement** affiché ? |
| `distal_advice_reliable` | bool | — | Le conseil distal était-il **réellement** fiable ? |
| `distal_advice_choice` | string | `left`/`right`/∅ | Conseil distal donné. |
| `distal_best_valley` | string | `left`/`right`/∅ | Vallée réellement optimale. |
| `distal_scan_choice` | string | —/∅ | Choix du participant dans la scène de scan distal. |
| `distal_scene` | JSON | — | Config de la scène distale (min/max bugs, green ratio, gap). |

## Explanations (distal / motor / proximal)

Pour chaque advice `X` ∈ {`distal`, `motor`, `proximal`}, 5 colonnes `X_advice_explanation_*` :

| Suffixe | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `_display_mode` | string | `forced`/`opt-in`/`none` | Mode d'affichage de l'explication. |
| `_content_variant` | string | `short`/`long`/∅ | Variante de contenu affichée. |
| `_text_id` | string | id/∅ | Identifiant (chercheur) du texte affiché. |
| `_clicked` | bool | true/false/∅ | En mode opt-in : le participant a-t-il ouvert l'explication ? |
| `_display_duration_ms` | int | ms/∅ | Durée d'affichage de l'explication (opt-in). |

## Résultats & performance

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `proximal_choice` | string | `left`/`right`/`unknown` | Nuage effectivement collecté. |
| `optimal_path_visible` | bool | — | Le chemin optimal était-il visible ? |
| `path_is_suboptimal` | bool | — | Le chemin **affiché** était-il suboptimal ? |
| `choice_correct` | bool | — | Le nuage choisi était-il le meilleur ? (⇔ `proximal_choice == true_cloud`) |
| `true_cloud` | string | `left`/`right`/`none` | Nuage réellement optimal. |
| `green_bugs_collected` | int | ≥ 0 | Bugs verts collectés sur ce trial. |
| `green_bugs_accumulated` | int | ≥ 0 | Score vert cumulé dans le bloc (monotone croissant). |
| `traps_hit` | int | ≥ 0 | Nombre de pièges déclenchés. |
| `steps` | int | ≥ 0 | Nombre de pas effectués. |
| `overtime_steps` | int | ≥ 0 | Pas au-delà du budget (`cloud_distance`). |
| `followed_advisor_path` | bool | — | A suivi le chemin **affiché** (⚠️ pas forcément optimal — voir §Limites). |
| `player_path_log` | JSON array | `[{x,y,t}]` | Trajectoire complète, `t` = timestamp ISO UTC par pas. |

## Questionnaire & horodatage

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `acceptability_question` | string | réponse | Réponse dimension « acceptabilité ». (mapping à confirmer — Q-011) |
| `sens_of_agency_question` | string | réponse | Réponse dimension « sense of agency ». (Q-011) |
| `human_likeness_question` | string | réponse | Réponse dimension « human-likeness ». Renseignée via PATCH post-bloc. (Q-011) |
| `started_at` | string | ISO 8601 UTC | Début du trial. |
| `ended_at` | string | ISO 8601 UTC | Fin du trial. |

---

## Limites connues (à lire avant analyse)

1. **`followed_advisor_path` = adhérence au chemin AFFICHÉ**, pas nécessairement optimal. Pour l'adhérence au chemin *optimal*, recalculer post-hoc via `player_path_log` + `optimal_path_length` / `map_config`.
2. **Temps de décision non fourni directement** : à recalculer (`started_at` vs 1er `t` de `player_path_log`, ou latences inter-pas dans `player_path_log`).
3. **Positions des pièges absentes** de l'export : impossible de croiser erreurs et géographie des pièges à partir du CSV seul.
4. **Bugs verts du nuage non-choisi non loggés** : pas de mesure directe du contraste entre les deux options.
5. **Colonnes nullables** (`valley_choice`, `advisor_forced_value`, `motor_forced_set`, `distal_advice_choice`, `distal_best_valley`, `distal_scan_choice`, `human_likeness_question` au POST) : peuvent être **vides**. Traiter le vide explicitement dans les scripts d'analyse.
6. **Reproductibilité / `trial_seed`** : vérifier (campagne, Axe 6) que le seed loggé régénère bien le trial — il peut refléter `randomizationSeed` de session plutôt que le seed per-trial.
7. **Questions ouvertes impactant le sens** : Q-007 (modèle de perte de bugs), Q-010 (pattern de fiabilité), Q-011 (mapping des 3 dimensions du questionnaire). À trancher avant analyse définitive.
