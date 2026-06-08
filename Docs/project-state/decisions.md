# Registre des décisions

> Ce fichier est la source de vérité pour les décisions prises sur le projet.
> Mis à jour par tous les rôles en fin de session. Consulté par tous les rôles en début de session.
> Append-only : ne jamais supprimer une entrée. Marquer `ANNULÉE` si une décision est invalidée.

---

## Format

```
### DEC-[NNN] — [Titre court]
- **Date :** YYYY-MM-DD
- **Tag :** [SCOPE] / [FONC] / [TECH] / [PLANNING] / [CLIENT]
- **Décision :** [Ce qui a été décidé, en 1-2 phrases]
- **Raison :** [Pourquoi]
- **Impact :** [Ce que ça change pour la suite]
- **Statut :** ACTIF / EN ATTENTE / ANNULÉE
```

---

## Décisions

### DEC-001 — Meta-choice une fois par bloc
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** Le participant fait un meta-choice (confirmer ou changer de stratégie) une seule fois au début de chaque bloc, pas à chaque trial.
- **Raison :** Évite la fatigue décisionnelle. Le chercheur veut mesurer la stratégie globale, pas les micro-ajustements.
- **Impact :** L'écran meta-choice n'apparaît qu'en début de bloc. Le flow intra-bloc est simplifié.
- **Statut :** ACTIF

### DEC-002 — Transitions par fade noir
- **Date :** 2026-03-03
- **Tag :** [TECH]
- **Décision :** Toutes les transitions entre écrans utilisent un fade to black / fade from black.
- **Raison :** Simple, universel, pas de risque d'état visible incohérent pendant la transition.
- **Impact :** Un composant FadeTransition sera nécessaire. Durée configurable.
- **Statut :** ACTIF

### DEC-003 — UI montagne pour progression dans le bloc
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** La progression dans un bloc est visualisée par une métaphore de montagne (ascension).
- **Raison :** Cohérence thématique avec le jeu d'exploration. Feedback visuel motivant.
- **Impact :** Nécessite un écran dédié ou un overlay. Design visuel à préciser.
- **Statut :** ACTIF

### DEC-004 — Consent screen dans le scope
- **Date :** 2026-03-03
- **Tag :** [SCOPE]
- **Décision :** L'écran de consentement éclairé est dans le périmètre du jeu, pas géré par une plateforme externe.
- **Raison :** Contrôle total du flow. Données de consentement traçables dans le pipeline.
- **Impact :** Un écran consent sera nécessaire avec checkbox obligatoire + bouton continuer.
- **Statut :** ACTIF

### DEC-005 — Questionnaires hors scope
- **Date :** 2026-03-03
- **Tag :** [SCOPE]
- **Décision :** Les questionnaires pré/post expérience sont hors scope du jeu Unity. Gérés par un outil externe (Qualtrics ou similaire).
- **Raison :** Réutiliser des outils spécialisés plutôt que recoder un moteur de questionnaire.
- **Impact :** Le flow du jeu démarre après le questionnaire pré et se termine avant le questionnaire post. Redirection URL à prévoir.
- **Statut :** ACTIF

### DEC-006 — Score cumulé entre trials
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** EN ATTENTE — le score se cumule-t-il entre les trials d'un même bloc, ou est-il remis à zéro ?
- **Raison :** Impacte la mécanique de feedback et la motivation du participant.
- **Impact :** Si cumulé → besoin d'un ScoreManager persistant. Si reset → le score est local au trial.
- **Statut :** RÉSOLU → voir DEC-010

### DEC-007 — Claude Code comme environnement unique pour tous les rôles
- **Date :** 2026-03-03
- **Tag :** [TECH]
- **Décision :** Les 5 rôles de gestion de projet (pilotage, analyse fonc, archi tech, planification, support dev) sont intégrés dans Claude Code via des fichiers de rôle dans le repo, plutôt que dans des projets Claude séparés via le navigateur.
- **Raison :** Évite la duplication de documents entre projets, permet la traçabilité git, et unifie l'environnement de travail.
- **Impact :** Structure `Docs/roles/` + `Docs/project-state/` ajoutée au repo. `CLAUDE.md` enrichi avec un système d'aiguillage.
- **Statut :** ACTIF

### DEC-008 — Micro-questionnaires post-bloc in-game
- **Date :** 2026-03-16
- **Tag :** [SCOPE]
- **Décision :** Les 2-3 questions posées après chaque bloc sont intégrées directement dans le jeu Unity (scène dédiée), pas dans un outil externe. Les réponses sont enregistrées comme partie du bloc en cours. Les mêmes questions sont reposées à chaque bloc.
- **Raison :** Les questions font partie intégrante du flux expérimental de chaque bloc. Les réponses doivent être associées au bloc courant. Pas de redirection vers un outil externe entre les blocs.
- **Impact :** Nécessite une scène QuestionnaireScene, un composant QuestionnaireUI. Les réponses sont stockées comme colonnes (`q1_text`/`q1_response`, `q2`, `q3`) dans la table `trial_responses`, remplies uniquement sur la dernière ligne du bloc. DEC-005 reste valide pour les questionnaires pré/post expérience longs (Qualtrics).
- **Statut :** ACTIF

### DEC-009 — Config de session chargée par URL + API
- **Date :** 2026-03-16
- **Tag :** [TECH]
- **Décision :** L'identifiant de session est passé via un paramètre d'URL (ex: `game.com/?session=abc-123`). Le jeu fait un appel API au boot pour récupérer la config complète depuis Supabase. Les args CLI de SessionManager deviennent un fallback développeur uniquement.
- **Raison :** Les args CLI ne fonctionnent pas en WebGL. L'URL est le seul vecteur fiable. Passer tous les paramètres dans l'URL serait trop long — un seul ID suffit, le reste est chargé côté serveur.
- **Impact :** Refonte de SessionManager pour lire depuis FlowController au lieu du CLI. Nouveau composant ApiClient pour les appels HTTP. Le dashboard web génère des URLs de lancement avec le session_id.
- **Statut :** ACTIF

### DEC-010 — Score cumulé dans le bloc
- **Date :** 2026-03-16
- **Tag :** [FONC]
- **Décision :** Le score se cumule entre les trials d'un même bloc. Il est remis à zéro au début de chaque nouveau bloc.
- **Raison :** Réponse du développeur/chercheur. Donne un sens de progression cohérent sur la durée du bloc.
- **Impact :** FlowController porte un accumulateur `green_bugs_accumulated` persistant entre trials, remis à 0 à chaque nouveau bloc. Colonne `green_bugs_accumulated` dans `trial_responses`. DEC-006 est résolue.
- **Statut :** ACTIF

### DEC-006 — Score cumulé entre trials *(résolu)*
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** RÉSOLU par DEC-010 — le score est cumulé dans le bloc.
- **Raison :** —
- **Impact :** —
- **Statut :** RÉSOLU → voir DEC-010

### DEC-011 — Table plate `trial_responses` dénormalisée
- **Date :** 2026-03-16
- **Tag :** [TECH]
- **Décision :** Toutes les données d'un essai sont stockées dans une seule table `trial_responses`, une ligne par essai. Table plate, dénormalisée, exportable en CSV sans jointure. Les colonnes embarquent : identification, paramètres du bloc, choix du joueur, config de la map, paramètres advisor, résultats gameplay, questionnaire (colonnes q1..q3 sur le dernier essai du bloc).
- **Raison :** Les chercheurs ont besoin d'un CSV auto-suffisant pour leurs analyses. Chaque ligne doit contenir tout le contexte. Pas de jointures.
- **Impact :** Remplace l'architecture normalisée (participant_sessions, participant_blocks, trials, question_responses) par une seule table. Simplifie le code Unity (1 POST par essai). Le dashboard expose un bouton "Exporter CSV" qui fait un `SELECT *`.
- **Statut :** ACTIF

### DEC-012 — Consent = gate, pas de tracking
- **Date :** 2026-03-16
- **Tag :** [FONC]
- **Décision :** L'écran de consentement est un gate client-side. Si le joueur accepte → le jeu continue. Si le joueur refuse → le jeu s'arrête. Rien n'est enregistré en base concernant le consentement.
- **Raison :** Le consentement est un prérequis éthique, pas une donnée de recherche. Si le joueur refuse, il n'y a pas de participant_id et rien à tracer.
- **Impact :** ConsentScene reste dans le flow mais ne communique pas avec l'API. DEC-004 est affinée (consent in scope mais sans tracking).
- **Statut :** ACTIF

### DEC-013 — Questionnaire envoyé par PATCH
- **Date :** 2026-03-16
- **Tag :** [TECH]
- **Décision :** Les réponses au questionnaire post-bloc sont envoyées par un PATCH sur la dernière ligne `trial_responses` du bloc (celle dont l'UUID est conservé dans `State.last_trial_response_id`), plutôt que de retarder l'envoi du dernier trial.
- **Raison :** Découple l'envoi du trial (immédiat, après collecte) de l'envoi du questionnaire (après réponses). Si le joueur ferme le navigateur avant le questionnaire, les données du trial sont déjà en base. Le PATCH ne peut modifier que les colonnes `q1..q3`.
- **Impact :** `ApiClient` expose une méthode `PatchQuestionnaireResponses()`. `FlowController.State.last_trial_response_id` est mis à jour à chaque POST réussi. Le flow est : POST dernier trial → UI questionnaire → PATCH q1..q3.
- **Statut :** ACTIF

### DEC-014 — Tutorial modélisé comme BlockConfig
- **Date :** 2026-03-16
- **Tag :** [TECH]
- **Décision :** Le bloc tutorial est modélisé comme un `BlockConfig` normal avec un flag `is_tutorial: true`. Config hardcodée (pas chargée depuis l'API). Parcourt le même flow qu'un bloc normal (Advisor → Distal → Proximal ×N) mais ne déclenche aucun envoi API et n'a pas de questionnaire.
- **Raison :** Réutilise l'architecture existante sans branche spéciale. Permet d'activer/désactiver le tutorial via `tutorial_enabled` dans SessionConfig.
- **Impact :** `BlockConfig` a un champ `is_tutorial`. `TrialManager` skip l'envoi si tutorial. `FlowController` skip le questionnaire si tutorial. Pas de ligne en base pour les trials tuto.
- **Statut :** ACTIF

### DEC-015 — participant_id généré client-side
- **Date :** 2026-03-16
- **Tag :** [TECH]
- **Décision :** Le `participant_id` est un UUID généré côté client via `System.Guid.NewGuid()` dans `FlowController.Initialize()`. Pas d'appel serveur pour créer le participant.
- **Raison :** Simplifie le boot (une requête HTTP en moins). Le participant n'existe en base que comme colonne dans `trial_responses`, pas comme entité séparée. L'UUID client est suffisamment unique.
- **Impact :** Plus de table `participants`. Plus de méthode `CreateParticipant()` sur ApiClient. Le `participant_id` apparaît pour la première fois quand le premier trial est POST.
- **Statut :** ACTIF

### DEC-017 — Explanations short/long : périmètre et modèle d'édition
- **Date :** 2026-05-20
- **Tag :** [FONC]
- **Décision :** La feature `Explanations short/long` couvre les **3 advice** : distal, proximal, motor. Pour chaque advice, le chercheur édite **deux textes alternatifs** (short et long) **dans le session config panel**. Le mode actif sur un trial est `none` | `short` | `long`. **Précondition d'affichage** : une explanation n'est affichée que si (a) un advisor a été choisi par le participant ET (b) un advice est effectivement donné sur ce trial. Sinon, mode = `none` forcé.
- **Raison :** Cadrage en session avec le pilotage. Couvre H8 (explanations × abstraction) sur les 3 niveaux. Résout Q-003 (motor advice doit aussi porter une explanation) et Q-006 (modèle d'édition = session config panel).
- **Impact :** Spec fonc `Docs/specs/explanations-short-long/spec-fonc.md` produite. 3 colonnes ajoutées dans `trial_responses` : `distal_advice_explanation_mode`, `proximal_advice_explanation_mode`, `motor_advice_explanation_mode` (valeurs `none/short/long`). Q-003 et Q-006 marquées RÉSOLUES par renvoi à cette spec. Granularité de pilotage (par bloc / par trial / mixte) et 9 autres points restent ouverts dans la spec (Q-EXP-1 à Q-EXP-10) et seront affinés à la validation chercheur.
- **Statut :** ACTIF

### DEC-018 — Explanations short/long : consolidation post-échange chercheurs
- **Date :** 2026-05-26
- **Tag :** [FONC]
- **Décision :** Consolidation de 8 des 10 sous-questions ouvertes du chantier Explanations (Q-EXP-1, 2, 3, 4, 5, 7, 8, 9). Introduction d'une nouvelle dimension `display_mode` ∈ {`forced`, `opt-in`, `none`} orthogonale au `content_variant` ∈ {`short`, `long`}. Granularité **blockwise** pour les deux dimensions. **Cross-level interdit** (1-to-1). Jusqu'à **3 explanations simultanées** par trial (1 par advice), layout L/R (motor à gauche, proximal à droite). **Corpus indexé par advisor type** : 2 corpora distincts par bloc (human-bot, bot-bot), 12 textes par bloc au total. **Format CSV** : enum + id côte à côte. **Tracking opt-in obligatoire** quand `display_mode = opt-in` : enregistrement du clic et de la durée d'affichage. **Contrainte UI** : no-overlap avec la map. Principe directeur : « keep it simple, adapt later ».
- **Raison :** Échange mail Florian / Valerian / Mark (mai 2026). Valerian s'est exprimé sur les 9 questions, Mark sur Q1/Q4/Q5/Q6 ; synthèse finale Florian sous principe « keep it simple ». Convergence sur le `display_mode` à 3 valeurs (Mark + Valerian). Indexation par advisor type retenue malgré l'absence de réponse de Mark : intérêt scientifique à différencier le framing AI (Valerian). Tracking opt-in indispensable au sens scientifique du mode opt-in (demande Mark).
- **Impact :** Spec fonc révisée le 2026-05-26 (statut reste `draft` car Q-EXP-6 et Q-EXP-10 toujours ouvertes). **15 colonnes** dans `trial_responses` (5 par advice × 3 advices) : `*_display_mode`, `*_content_variant`, `*_text_id`, `*_clicked`, `*_display_duration_ms`. **Renommage** : ancien `*_explanation_mode` → `*_explanation_content_variant`. **12 textes par bloc** côté corpus session config panel (3 advice × 2 variants × 2 advisor types). Q-EXP-1/2/3/4/5/7/8/9 marquées RÉSOLUES dans `questions-client.md`. Q-EXP-6 reformulée en « UI test à mener ». Proposition cross-level de Mark consignée comme alternative évaluée (§6 spec fonc). Spec tech à produire en suivant.
- **Statut :** ACTIF

### DEC-019 — Free/forced choices : consolidation 11 arbitrages chercheur + Notes 1/2/3
- **Date :** 2026-05-26
- **Tag :** [FONC]
- **Décision :** Consolidation des 9 questions chercheur (Q-FF-1 à Q-FF-9) et des 2 questions équipe (Q-FF-10, Q-FF-11) ouvertes sur le chantier free/forced choices, complétée par 3 notes complémentaires du chercheur. **Modèle final de configuration par bloc (8 champs `BlockConfig`)** : meta (`advisor_forced: bool` + `advisor_forced_value ∈ {none, human, robot}`), distal (`distal_forced: bool` + `distal_forced_optimal_probability ∈ {0, 1}`), proximal (`proximal_forced_probability ∈ [0, 1]` + `proximal_forced_optimal_probability ∈ {0, 1}`), motor (`motor_forced_probability ∈ [0, 1]` + `motor_forced_set ∈ {QZD, FTH, KOM}`). **Note 1** : « Advisor follows forced » — l'advice est obligatoirement subordonné au forced aux 3 niveaux (distal/proximal/motor), strict même en mode unreliable. La reliability ne s'exprime que sur les trials free. Remplace l'orthogonalité forced × advice initialement posée par TR3. **Note 2** : overlay diégétique « Equipment failure » affiché à l'écran uniquement quand un trial est forced ET que l'advisor est `none`. **Note 3** : motor passe de block-wise à trial-wise via la proba `motor_forced_probability`; le set imposé `motor_forced_set` s'applique aux trials forced uniquement, les trials free conservent le tirage aléatoire par trial. Validation des choix imposés par clic explicite (pas d'auto-validation). Cloud non imposé en proximal forced : instancié mais masqué par fog of war permanent. Colonnes `*_match_advice` calculées comme en free (trivialement true sur trials forced avec advice). Pas de colonne consolidée `any_choice_is_forced`.
- **Raison :** Échanges chercheur post-20/05/26. Préserve la dissociation expérimentale entre **agency** (avoir choisi vs subir) et **information** (recevoir un advice), tout en simplifiant la combinatoire (Note 1) et en justifiant narrativement les cas-limites sans advisor (Note 2). Le passage du motor en trial-wise (Note 3) aligne ce niveau sur le proximal et autorise des blocs mixtes free/forced sur les sets de touches. Tous les paramètres sont édités dans le session config panel (cohérent avec DEC-009).
- **Impact :** Spec fonc `Docs/specs/free-forced-choices/spec-fonc.md` passe en statut **`validé`** (révision 2026-05-26). **14 nouvelles colonnes** dans `trial_responses` (CSV V1) : `meta_choice_is_forced`, `meta_choice_forced_value`, `distal_choice_is_forced`, `distal_choice_forced_value`, `distal_choice_forced_was_optimal`, `distal_choice_forced_optimal_probability`, `proximal_choice_is_forced`, `proximal_choice_forced_value`, `proximal_choice_forced_was_optimal`, `proximal_choice_forced_probability`, `proximal_choice_forced_optimal_probability`, `motor_choice_is_forced`, `motor_choice_forced_set`, `motor_choice_forced_probability`. TR3 et TR6 réécrites. §2.4 motor entièrement repensé (passage block-wise → trial-wise, R17–R20 réécrits). Q-005 et Q-FF-1 à Q-FF-11 marquées RÉPONDU dans `questions-client.md`. **Questions résiduelles** : Q-FF-12 (articulation `distal_forced_optimal_probability` vs reliability) et Q-FF-13 (présentation du mode forced en tutorial) restent ouvertes (priorité 🟡, non bloquantes). Spec tech à produire ensuite. Implémentation côté Unity : `BlockConfig` à étendre avec 8 nouveaux champs, `TrialResponseRow` avec 14 nouvelles colonnes, scènes `AdvisorChoiceScene` / `DistalChoiceScene` / `ProximalScene` à instrumenter, overlay « Equipment failure » à designer et intégrer.
- **Statut :** ACTIF

### DEC-016 — Architecture audio : ScriptableObjects + AudioMixer + AudioManager singleton
- **Date :** 2026-05-12
- **Tag :** [TECH]
- **Décision :** Le sous-système audio repose sur trois piliers : (1) `AudioMixer` Unity avec 5 groupes (Master / Music / Ambience / SFX_Gameplay / SFX_UI), (2) deux ScriptableObjects `MusicTrack` et `SoundEffect` pour le data-driven, (3) un `AudioManager` singleton persistant (`DontDestroyOnLoad`, `[DefaultExecutionOrder(-350)]`) instancié dans `BootScene`. Chaque scène pose un composant `SceneMusic` qui déclenche musique + ambient via le manager. Crossfade géré côté manager (no-op si même track). SFX gameplay déclenchés par appels directs depuis `GameManager` / `GridMoverNewInput` / `Trap` / `FadeTransition`. SFX UI via composant `UIButtonSound` réutilisable. Pas d'EventBus dédié. Pas de FMOD/Wwise. Volumes persistés en `PlayerPrefs`, infra prête mais UI Settings reportée.
- **Raison :** Le projet a 9 scènes orchestrées par `FlowController` persistant → la musique doit survivre aux `LoadScene`, ce qui impose un manager persistant. Le pattern singleton est déjà standard (`LevelRegistry`, `GameManager`, etc.) — l'AudioManager s'y aligne. Les ScriptableObjects introduisent le data-driven dans un projet qui n'en a pas encore, au moment idéal (zéro dette audio). FMOD/Wwise est disproportionné pour un jeu de grille discret sans musique adaptative. Un EventBus serait introduit juste pour ce chantier — disproportionné aussi.
- **Impact :** Nouveau dossier `Assets/Game/Audio/` (Mixers, Music, Ambience, Sfx). Nouveau dossier `Assets/Game/Scripts/Audio/` (5 scripts). Nouveau prefab `AudioManager.prefab` instancié dans BootScene. Modifications mineures (ajout de hooks `PlaySfx`) dans `GameManager`, `GridMoverNewInput`, `Trap`, `FadeTransition`. Ajout d'un GameObject `SceneMusic` dans chaque scène du flow (9 scènes). Ajout du composant `UIButtonSound` sur tous les boutons interactifs. Mapping musical acté : 3 MusicTrack (Pregame / Choice / Gameplay) + 1 Ambient (Gameplay). Spec technique : `Docs/specs/audio/spec-tech.md`.
- **Statut :** ACTIF

### DEC-020 — Composant UI in-context tutorial : agnostique, gating porté par l'intégrateur
- **Date :** 2026-05-29
- **Tag :** [TECH]
- **Décision :** Le tutoriel *in-context* (overlay modal titre + texte affiché à l'arrivée sur `AdvisorChoiceScene` / `DistalChoiceScene` / `ProximalScene`) est livré comme composant UI autonome `InContextTutorialUI`, **distinct** du « How To Play » (onboarding paginé). Le composant est **100 % agnostique** : API `Show(title, body)` / `Show()` / `SetContent` / `Close()` + events `Shown` / `Closed`, propriétés `IsOpen` / `HasContent`, fond modal bloquant la souris, auto-show dummy Inspector pour le test isolé. Contenu = **titre + texte uniquement** (pas d'image, pas de pagination). La logique « afficher ou non » (bloc flaggé tutorial, scène avec contenu, et pour Proximal **au 1er trial du bloc seulement**) ainsi que le **verrou des inputs clavier gameplay** sont portés par **l'intégrateur**, pas par le composant.
- **Raison :** Cohérence avec le pattern `HowToPlayUI` (composant réutilisable, testable en isolation, zéro couplage au flow). Sépare la livraison UI (porteur) du branchement back-end (collègue). Le gating « 1er trial » dépend de l'état de session (`FlowController.State.current_trial_index`), qui n'appartient pas au composant (TR1/TR2 spec fonc).
- **Impact :** Livré : `Assets/Game/Scripts/UI/InContextTutorialUI.cs`, prefab `Assets/Game/Prefabs/UI/InContextTutorial/InContextTutorialPanel.prefab`, sandbox `InContextTutorialSandbox.unity` + `InContextTutorialTestRunner` + `InContextTutorialSceneArrivalDemo` (mock du gating, testable en éditeur). Specs `Docs/specs/in-context-tutorial/` (spec-fonc, spec-tech, integration-guide). À la charge de l'intégrateur (cf. `integration-guide.md`) : étendre `BlockConfig` avec `InContextTutorialConfig` (titre + texte par scène) + `DeepClone`, poser le prefab dans les 3 scènes, gater l'affichage (Proximal : `current_trial_index == 0`), verrou `GameManager.inputLocked`, et recette de test offline du vrai flux (hook dev skip-fetch + `SessionConfig` local). Aucun tracking `trial_responses`. Questions ouvertes Q-ICT-1..6 dans `questions-client.md`.
- **Mise à jour 2026-05-29 (étape 2 — scaffold d'intégration) :** prefab + composant `InContextTutorialSceneBinder` posés dans les 3 scènes (`AdvisorChoiceScene`, `DistalChoiceScene`, `ProximalScene`) sous un Canvas dédié `InContextTutorialCanvas` (Overlay, sortingOrder 50). Le binder réalise **pour de vrai** le gating (`is_tutorial`, 1er trial Proximal via `current_trial_index`) + le verrou input Proximal (`GameManager.SetInputLocked`) + un self-test éditeur. **Contenu via placeholder sérialisé + seam `LoadContent()` (// TODO)** — décision : on **n'étend PAS `BlockConfig`** ici, le collègue garde la main sur le modèle de données backend. Reste au collègue : étendre `BlockConfig`, brancher `LoadContent()`, coordonner le verrou input Proximal avec `GameManager.GetReadySequence`. Cf. `integration-guide.md` §0/§4.
- **Statut :** ACTIF

### DEC-021 — Feedback visuel de perte de bugs : architecture événementielle
- **Date :** 2026-06-08
- **Tag :** [TECH]
- **Décision :** En ProximalScene, chaque perte réelle de bugs verts (piège, dépassement du budget de pas, touche de direction invalide) fait jaillir un petit nombre rouge (« -N ») au-dessus du nuage concerné. Architecture événementielle : `BugCloud` expose un **event statique** `OnBugsLost(BugCloud, int)` déclenché dans `AddBugs()` **uniquement si la perte réelle est > 0**. Un composant de scène `PenaltyFeedbackController` (présentation seule) s'y abonne, filtre sur `BugCloud.IsVisible` (ne pas trahir un nuage masqué) et instancie un prefab world-space `FloatingPenaltyNumber` (TMP 3D billboard, montée + fondu par coroutine, auto-destruction). Un nombre indépendant par événement (cascade autorisée).
- **Raison :** `AddBugs()` est le seul point qui connaît la perte réellement appliquée (après clamp) → déclencher l'event ici garantit la justesse sans coordination. Event statique car les nuages sont spawnés au runtime (abonnement unique, pas de câblage par instance). Séparation donnée (`BugCloud`) / présentation (`PenaltyFeedbackController`) conforme aux principes du rôle archi. Continuité avec le pattern d'event existant (`OnRoundEnded`) et les coroutines maison (`FadeTransition`/`GridMover`, pas de DOTween).
- **Impact :** `BugCloud.cs` : +event statique `OnBugsLost`, +propriété `IsVisible` (positionnée dans `SetVisible`), invoke dans `AddBugs`. Nouveaux : `Assets/Game/Scripts/UI/FloatingPenaltyNumber.cs`, `Assets/Game/Scripts/UI/PenaltyFeedbackController.cs`, prefab `Assets/Game/Prefabs/UI/FloatingPenaltyNumber.prefab`. GameObject `PenaltyFeedbackController` ajouté en ProximalScene. Validé en Play Mode (trap → 2 nombres ; nuage vide → 0 ; nuage masqué → 0, perte tout de même appliquée). **Reste à faire (visuel, hors portée logique) :** réglage fin de la taille/placement du nombre et du tri de rendu vs brouillard (cf. [[fog-path-transparent-sorting]] — option `ZTest Always` / render queue dédiée) à l'œil dans l'éditeur ; tous les paramètres sont exposés sur le contrôleur + fontSize sur le prefab.
- **Statut :** ACTIF

---

## Index par tag

- **[SCOPE]** : DEC-004, DEC-005, DEC-008
- **[FONC]** : DEC-001, DEC-003, DEC-006 *(résolu)*, DEC-010, DEC-012, DEC-017, DEC-018, DEC-019
- **[TECH]** : DEC-002, DEC-007, DEC-009, DEC-011, DEC-013, DEC-014, DEC-015, DEC-016, DEC-020, DEC-021
- **[PLANNING]** : _(aucune pour l'instant)_
- **[CLIENT]** : _(aucune pour l'instant)_
