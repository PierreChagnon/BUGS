# Revue de couverture bidirectionnelle — besoin ↔ code ↔ documentation

> **Date :** 28/07/2026 · **Rôle :** Pilotage
> **Méthode :** confrontation ligne à ligne des documents de référence
> (`GameDocument_2_0.docx`, `specs-light.md`, **`CSV_BUGS_Output_V1.xlsx`**,
> `Plan_Implementation_MultiScreen_Flow_v1.md`) et des 41 règles numérotées des `spec-fonc`
> au code réel (63 scripts C#), puis cross-référencement automatisé de chaque classe et de
> chaque champ de `TrialResponseRow` contre l'intégralité de `Docs/`.
> **Aucune modification de code n'a été faite.** Chaque affirmation cite un `fichier:ligne` vérifiable.
>
> **Complémentaire de [`revue-completude-2026-07-28.md`](revue-completude-2026-07-28.md)**, qui
> reste la référence sur les écrans incomplets (E1-E3), le pipeline de données (D1-D5), les
> écarts fonctionnels (F1-F8) et les lots A-D. Ce document ne les répète pas — il ajoute les
> deux matrices de couverture que cette revue n'avait pas produites. **Une seule
> requalification** : E1 (cf. N1-D ci-dessous).

---

## Pourquoi ces deux matrices n'existaient pas

`Docs/roles/analyse-fonctionnelle.md` prévoit que le Rôle 2 produise une « matrice de
traçabilité **comportement → colonne CSV V1** ». Elle n'a jamais été produite.
`Docs/validation/matrice-tracabilite.md` oppose le **code** au **CSV exporté** — jamais le
code au **CSV attendu par le client** (`Docs/references/CSV_BUGS_Output_V1.xlsx`).

C'est le trou méthodologique qui explique la majorité des constats ci-dessous : les
divergences entre ce que le chercheur a demandé et ce que le jeu produit n'ont jamais eu
d'endroit où être vues.

---

# NIVEAU 1 — Le besoin exprimé est-il implémenté ?

## Verdict

**Le jeu est couvert. La mesure ne l'est pas.**

Gameplay sur grille, flow 10 écrans, free/forced sur les 4 niveaux, explanations short/long,
motor advice, randomisation des blocs, pauses : tout est implémenté et fonctionne. Le trou
est **en sortie de données**, et il touche précisément les variables dépendantes du protocole
scientifique.

---

## 1. Les quatre trous majeurs

### N1-A 🔴 — Les trois colonnes `*_match_advice` n'existent pas

`Docs/specs/free-forced-choices/spec-fonc.md:216` (règle **R13**) et `:319` désignent
`valley_advisor_choice_match`, `target_choice_match_advice` et `motor_choice_match_advice`
comme des « **colonnes existantes** », et spécifient leur mode de calcul en mode forced.

`grep -rn "target_choice_match_advice\|motor_choice_match_advice\|valley_advisor_choice_match" Assets/`
→ **zéro occurrence**. Ces trois noms viennent du CSV client (`CSV_BUGS_Output_V1.xlsx`,
colonnes `AO4`, `BI4`, `BP4`) et n'ont jamais été implémentés. La spec les a supposées
présentes parce qu'elles figuraient dans le template.

**Conséquence.** L'**advice-taking** — variable dépendante comportementale n°1 du GDD, qui
porte H1, H4 et H6 — n'a **aucune colonne dédiée** :

| Niveau | Recalculable post-hoc ? | Comment |
| :-- | :-- | :-- |
| Distal | ✅ oui | `valley_choice` vs `distal_advice_choice` (les deux sont loggés) |
| Proximal — cible | 🟡 approximativement | `choice_correct` sert de proxy car le conseil proximal pointe toujours vers le meilleur nuage — mais **uniquement quand le conseil est fiable** ; en cas d'advice non fiable la cible conseillée n'est nulle part |
| Proximal — chemin | ✅ oui | `followed_advisor_path` |
| **Moteur** | ❌ **non** | rien n'est loggé (cf. N1-B) |

**Preuve :** `Assets/Game/Scripts/Data/FlowDataModels.cs:461-543`

---

### N1-B 🔴 — Le motor advice n'émet aucune donnée de résultat

`TrialResponseRow` ne contient, pour le niveau moteur, que :

- `motor_forced_probability`, `motor_forced_set` (paramètres de forçage)
- `motor_advice_visible_probability`, `motor_advice_reliable_probability` (**probabilités de
  configuration**, recopiées telles quelles depuis la session)
- `motor_advice_explanation_*` (5 colonnes d'explication)

Ne sont loggés **nulle part** :

| Donnée | Où elle vit en mémoire |
| :-- | :-- |
| Set **actif** du trial | `MotorAdviceController.ActiveSet` |
| Set **affiché** au participant | `MotorAdviceController.DisplayedSet` |
| L'advice a-t-il été affiché sur **ce** trial ? | `MotorAdviceController.AdviceVisible` |
| L'advice était-il fiable sur **ce** trial ? | `MotorAdviceController.AdviceReliable` |

**Conséquence.** Le niveau moteur — un des trois niveaux d'intention qui portent l'ensemble
des hypothèses du GDD (§2.6.1) — est **inanalysable**. On sait avec quelle probabilité
l'advice *pouvait* apparaître, pas s'il est apparu ; on ne sait ni ce que le participant a
vu, ni ce qu'il a fait.

`Q-002` (« Quelles colonnes CSV V1 pour le motor advice ? ») est ouverte depuis le
**12/03/26** et marquée « traçabilité ». Cette revue la requalifie : ce n'est pas un sujet de
traçabilité, c'est **une manipulation expérimentale sans mesure**.

**Preuve :** `Assets/Game/Scripts/Data/FlowDataModels.cs:508-517` ·
`Assets/Game/Scripts/Controllers/MotorAdviceController.cs` ·
`Assets/Game/Scripts/UI/MotorAdviceUI.cs:55-73`

---

### N1-C 🔴 — Le stimulus distal réellement affiché n'est pas enregistré

`DistalChoiceUI` génère, par bloc, les valeurs effectivement présentées au participant sur
l'écran de scan des deux vallées (nombre total de bugs et ratio vert de chaque côté) :

```
DistalChoiceUI.cs:36-37   public BugCloudSample LeftScanData  { get; private set; }
                          public BugCloudSample RightScanData { get; private set; }
DistalChoiceUI.cs:67-72   LeftScanData  = distalScans.firstCloud;
                          RightScanData = distalScans.secondCloud;
                          ApplyValleyScan(LeftScanData,  ref _autoValleyAScan);
                          ApplyValleyScan(RightScanData, ref _autoValleyBScan);
```

`grep -rn "LeftScanData\|RightScanData" --include="*.cs" Assets/` → **ces deux propriétés ne
sont lues nulle part ailleurs**. Elles servent uniquement à piloter les particules.

La ligne envoyée ne contient que `distal_scene` (les **bornes** de configuration :
min/max total, min/max ratio, gap) et `distal_best_valley`. Le CSV client demande
explicitement les valeurs réalisées : `valley_total_bugs_nb`, `left_valley_green_bugs_nb`,
`left_valley_red_bugs_nb`, `right_valley_green_bugs_nb`, `right_valley_red_bugs_nb`.

**Conséquence.** Impossible d'analyser le choix distal contre ce que le participant a vu. On
connaît la distribution dans laquelle le stimulus a été tiré, pas le stimulus.

**Asymétrie révélatrice** : côté proximal, les valeurs réalisées **sont** loggées via
`map_config` (`{gridWidth, gridHeight, leftCloud{x,y,totalBugs,greenRatio}, rightCloud{…}}`,
`TrialManager.cs:175-204`). Le distal a été oublié.

---

### N1-D 🔴 — Requalification de E1 : la QuestionnaireScene n'est jamais chargée

`revue-completude-2026-07-28.md` §2 décrit la QuestionnaireScene comme une coquille vide qui
« consomme actuellement un chargement de scène et un fade pour rien ». **C'est plus grave que
cela : la scène n'est jamais chargée du tout.**

`grep -n "AdvanceToPhase(" Assets/Game/Scripts/Systems/FlowController.cs` → **12 sites
d'appel**, ciblant `Intro`, `Welcome`, `Consent`, `AdvisorChoice`, `DistalChoice`, `Proximal`,
`Break`, `EndSession`. **Aucun ne cible `GamePhase.Questionnaire`.** La scène est présente dans
`EditorBuildSettings.asset` (activée, index 8) mais inatteignable ; `OnQuestionnaireComplete`
(`FlowController.cs:309-329`) est du code mort dans le flow actuel.

Et le blocage est **triple** — le câbler suppose bien plus que remplir une liste :

| Verrou | Constat |
| :-- | :-- |
| **1. Aucune source de configuration** | `QuestionConfig` (type complet : `order`, `text`, `type`, `options[]`, `min_value=1`, `max_value=7`, `min_label`, `max_label`) n'est référencé par **aucun champ** de `SessionConfig` ni de `BlockConfig` (`FlowDataModels.cs:44-114`, `:383-390`). C'est un **type orphelin** : le chercheur n'a aucun moyen de saisir des questions. |
| **2. Liste hardcodée vide** | `QuestionnaireUI.cs:38` — `_questions = new List<QuestionConfig>()`, inconditionnel. |
| **3. Sortie amputée** | Même remplie, `ApiClient.QueueQuestionnairePatchForTrial` (`:136-147`) construit un payload ne contenant **que** `human_likeness_question` et **jette silencieusement toutes les autres réponses** ; il abandonne aussi complètement si ce champ est vide (`:144-145`). |

**Conséquence.** Le **Trust in Technology Questionnaire** (instrument listé au GDD §2.7 comme
prédicteur de H7) et les **questions block-wise** du GDD §5 n'ont de chemin de données **ni en
entrée ni en sortie**.

⇒ **Q-QUEST-1 doit être reformulée** : « câbler ou retirer » sous-entend que câbler est un
branchement. Ce serait une extension du modèle de données (`BlockConfig.questions`) **et** du
contrat de PATCH.

---

## 2. Confrontation complète au CSV de référence client

`Docs/references/CSV_BUGS_Output_V1.xlsx` (feuille « BUGS CSV V1 ») définit **76 colonnes**.
`TrialResponseRow` en compte **82**. Correspondances de nom exactes : **10**.

> ⚠️ **Lecture équitable de cet écart.** Le template CSV V1 date de **mars 2026** ; il est
> antérieur à DEC-017, DEC-018 (explanations), DEC-019 (free/forced) et DEC-022
> (randomisation/pauses). Une grande partie de la divergence est une **évolution légitime du
> design** : le code émet une trentaine de colonnes que le template ne prévoyait pas.
> Le problème n'est pas que le code diverge — **c'est que personne n'a jamais acté l'écart**,
> ni côté renommages, ni côté absences.

Légende : ✅ présent · 🔁 renommé (équivalent sémantique) · ⚠️ dérivable mais non fourni tel quel ·
❌ absent · 🟢 couvert ailleurs.

| # | Colonne client | Champ code | Statut |
| :-: | :-- | :-- | :-: |
| 1 | `game_session_id` | `participant_id` (UUID) + `session_template_id` | 🔁 |
| 2 | `randomization_seed` | `trial_seed` | 🔁 |
| 3 | `block_id` | `block_index` | 🔁 |
| 4 | `screen_type` | — | ❌ |
| 5 | `screen_id` | — | ❌ |
| 6 | `username` | — | ❌ |
| 7 | `timestamp` | `started_at` + `ended_at` | 🔁 |
| 8 | `min_total_bugs` | `min_total_bugs` | ✅ |
| 9 | `max_total_bugs` | `max_total_bugs` | ✅ |
| 10 | `min_green_bugs_prop` | `min_green_ratio` | 🔁 |
| 11 | `max_green_bugs_prop` | `max_green_ratio` | 🔁 |
| 12 | `min_visibility_noise` | — | ❌ |
| 13 | `max_visibility_noise` | — | ❌ |
| 14 | `visibility_noise_variance` | — | ❌ |
| 15-20 | `{distal,proximal,motor}_explanation_{short,long}` | `*_advice_explanation_content_variant` + `_text_id` (le corpus vit dans la config de session, pas dans la ligne) | 🔁 |
| 21 | `distal_advice_reliability` | `distal_advice_reliable_probability` **+ `distal_advice_reliable`** (réalisé) | ✅ |
| 22 | `proximal_advice_reliability` | — (exprimé indirectement par `suboptimal_path_probability`) | ❌ |
| 23 | `motor_advice_reliability` | `motor_advice_reliable_probability` (paramètre) ; **réalisé absent** | ⚠️ |
| 24 | `explanation_given_frequency` | `display_probability` — **existe en config, jamais loggé** (cf. N2-C) | ⚠️ |
| 25 | `advice_given_frequency` | `distal_advice_visible_probability`, `path_visible_probability`, `motor_advice_visible_probability` | 🔁 |
| 26-27 | `min_traps_nb`, `max_traps_nb` | `trap_count` (valeur unique, pas de bornes) | 🔁 |
| 28 | `advisor_type` | `advisor_choice` | 🔁 |
| 29 | `valley_total_bugs_nb` | — (cf. **N1-C**) | ❌ |
| 30 | `left_valley_green_bugs_nb` | — (cf. **N1-C**) | ❌ |
| 31 | `left_valley_red_bugs_nb` | — (cf. **N1-C**) | ❌ |
| 32 | `left_valley_visibility_noise` | — (F1) | ❌ |
| 33 | `right_valley_green_bugs_nb` | — (cf. **N1-C**) | ❌ |
| 34 | `right_valley_red_bugs_nb` | — (cf. **N1-C**) | ❌ |
| 35 | `right_valley_visibility_noise` | — (F1) | ❌ |
| 36 | `true_valley` | `distal_best_valley` | 🔁 |
| 37 | `distal_advice_given` | `distal_advice_visible` | 🔁 |
| 38 | `distal_advice` | `distal_advice_choice` | 🔁 |
| 39 | `valley_advice_explanation` | `distal_advice_explanation_*` (5 colonnes) | ✅ |
| 40 | `valley_choice` | `valley_choice` | ✅ |
| 41 | `valley_advisor_choice_match` | — (cf. **N1-A**) — recalculable | ❌ |
| 42 | `trial_nb` | `trial_index` | 🔁 |
| 43 | `forest_total_bugs_nb` | dans `map_config` (JSON) | ⚠️ |
| 44 | `cloud_a_position` | `map_config.leftCloud{x,y}` | ⚠️ |
| 45 | `cloud_a_green_bugs` | dérivable de `map_config` (`totalBugs × greenRatio`) | ⚠️ |
| 46 | `cloud_a_red_bugs` | dérivable de `map_config` | ⚠️ |
| 47 | `cloud_a_visiblity_noise` | — (F1) | ❌ |
| 48-50 | `cloud_b_position`, `cloud_b_green_bugs`, `cloud_b_red_bugs` | idem colonnes 44-46 | ⚠️ |
| 51 | `cloud_b_visiblity_noise` | — (F1) | ❌ |
| 52 | `true_cloud` | `true_cloud` | ✅ |
| 53 | `optimal_path_length` | `optimal_path_length` | ✅ |
| 54 | `traps_nb` | `trap_count` | 🔁 |
| 55 | `map_layout` | `map_config` — **ne contient ni les murs ni les positions de pièges** (cf. PC-8) | ⚠️ |
| 56 | `proximal_advice_given` | `optimal_path_visible` | 🔁 |
| 57 | `proximal_advice_target` | — | ❌ |
| 58 | `proximal_advice_path` | — le chemin **affiché** n'est jamais loggé | ❌ |
| 59 | `proximal_advice_explanation` | `proximal_advice_explanation_*` (5 colonnes) | ✅ |
| 60 | `proximal_choice_target` | `proximal_choice` | 🔁 |
| 61 | `target_choice_match_advice` | — (cf. **N1-A**) | ❌ |
| 62 | `path_choice_match_advice` | `followed_advisor_path` | 🔁 |
| 63 | `motor_choice_active_config` | — (cf. **N1-B**) | ❌ |
| 64 | `motor_advice_given` | — (cf. **N1-B**) | ❌ |
| 65 | `motor_advice` | — (cf. **N1-B**) | ❌ |
| 66 | `motor_advice_explanation` | `motor_advice_explanation_*` (5 colonnes) | ✅ |
| 67 | `motor_choice` | — (cf. **N1-B**) | ❌ |
| 68 | `motor_choice_match_advice` | — (cf. **N1-A** et **N1-B**) | ❌ |
| 69 | `player_path_log` | `player_path_log` | ✅ |
| 70 | `trap_hit` | `traps_hit` | 🔁 |
| 71 | `final_reward` | `green_bugs_collected` | 🔁 |
| 72 | `acceptability_question` | `acceptability_question` | ✅ |
| 73 | `human_likeness_question` | `human_likeness_question` ⚠️ **répliqué sur toutes les lignes du bloc** (cf. N2-B) | ✅ |
| 74 | `sens_of_agency_question` | `sens_of_agency_question` | ✅ |
| 75 | `Trust in Technology Questionnaire` | — (cf. **N1-D**) | ❌ |
| 76 | `final_comments` | table `participant_notes`, hors `trial_responses` (cf. **N1-F**) | 🟢 |

**Bilan : 29 colonnes ❌**, dont 3 recalculables post-hoc (41, 61 côté distal), 1 couverte
ailleurs (76), 7 relevant de `visibility_noise` (F1/Q-013), 5 du stimulus distal (N1-C) et
6 du niveau moteur (N1-B).

**Colonnes émises par le code et absentes du template client (~30)** — évolution légitime,
à acter : `session_name`, `build_version`, `block_template_id`, `trial_count`, `is_tutorial`,
tous les `*_forced*` (14 colonnes, DEC-019), `gap_min`/`gap_max`, `fog_probability`,
`suboptimal_path_probability`, `detour_probability`, `suboptimal_trap_probability`,
`min/max_suboptimal_traps`, `min/max_distance`, `distal_scene`, `distal_scan_choice`,
`cloud_distance`, `show_numerical_feedback`, `path_is_suboptimal`, `choice_correct`,
`green_bugs_accumulated`, `steps`, `overtime_steps`.

---

## 3. Granularité de ligne — divergence structurelle jamais explicitée

| | Client (`CSV_BUGS_Output_V1.xlsx`) | Code |
| :-- | :-- | :-- |
| Unité de ligne | **1 ligne par écran** (`screen_type`, `screen_id`) — l'écran « mountain » a sa propre ligne | **1 ligne par trial** non-tutorial (DEC-011, table plate) |
| Données distales | portées par la ligne « mountain screen » du bloc | **recopiées à l'identique sur chaque trial** du bloc |

La divergence est défendable (dénormalisation assumée, CSV sans jointure — c'est l'objet
même de DEC-011). Mais elle n'a **jamais été explicitée au chercheur**, et elle a des
conséquences d'analyse directes : toute agrégation naïve sur les lignes surpondère les
variables distales et le questionnaire par `trial_count`.

⇒ **Q-ROW-1** ouverte.

---

## 4. Questions trial-wise — divergences de protocole non documentées

Le GDD (§2.3 « Actions/choices ») dit : *« The player can rate their level of control
**after some trials** »*.

`TrialQuestionsUI.cs:78-108` pose **2 questions après CHAQUE trial** (acceptabilité +
agentivité) **+ 1** au dernier trial du bloc (human-likeness). **Aucun paramètre de
fréquence n'existe.** À 36 trials/bloc (valeur GDD), cela fait **72 interruptions Likert par
bloc**.

Trois détails d'implémentation jamais documentés :

1. **Échelle à 5 points** (`_choiceToggles`, `TrialQuestionsUI.cs:32`), réponse stockée comme
   `"1"`…`"5"` — alors que le `QuestionConfig` orphelin a pour défaut `min_value=1`,
   `max_value=7`.
2. **L'ordre des questions est mélangé** (Fisher-Yates) avec
   `new System.Random((int)flow.CurrentTrialSeed)` (`:100-105`) — reproductible, mais
   l'ordre présenté n'est pas loggé.
3. La question d'acceptabilité **n'est pas posée** si `advisor_choice == None` (`:91`) —
   comportement voulu, mais c'est la cause racine de la perte de trials D-001.

⇒ **Q-FREQ-1** ouverte.

---

## 5. Écrans du plan de référence

| Écran (`Plan_Implementation_MultiScreen_Flow_v1.md`) | État |
| :-- | :-- |
| **TRIAL_SUMMARY** (§2.5) | ✅ `RoundUI` — bugs verts collectés / échappés, panneaux succès-échec, bouton continuer, masquage conditionnel des valeurs numériques (`show_numerical_feedback`) |
| **BLOCK_RECAP** (§2.6) | ❌ `BreakSceneController` n'affiche **qu'un countdown MM:SS** et un bouton de reprise — ni total du bloc, ni advisor utilisé, ni vallée choisie, ni performance comparative |
| **END** (§2.7) | ✅ `EndSessionScene` + `ParticipantNoteUI` + `PlatformUrlDisplay` |
| **QUIT_OVERLAY** (§2.8) | ❌ `SettingsPanelUI.cs:61-68` = `Application.Quit()` sec — ni modale de confirmation, ni sauvegarde partielle, ni flag `session_abandoned` + timestamp + dernier écran atteint (= **F4**) |

---

## 6. Deux points où la documentation **sous-estime** la couverture

### N1-F 🟢 — `final_comments` est implémenté, via une table que la doc ignore

`ParticipantNoteUI` (EndSessionScene) recueille un texte libre du participant et l'envoie sur
un **3ᵉ endpoint** : `POST {root}/api/participant-notes`, payload
`{participant_id, session_template_id, note}`.

**Preuve :** `Assets/Game/Scripts/UI/ParticipantNoteUI.cs:46-56` ·
`Assets/Game/Scripts/Network/ApiClient.cs:41,233-246`

`grep -rn "participant-notes\|ParticipantNote" Docs/` → **aucun résultat**. Ni le
dictionnaire de données, ni la matrice de traçabilité, ni le TDD, ni aucune spec ne
mentionnent cette table. **Un chercheur ignore aujourd'hui qu'il dispose de ces données.**
Cf. N2-J.

### Garantie GDD respectée mais non écrite

Le GDD (§7.1) exige : *« The same total number of bugs (for perceptual consistency) and only
vary the green/red ratio »*. `BugCloudGenerationUtility.cs:99-104` le garantit strictement —
un seul `totalBugs` est tiré et partagé par les deux nuages, seuls les ratios diffèrent
(séparés par un `gap` tiré dans `[gap_min, gap_max]`). Cette garantie n'est documentée nulle
part et mérite de l'être : c'est une contrainte de validité perceptive centrale.

---

## 7. Points déjà correctement identifiés le 28/07 (confirmés, non repris)

F1 `visibility_noise` inexistant · F2 retour arrière non bloqué · F3 2 paths au lieu de 4 ·
F4 protocole de sortie · F5 Mountain UI · F6 textes de questionnaire placeholders ·
F7 pas de boot hors-ligne · Q-009 smooth camera pan + auto-walk · Q-010 reliability pattern
non spécifié côté chercheur.

---

# NIVEAU 2 — Ce qui est dans le code sans être documenté

## 8. Le chiffre

Cross-référence automatisée de chaque classe du projet contre `Docs/TDD.md`,
`Docs/specs/**`, `Docs/references/**`, `avancement.md` et `decisions.md` :

- **63 scripts** dans `Assets/Game/Scripts` + `Assets/Game/Utils/Maze`
- **30 n'ont aucune occurrence dans `TDD.md`**
- **13 n'apparaissent nulle part dans `Docs/`** : `WrongInputFeedbackController`,
  `BugCloudGenerationUtility`, `AudioPrefsMenu`, `PlayerAnimator`, `TrapVisibility`,
  `AdviceExplanationUIBase`, `BootLoadingBar`, `BugCloudParticleUtility`,
  `DistalExplanationUI`, `MotorExplanationUI`, `ProximalExplanationUI`,
  `ProximalForcedExplanationSequence`, `FogSpawner` (absent des specs).

**Cause structurelle (git).** Dernière spec créée : **29/05/2026**. Depuis, 8 chantiers
livrés dont 5 sans le moindre document. `session-map.md` figé au 12/03,
`plan-global.md` et `risques.md` au 03/03 (désormais archivés).

---

## 9. N2-A 🔴 — Des aléas non seedés touchent des manipulations expérimentales

Tout le gameplay est correctement seedé via `LevelRegistry.CreateRng(scope)` dérivé de
`trial_seed` (FNV-1a, reproductible cross-platform). **Trois tirages y échappent** et
utilisent `UnityEngine.Random` — non seedé, non reproductible, **non loggé** :

### 9.1 L'ordre des trois options d'advisor est mélangé à chaque affichage

`AdvisorChoiceUI` applique un **shuffle Fisher-Yates** sur les trois prefabs
(`none` / `human` / `robot`) avant de les instancier dans les slots gauche / milieu / droite.

Le **méta-choix est la manipulation la plus en amont du protocole** (il détermine l'advisor
de tout le bloc, et H1 porte dessus). Un biais de position gauche/milieu/droite est un
confondant classique en psychologie expérimentale. Aujourd'hui :

- l'ordre présenté **n'est nulle part** dans `TrialResponseRow` ;
- il **n'est pas reproductible** (pas de seed) ;
- il **n'est documenté dans aucune spec** — le GDD décrit un râtelier à 3 crochets, sans
  préciser si l'ordre doit être fixe ou randomisé.

⇒ **Impossible de contrôler ou de tester ce biais post-hoc.**

### 9.2 Le genre du badge « advisor humain » est tiré indépendamment dans chaque composant

Trois composants tirent chacun leur propre `Random.value < 0.5f` au `Awake` pour choisir
entre le badge humain masculin et féminin : `AdviceExplanationUIBase`, `DistalChoiceUI`,
`MotorAdviceUI`.

**Conséquence directe** : sur un même trial, l'indicateur de conseil distal peut afficher une
femme pendant que l'encart d'explication affiche un homme. Le GDD s'interroge explicitement
sur les biais de perception liés à l'apparence de l'advisor (§7 Misc notes : *« Should we be
careful about human-bot and bot-bot being dissimilar in terms of cuteness because this can
bias selection of advisors? »*).

### 9.3 Cosmétique (sans enjeu)

Jitter horizontal du nombre flottant de pénalité ; choix du clip / volume / pitch des SFX.

⇒ **Q-RANDOM-1** ouverte.

---

## 10. N2-B 🟠 — `human_likeness_question` est répliqué sur toutes les lignes du bloc

```
ApiClient.cs:150-186   QueueHumanLikenessPatchForBlock(participantId, blockIndex, trialCount, …)
                       int pendingCount = Mathf.Max(1, trialCount);
                       for (int trialIndex = 1; trialIndex <= pendingCount; trialIndex++)
                           QueueQuestionnairePatchPayloadForTrial(…, trialIndex, …);
```

La réponse est PATCHée sur **chaque** ligne du bloc, de 1 à `trial_count`.

Cela **contredit** :

- **DEC-013** — « Questionnaire envoyé par PATCH **sur la dernière ligne du bloc** »
  (`decisions.md:129`) ;
- `dictionnaire-donnees.md:110` — « Renseignée via PATCH post-bloc », sans mention de
  réplication ;
- `matrice-tracabilite.md:130` — « envoyée en PATCH (null au POST) ».

**Conséquence d'analyse** : un script qui moyenne ou compte la colonne sur les lignes
surpondère la réponse ×`trial_count`. Le comportement peut être le bon (il rend la colonne
utilisable sans jointure, cohérent avec l'esprit de DEC-011) — mais il doit être **écrit**.

Ce constat **ferme le point chaud PC-7** (« le PATCH atterrit-il sur la bonne ligne ? ») :
il atterrit sur **toutes**, par construction.

⇒ **Q-HL-1** ouverte.

---

## 11. Paramètres de configuration invisibles pour le chercheur

### N2-C 🔴 — `display_probability` : zéro occurrence dans `Docs/`

```
FlowDataModels.cs:286        public float display_probability = 1f;
FlowController.cs:660-695    bool showExplanation = adviceVisible && RollExplanationProbability(…);
                             // Tirage independant a chaque trial
                             // (remplace l'ancienne restriction "1er trial du bloc uniquement").
```

Tirage de Bernoulli **par trial** décidant si l'explanation proximale ou motrice s'affiche.
Ajouté le **17/07/2026** (commit `2b58981b`, « [EXPLANATIONS] params probabilistes proximal »).

Trois problèmes :

1. **Il contredit TR7** de `Docs/specs/explanations-short-long/spec-fonc.md:196` :
   « `display_mode` et `content_variant` sont définis **par bloc** dans `BlockConfig`, pas
   par trial. **Tous les trials d'un même bloc partagent la même configuration
   explanation.** (Q-EXP-1, DEC-018) ». Le commentaire du code annonce explicitement qu'il
   remplace une règle antérieure — sans que la spec ait été mise à jour.
2. **Asymétrie non documentée** : seuls le proximal et le motor ont ce tirage.
   `ResolveDistalExplanationForCurrentBlock` (`FlowController.cs:808`) n'en a pas.
3. `grep -rn "display_probability" Docs/` → **rien**. Absent du dictionnaire de données, de
   la matrice de traçabilité et de la matrice de couverture de `config-session-test.md`.

**Conséquence.** Un chercheur configurant une session dans Supabase dispose d'un paramètre
qui modifie silencieusement la manipulation d'explication (la brique de **H8**), sans
qu'aucun document ne l'en informe — et la valeur réalisée du tirage n'apparaît qu'indirectement
via `*_advice_explanation_display_mode = none`.

### N2-D 🟠 — `block_template_id` absent du codebook

Le champ existe dans le code (`FlowDataModels.cs:465`, ajouté par **DEC-022** pour tracer le
template de bloc joué après randomisation de l'ordre) mais **n'apparaît ni dans
`dictionnaire-donnees.md` ni dans `matrice-tracabilite.md`**, tous deux pourtant mis à jour
le 28/07. La matrice annonce « Total champs code = **80** » ; le décompte réel est **82**.

---

## 12. Mécaniques de jeu sans aucune spec

### N2-E 🟠 — La pénalité « mauvaise touche » n'est jamais comptée en base

Le GDD l'exige (§7.1, *« The player must have a cost when missing a key »*), et le code
l'applique :

```
GameManager.cs:280-295   OnInvalidMoveKeyPressed()
                         → -1 bug vert sur chaque nuage
                         → SFX _sfxPlayerMissedKey
                         → StartWrongInputCooldown()  (1 s, _wrongInputCooldownDuration:46)
GameManager.cs:297-318   cooldown + event OnWrongInputCooldownChanged
WrongInputFeedbackController.cs  → overlay « WRONG INPUT »
```

**Mais aucun compteur ne part en base.** Trois sources de perte de bugs verts coexistent
(piège, dépassement du budget de pas, touche invalide) pour **deux** compteurs seulement
(`traps_hit`, `overtime_steps`).

**Conséquence.** `green_bugs_collected` n'est pas décomposable : le chercheur ne peut pas
distinguer un participant qui a mal navigué d'un participant qui a mal identifié le set de
touches actif — alors que c'est précisément l'effet du **motor advice** que l'étude cherche à
mesurer.

Le cooldown, l'overlay et `WrongInputFeedbackController` n'ont **ni spec ni entrée TDD**
(feature livrée les 17-20/07 sur la branche `feature/motor-input-error-feedback`).

### N2-F 🟠 — La pénalité de dépassement du budget de pas n'est dans aucun document de référence

```
GameManager.cs:250-260   OnStepBudgetExceeded() → overtimeSteps++, -1 vert sur chaque nuage
```

Elle s'applique **à chaque pas supplémentaire** au-delà de `LevelRegistry.stepBudget`
(distance de Manhattan joueur → nuages).

Ni le GDD ni `specs-light.md` ne prévoient de perte hors obstacles : le GDD dit *« time lost
to obstacles will result in bugs flying out »*. C'est un **ajout de design** — documenté au
TDD, mais jamais soumis au chercheur comme tel. `questions-client.md` ne le mentionne que
depuis la reformulation de Q-007 le 28/07.

### N2-G 🟠 — L'overlay « Equipment failure » n'est jamais spécifié

```
FlowController.cs:104   ShouldShowEquipmentFailureOverlay =>
                        IsAnyChoiceForcedThisTrial && State.advisor_choice == AdvisorType.None
SessionManager.cs:64    exposé à la scène
```

Il est mentionné en passant dans `free-forced-choices/spec-fonc.md` (R9, R16, TR6, et
DEC-019 « Note 2 ») comme justification narrative — mais **son déclencheur exact, son
contenu, sa durée et son comportement ne sont spécifiés nulle part**, et il est absent du TDD.

### N2-H 🟡 — Deux conventions de départage contradictoires dans le même codebase

| Emplacement | Comportement à égalité |
| :-- | :-- |
| `BugCloudGenerationUtility.cs:27` | `secondGreenBugs > firstGreenBugs ? 1 : 0` → le **premier** nuage gagne |
| `GameManager.cs:404` | `_leftCloud.greenBugs > _rightCloud.greenBugs` → le nuage de **droite** gagne |

**Q-TIE-1** (ouverte le 28/07) ne couvre que la seconde. La première détermine
`BestCloudIndex`, utilisé en amont dans la génération.

### N2-I 🟡 — Lecture d'input par position physique, affichage par libellé OS

`MotorAdviceController` lit les touches **par position physique** sur un layout US de
référence (`wKey`/`aKey`/`sKey`/`dKey`, `tKey`/`fKey`/`gKey`/`hKey`,
`iKey`/`jKey`/`kKey`/`lKey`) mais **affiche les libellés via `KeyControl.displayName`**,
c'est-à-dire l'étiquette réelle renvoyée par l'OS.

Le comportement moteur est donc **layout-agnostique** et l'affichage **layout-dépendant** :
sur un poste QWERTY, le set « ZQSD » s'affiche « W A S D » tout en se jouant aux mêmes
positions. C'est le bon design pour une passation multi-postes — mais il n'est documenté
nulle part, et un fallback AZERTY codé en dur prend le relais sans clavier détecté.

---

## 13. Surface de données non documentée

### N2-J 🟠 — Trois endpoints, un seul documenté en détail

| Endpoint | Verbe | Alimenté par | Documenté ? |
| :-- | :-- | :-- | :-- |
| `api/sessions/{id}` | GET | `FlowController.BootstrapFlow` | ✅ spec multi-screen-flow |
| `api/trial-responses` (+ PATCH `/{id}`) | POST/PATCH | `TrialManager` → `ApiClient` | ✅ dictionnaire + matrice |
| **`api/participant-notes`** | POST | `ParticipantNoteUI` | ❌ **nulle part** |

Cf. N1-F. C'est un troisième sink de données, avec sa propre table, invisible pour le
chercheur qui lira le codebook.

---

## 14. Documentation fausse (pas seulement absente)

### N2-K 🟠 — `CLAUDE.md`, le document d'onboarding, est périmé

| Ligne | Affirmation | Réalité |
| :-- | :-- | :-- |
| `CLAUDE.md:116` | « TrapSpawner […] Count configurable via args de ligne de commande » | Aucun parsing de `trapCount` nulle part |
| `CLAUDE.md:126` | « SessionManager : Parse les args de ligne de commande (`trapCount=N`, `sessionId=X`) » | `SessionManager.cs:66-87` ne parse **rien** ; il copie la config depuis `FlowController` |
| `CLAUDE.md:284` | « SessionManager.Start() : Parse les args de ligne de commande » | idem |
| `TDD.md:723` | « Valeur par défaut : 10, overridable via arg CLI "trapCount=N" » | idem |

Le seul argument lu est `sessionId=`, dans `FlowController.cs:389`
(`ExtractSessionIdFromCommandLineArgs`).

### N2-L 🟡 — Résidus de la correction de pénalité du 28/07

L'entrée de journal `TDD.md:1408` déclare les 18 mentions de « −2 bugs » corrigées. Il en
reste **deux**, dans la section §3.5 GridMover que la passe a manquée :

- `TDD.md:818` — « `GameManager.OnInvalidMoveKeyPressed()` — pénalité **-2 × 2 nuages** »
- `TDD.md:850` — « → `GameManager.OnInvalidMoveKeyPressed()` appelé (pénalité **-2 × 2 nuages**) »

(La mention `:1406` est une entrée de journal historique datée du 12/03/26 : elle doit rester.)

### N2-M 🟡 — Inventaire des dépendances incomplet

`TDD.md §11.2` liste **5 packages** ; `Packages/manifest.json` en compte **19** hors modules
Unity. Manquent notamment :

| Package | Usage réel |
| :-- | :-- |
| `com.unity.nuget.newtonsoft-json` 3.2.2 | **Tout `ApiClient`** en dépend (`using Newtonsoft.Json`, désérialisation de `SessionConfig`) |
| `com.unity.cinemachine` 3.1.6 | `InteractionManagerProto` dans `WelcomeScene` et `AdvisorChoiceScene` |
| `com.unity.probuilder` 6.0.9 | prototypage de scènes |
| `com.unity.render-pipelines.high-definition` 17.3.0 | **installé en parallèle d'URP** — à vérifier / retirer |

### N2-N 🟡 — `readme.md` racine périmé

Décrit l'architecture pré-`FlowController` : `POST /api/trials`, `screenCounter`, rechargement
de scène en fin de manche. L'endpoint réel est `api/trial-responses` (`ApiClient.cs:40`).

---

## 15. Zones « code-first », branches et hygiène

### Chantiers livrés sans aucune spec

Block randomization + BreakScene (23/07 — couvert par DEC-022 seulement) · wrong-input
feedback (17-20/07) · panneau « entering explanation block » (20/07) · consent decline modal
(17/07) · settings panel (27/05) · platform URL (08/07) · mission report · player animations ·
distal advisor badges.

### Chantiers livrés sans spec-tech alors qu'ils portent H1–H8

`free-forced-choices` (code le 01/06, commit `2dead4f0`) et `explanations-short-long`
(code les 02-03/06, refonte probabiliste les 17-20/07). Conséquences directes constatées :

- les tie-breakers **R6** (vallée optimale) et **R15** (cloud optimal) renvoient
  explicitement à une spec technique **inexistante** — alors que le code tranche déjà
  (`FlowController.ResolveMostRewardingValley`, égalité → 50/50 sur RNG bloc salt 1) ;
- **N2-C** (`display_probability`) n'a jamais eu de contradicteur documentaire ;
- **N1-A** (colonnes `*_match_advice` supposées existantes) n'a jamais été confronté au code.

### À trancher

| Élément | État |
| :-- | :-- |
| `origin/feature/bug-cloud-feedback` | ~270 lignes C# jamais mergées (`DamageLabel`, `SpawnsDamagePopup`), fonctionnellement remplacées par `FloatingPenaltyNumber` + `PenaltyFeedbackController` (DEC-021) — **rien ne l'acte** |
| `scene/mission-dashboard` | 1 commit (18/05), scène prototype maintenue 2 mois sans décision |
| `QuestionnaireScene` | toujours activée dans le build alors qu'inatteignable (**N1-D**) |
| `ProximalSceneFLO.unity` | scène orpheline hors build |
| 3 scènes `[LEGACY]`, 3 prefabs `_DRAFT`, `EndOfBlockPanel_TODO.prefab` | conservés |
| `mono_crash.137d23bcc1.0.json`, `mono_crash.289ebed4aa.0.json` | 334 Ko de dumps d'éditeur **versionnés** |

### Non-constat — à ne pas confondre avec une fuite

La clé Supabase stockée dans `BootScene.unity` est la clé **anon** (publique par conception,
la sécurité repose sur les policies RLS côté Supabase). Ce **n'est pas** une fuite de secret.
En revanche **les policies RLS ne sont documentées nulle part** — à vérifier avant toute
passation avec de vrais participants. Le service account Google et `.env` sont, eux,
correctement ignorés (`.gitignore:111-114`).

---

# 16. Ce que cette revue ajoute au plan d'action existant

`revue-completude-2026-07-28.md` §10 pose l'ordre : arbitrages chercheur → Lot A (pipeline) →
textes placeholders → campagne de validation → dette spec. **Cet ordre reste valide.** Cette
revue ajoute un lot et durcit deux points :

## Lot E — Couverture de la donnée de sortie *(nouveau, à arbitrer avec le chercheur)*

À traiter **au même niveau de priorité que le Lot A** : le Lot A garantit qu'aucune ligne ne
se perd, le Lot E garantit que les lignes qui arrivent contiennent de quoi tester les
hypothèses.

| Tâche | Objet | Dépend de |
| :-- | :-- | :-- |
| **E1** | Logger le résultat du motor advice (set actif, set affiché, visible, fiable) | Q-MOTOR-1 |
| **E2** | Logger le stimulus distal réalisé (`LeftScanData` / `RightScanData`) | Q-DISTAL-1 |
| **E3** | Ajouter (ou acter comme recalculables) les 3 colonnes `*_match_advice` | Q-MOTOR-1 |
| **E4** | Compteur de touches invalides, pour décomposer `green_bugs_collected` | Q-007 |
| **E5** | Seeder et logger l'ordre des options d'advisor ; unifier le genre du badge humain | Q-RANDOM-1 |

## Durcissements

- **Q-002 change de nature.** Ce n'est pas une question de traçabilité mais une manipulation
  expérimentale sans mesure (N1-B). Elle devrait rejoindre Q-010 et Q-011 au rang des
  arbitrages 🔴 bloquants.
- **Q-QUEST-1 change de portée.** Câbler la QuestionnaireScene n'est pas un branchement mais
  une extension du modèle de données et du contrat de PATCH (N1-D).

## Gate supplémentaire suggéré pour la campagne

`campagne-test-validation.md` Axe 7 prévoit une vérification « hypothesis-readiness » (pour
chaque H1–H8, lister les colonnes nécessaires et confirmer leur existence). **Cette revue
montre que cet axe échouerait aujourd'hui sur H1, H4 et H6** (advice-taking) et sur toute
hypothèse impliquant le niveau moteur. Il serait plus économique de l'exécuter **avant** G0
plutôt qu'en fin de campagne.

---

## Index des constats

| Réf | Niveau | Sévérité | Sujet | Divergence |
| :-- | :-- | :-: | :-- | :-- |
| **N1-A** | 1 | 🔴 | Colonnes `*_match_advice` inexistantes | D-007 |
| **N1-B** | 1 | 🔴 | Motor advice sans donnée de résultat | D-008 |
| **N1-C** | 1 | 🔴 | Stimulus distal non loggé | D-009 |
| **N1-D** | 1 | 🔴 | QuestionnaireScene inatteignable, `QuestionConfig` orphelin | D-010 |
| **N1-F** | 1 | 🟢 | `final_comments` implémenté hors codebook | D-015 |
| **N2-A** | 2 | 🔴 | Aléas non seedés sur des manipulations | D-012 |
| **N2-B** | 2 | 🟠 | `human_likeness` répliqué sur tous les trials | D-011 |
| **N2-C** | 2 | 🔴 | `display_probability` absent des docs, contredit TR7 | D-013 |
| **N2-D** | 2 | 🟠 | `block_template_id` hors codebook | D-014 |
| **N2-E** | 2 | 🟠 | Pénalité touche invalide non comptée | — |
| **N2-F** | 2 | 🟠 | Pénalité budget de pas hors GDD | — |
| **N2-G** | 2 | 🟠 | Overlay « Equipment failure » non spécifié | — |
| **N2-H** | 2 | 🟡 | Deux conventions de départage | — |
| **N2-I** | 2 | 🟡 | Input physique vs affichage OS | — |
| **N2-J** | 2 | 🟠 | Endpoint `participant-notes` hors codebook | D-015 |
| **N2-K** | 2 | 🟠 | `CLAUDE.md` périmé (parsing CLI) | — |
| **N2-L** | 2 | 🟡 | Résidus « −2 » dans `TDD.md` | — |
| **N2-M** | 2 | 🟡 | Inventaire des packages incomplet | — |
| **N2-N** | 2 | 🟡 | `readme.md` racine périmé | — |

**Questions ouvertes par cette revue :** Q-MOTOR-1, Q-DISTAL-1, Q-ROW-1, Q-TRUST-1,
Q-FREQ-1, Q-RANDOM-1, Q-HL-1 — cf. [`questions-client.md`](questions-client.md).
