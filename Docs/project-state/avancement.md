# État d'avancement du projet

> Ce fichier est le résumé condensé de l'état du projet.
> Objectif : un rôle peut comprendre où en est le projet en lisant CE SEUL FICHIER.
> Mis à jour après chaque session qui fait avancer le projet.
> Dernière mise à jour : 2026-09-08 (revue de livraison — cf. `revue-livraison-2026-09-08.md`)

---

## Vue macro

**Avancement global estimé :** ~80-85% du périmètre fonctionnel GDD
**Phase actuelle :** Flow multi-écran complet et jouable de bout en bout (10 scènes dans le build). **Free/forced et explanations short/long sont implémentés** — contrairement à ce qu'annonçait la version précédente de ce fichier. Audio, motor advice, tutoriels, randomisation des blocs et pauses inter-blocs livrés.

**Ce qui bloque la livraison** (revues du 28/07 + **revue de livraison du 08/09**, cf. `revue-livraison-2026-09-08.md`) :

1. 🔴 **Deux écrans incomplets** (était trois) — QuestionnaireScene ne collecte rien (et n'est **jamais chargée**, cf. E1 révisé) ; AdvisorChoiceScene montre encore un texte de dev (E3). ✅ **E2 réglé le 08/09/26** : les vrais textes de consentement vivent dans `ConsentScene.unity` (commits `78f9b6e1`, `a4a98569`) et l'écrasement placeholder de `ConsentUI.Start` est neutralisé (`_titleText`/`_bodyText` débranchés dans la scène). Résidus : code mort dans `ConsentUI.cs`, texte non éditable via l'API (pas de champ `consent_text`, cf. Q-CONSENT-1).
2. 🟠 **Le pipeline de données peut encore perdre des trials** (était 🔴) — ✅ la perte systématique de la condition `advisor = none` est **corrigée depuis le 26/08** (commit `fe5fc771`, DEC-026). Restent : aucune persistance locale de la file (`ApiClient.cs:58`, Lot A2), file bloquée après échec (`ApiClient.cs:295-301`, Lot A3), et la ligne encore jetée si le participant abandonne réellement le questionnaire (`TrialManager.cs:145-147`).
3. 🔴 **La donnée de sortie ne couvre pas tout le protocole** — le niveau moteur et le stimulus distal ne sont toujours pas mesurés, l'advice-taking n'a pas de colonne. Progrès depuis le 28/07 : `advisor_path_config`, `map_config.cells[]`, `proximal_advice_reliable` ferment 3 colonnes client ; les valeurs réalisées du motor advice existent désormais en mémoire structurée (`TrialDrawResolver.MotorAdviceDraw`) — **l'export est devenu un branchement trivial**, il ne manque que l'arbitrage Q-MOTOR-1. Voir section ci-dessous.
4. 🔴 **Arbitrages chercheur bloquants** — Q-010 (pattern de fiabilité), Q-011 (mapping des questions + textes réels), **Q-MOTOR-1**, **Q-DISTAL-1**, **Q-RANDOM-1** (volet ordre des advisors ; le volet genre du badge est réglé, cf. `AdvisorBadgeUtility`, commit `48b05b25`).

> ⚠️ Les deux chantiers les plus critiques scientifiquement (`free-forced-choices`, `explanations-short-long`) ont été **implémentés sans que leur `spec-tech.md` soit produite**. Aucune référence technique écrite pour les features qui portent H1–H8.

---

## Livraisons du 24/08 au 08/09/26 (27 commits depuis les revues du 28/07)

| Chantier | Contenu | Commit(s) / décision |
|:---|:---|:---|
| **Correctif pipeline critiques** | Trials `advisor = none` envoyés (champs questionnaire null) ; `optimal_path_length`/`cloud_distance` nullables ; `SessionManager` miroir fidèle de la config ; fingerprint stable côté backend | `fe5fc771` (26/08), DEC-026, MàJ DEC-022 |
| **Communication Report** | Panneau DistalChoiceScene toujours affiché, texte dynamique 3 qualités × advisor/none | `d05931f7`→`485a35ed` (27-28/08), DEC-023 |
| **`advisor_path_config`** | Le chemin **affiché** par l'advisor est loggé (JSON de cellules, null si non visible) — ferme la colonne client 58 | `8c3baa07` (24/08) |
| **`map_config.cells[]`** | État de chaque cellule de la grille (trap/path/wall/cloud/suboptimalPath/playerStart) — ferme PC-8 | `f7c8f3fc` (25/08) |
| **`proximal_advice_reliable_probability` + réalisé** | Paramètre + valeur réalisée `proximal_advice_reliable` — ferme la colonne client 22 | `62a52f58` (24/08) |
| **`green_bugs_session_total`** | Score cumulé de session exporté par trial + affiché dans la BreakScene | `c428bac8` (24/08) |
| **Explanations probabilistes** | `display_mode = "forced/opt-in"` et `content_variant = "short/long"` résolus par trial via `display_mode_forced_probability` / `content_variant_long_probability` ; seules les valeurs résolues sont exportées | `3e5560e5` (24/08) |
| **Refacto config** | DTO sans valeurs par défaut, dashboard = source unique, validation Zod backend ; `BuildInfo.Version = "1.1.0"` source unique de `build_version` | `7d098281` (28/08), DEC-027 |
| **Refacto architecture** | `SeedUtility` (FNV-1a centralisé), `BlockDrawResolver` / `TrialDrawResolver` (tous les tirages bloc/trial avec salts documentés), `AdvisorBadgeUtility` (genre du badge advisor tiré **une fois par bloc, seedé**, champ `advisor_display_is_male` — ferme le volet 9.2 de N2-A), `ModalPanelUIBase` | `48b05b25` (29/08), `d4d69e28` |
| **RoundUI trials forced** | Panneaux succès/échec dédiés aux trials proximal forced | `dd5877ac` (24/08) |
| **EndSession partie 2** | Écran de transition vers la partie 2 (`platform_url`) ; retrait de `ParticipantNoteUI` et de l'endpoint `participant-notes` | `7a86e064` (07/09), DEC-024 |
| **Consent / Welcome** | Vrais textes de consentement et d'accueil posés dans les scènes (feedbacks Mark) — règle E2 | `78f9b6e1`, `a4a98569` (08/09) |
| **Questionnaire 7 points + 2ᵉ acceptabilité** | Échelle Likert 7 points ; 2 questions d'acceptabilité par trial (sliders 1-7) ; agentivité et human-likeness en 7 radios ; colonnes `acceptability_question_1`/`_2` remplacent `acceptability_question` | `ec18eff1`, `128bdcd4`, `e4bd00c7` (07-08/09), DEC-025, DEC-028 |
| **Murs organiques** | Réécriture de `CorridorWallsGenerator` (génération moins rectiligne) | `624f9a99` (PR #3) |

---

## 🔴 Couverture de la donnée de sortie

> Ajouté le 2026-07-28 par `revue-couverture-2026-07-28.md` — première confrontation du code
> au **CSV de référence client** (`Docs/references/CSV_BUGS_Output_V1.xlsx`, 76 colonnes).
> Cette confrontation n'avait jamais été faite : `matrice-tracabilite.md` compare le code au
> CSV *exporté*, jamais au CSV *attendu*.

**Le jeu est couvert. La mesure ne l'est pas.** Le gameplay, le flow et les 4 manipulations
fonctionnent — mais une partie des variables dépendantes du protocole n'atteint jamais la base.

| Réf | Constat | Impact scientifique | Divergence |
| :-- | :-- | :-- | :-- |
| **N1-A** | **Les 3 colonnes `*_match_advice` n'existent pas.** `free-forced-choices/spec-fonc.md:216,319` les déclare « colonnes existantes » — elles venaient du CSV client, jamais implémentées | **L'advice-taking, variable dépendante n°1 (H1, H4, H6), n'a aucune colonne.** Recalculable au distal et sur le chemin ; pas au moteur | D-007 |
| **N1-B** | **Le motor advice n'émet aucune donnée de résultat** — seulement des probabilités de config. Set actif, set affiché, advice donné, advice fiable : rien. ⚠️ MàJ 08/09 : les 4 valeurs réalisées existent désormais proprement dans `TrialDrawResolver.MotorAdviceDraw` — **l'export est devenu un branchement trivial**, seul l'arbitrage Q-MOTOR-1 manque | **Le niveau moteur est inanalysable.** Requalifie Q-002 de « traçabilité » 🟠 en arbitrage 🔴 | D-008 |
| **N1-C** | **Le stimulus distal réel n'est pas enregistré.** `DistalChoiceUI.LeftScanData`/`RightScanData` ne sont lues nulle part. On logge les bornes de tirage, pas ce que le participant voit. Le proximal, lui, logge les valeurs réalisées (`map_config`) | Choix distal non analysable contre le stimulus | D-009 |
| **N1-D** | **E1 révisé : la QuestionnaireScene n'est jamais chargée** — `AdvanceToPhase(GamePhase.Questionnaire)` n'existe nulle part. Et `QuestionConfig` est un **type orphelin** (aucun champ de config ne le porte), et le PATCH ne conserve qu'1 réponse sur N | **Trust in Technology Questionnaire sans chemin de données**, ni en entrée ni en sortie. Q-QUEST-1 à reformuler : câbler ≠ branchement | D-010 |
| **N2-A** | **Aléas non seedés sur des manipulations** : l'ordre des 3 options d'advisor est remélangé à chaque affichage (ni seedé, ni loggé, ni spécifié — `AdvisorChoiceUI.cs:30`, **toujours ouvert**). ✅ Volet genre du badge **réglé le 29/08** (`AdvisorBadgeUtility`, tirage unique par bloc, seedé salt 6, champ `advisor_display_is_male`) | **Biais de position sur le méta-choix invérifiable post-hoc** (volet restant) | D-012 |
| **N2-B** | **`human_likeness_question` est répliqué sur tous les trials du bloc**, pas la dernière ligne — contredit DEC-013 et le codebook | Surpondération ×`trial_count` en analyse. **Ferme PC-7** | D-011 |
| **N2-C** | **`display_probability`** : tirage par trial de l'affichage des explanations proximale/motrice. ⚠️ MàJ 08/09 : mécanisme **étendu** (`3e5560e5`) — `display_mode = "forced/opt-in"` et `content_variant = "short/long"` résolus par trial via `display_mode_forced_probability`/`content_variant_long_probability` ; désormais **documenté** (spec-fonc explanations R9, dictionnaire). L'asymétrie distale demeure (commentée dans le code) | La manipulation de **H8** est maintenant documentée ; reste l'alignement TR7 historique | D-013 — largement résorbé |

**Bilan de la confrontation au CSV client** *(MàJ 08/09/26)* : 76 colonnes attendues, **87 champs émis**
(82 au 28/07). Colonnes client **fermées depuis le 28/07** : n°22 (`proximal_advice_reliability` →
`proximal_advice_reliable_probability` + réalisé), n°55 (`map_layout` → `map_config.cells[]` complet,
ferme PC-8), n°58 (`proximal_advice_path` → `advisor_path_config`). Colonne n°72 : `acceptability_question`
**remplacée par 2 colonnes** `acceptability_question_1`/`_2` (DEC-028). Colonne n°76 (`final_comments`) :
**redevient ❌** — le recueil in-game est supprimé, repris par la partie 2 (DEC-024). Le reste du bilan
du 28/07 tient : l'écart global n'a jamais été acté formellement avec le chercheur.

**Lot E — Couverture de la donnée de sortie** *(à traiter au même rang que le Lot A)* :
E1 logger le résultat du motor advice (**devenu trivial** : lire `TrialDrawResolver.MotorAdviceDraw`) ·
E2 logger le stimulus distal réalisé · E3 colonnes `*_match_advice` (ou acter qu'elles sont
recalculables — la recalculabilité s'est améliorée avec `advisor_path_config` et `map_config.cells`) ·
E4 compteur de touches invalides · E5 seeder et logger l'ordre des advisors (~~genre du badge~~ ✅ fait le 29/08).

> **Le Lot A garantit qu'aucune ligne ne se perd. Le Lot E garantit que les lignes qui
> arrivent permettent de tester les hypothèses.** Les deux sont des pré-requis à une passation.

**Autres constats de la revue de couverture (documentaires, non bloquants)** :
30 scripts sur 63 absents du TDD (13 absents de tout `Docs/`) · pénalité « mauvaise touche »
appliquée mais jamais comptée (N2-E) · pénalité de dépassement du budget de pas absente du
GDD (N2-F) · overlay « Equipment failure » non spécifié (N2-G) · deux conventions de départage
contradictoires (N2-H) · endpoint `participant-notes` non documenté (N1-F/N2-J) — **clos le
07/09/26 : DEC-024 supprime le recueil in-game, le `final_comments` passe en partie 2** ·
`CLAUDE.md` périmé sur le parsing CLI (N2-K) — **corrigé** ·
résidus « −2 » dans le TDD (N2-L) — **corrigés (plus aucune occurrence au 08/09)** ·
inventaire des packages incomplet (N2-M — à revoir lors de la passe TDD) ·
`readme.md` racine périmé (N2-N) — **réécrit le 08/09/26**.

---

## Ce qui est TERMINÉ et stable

### Cœur gameplay (forest screen)

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
| Seeded RNG (FNV-1a, reproductibilité) | ✅ Stable, documenté | 27/02/26 |
| Step Budget Penalty (dépassement distance Manhattan) | ✅ Stable, documenté | 02/03/26 |

### Flow multi-écran et data

| Composant | État | Depuis |
|:---|:---|:---|
| FlowController (DDOL, transitions fade) | ✅ Implémenté | — |
| ApiClient (HTTP unique, POST/PATCH) | ✅ Implémenté | — |
| FadeTransition (DDOL) | ✅ Implémenté | — |
| TrialManager (assemble + envoi) | ✅ Stable | 02/03/26 |
| Ordre joué des blocs (randomisation, verrous, tutoriels, fingerprints) | ✅ Implémenté | 23/07/26 |
| Pauses obligatoires inter-blocs (compteur hors tutoriel, countdown API) | ✅ Implémenté | 23/07/26 |
| **Free/forced (meta / distal / proximal / motor)** | ✅ Implémenté — `FlowController.cs:99-171`, `239-256` | 28/07/26 (constaté) |
| **Explanations short/long (3 advices, opt-in + tracking)** | ✅ Implémenté — `ExplanationResolver.cs` | 28/07/26 (constaté) |
| **Motor advice (ZQSD / TFGH / IJKL, visible × fiable)** | ✅ Implémenté — `MotorAdviceController.cs` | 28/07/26 (constaté) |
| **Redirect post-expérience (platform_url)** | ✅ Implémenté — `PlatformUrlDisplay.cs` | 28/07/26 (constaté) |
| Spec tech multi-écran flow | ✅ Finalisée | 16/03/26 |
| Data model Supabase (`trial_responses` plate) | ✅ Finalisé | 16/03/26 |
| Décisions DEC-001 à DEC-028 | ✅ Enregistrées | 08/09/26 |
| **Spec tech `free-forced-choices`** | ❌ **Jamais produite** — code livré sans référence technique | — |
| **Spec tech `explanations-short-long`** | ❌ **Jamais produite** — code livré sans référence technique | — |

### Scènes du flow

| Scène | État |
|:---|:---|
| BootScene, IntroScene | ✅ Créées |
| ConsentScene + ConsentUI (gate, DEC-012) | ✅ **E2 réglé le 08/09/26** — vrais textes dans la scène (`78f9b6e1`, `a4a98569`), écrasement placeholder neutralisé (`_titleText`/`_bodyText` débranchés, `ConsentScene.unity:240-241`), modale de refus via `FlowContinueScreenUI`. Résidus : code mort dans `ConsentUI.cs`, texte non éditable via l'API (Q-CONSENT-1 option A de facto) |
| WelcomeScene | ✅ Créée |
| AdvisorChoiceScene + AdvisorChoiceUI + AdvisorOptionButton | 🔴 Fonctionnelle mais **affiche un texte de dev périmé** au participant — cf. E3 |
| DistalChoiceScene + DistalChoiceUI + DistalValleyScanView | ✅ Implémentées — Communication Report toujours affiché, texte dynamique 3 cas × advisor (DEC-023, 27/08/26) |
| ProximalScene (forest screen) + RoundUI + TrialQuestionsUI | ✅ Implémentée — c'est **ici** que les dimensions sont réellement collectées. Depuis le 08/09 : **4 questions** (2 acceptabilité en slider 1-7, agentivité et human-likeness en 7 radios), échelle 7 points (DEC-025, DEC-028). RoundUI a des panneaux dédiés aux trials proximal forced (`dd5877ac`) |
| QuestionnaireScene + QuestionnaireUI (DEC-008) | 🔴 **Coquille vide** — s'auto-enchaîne avec zéro réponse, cf. E1 |
| EndSessionScene + PlatformUrlDisplay | ✅ Implémentée — **écran de transition vers la partie 2** (DEC-024, 07/09/26), plus l'écran terminal. Le recueil de commentaire libre a été retiré ; le bouton vers `platform_url` est toujours visible. ⚠️ `platform_url` doit être renseigné dans le dashboard, sinon la partie 2 est inatteignable |
| BreakScene + countdown et reprise verrouillée | ✅ Scène créée, bindée et dans le build — affiche depuis le 24/08 le **total de bugs verts de la session** (`c428bac8`). Le BLOCK_RECAP complet du plan §2.6 (advisor utilisé, vallée choisie, perf comparative) reste non fait (F5) |

#### 🔴 Écrans incomplets — détail

| Réf | Écran | Constat (preuve code) |
|:---|:---|:---|
| **E1** | QuestionnaireScene | **Révisé le 28/07/26 (N1-D)** : la scène **n'est jamais chargée** — `AdvanceToPhase(GamePhase.Questionnaire)` n'existe nulle part (12 sites vérifiés), alors qu'elle est activée dans le build. Blocage **triple** : (1) `QuestionConfig` est un **type orphelin** — aucun champ de `SessionConfig`/`BlockConfig` ne le porte, le chercheur ne peut rien saisir ; (2) liste hardcodée vide (`QuestionnaireUI.cs:38`) ; (3) `ApiClient.cs:136-147` ne conserve **que** `human_likeness_question` et jette les autres réponses. **À retirer, ou à construire — ce n'est pas un simple câblage.** |
| ~~**E2**~~ | ConsentScene | ✅ **Réglé le 08/09/26.** Les vrais textes (anglais, expérience en 2 parties, contact IJN.Agency) vivent dans `ConsentScene.unity` ; l'écrasement au `Start` est neutralisé car `_titleText`/`_bodyText` sont débranchés dans la scène (`fileID: 0`). Résidus non bloquants : code mort dans `ConsentUI.cs` (placeholders + message « la session s'arrête ici »), toujours pas de champ `consent_text` dans `SessionConfig` — toute retouche du texte impose un rebuild (Q-CONSENT-1, option A de facto). |
| **E3** | AdvisorChoiceScene | `AdvisorChoiceUI.cs:56` : « Ce choix est enregistre mais n'affecte pas encore la generation de la map » — faux depuis l'implémentation du free/forced. |

### Audio (DEC-016, 12/05/26)

| Composant | État |
|:---|:---|
| AudioManager singleton DDOL (BootScene) | ✅ Implémenté |
| ScriptableObjects MusicTrack / SoundEffect | ✅ Implémentés |
| SceneMusic component (musique + ambient par scène) | ✅ Implémenté |
| UIButtonSound (SFX UI réutilisable) | ✅ Implémenté |
| AudioMixer 5 groupes (Master/Music/Ambience/SFX_Gameplay/SFX_UI) | ✅ Configuré |

### UI — Composants tutoriels (overlay)

| Composant | État |
|:---|:---|
| HowToPlayUI (tutoriel paginé onboarding) + sandbox | ✅ Composant livré (specs `how-to-play`, 28/05/26) — branchement flow par l'intégrateur |
| InContextTutorialUI (overlay instructions sur scène de jeu) + sandbox | ✅ Composant livré (specs `in-context-tutorial`, 29/05/26, DEC-020) — agnostique, testable en isolation |
| InContextTutorialSceneBinder + pose dans les 3 scènes | ✅ **Intégration terminée** : `BlockConfig.in_context_tutorial` existe, `LoadContent()` est branché (`InContextTutorialSceneBinder.cs:83`), et la course verrou input vs `GetReadySequence` est résolue (`GameManager.cs:59`). La note « non résolu » de `in-context-tutorial/integration-guide.md:195-199` est **périmée**. |

### Outillage / documentation

| Composant | État |
|:---|:---|
| TutorialSessionFactory (DEC-014) | ❌ **Supprimée du projet** — créée le 17/03/26, plus aucun fichier `TutorialSessionFactory*` sous `Assets/`. Ligne restée fausse jusqu'au 28/07/26. DEC-014 à réexaminer : le besoin est-il couvert autrement (blocs `is_tutorial` + `BlockOrderRandomizer`) ou abandonné ? |
| TDD v2.5 + diagrammes Mermaid architecture | ✅ À jour |
| Matrice responsabilités systèmes | ✅ Documentée dans spec-tech |

---

## Analyse de gap vs GDD 2.0 (23/11/25)

### 🟡 Partiellement couvert

| Périmètre GDD | Gap identifié |
|:---|:---|
| **Distal advice / écran distal** | UI créée mais le GDD demande des "shimmering lights" green/red et un paramètre de **visibility/saturation noise**. À auditer. |
| **Proximal advice reliability** | Path optimal calculé, mais la notion de **sub-optimal path avec pièges en cas d'advice unreliable** n'est pas confirmée. |
| **Frequency/pattern d'apparition de l'advice** | GDD : "advice provided periodically following a given pattern". Pas de paramètre fréquence trouvé. |
| **Questionnaire in-game (3 dimensions)** | Collecte fonctionnelle via `TrialQuestionsUI` — depuis le 08/09 : **4 questions** (acceptability ×2 en slider 1-7, sens_of_agency et human_likeness en 7 radios, DEC-025/DEC-028). Mais les **4 textes sont des placeholders** (`TrialQuestionsUI.cs:29-32` : « (a definir) ») et le mapping aux dimensions GDD reste à confirmer (Q-011). |
| **Saturation/blur noise sur clouds proximaux** | Ratio bug exposé, pas de paramètre de **visibility noise** sur le rendu. À vérifier shader/particules. |
| **Motor advice — colonnes CSV, explanation, format** | Q-002 et Q-004 toujours en attente. Q-003 RÉSOLUE par DEC-017 (explanation motor confirmée, cf. spec explanations-short-long). |

### 🔴 Non couvert ou non démarré

| Périmètre GDD | Impact |
|:---|:---|
| **Écrans incomplets E1 / E3** (E2 réglé le 08/09) | QuestionnaireScene vide et jamais chargée, texte de dev en prod sur AdvisorChoice. Cf. section « Scènes du flow ». |
| **Pertes de trials silencieuses** (réduites depuis le 26/08) | ✅ Trials `advisor = none` corrigés (`fe5fc771`, DEC-026). Restent : ligne jetée si abandon réel du questionnaire (`TrialManager.cs:145-147`) ; file d'envoi en mémoire seule (`ApiClient.cs:58`) ; file bloquée après échec (`ApiClient.cs:295-301`). |
| **Pas de protocole de sortie** | `SettingsPanelUI.cs:61-68` = `Application.Quit()` sec. Ni confirmation, ni sauvegarde partielle, ni flag `session_abandoned` (plan de référence §2.8). |
| **`visibility_noise`** | Inexistant dans le code et dans `TrialResponseRow`. Attendu par `specs-light` §2 et le plan de référence §2.3. Q-013. |
| **Retour en arrière non bloqué** | `LevelRegistry.cs:266` : `IsWalkable = InBounds && !IsWall`. `specs-light` §4 l'interdit. Le flag `Visited` existe mais ne sert qu'au fog. **À trancher : probablement remplacé de fait par le budget de pas.** |
| **4 paths visibles (2 par cloud)** | GDD : 4 paths. Code : 2 paths (`PathSpawner.cs:129-130`). Q-008. |
| **Smooth camera pan + auto-walk entre trials** | GDD demande transition smooth verticale ; DEC-002 a tranché fade noir → écart à reconfirmer. |
| **Mountain UI** (progression dans le bloc, DEC-003) | Criticité basse, mais prévue. Prefab `EndOfBlockPanel_TODO.prefab` non fini. |
| **Reliability manipulation pattern documenté** | Section GDD vide ("...") → spec chercheur manquante. Q-010. |
| **Nb trials/block = 36 et nb training blocks** | Paramètres existent, valeurs cibles non actées. Q-012. |
| **Boot hors-ligne impossible** | `FlowController.cs:392-434` exige un `sessionId` + fetch réussi. Seul retour d'erreur visible : barre de chargement figée à 90 %. |

> ✅ **Sorti de cette liste depuis la revue du 28/07/26** : free/forced, explanations short/long, redirect post-expérience, modèle de perte de bugs (le code applique bien −1 vert par nuage par piège, cf. `GameManager.cs:262-275` + `BugCloud.cs:67-84` — conforme GDD, reste le sign-off chercheur), end-of-block (BreakScene + RoundUI).

---

## Décisions en attente

| Réf | Sujet | Bloque quoi | Urgence |
|:---|:---|:---|:---|
| Q-002 → **Q-MOTOR-1** | Colonnes CSV motor advice | ~~Traçabilité data~~ → **le niveau moteur n'a aucune donnée de résultat** (N1-B) | 🔴 **Arbitrage bloquant** — rejoint Q-010/Q-011 |
| **Q-DISTAL-1** | Logger le stimulus distal réellement affiché ? | Analyse du choix distal | 🔴 **Nouveau 28/07/26** (N1-C) |
| **Q-RANDOM-1** | Ordre des advisors et apparence de l'advisor humain : à contrôler ? | Biais de position sur le méta-choix (H1) | 🔴 **Nouveau 28/07/26** (N2-A) |
| **Q-ROW-1** | 1 ligne par écran vs 1 ligne par trial : acter la divergence | Interprétation de tout export | 🟠 **Nouveau 28/07/26** |
| **Q-TRUST-1** | Trust in Technology dans le jeu ou via Qualtrics (DEC-005) ? | H7 ; conditionne Q-QUEST-1 | 🔴 **Nouveau 28/07/26** (N1-D) |
| **Q-FREQ-1** | Fréquence des questions trial-wise (volet échelle ✅ tranché par DEC-025 : 7 points) | Charge participant — désormais **3 questions/trial** (+1 fin de bloc) soit ≈ **108 interruptions** à 36 trials/bloc | 🟠 **Nouveau 28/07/26** — volet fréquence toujours ouvert |
| **Q-HL-1** | `human_likeness` sur toutes les lignes ou la dernière ? | Sémantique de la colonne | 🟠 **Nouveau 28/07/26** (N2-B) |
| Q-003 | Motor advice avec explanation ? | — | ✅ RÉSOLUE par DEC-017 |
| Q-004 | Format d'affichage du set de touches | UI + compréhension participant | 🟡 Bloque polish UI |
| Q-006 | Modèle d'édition des explanations | — | ✅ RÉSOLUE par DEC-017 (session config panel) |
| Q-EXP-1, 2, 3, 4, 5, 7, 8, 9 | Sous-questions de cadrage Explanations | — | ✅ RÉSOLUES par DEC-018 (2026-05-26) |
| Q-EXP-6 | Modalités UI hide / no-overlap | UI test à mener avant freeze UI | 🟡 |
| Q-EXP-10 | Localisation corpus FR/multilingue | Structure config | 🟢 Non bloquante |
| Q-005 + Q-FF-1..11 | Free/forced choices : périmètre + arbitrages | — | ✅ RÉSOLUE par DEC-019 (26/05/26) — spec fonc `validé`. Q-FF-12 et Q-FF-13 résiduelles 🟡 non bloquantes. |
| Q-FF-12, Q-FF-13 | Forced × reliability ; forced en tutorial | — | 🟡 Non bloquantes — reportées dans `questions-client.md` le 28/07/26 |
| — | **Texte de consentement réel** | ConsentScene (E2) — accord éthique | ✅ **Réglé le 08/09/26** — vrais textes posés dans la scène (option A de facto : figé, rebuild requis pour toute retouche). Reste le sign-off formel Q-CONSENT-1 si l'éditabilité API est souhaitée |
| — | **QuestionnaireScene : câbler ou retirer ?** | E1 — un chargement de scène pour rien | 🔴 **Nouveau 28/07/26** |
| — | Modèle de perte de bugs (1 green/piège/cloud vs total décrémenté) | Cohérence protocole | 🟡 **Le code applique déjà le modèle GDD** — reste le sign-off chercheur (Q-007) |
| — | 4 paths visibles vs 2 paths | Cohérence GDD | 🟡 Écart design assumé ? |
| — | Smooth pan vs fade noir | Cohérence GDD | 🟡 DEC-002 à reconfirmer |
| — | Retour en arrière autorisé (specs-light §4) | Cohérence protocole | 🟡 **Nouveau 28/07/26** — non implémenté, sans doute remplacé par le budget de pas |
| — | Preview floue distal screen | Rendu : statique vs dynamique | 🟢 Peut attendre design |

---

## Risques actifs

| Risque | Impact | Statut |
|:---|:---|:---|
| **Toute trial en condition `advisor = none` est perdue** | **La condition contrôle de l'étude ne remonte jamais en base** | ✅ **Levé le 26/08/26** (`fe5fc771`, DEC-026) — `TrialManager.cs:143-148` envoie la ligne avec les champs questionnaire à `null` (omis du payload). Reste le sign-off Q-DATA-1 (option A « null assumé » tranchée de facto) |
| **Trial jeté si questionnaire réellement incomplet** (participant qui abandonne en cours de questionnaire) | Perte sèche de tout le gameplay du trial | 🟠 **Résidu du Lot A1** — un `sens_of_agency_question` vide annule encore l'envoi avec un simple `LogWarning` (`TrialManager.cs:145-147`) |
| **Aucune persistance locale de la file d'envoi** (`ApiClient.cs:58`) | Fermeture navigateur / crash = trials perdus | 🔴 **Lot A2** — non corrigé. Confirme PC-3 de `journal-divergences.md` |
| **File d'envoi bloquée après échec** (`ApiClient.cs:295-301`) | Échec sur le dernier trial = jamais rejoué | 🔴 **Lot A3** — non corrigé. Confirme PC-2 |
| **QuestionnaireScene ne collecte rien** (E1) | Écran mort dans le flow | 🔴 À câbler ou retirer |
| **Texte de consentement inaffichable** (E2) | Conformité éthique | ✅ **Levé le 08/09/26** — vrais textes en scène, écrasement placeholder neutralisé |
| **Reliability pattern non spécifié côté chercheur** | Impossible d'implémenter la manipulation | 🔴 Q-010 — spec chercheur manquante |
| **Mapping des 3 questions non tranché + textes placeholders** | Validité du questionnaire | 🔴 Q-011 |
| **`spec-tech` absentes pour free/forced et explanations** | Aucune référence opposable sur H1–H8 | 🟠 Rétro-documentation à produire |
| `motor_forced_set` : fallback silencieux vers ZQSD | Condition expérimentale fausse sans trace | 🟠 **Lot A4** — non corrigé (`FlowValueConverters.ToMotorKeySet`, `FlowDataModels.cs:682-683` ; accepte désormais aussi les graphies `ZQSD`/`TFGH`/`IJKL`, mais une valeur inconnue retombe toujours sur ZQSD sans log). Atténué par DEC-027 : la validation Zod côté backend est censée garantir le domaine |
| `build_version` incohérent (0.4.0 vs 1.0.0) | Colonne CSV ambiguë | ✅ **Levé le 28/08/26** — constante unique `BuildInfo.Version` (Lot A5) |
| **Modèle de perte de bugs divergent code vs GDD** | Résultats non comparables au protocole | ✅ **Levé le 28/07/26** — le code applique le modèle GDD ; reste le sign-off Q-007 |
| **Free/forced choices : implémentation absente** | Validité expérimentale | ✅ **Levé le 28/07/26** — implémenté |
| **Explanations short/long absentes** → H8 non testable | Validité expérimentale | ✅ **Levé le 28/07/26** — implémenté |
| State leaking entre trials dans le flow multi-écran | Données recherche corrompues | ✅ Couvert par spec tech |
| WebGL + System.Environment.GetCommandLineArgs | Potentiellement non fonctionnel | ✅ Résolu par DEC-009 |
| **Contrat de colonnes du questionnaire non vérifié** — le code POSTe désormais `acceptability_question_1`/`_2`/`sens_of_agency_question`/`human_likeness_question` (4 champs, DEC-028) ; le schéma SQL de référence (`specs/multi-screen-flow/spec-tech.md:766`) déclare toujours `q1_response`/`q2_response`/`q3_response` et ignore la 2ᵉ acceptabilité | Si la table Supabase suit le schéma documenté, les réponses n'atterrissent nulle part | 🟠 **À vérifier côté back-end** — hypothèse, pas constat (cf. F8/D-003). ⚠️ S'assurer aussi que la table a bien reçu la colonne `acceptability_question_2` |
| **`TDD.md` contredit ce fichier sur la pénalité** — 18 occurrences décrivaient −2 bugs, le code applique −1 vert | Un nouvel arrivant lit une règle de scoring fausse | ✅ **Corrigé le 28/07/26** via le protocole `!doc` (cf. D-002) |
| **Campagne de validation données jamais exécutée** | CSV non confronté au code, codebook non signé | 🟠 6 gates G0–G5 encore ⬜ |
| **`plan-global.md`, `risques.md`, `session-map.md` périmés (~4,5 mois)** | Pilotage sur info fausse | ✅ Archivés le 28/07/26 |

---

## Recommandations de priorisation

> Détail complet des lots dans `revue-completude-2026-07-28.md`.

1. **Arbitrages chercheur bloquants** — Q-010 (pattern de fiabilité), Q-011 (mapping des 4 questions + textes réels), **décision sur QuestionnaireScene** (E1). ~~Texte de consentement (E2)~~ ✅ réglé le 08/09. Rien d'autre ne peut être figé sans ça.
2. **Lot A — fiabiliser le pipeline de données.** A1 ne plus jeter le trial si le questionnaire est incomplet (✅ **fait le 26/08/26**, `fe5fc771`/DEC-026 — reste le résidu « abandon réel », `TrialManager.cs:145-147`) · A2 persister la file d'envoi · A3 débloquer la file après échec · A4 supprimer le fallback silencieux `motor_forced_set` · A5 unifier `build_version` (✅ fait le 28/08/26, `BuildInfo.Version`). **Aucune passation avec de vrais participants avant A2/A3.**
2 bis. **Lot E — couvrir la donnée de sortie** (même rang de priorité que le Lot A) : E1 résultat du motor advice (**devenu trivial** via `TrialDrawResolver.MotorAdviceDraw`) · E2 stimulus distal réalisé · E3 colonnes `*_match_advice` · E4 compteur de touches invalides · E5 ordre des advisors seedé et loggé (volet genre du badge ✅ fait le 29/08). Dépend des arbitrages Q-MOTOR-1 / Q-DISTAL-1 / Q-RANDOM-1.
3. **Nettoyer les textes placeholders** — E3 (`AdvisorChoiceUI.cs:56`) et F6 (`TrialQuestionsUI.cs:29-32`, 4 textes). Rapide et visible par le participant.
4. **Lot D — exécuter la campagne de validation** (`Docs/validation/`) : ouvrir G0, dérouler les axes 1→7, jusqu'au codebook signé (G5).
5. **Arbitrer les écarts GDD restants** : 4 paths vs 2, smooth pan vs fade, retour en arrière, `visibility_noise`.
6. **Dette documentaire** — produire a posteriori les 2 `spec-tech.md` manquantes par rétro-documentation du code, et compléter le TDD (section 7 manquante, ~27 classes non documentées, type `ValleyPreview` fantôme).
7. **Compléter** Mountain UI et le protocole de sortie (F4) — criticité moyenne, après les blocs scientifiques.
