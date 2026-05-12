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

### DEC-016 — Architecture audio : ScriptableObjects + AudioMixer + AudioManager singleton
- **Date :** 2026-05-12
- **Tag :** [TECH]
- **Décision :** Le sous-système audio repose sur trois piliers : (1) `AudioMixer` Unity avec 5 groupes (Master / Music / Ambience / SFX_Gameplay / SFX_UI), (2) deux ScriptableObjects `MusicTrack` et `SoundEffect` pour le data-driven, (3) un `AudioManager` singleton persistant (`DontDestroyOnLoad`, `[DefaultExecutionOrder(-350)]`) instancié dans `BootScene`. Chaque scène pose un composant `SceneMusic` qui déclenche musique + ambient via le manager. Crossfade géré côté manager (no-op si même track). SFX gameplay déclenchés par appels directs depuis `GameManager` / `GridMoverNewInput` / `Trap` / `FadeTransition`. SFX UI via composant `UIButtonSound` réutilisable. Pas d'EventBus dédié. Pas de FMOD/Wwise. Volumes persistés en `PlayerPrefs`, infra prête mais UI Settings reportée.
- **Raison :** Le projet a 9 scènes orchestrées par `FlowController` persistant → la musique doit survivre aux `LoadScene`, ce qui impose un manager persistant. Le pattern singleton est déjà standard (`LevelRegistry`, `GameManager`, etc.) — l'AudioManager s'y aligne. Les ScriptableObjects introduisent le data-driven dans un projet qui n'en a pas encore, au moment idéal (zéro dette audio). FMOD/Wwise est disproportionné pour un jeu de grille discret sans musique adaptative. Un EventBus serait introduit juste pour ce chantier — disproportionné aussi.
- **Impact :** Nouveau dossier `Assets/Game/Audio/` (Mixers, Music, Ambience, Sfx). Nouveau dossier `Assets/Game/Scripts/Audio/` (5 scripts). Nouveau prefab `AudioManager.prefab` instancié dans BootScene. Modifications mineures (ajout de hooks `PlaySfx`) dans `GameManager`, `GridMoverNewInput`, `Trap`, `FadeTransition`. Ajout d'un GameObject `SceneMusic` dans chaque scène du flow (9 scènes). Ajout du composant `UIButtonSound` sur tous les boutons interactifs. Mapping musical acté : 3 MusicTrack (Pregame / Choice / Gameplay) + 1 Ambient (Gameplay). Spec technique : `Docs/specs/audio/spec-tech.md`.
- **Statut :** ACTIF

---

## Index par tag

- **[SCOPE]** : DEC-004, DEC-005, DEC-008
- **[FONC]** : DEC-001, DEC-003, DEC-006 *(résolu)*, DEC-010, DEC-012
- **[TECH]** : DEC-002, DEC-007, DEC-009, DEC-011, DEC-013, DEC-014, DEC-015, DEC-016
- **[PLANNING]** : _(aucune pour l'instant)_
- **[CLIENT]** : _(aucune pour l'instant)_
