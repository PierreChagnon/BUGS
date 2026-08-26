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
| `block_template_id` | string | id | Modèle du bloc effectivement joué. **Indispensable quand `randomize_blocks = true`** (DEC-022) : `block_index` donne la position jouée, `block_template_id` donne le bloc expérimental. Grouper les analyses par condition sur **cette** colonne, pas sur `block_index`. |
| `block_index` | int | ≥ 1 | Numéro du bloc dans la session (1-based) — **position jouée**, pas identité du bloc (cf. `block_template_id`). |
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
| `motor_forced_set` | string | `QZD`/`FTH`/`JIL`/∅ | Set de touches imposé si applicable. ⚠️ `JIL` (set IJKL) a remplacé l'ancien `KOM` (set OKLM) au commit `7b3a9e86`. Les valeurs encodent les touches gauche-haut-droite des sets ZQSD / TFGH / IJKL. Une valeur inconnue est **silencieusement convertie en ZQSD** par `FlowValueConverters.ToMotorKeySet` — vérifier les configs de session. |

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
| `proximal_advice_reliable_probability` | float | [0,1] | Probabilité que le chemin advisor désigne le meilleur nuage. Bypassée si proximal forced. Vide sur les lignes antérieures au champ. |
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

> ⚠️ **Paramètre `display_probability` — non exporté mais déterminant** (ajouté le 28/07/26,
> divergence D-013). Chaque advice porte en configuration un `display_probability ∈ [0,1]`
> (`AdviceExplanationConfig`) qui déclenche, **à chaque trial**, un tirage décidant si
> l'explication s'affiche. **Ce paramètre n'apparaît dans aucune colonne** : quand le tirage
> est négatif, la seule trace est `*_display_mode = none`, indistinguable d'un bloc
> volontairement configuré en `none`.
> Deux points à connaître avant analyse : (1) il **contredit TR7** de la spec explanations
> (« granularité blockwise ») — la manipulation d'explication n'est donc pas constante à
> l'intérieur d'un bloc ; (2) **seuls le proximal et le motor ont ce tirage**, le distal est
> résolu au niveau bloc. Mettre ce paramètre à 0 ou 1 dans les sessions de couverture.

## Résultats & performance

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `proximal_choice` | string | `left`/`right`/`unknown` | Nuage effectivement collecté. |
| `optimal_path_visible` | bool | — | Le chemin optimal était-il visible ? |
| `path_is_suboptimal` | bool | — | Le chemin **affiché** était-il suboptimal ? Vrai aussi quand l'advice non fiable désignait le mauvais nuage. |
| `proximal_advice_reliable` | bool | — | **Réalisé** : le chemin advisor désignait-il le meilleur nuage ? Si proximal forced : le nuage imposé était-il le meilleur. `false` sans advisor. Vide sur les lignes antérieures au champ. |
| `choice_correct` | bool | — | Le nuage choisi était-il le meilleur ? (⇔ `proximal_choice == true_cloud`) |
| `true_cloud` | string | `left`/`right`/`none` | Nuage réellement optimal. |
| `green_bugs_collected` | int | ≥ 0 | Bugs verts collectés sur ce trial. |
| `green_bugs_accumulated` | int | ≥ 0 | Score vert cumulé dans le bloc (monotone croissant). |
| `green_bugs_session_total` | int | ≥ 0 | Score vert cumulé sur toute la session, blocs tutoriels exclus (monotone croissant, jamais remis à zéro entre les blocs). |
| `traps_hit` | int | ≥ 0 | Nombre de pièges déclenchés. |
| `steps` | int | ≥ 0 | Nombre de pas effectués. |
| `overtime_steps` | int | ≥ 0 | Pas au-delà du budget (`cloud_distance`). |
| `followed_advisor_path` | bool | — | A suivi le chemin **affiché** (⚠️ pas forcément optimal — voir §Limites). |
| `player_path_log` | JSON array | `[{x,y,t}]` | Trajectoire complète, `t` = timestamp ISO UTC par pas. |
| `advisor_path_config` | JSON array | `[{x,y}]` ou ∅ | Cases ordonnées du chemin advisor **affiché** (même forme que `player_path_log`, sans timestamps). Vide quand `optimal_path_visible` est faux ou sur les lignes de builds antérieurs au champ. À croiser avec `player_path_log` pour repérer les cases parcourues hors du chemin conseillé. |

## Questionnaire & horodatage

| Colonne | Type | Domaine | Description |
| :-- | :-- | :-- | :-- |
| `acceptability_question` | string | `1`…`5` | Réponse dimension « acceptabilité ». Échelle Likert à **5 points** (1 = pas du tout d'accord → 5 = tout à fait d'accord). **Vide quand `advisor_choice = none`** : la question n'est délibérément pas posée (`TrialQuestionsUI.cs:91`). (mapping et textes à confirmer — Q-011 ; valeur attendue sans advisor — Q-DATA-1) |
| `sens_of_agency_question` | string | `1`…`5` | Réponse dimension « sense of agency ». Même échelle. (Q-011) |
| `human_likeness_question` | string | `1`…`5` | Réponse dimension « human-likeness ». Posée **une seule fois par bloc** (au dernier trial), puis renseignée via PATCH. ⚠️ **La réponse est recopiée sur TOUTES les lignes du bloc**, pas seulement la dernière (`ApiClient.cs:150-186`) — cf. §Limites n°8. (Q-011, Q-HL-1) |
| `started_at` | string | ISO 8601 UTC | Début du trial. |
| `ended_at` | string | ISO 8601 UTC | Fin du trial. |

---

## Données collectées hors `trial_responses`

> Ajouté le 2026-07-28 (revue de couverture, constat N1-F / divergence D-015). Cette table
> existait depuis le 16/06/2026 et n'était documentée nulle part.

### Table `participant_notes`

Commentaire libre saisi par le participant sur l'écran de fin de session
(`ParticipantNoteUI`, EndSessionScene → `POST api/participant-notes`).
C'est la réponse au champ `final_comments` du document de référence client.

| Colonne | Type | Description |
| :-- | :-- | :-- |
| `participant_id` | string (UUID) | Jointure avec `trial_responses.participant_id`. |
| `session_template_id` | string | Modèle de session. |
| `note` | string | Texte libre, trimé. Une ligne n'est créée que si le participant saisit quelque chose et clique « Send ». |

**Limites** : pas d'horodatage côté client ; l'envoi est optimiste (la confirmation s'affiche
avant la réponse du serveur, avec restauration du formulaire en cas d'échec) ; aucune reprise
en cas d'échec réseau définitif.

---

## Limites connues (à lire avant analyse)

1. **`followed_advisor_path` = adhérence au chemin AFFICHÉ**, pas nécessairement optimal. Pour l'adhérence au chemin *optimal*, recalculer post-hoc via `player_path_log` + `optimal_path_length` / `map_config`.
2. **Temps de décision non fourni directement** : à recalculer (`started_at` vs 1er `t` de `player_path_log`, ou latences inter-pas dans `player_path_log`).
3. **Positions des pièges absentes** de l'export : impossible de croiser erreurs et géographie des pièges à partir du CSV seul.
4. **Bugs verts du nuage non-choisi non loggés** : pas de mesure directe du contraste entre les deux options.
5. **Colonnes nullables** (`valley_choice`, `advisor_forced_value`, `motor_forced_set`, `distal_advice_choice`, `distal_best_valley`, `distal_scan_choice`, `human_likeness_question` au POST) : peuvent être **vides**. Traiter le vide explicitement dans les scripts d'analyse.
6. **Reproductibilité / `trial_seed`** : vérifier (campagne, Axe 6) que le seed loggé régénère bien le trial — il peut refléter `randomizationSeed` de session plutôt que le seed per-trial.
7. **Questions ouvertes impactant le sens** : Q-007 (modèle de perte de bugs), Q-010 (pattern de fiabilité), Q-011 (mapping des 3 dimensions du questionnaire). À trancher avant analyse définitive.

> Limites 8 à 13 ajoutées le 2026-07-28 par la **revue de couverture bidirectionnelle**
> (`Docs/project-state/revue-couverture-2026-07-28.md`). Ce sont des **trous de mesure**,
> pas des précautions d'usage : les lire avant de planifier une analyse.

8. **`human_likeness_question` est répliqué sur toutes les lignes du bloc.** La question n'est posée qu'une fois (dernier trial) mais la réponse est PATCHée sur chaque ligne de 1 à `trial_count` (`ApiClient.cs:150-186`). **Toute moyenne ou tout comptage sur les lignes surpondère la réponse ×`trial_count`.** Dédupliquer par (`participant_id`, `block_index`) avant analyse. Idem, plus largement, pour toutes les variables de niveau bloc recopiées sur chaque trial (choix et conseil distal, paramètres de bloc) — cf. limite 11.
9. 🔴 **Le niveau moteur n'a aucune donnée de résultat.** Les colonnes `motor_advice_*_probability` sont des **paramètres de configuration**, pas des observations. Ni le set de touches actif, ni le set affiché, ni « le conseil a-t-il été affiché sur ce trial », ni « était-il fiable » ne figurent dans l'export. **L'adhérence au conseil moteur n'est pas mesurable, ni directement, ni par recalcul.** (D-008, Q-MOTOR-1)
10. 🔴 **Le stimulus distal réellement affiché n'est pas enregistré.** `distal_scene` contient les **bornes de tirage** (min/max total, min/max ratio, gap), pas les valeurs vues par le participant. `distal_best_valley` indique la vallée objectivement meilleure, sans dire de combien. Le contraste perçu à l'écran distal n'est donc pas reconstituable. (Côté proximal, `map_config` fournit bien les valeurs réalisées.) (D-009, Q-DISTAL-1)
11. **Granularité : 1 ligne = 1 trial**, pas 1 écran. Le tableau de référence client prévoyait une ligne par écran (`screen_type`, `screen_id`). Les informations de niveau bloc — `valley_choice`, `distal_advice_*`, `distal_scene`, `advisor_choice`, tous les paramètres `*_forced*` et map — sont **recopiées à l'identique sur chaque trial du bloc**. (D-011 / Q-ROW-1)
12. **Aucune colonne d'adhérence au conseil (`*_match_advice`).** À recalculer post-hoc : distal = `valley_choice` vs `distal_advice_choice` ; chemin proximal = `followed_advisor_path` (déjà fourni) ; cible proximale = `choice_correct` comme proxy **uniquement quand le conseil était fiable** ; moteur = impossible (limite 9). (D-007)
13. **Deux tirages ne sont ni reproductibles ni enregistrés** : l'**ordre d'affichage des 3 options d'advisor** (remélangé à chaque affichage — un éventuel effet de position sur le méta-choix est donc invérifiable) et le **genre du badge de l'advisor humain** (tiré indépendamment par composant, donc potentiellement incohérent au sein d'un même trial). Le reste du gameplay est reproductible depuis `trial_seed`. (D-012, Q-RANDOM-1)
