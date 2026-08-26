# Revue de complétude — code réel vs attendu

> **Date :** 28/07/2026 · **Rôle :** Pilotage · **Méthode :** confrontation de `Docs/references/`, `Docs/project-state/`, `Docs/specs/` et `Docs/validation/` à la lecture directe du code (69 scripts C#, `EditorBuildSettings.asset`, historique git).
> **But :** savoir si l'application est achevée, et sur quoi arbitrer avec l'équipe et le chercheur.
> **Aucune modification de code n'a été faite pour produire ce rapport.** Chaque affirmation cite un `fichier:ligne` vérifiable.

---

## Verdict

**Le jeu est fonctionnellement bien plus avancé que ne le dit le pilotage, mais il n'est pas livrable en l'état.**

Le blocage n'est plus le gameplay — c'est :

1. **Trois écrans du flow sont des coquilles vides** (QuestionnaireScene, texte de consentement, texte AdvisorChoice) ;
2. **Le pipeline de données perd des trials silencieusement** — et pas seulement par accident : **toute la condition `advisor = none` n'atteint jamais la base** (D1) ;
3. **La doc de pilotage est périmée au point d'être trompeuse** — elle déclare « à démarrer » des chantiers livrés depuis des semaines.

---

## 1. Ce qui est réellement implémenté

Le flow multi-écran complet est livré, en **architecture multi-scènes** (10 scènes actives dans `EditorBuildSettings.asset`), et non en scène unique comme le recommandait `Docs/references/Plan_Implementation_MultiScreen_Flow_v1.md` §3.1. Divergence assumée, sans impact constaté.

| Brique | Preuve code |
| :-- | :-- |
| Flow 10 scènes | Boot, Welcome, Consent, Intro, AdvisorChoice, DistalChoice, Proximal, Questionnaire, Break, EndSession |
| **Free/forced (4 niveaux)** | `FlowController.cs:99-171`, `239-256` — meta / distal / proximal / motor |
| **Explanations short/long** | `ExplanationResolver.cs` — `display_mode` {none, forced, opt-in} × `content_variant`, tracking clic + durée |
| Motor advice | `MotorAdviceController.cs` (ZQSD / TFGH / IJKL) ; `GridMover.cs:113` n'accepte que le set actif |
| Chemin suboptimal + détour | `PathSpawner.cs:172-213` |
| **Modèle de perte de bugs** | `GameManager.cs:262-275` + `BugCloud.cs:67-84` → **−1 bug vert par nuage par piège**, conforme GDD |
| Révélation map en fin de trial | `GameManager.cs:339` → `FogController.RevealAll()` |
| **Redirect post-expérience** | `PlatformUrlDisplay.cs` + `FlowController.PlatformUrl` |
| Tutoriel in-context | `InContextTutorialSceneBinder.cs` ← `BlockConfig.in_context_tutorial` |
| How-to-play | `IntroSceneController.cs:80` → `HowToPlayUI.SetPages()`, images via `ApiClient.FetchImage` |
| Audio | `AudioManager.cs` (`[DefaultExecutionOrder(-350)]`) + persistance PlayerPrefs |
| Randomisation blocs + pauses | `BlockOrderRandomizer.cs`, `BreakSceneController.cs`, `BreakScene.unity` (dans le build) |
| Questionnaire 3 dimensions | `TrialQuestionsUI.cs` — par trial, dans ProximalScene, **fonctionnel** |

---

## 2. 🔴 Trois écrans incomplets

Aucun de ces trois points n'est signalé dans `avancement.md`, `risques.md` ni les specs.

| # | Écran | Constat vérifié |
| :-- | :-- | :-- |
| **E1** | **QuestionnaireScene** | `QuestionnaireUI.cs:38` force `_questions = new List<QuestionConfig>()` — **liste toujours vide**. Le test `if (_questions.Count == 0)` ligne 43 déclenche donc systématiquement `OnQuestionnaireComplete(_responses)` avec **zéro réponse**. Les ~110 lignes de rendu scale / dropdown / free-text sont inatteignables. `QuestionConfig` n'est référencé nulle part ailleurs dans le code : la scène charge, affiche « Questionnaire », et s'auto-enchaîne. |
| **E2** | **ConsentScene** | `ConsentUI.cs:16-20` **écrase au `Start`** le texte de la scène par un placeholder codé en dur : `"Consentement"` / `"Veuillez accepter le consentement pour demarrer la session."`. Il n'existe **aucun champ `consent_text`** dans `SessionConfig` (`FlowDataModels.cs`) — le vrai texte de consentement ne peut donc être affiché ni par la scène, ni par l'API. **Bloquant pour une étude avec accord éthique.** De plus `ConsentUI.cs:34` annonce « La session s'arrete ici » alors que `OnConsentDeclined()` renvoie vers Welcome. |
| **E3** | **AdvisorChoiceScene** | `AdvisorChoiceUI.cs:56` affiche au participant un texte de dev périmé : *« Ce choix est enregistre mais n'affecte pas encore la generation de la map. »* — faux depuis que le free/forced est implémenté. |

> **E1 n'invalide pas la collecte des 3 dimensions** : celle-ci passe par `TrialQuestionsUI` dans ProximalScene et fonctionne. QuestionnaireScene est donc soit à câbler, soit à retirer du flow — mais elle consomme actuellement un chargement de scène et un fade pour rien.

---

## 3. 🔴 Pipeline de données — le vrai bloqueur livraison

Pour une expé avec participants recrutés, chaque perte peut invalider un participant.

| # | Défaut | Preuve |
| :-- | :-- | :-- |
| **D1** | 🔴🔴 **Toute trial jouée en condition `advisor = none` est perdue — systématiquement.** Ce n'est pas un aléa de complétion : la chaîne est déterministe. (1) `TrialQuestionsUI.cs:91` ne pose **pas** la question d'acceptabilité quand `advisor_choice == None` (« elle n'a pas de sens si aucun advisor n'a été choisi ») ; (2) `FlowSerializationUtility.cs:45` remet `acceptability_question = null` et ne le remplit que si la clé est présente dans les réponses ; (3) `TrialManager.cs:119-124` jette alors la ligne entière avec un simple `Debug.LogWarning`. Tout le gameplay (path, bugs, pièges, timings) part avec elle. **`advisor = none` est une condition expérimentale de premier plan** : option sélectionnable (`AdvisorOptionButton._advisorType`), défaut de `advisor_forced_value`, état réinitialisé à chaque bloc (`FlowController.cs:442`), et pré-requis de l'overlay « equipment failure » (`FlowController.cs:105`). La condition contrôle de l'étude ne remonte donc jamais en base. Le cas « questionnaire réellement incomplet » (participant qui ferme l'onglet) existe aussi, mais il est second. | `TrialQuestionsUI.cs:91`, `FlowSerializationUtility.cs:45`, `TrialManager.cs:119-124` |
| **D2** | **Aucune persistance locale** — `_pendingTrialRequests` est une `Queue<>` **en mémoire seule**. `PlayerPrefs` ne sert qu'à l'audio. Fermeture navigateur / crash / reload = perte sèche. | `ApiClient.cs:46` |
| **D3** | **File d'envoi bloquée sur échec** — après épuisement des 3 retries, `break` sort de la boucle **sans dépiler**. La file ne repart qu'au prochain appel de `RetryPendingTrialUploads()`. Un échec sur le **dernier** trial de session n'est jamais rejoué. | `ApiClient.cs:349-353`, `218-224` |
| **D4** | **`motor_forced_set` : fallback silencieux** — le code est cohérent (`QZD` / `FTH` / `JIL` = touches gauche-haut-droite des sets ZQSD / TFGH / IJKL), mais **toute valeur inconnue retombe sans alerte sur `ZQSD`**. Or 4 documents annoncent encore `KOM` (ancien set OKLM, remplacé au commit `7b3a9e86`). Une config Supabase restée en `"KOM"` produirait du ZQSD **sans aucune trace**. | `FlowDataModels.cs:620-649` |
| **D5** | 🟢 **`build_version` incohérent — tranché.** `FlowController.cs:26` = `"0.4.0"`, `SessionManager.cs:26` = `"1.0.0"`. Qui l'emporte : `SessionManager.cs:115` (`buildVersion = flow.BuildVersion`) est dans `CopyConfigFromFlowController()`, donc **en session pilotée par le flow — le cas réel — la colonne vaut toujours `"0.4.0"`**. Le `"1.0.0"` ne survit que si ProximalScene est lancée seule en éditeur. Comportement déterministe : incohérence de version affichée, pas ambiguïté de données. A5 reste utile mais descend en priorité. | `SessionManager.cs:115`, `TrialManager.cs:244` |

> **D1 + D2 combinés** : le scénario « le participant ferme l'onglet pendant le questionnaire » perd le trial **et** tout ce qui n'était pas encore uploadé.

Ces constats **confirment** les points chauds PC-2 et PC-3 pré-identifiés dans `Docs/validation/journal-divergences.md`.

---

## 4. Écarts fonctionnels (à arbitrer)

| # | Écart | Preuve |
| :-- | :-- | :-- |
| **F1** | **`visibility_noise` inexistant** — aucune occurrence dans le code. Attendu par `specs-light.md` §2 (bruit de discrimination sur les nuages) et par le plan de référence §2.3 (`ValleyParams.visibilityNoise` + colonnes CSV `*_valley_visibility_noise`). Absent aussi de `TrialResponseRow`. Q-013. | grep ∅ |
| **F2** | **Retour en arrière non bloqué** — `IsWalkable = InBounds && !IsWall`. Le flag `Visited` existe mais ne sert qu'au fog. `specs-light.md` §4 : « le joueur ne peut pas aller sur une case déjà visitée ». **Probablement à clore par une décision** « non retenu, remplacé par le budget de pas (`overtime_steps`) » plutôt que par du code. | `LevelRegistry.cs:253,266` |
| **F3** | **2 chemins au lieu de 4** — `PathSpawner.cs:129-130` réserve `PathLeft`/`PathRight` uniquement. Q-008. | — |
| **F4** | **Pas de protocole de sortie** — `SettingsPanelUI.cs:61-68` fait `Application.Quit()` sec : ni modale de confirmation, ni sauvegarde partielle, ni flag `session_abandoned` + timestamp. Le plan de référence §2.8 les exige. | — |
| **F5** | **Mountain UI absente** (DEC-003, progression du bloc) — aucun script dédié ; prefab `EndOfBlockPanel_TODO.prefab` non fini. | — |
| **F6** | **Textes de questionnaire = placeholders** — `"Question d'acceptabilite (a definir)"`, `"Question de sens d'agentivite (a definir)"`, `"Question de ressemblance humaine (a definir)"`. Sérialisés, donc surchargeables en scène, mais les défauts commités disent littéralement « à définir ». Dépend de Q-011. | `TrialQuestionsUI.cs:24-26` |
| **F7** | **Pas de boot hors-ligne** — exige un `sessionId` et un `FetchSessionConfig` réussi, sinon `LogError` et arrêt. Le seul retour utilisateur d'un échec est une barre de chargement figée à 90 % (`BootLoadingBar.cs`). Tester une scène isolée sans back-end est impossible. | `FlowController.cs:392-434` |
| **F8** | 🟠 **Contrat de colonnes du questionnaire — hypothèse à lever avant toute passation.** Le code POSTe `acceptability_question` / `sens_of_agency_question` / `human_likeness_question`. Le **schéma SQL de référence** de `trial_responses` (`specs/multi-screen-flow/spec-tech.md:758`), le corps de cette même spec (`:448`) et `TDD.md:2476,2528,2614` déclarent `q1_response` / `q2_response` / `q3_response`. **Si la table Supabase a été créée depuis ce schéma, les trois réponses n'atterrissent nulle part.** À vérifier côté back-end — c'est une hypothèse, pas un constat : le code n'expose pas le schéma réel de la table. Requalifie le point chaud PC-1 (cf. `validation/journal-divergences.md`). | `FlowDataModels.cs:538-540` vs `spec-tech.md:758` |

---

## 5. Dette documentaire

### 5.1 `avancement.md` (23/07/26) sous-estime lourdement l'état réel

| Ce que dit `avancement.md` | Réalité du code |
| :-- | :-- |
| Free/forced : « 🔴 implémentation à démarrer » | ✅ implémenté (`FlowController.cs`) |
| Explanations short/long : « implémentation à démarrer » | ✅ implémenté (`ExplanationResolver.cs`) |
| Redirect post-expérience : « Non implémenté » | ✅ implémenté (`PlatformUrlDisplay.cs`) |
| BreakScene : « 🟡 scène/UI à créer » | ✅ créée et dans le build |
| Modèle de perte de bugs : « 🔴 divergent, à arbitrer » | ✅ le code applique le modèle GDD — reste la validation formelle chercheur |
| Tutoriel in-context : « reste au collègue de brancher » | ✅ branché (`InContextTutorialSceneBinder`) |

### 5.2 Le process spec a été court-circuité sur les 2 chantiers les plus critiques

`free-forced-choices` et `explanations-short-long` ont été **implémentés sans que leur `spec-tech.md` soit produite** (les deux fichiers sont annoncés « à produire » et n'existent pas). Il n'existe donc **aucune référence technique écrite pour les fonctionnalités qui portent H1–H8**.

Conséquences concrètes relevées dans les specs fonctionnelles :

- Les **tie-breakers** « vallée optimale » (R6, `free-forced-choices/spec-fonc.md:157`) et « cloud optimal » (R15, `:208`) sont explicitement renvoyés à la spec tech inexistante → règle de départage non écrite alors que le code tranche déjà.
- Les **4 colonnes CSV du motor advice** sont encore `TBD` (`MotorAdvice/spec-fonc.md:81-84`) — Q-002.

### 5.3 Autres constats de structure

- **Aucun `tickets.md` n'existe** sur les 7 chantiers, alors que `plan-global.md` et 3 spec-tech en font la convention. Aucun suivi de tâches formalisé.
- **Aucune section « Critères d'acceptation » / DoD** dans les specs réelles. Ce sont les « Scénarios de validation » (14 items audio, 12 how-to-play, 11 in-context-tutorial, 7 multi-screen-flow) qui en tiennent lieu — **aucun n'est coché**.
- `plan-global.md` + `risques.md` — gelés au **03/03/26**. Aucune case ✅ ; ne mentionnent aucun des 8 chantiers réellement menés (audio, free/forced, explanations, how-to-play, tutoriel, motor advice, penalty feedback, randomisation/pauses). R1–R4 obsolètes.
- `session-map.md` — gelé au **12/03/26**, MotorAdvice à **0/5 tâches**, alors que la feature est livrée.
- `questions-client.md` — gelé au **29/05/26**. Q-FF-12 et Q-FF-13 n'y figurent pas (elles n'existent que dans `specs/free-forced-choices/spec-fonc.md:342-343`).
- `TDD.md` (v3.0, 23/03/26) — **section 7 manquante** (saute de 6 à 8) ; ~27 classes non documentées (tout `Audio/`, les explanations, les tutoriels, `BreakSceneController`, `BlockOrderRandomizer`, `TrialQuestionsUI`, `PenaltyFeedbackController`, `WrongInputFeedbackController`, `SettingsPanelUI`, `PlatformUrlDisplay`, `DistalValleyScanView`…) ; §5.2 documente un type **`ValleyPreview` qui n'existe pas dans le code** ; §6.7 décrit QuestionnaireUI comme fonctionnel (cf. E1). `grep -i "free.forced\|explanation" Docs/TDD.md` ne retourne **rien** : les deux chantiers qui portent H1–H8 sont absents du TDD.
- 🔴 **`TDD.md` est faux sur la règle de scoring centrale** — **18 occurrences** décrivent une pénalité de **−2 bugs** par nuage (`TDD.md:238`, `1207-1209`, `1280-1282`, `1364-1366`, `1388`, journal `1401` : « toutes les pénalités uniformisées à -2 »). Le code applique **−1, sur les bugs verts uniquement**. Depuis la mise à jour du 28/07, `TDD.md` **contredit frontalement `avancement.md`** sur le point que le chercheur doit signer (Q-007). Or le TDD est le document que lit un nouvel arrivant. À corriger via le protocole `!doc`.
- **Noms de colonnes du questionnaire** — `TDD.md:2476,2528,2614` et `specs/multi-screen-flow/spec-tech.md:448` **et `:758` (schéma SQL de référence)** nomment les colonnes `q1_response` / `q2_response` / `q3_response`. Le code émet `acceptability_question` / `sens_of_agency_question` / `human_likeness_question` (`FlowDataModels.cs:538-540`). Cf. F8.
- `Docs/validation/` — méthodologie complète et solide, mais **jamais exécutée** : 6 gates G0–G5 tous ⬜, colonnes `CSV ?`/`Statut` de la matrice vides, `journal-divergences.md` vide.
- **Deux notes d'intégration périmées** : la course « verrou input vs `GetReadySequence` » signalée « non résolue » dans `in-context-tutorial/integration-guide.md:195-199` **est corrigée** (`GameManager.cs:59`), et l'étape 2 de branchement back-end est faite pour les deux tutoriels.
- **Hygiène repo** : 3 scènes `[LEGACY]` (désactivées dans le build), `ProximalSceneFLO.unity` orpheline (hors build settings), 3 prefabs `_DRAFT`, `EndOfBlockPanel_TODO.prefab`, **aucun test ni `.asmdef`** dans le projet.

---

## 6. Ce qui reste bloqué côté chercheur

| Q | Sujet | Impact |
| :-- | :-- | :-- |
| **Q-010** | Pattern de fiabilité des advisors — section GDD littéralement vide | 🔴 cœur scientifique, non implémentable |
| **Q-011** | Mapping des 3 questions ↔ dimensions + textes réels | 🔴 valide/invalide le questionnaire (cf. E1, F6) |
| **(nouveau)** | **Texte de consentement réel** — aucun champ prévu côté API ni côté scène | 🔴 cf. E2 |
| Q-007 | Modèle de perte de bugs | 🟡 le code tranche déjà côté GDD — reste à faire acter |
| Q-002 / Q-004 | Colonnes CSV motor advice / format d'affichage du set | 🟠 traçabilité |
| Q-008 | 4 paths vs 2 paths | 🟠 cf. F3 |
| Q-012 | Nb de trials/bloc cible (36 ?) et nb de blocs d'entraînement | 🟠 volumétrie |
| Q-013 | Rendu du visibility noise | 🟠 cf. F1 |
| **Q-TIE-1** *(nouveau)* | **Départage à égalité des deux nuages** — `GameManager.cs:404` compare avec un `>` strict : à égalité, le nuage de **droite** est toujours déclaré correct, sans tirage. Atteignable dès que les pénalités vident les deux nuages (elles s'arrêtent à zéro), donc **précisément sur les trials les moins bien joués** | 🟠 biaise `choice_correct` et `true_cloud` |
| Q-FF-12 / Q-FF-13 | Articulation forced × reliability ; forced en tutorial | 🟡 non bloquantes |

---

## 7. Lot A — Fiabiliser le pipeline de données *(non exécuté — priorité absolue)*

**Objectif : plus aucun trial ne disparaît sans trace.** Pré-requis à toute passation avec des participants réels.

| Tâche | Fichier | Détail |
| :-- | :-- | :-- |
| **A1** | `TrialManager.cs:119-124` | **Priorité absolue du lot** (cf. D1 requalifié). Ne plus abandonner la ligne quand un champ questionnaire est vide : envoyer avec les champs à `null`. Le mécanisme existe déjà (`[IncludeNullInJson]` + `ApiClient.ToJsonObjectSkippingNullStrings`). Sans ce correctif, **aucune trial de la condition `advisor = none` n'atteint la base**. **Décision chercheur requise** — la bonne question n'est pas « ligne partielle ou pas de ligne » : pour un trial sans advisor la question d'acceptabilité n'est délibérément *pas posée*, donc que doit valoir `acceptability_question` ? `null` assumé, ou une valeur « non applicable » distinguable d'une non-réponse ? |
| **A2** | `ApiClient.cs:46` | Persister la file d'envoi (sérialisation JSON en `PlayerPrefs`, compatible WebGL — même mécanique que `AudioManager.cs:332-396`), et la rejouer au boot dans `Awake`. |
| **A3** | `ApiClient.cs:349-353` | Remplacer le `break` par un `continue` avec backoff, ou dépiler vers une file « échecs » persistée. Déclencher `RetryPendingTrialUploads()` sur `OnApplicationPause` / `OnApplicationQuit`. |
| **A4** | `FlowDataModels.cs:647-648` | Remplacer le fallback silencieux `default → ZQSD` par un `Debug.LogError` explicite. |
| **A5** | `FlowController.cs:26` / `SessionManager.cs:26` | Unifier `build_version` sur une source unique. |

**Vérification** : couper le réseau en cours de trial → relancer le build → les trials en attente repartent et arrivent dans Supabase. Soumettre un questionnaire partiel → une ligne existe avec les colonnes questionnaire vides. Rejouer les scénarios §Résilience de `Docs/validation/protocole-sessions-test.md`.

---

## 8. Lot C — Remise à niveau documentaire *(exécuté le 28/07/26)*

Aucune modification de code. Objectif : que `Docs/project-state/` redevienne pilotable.

| Tâche | Fichier | Détail |
| :-- | :-- | :-- |
| **C1** | `avancement.md` | Corriger les 6 lignes fausses du tableau §5.1. Reclasser free/forced, explanations, redirect, BreakScene, tutoriel in-context en ✅. Requalifier le risque « perte de bugs » de 🔴 à 🟡. Ajouter les 3 écrans incomplets (E1/E2/E3) en 🔴. Compléter l'index des décisions (DEC-021, DEC-022). Réviser l'estimation d'avancement à la hausse. |
| **C2** | `session-map.md` | Clore la session MotorAdvice : 5/5 tâches, note de clôture renvoyant vers `MotorAdviceController.cs` + `MotorAdviceUI.cs`. |
| **C3** | `plan-global.md`, `risques.md` | Bandeau d'archivage en tête. Non réécrits : ils décrivent un découpage abandonné. |
| **C4** | `dictionnaire-donnees.md`, `matrice-tracabilite.md`, `config-session-test.md`, `free-forced-choices/spec-fonc.md` | `KOM` → `JIL`, avec note expliquant le remplacement du set OKLM par IJKL (commit `7b3a9e86`). |
| **C5** | `questions-client.md` | Ajouter Q-FF-12, Q-FF-13 et la question du texte de consentement (E2). |

**Vérification** : chaque ligne ✅/🔴 d'`avancement.md` correspond à une preuve code citée ici ; `grep -rn "KOM" Docs/` ne retourne plus que des mentions historiques datées.

---

## 9. Lots B et D — documentés, non exécutés

- **Lot B — écarts fonctionnels.** F4 (protocole de sortie) est chiffrable tout de suite. F1 / F2 / F3 / F5 attendent un arbitrage chercheur. E1 / E2 / E3 attendent les contenus réels (textes de consentement, de questionnaire, décision sur QuestionnaireScene).
- **Lot D — campagne de validation.** Ouvrir G0 (sign-off Q-007 / Q-010 / Q-011) puis dérouler les axes 1→7 de `Docs/validation/campagne-test-validation.md`. Les points chauds PC-2 / PC-3 y sont déjà pré-identifiés et sont **confirmés** par cet audit (D2 / D3) — ils pourront être fermés dès le Lot A livré.
- **Dette spec.** Produire a posteriori les 2 `spec-tech.md` manquantes par rétro-documentation du code livré : c'est le seul moyen d'avoir une référence opposable sur les features qui portent H1–H8, et d'écrire noir sur blanc les tie-breakers R6/R15 tels qu'implémentés.

---

## 10. Ordre de priorité proposé

1. **Arbitrages chercheur bloquants** — Q-010, Q-011, texte de consentement (E2), décision sur QuestionnaireScene (E1).
2. **Lot A** — pipeline données. Rien ne doit être passé avec de vrais participants avant.
3. **Lot B/E3** — nettoyage des textes placeholders (rapide, visible par le participant).
4. **Lot D** — campagne de validation, jusqu'au codebook signé (G5).
5. **Dette spec + TDD** — rétro-documentation.
