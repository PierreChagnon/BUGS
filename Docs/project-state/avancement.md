# État d'avancement du projet

> Ce fichier est le résumé condensé de l'état du projet.
> Objectif : un rôle peut comprendre où en est le projet en lisant CE SEUL FICHIER.
> Mis à jour après chaque session qui fait avancer le projet.
> Dernière mise à jour : 2026-05-29

---

## Vue macro

**Avancement global estimé :** ~60-65% du périmètre fonctionnel GDD
**Phase actuelle :** Squelette multi-écran en place (toutes les scènes du flow créées). Système audio livré. **Specs fonctionnelles `free-forced-choices` et `explanations-short-long` validées** (DEC-019 + DEC-018, 26/05/26). Restent à produire les specs tech et à implémenter ces briques côté Unity. La spec `reliability patterns` est encore à cadrer côté chercheur, ainsi que plusieurs points de mise en cohérence avec le GDD (modèle de perte de bugs, 4 paths, smooth pan).

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
| Spec tech multi-écran flow | ✅ Finalisée | 16/03/26 |
| Data model Supabase (`trial_responses` plate) | ✅ Finalisé | 16/03/26 |
| Décisions DEC-001 à DEC-016 | ✅ Enregistrées | 12/05/26 |

### Scènes du flow

| Scène | État |
|:---|:---|
| BootScene, IntroScene | ✅ Créées |
| ConsentScene + ConsentUI (gate, DEC-012) | ✅ Implémentées |
| WelcomeScene | ✅ Créée |
| AdvisorChoiceScene + AdvisorChoiceUI + AdvisorOptionButton | ✅ Implémentées |
| DistalChoiceScene + DistalChoiceUI + DistalValleyScanView | ✅ Implémentées |
| ProximalScene (forest screen) + RoundUI | ✅ Implémentée |
| QuestionnaireScene + QuestionnaireUI (DEC-008) | ✅ Implémentée |
| EndSessionScene | ✅ Créée |

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
| InContextTutorialSceneBinder + pose dans les 3 scènes | ✅ Scaffold d'intégration en place (étape 2, 29/05/26) : prefab + binder sous `InContextTutorialCanvas` dans Advisor/Distal/Proximal ; gating réel (`is_tutorial`, 1er trial) + verrou input Proximal + self-test éditeur. Reste au collègue : étendre `BlockConfig`, brancher `LoadContent()`, coordonner verrou vs `GetReadySequence` (cf. `integration-guide.md`) |

### Outillage / documentation

| Composant | État |
|:---|:---|
| TutorialSessionFactory (DEC-014) | ✅ Implémentée |
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
| **Questionnaire in-game (3 dimensions)** | Colonnes `q1..q3` prêtes (DEC-008). À confirmer que les 3 dimensions GDD (sense of agency, acceptability, human-likeness) sont mappées + textes éditables côté chercheur. |
| **Saturation/blur noise sur clouds proximaux** | Ratio bug exposé, pas de paramètre de **visibility noise** sur le rendu. À vérifier shader/particules. |
| **Motor advice — colonnes CSV, explanation, format** | Q-002 et Q-004 toujours en attente. Q-003 RÉSOLUE par DEC-017 (explanation motor confirmée, cf. spec explanations-short-long). |

### 🔴 Non couvert ou non démarré

| Périmètre GDD | Impact |
|:---|:---|
| **Free/forced mechanic** sur les 4 choix (meta/distal/proximal/motor) | 🟢 **Spec fonc validée 26/05/26** (`Docs/specs/free-forced-choices/spec-fonc.md`, statut `validé`). DEC-019 + 11 sous-questions résolues + 3 notes intégrées (advisor follows forced, equipment failure overlay, motor passe trial-wise). Q-FF-12 et Q-FF-13 restent ouvertes (non bloquantes). **Spec tech à produire**, puis implémentation Unity. |
| **Explanations short/long** liées à chaque advice | 🟢 Spec fonc consolidée (`Docs/specs/explanations-short-long/spec-fonc.md`, draft révisé 26/05/26). DEC-017 + DEC-018 actées. 8/10 sous-questions résolues via échange chercheurs (Q-EXP-1, 2, 3, 4, 5, 7, 8, 9). Reste : Q-EXP-6 (UI test à mener) et Q-EXP-10 (localisation, non bloquante). Implémentation : à démarrer après production de la spec tech. |
| **Smooth camera pan + auto-walk entre trials** | GDD demande transition smooth verticale ; DEC-002 a tranché fade noir → écart à reconfirmer. |
| **Modèle de perte de bugs** | TDD : "total décrémenté, ratio recalculé". GDD : "1 green bug perdu par cloud par piège". Modèles divergents. |
| **4 paths visibles (2 par cloud)** | GDD : 4 paths. Code : 2 paths totaux. |
| **Mountain UI** (progression dans le bloc, DEC-003) | Criticité basse, mais prévue. |
| **End-of-block summary screen** | Manquant. |
| **Redirect post-expérience** (Qualtrics, DEC-005) | Non implémenté. |
| **Ending / FinalComments collection** | EndSessionScene existe mais contenu à définir. |
| **Reliability manipulation pattern documenté** | Section GDD vide ("...") → spec chercheur manquante. |
| **Nb trials/block = 36 et nb training blocks** | Paramètres existent, valeurs cibles non actées. |

---

## Décisions en attente

| Réf | Sujet | Bloque quoi | Urgence |
|:---|:---|:---|:---|
| Q-002 | Colonnes CSV motor advice | Traçabilité data | 🔴 Bloque finalisation motor advice |
| Q-003 | Motor advice avec explanation ? | — | ✅ RÉSOLUE par DEC-017 |
| Q-004 | Format d'affichage du set de touches | UI + compréhension participant | 🟡 Bloque polish UI |
| Q-006 | Modèle d'édition des explanations | — | ✅ RÉSOLUE par DEC-017 (session config panel) |
| Q-EXP-1, 2, 3, 4, 5, 7, 8, 9 | Sous-questions de cadrage Explanations | — | ✅ RÉSOLUES par DEC-018 (2026-05-26) |
| Q-EXP-6 | Modalités UI hide / no-overlap | UI test à mener avant freeze UI | 🟡 |
| Q-EXP-10 | Localisation corpus FR/multilingue | Structure config | 🟢 Non bloquante |
| Q-005 + Q-FF-1..11 | Free/forced choices : périmètre + arbitrages | — | ✅ RÉSOLUE par DEC-019 (26/05/26) — spec fonc `validé`. Q-FF-12 et Q-FF-13 résiduelles 🟡 non bloquantes. |
| — | Modèle de perte de bugs (1 green/piège/cloud vs total décrémenté) | Cohérence protocole | 🔴 À arbitrer avec chercheur |
| — | 4 paths visibles vs 2 paths | Cohérence GDD | 🟡 Écart design assumé ? |
| — | Smooth pan vs fade noir | Cohérence GDD | 🟡 DEC-002 à reconfirmer |
| — | Preview floue distal screen | Rendu : statique vs dynamique | 🟢 Peut attendre design |

---

## Risques actifs

| Risque | Impact | Statut |
|:---|:---|:---|
| **Free/forced choices : implémentation absente** → hypothèses H1-H8 non testables tant que pas implémentées | Validité expérimentale | 🟡 Cadrage finalisé (DEC-019, spec fonc validée 26/05/26) — implémentation à démarrer, Q-FF-12 et Q-FF-13 résiduelles non bloquantes |
| **Explanations short/long absentes** → H8 non testable | Validité expérimentale | 🟢 Cadrage finalisé (DEC-017 + DEC-018, spec fonc révisée 26/05/26) — implémentation à démarrer, Q-EXP-6 et Q-EXP-10 non bloquantes |
| **Modèle de perte de bugs divergent code vs GDD** | Résultats non comparables au protocole | 🔴 À arbitrer |
| **Reliability pattern non spécifié côté chercheur** | Impossible d'implémenter la manipulation | 🔴 Spec chercheur manquante |
| State leaking entre trials dans le flow multi-écran | Données recherche corrompues | ✅ Couvert par spec tech |
| WebGL + System.Environment.GetCommandLineArgs | Potentiellement non fonctionnel | ✅ Résolu par DEC-009 |
| Données perdues si fermeture navigateur avant SendTrials | Trials en mémoire perdus | ⚠️ Atténué : 1 POST par trial |
| **Avancement.md précédent obsolète (~2 mois)** | Pilotage sur info périmée | ✅ Résolu par cette mise à jour |

---

## Recommandations de priorisation

1. **Débloquer les questions client** Q-002 / Q-003 / Q-004 (motor advice) → action chef de projet, déblocage immédiat.
2. **Produire la spec tech `free-forced-choices`** (cadrage fonctionnel finalisé par DEC-019 le 26/05/26 — 11 sous-questions résolues, Notes 1/2/3 intégrées). Périmètre tech : 8 nouveaux champs `BlockConfig`, 14 nouvelles colonnes `trial_responses`, instrumentation des scènes Advisor/Distal/Proximal, overlay « Equipment failure », masquage par fog of war permanent du cloud non imposé.
3. **Produire la spec tech `explanations-short-long`** (cadrage fonctionnel finalisé par DEC-018 le 26/05/26 — 8/10 sous-questions résolues). Périmètre tech : 2 dimensions `display_mode` × `content_variant` par advice, 15 colonnes CSV, 2 corpora par bloc indexés par advisor_type, tracking opt-in, contrainte UI no-overlap.
4. **Arbitrer le modèle de perte de bugs** avec le chercheur (1 green/cloud/piège vs total décrémenté).
5. **Confirmer ou revisiter les écarts GDD assumés** : 4 paths vs 2 paths, smooth pan vs fade noir.
6. **Spec reliability pattern** (fréquence, distribution, déclenchement) — à demander au chercheur.
7. **Finaliser le mapping QuestionnaireUI → 3 dimensions GDD** (sense of agency / acceptability / human-likeness) et l'éditabilité des textes.
8. **Compléter** Mountain UI, End-of-block summary, Redirect post-expérience (criticité moyenne, après les blocs scientifiques).
