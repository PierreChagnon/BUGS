# Revue de livraison — état consolidé au 08/09/2026

> **Date :** 08/09/2026 · **Rôle :** Pilotage
> **Méthode :** re-vérification, contre le code actuel (branche `dev`, HEAD `e4bd00c7`), de chacun
> des constats des deux revues du 28/07 ([`revue-completude-2026-07-28.md`](revue-completude-2026-07-28.md),
> [`revue-couverture-2026-07-28.md`](revue-couverture-2026-07-28.md)), plus inventaire des
> 27 commits livrés depuis, plus audit de fraîcheur de l'ensemble de `Docs/`.
> **Ce document remplace les deux revues du 28/07 comme référence de pilotage** — elles restent
> valables comme photographies datées et pour le détail des analyses ; leur statut constat par
> constat est donné en §2.
> **Aucune modification de code n'a été faite.** Chaque affirmation cite un `fichier:ligne` ou un commit.

> ### 📌 Addendum du 09/09/2026 — deux commits ont atterri le soir même, après la rédaction
>
> Cette revue photographie le HEAD `e4bd00c7` (08/09 après-midi). Deux commits livrés le 08/09 au soir
> modifient deux de ses verdicts ; les corrections sont reportées **dans le corps du document**,
> marquées *(MàJ 09/09)* :
>
> - **`a919a683`** (DEC-030) — export des **tirages réalisés** : +6 champs dans `TrialResponseRow`
>   (`proximal_forced`, `proximal_forced_value`, `proximal_forced_was_optimal`, `motor_forced`,
>   `motor_advice_visible`, `motor_advice_reliable`) + migration Supabase `20260908150000`.
>   → **N1-B passe de ❌ à 🟡** : le motor advice a désormais 2 de ses 4 valeurs réalisées en base ;
>   restent le **set actif** et le **set affiché** (Q-MOTOR-1). Le total de champs passe de 87 à **93**.
> - **`88899e9a`** (DEC-029) — l'**ordre d'affichage des advisors** est tiré 1×/bloc, seedé (salt 7),
>   mais **non exporté**. → **N2-A §9.1 passe de ❌ à 🟡** : reproductible depuis la seed, mais le
>   tirage réalisé (ce que le participant a vu) n'est toujours pas enregistré — **export requis par
>   le pilotage** (MàJ DEC-029 du 09/09), le non-export initial n'est pas validé.

---

## Verdict

**Le projet a substantiellement convergé vers la livraison depuis le 28/07 — et, contrairement à
la période précédente, la documentation a suivi.** Les deux bloqueurs les plus graves de juillet
sont levés : la condition contrôle `advisor = none` arrive en base (D1, commit `fe5fc771`), et le
consentement affiche un vrai texte validé (E2). La doc de pilotage et de validation a été
maintenue en continu (DEC-023 à DEC-025, dictionnaire et matrice à jour).

**Ce qui sépare encore le projet d'une passation réelle tient en quatre points :**

1. **Le niveau moteur reste sans mesure** (N1-B) — mais le refacto d'août a rendu l'export
   trivial : toutes les valeurs réalisées existent désormais dans `TrialDrawResolver.MotorAdviceDraw`,
   il ne manque que le branchement vers `TrialResponseRow`.
2. **La résilience du pipeline n'est pas faite** (A2/A3) — file d'envoi en mémoire seule,
   bloquée après échec des retries.
3. **Les textes des 4 questions trial-wise sont toujours « (a definir) »** et le pattern de
   fiabilité des advisors (Q-010) reste non spécifié — arbitrages chercheur.
   *(MàJ 09/09 : point **entièrement levé** — les 4 textes réels sont posés dans `ProximalScene.unity`
   (DEC-035, dernier libellé via `746a4406`, F6 clos), et Q-010 est **résolue par DEC-037** : le
   pattern de fiabilité est probabiliste, configuré par le chercheur au niveau tâche —
   `*_advice_reliable_probability`, dashboard + Zod, tirages seedés, réalisés exportés.)*
4. **La campagne de validation n'a jamais démarré** : gates G0–G5 tous ⬜, et le contrat de
   colonnes côté Supabase (D-003) n'est toujours pas vérifié — aggravé par la rupture
   d'échelle 5→7 points et le passage à 2 colonnes d'acceptabilité.

---

## 1. Livré depuis le 28/07 (27 commits, `c40eca6b..e4bd00c7`)

| Chantier | Commits | Contenu | Décision |
| :-- | :-- | :-- | :-- |
| **Correctif pipeline critiques** | `fe5fc771` (26/08) | Trials `advisor=none` envoyés (champs questionnaire null, omis du payload) ; `optimal_path_length`/`cloud_distance` nullables ; `SessionManager` miroir fidèle de la config (règle « pas d'advisor » portée par `HasAdvisor`) ; fingerprint de condition stable côté backend | DEC-026 |
| **Communication Report** | `d05931f7`→`485a35ed` (27-28/08) | Le panneau d'entrée de bloc distal annonce la qualité de communication (`Perfect`/`Partial`/`None`, 6 variantes de texte) calculée depuis les probabilités d'advice **et** les explanations (`ExplanationResolver.GetCommunicationQuality`). Affichage pur — non loggé, mais recalculable depuis la config | DEC-023 |
| **Explanations probabilistes** | `3e5560e5` | `display_mode` accepte `"forced/opt-in"` et `content_variant` `"short/long"` ; résolution par trial via `display_mode_forced_probability` / `content_variant_long_probability` ; seules les valeurs résolues remontent | — |
| **Nouvelles colonnes de sortie** | `8c3baa07`, `f7c8f3fc`, `62a52f58`, `c428bac8` | `advisor_path_config` (chemin **affiché**, ferme la colonne client 58) ; `map_config.cells[]` (état de chaque cellule — ferme PC-8) ; `proximal_advice_reliable_probability` + réalisé `proximal_advice_reliable` (ferme la colonne client 22) ; `green_bugs_session_total` (+ affiché en BreakScene) ; `session_name` | — |
| **Refacto architecture** | `48b05b25`, `7d098281`, `d4d69e28` (28/08-04/09) | `SeedUtility` (FNV-1a centralisé), `BlockDrawResolver` (tirages bloc, salts documentés, tie-break vallée 50/50 explicite), `TrialDrawResolver` (tirages trial, dont motor advice réalisé), `AdvisorBadgeUtility` (genre du badge tiré 1×/bloc, seedé salt 6, exporté `advisor_display_is_male`), `ModalPanelUIBase` ; DTO de config sans valeurs par défaut (validation Zod backend, dashboard source unique) ; `BuildInfo.Version = "1.1.0"` source unique de `build_version` | DEC-027 |
| **Refonte questionnaire** | `ec18eff1`, `128bdcd4`, `e4bd00c7` (07-08/09) | Échelle **7 points** ; **2 questions d'acceptabilité** par trial (sliders 1-7, colonnes `acceptability_question_1`/`_2` remplacent `acceptability_question`) ; agentivité et human-likeness en 7 boutons radio | DEC-025, DEC-028 |
| **EndSession allégée** | `7a86e064` (07/09) | Scission expérience en 2 parties, redirection `platform_url` ; **retrait de `ParticipantNoteUI` et de l'endpoint `api/participant-notes`** — `final_comments` n'est plus collecté | DEC-024 |
| **Consent/Welcome réels** | `78f9b6e1`, `a4a98569` (08/09) | Vrais textes de consentement (anglais, expé 2 parties, contact) posés dans `ConsentScene.unity` ; ajustements sur feedbacks de Mark | — |
| **RoundUI forced** | `dd5877ac` | Panneaux succès/échec dédiés aux trials proximal forced (un seul nuage présenté) | — |
| **Murs organiques** | `624f9a99` | Réécriture de `CorridorWallsGenerator` (~300 lignes) pour des murs moins rectilignes | — |

---

## 2. Statut des constats du 28/07 — verdict par verdict

Légende : ✅ corrigé · 🔁 clos par retrait/décision · 🟡 partiellement · ❌ toujours ouvert · 📘 résorbé côté doc.

### Écrans et pipeline (revue de complétude)

| Réf | Sujet | Verdict | Preuve / état au 08/09 |
| :-- | :-- | :-: | :-- |
| **E1** | QuestionnaireScene coquille vide, inatteignable | ❌ | `QuestionnaireUI.cs:38` inchangé ; aucun `AdvanceToPhase(GamePhase.Questionnaire)` ; scène toujours au build (`EditorBuildSettings.asset:38-39`) ; `QuestionConfig` toujours orphelin |
| **E2** | Consentement placeholder | ✅ | Vrai texte dans `ConsentScene.unity` (commits `78f9b6e1`, `a4a98569`) ; l'écrasement au `Start` de `ConsentUI.cs:17-21` est neutralisé — `_titleText`/`_bodyText` débranchés dans la scène (`ConsentScene.unity:240-241`, `fileID: 0`) ; decline via vraie modale (`FlowContinueScreenUI`). **Résidus** : code mort dans `ConsentUI.cs` (placeholders + message « La session s'arrete ici ») ; toujours pas de champ `consent_text` dans `SessionConfig` — le texte est porté par la scène, pas configurable via l'API |
| **E3** | Texte dev périmé sur AdvisorChoice | ❌ | `AdvisorChoiceUI.cs:56` affiche toujours « Ce choix est enregistre mais n'affecte pas encore la generation de la map. » — faux, visible participant. **Quick win** |
| **D1** | Trials `advisor=none` perdus | ✅ | `fe5fc771` — `TrialManager.cs:143-148` : le trial part avec les champs acceptabilité null (omis du payload). **Résidu** : un `sens_of_agency_question` vide (abandon réel du questionnaire) jette encore la ligne avec un simple `LogWarning` |
| **D2** | Pas de persistance locale de la file d'envoi | ❌ | `ApiClient.cs:58` — `Queue<>` mémoire seule, zéro `PlayerPrefs` |
| **D3** | File bloquée après échec des retries | ❌ | `ApiClient.cs:295-301` — `break` sans dépiler ; pas de `OnApplicationPause`/`Quit` ; la file ne repart qu'au prochain enqueue |
| **D4** | Fallback silencieux `motor_forced_set` → ZQSD | 🟡 | `FlowValueConverters.ToMotorKeySet` accepte désormais aussi `ZQSD`/`TFGH`/`IJKL` (`FlowDataModels.cs:673-681`) mais toute valeur inconnue retombe toujours sur ZQSD **sans log** (`:682-683`). Contexte atténuant : validation Zod côté backend depuis DEC-027 |
| **D5** | `build_version` incohérent | ✅ | `BuildInfo.cs` (`Version = "1.1.0"`), source unique déclarée ; `TrialManager.cs:290` la lit ; plus aucune version en dur ailleurs |
| **F8** | Contrat colonnes questionnaire | 🟡⚠️ | Le code émet maintenant **4 colonnes** : `acceptability_question_1`, `_2`, `sens_of_agency_question`, `human_likeness_question`. Le schéma SQL de référence (`spec-tech.md`) déclare toujours `q1_response`… (D-003). **La vérification côté Supabase reste à faire — et l'écart s'est creusé** (2 colonnes d'acceptabilité, échelle 7 points) |

### Écarts fonctionnels

| Réf | Sujet | Verdict | État au 08/09 |
| :-- | :-- | :-: | :-- |
| **F1** | `visibility_noise` | 🔁 | *(MàJ 09/09)* **Clos par décision (DEC-031)** : acté avec les chercheurs que la difficulté de discrimination est portée par le ratio vert/rouge et le `gap` (paramètres existants `min/max_green_ratio`, `gap_min`/`gap_max`) — aucun bruit de rendu ne sera implémenté. Q-013 fermée ; les 7 colonnes client `visibility_noise` sont abandonnées (écart CSV à faire acter). Corollaire : renforce Q-DISTAL-1 (le ratio réalisé devient la mesure de difficulté, or le distal ne l'exporte pas — N1-C) |
| **F2** | Retour arrière non bloqué | 🔁 | *(MàJ 09/09)* **Clos par décision (DEC-032, Q-BACK-1 option A)** : le budget de pas remplace l'interdiction de `specs-light` §4 — tout détour coûte (−1 vert/nuage par pas excédentaire) et est mesuré (`overtime_steps`, `player_path_log`). L'interdiction dure risquait le soft-lock en cul-de-sac. Dépendance tracée : la pénalité de budget attend elle-même le sign-off Q-007 (N2-F) |
| **F4** | Protocole de sortie | 🔁 | *(MàJ 09/09)* **Clos par décision (DEC-033, re-scope WebGL)** : le §2.8 du plan est une architecture desktop — en WebGL `Application.Quit()` est inerte et la fermeture d'onglet n'est pas interceptable fiablement. Pas de bouton Quitter (le `QuitButton` de `SettingsOverlay.prefab` est désactivé, `m_IsActive: 0`) ; conservation des données portée par le **Lot A2/A3** (devenu non négociable) ; `session_abandoned` dérivé côté serveur, pas de colonne nouvelle |
| **F5** | BLOCK_RECAP | 🔁 | *(MàJ 09/09)* **Clos par décision (DEC-034)** : la BreakScene est un écran de repos (countdown + total de session), **comportement revu et validé par le chercheur** — le récap systématique du §2.6 est abandonné (structurellement incompatible avec les pauses DEC-022 : des blocs s'enchaînent sans BreakScene). La fuite potentielle total de session vs `show_numerical_feedback` est actée négligeable. **DEC-003 (Mountain UI) annulée** dans la foulée |
| **F6** | Textes questionnaire placeholders | ✅ | *(MàJ 09/09)* **Clos — vérifié dans `ProximalScene.unity`** : les 4 textes réels vivent en scène (DEC-035, modèle E2/consentement) — acceptabilité 1 « How helpful… », acceptabilité 2 « How satisfied were you with your advisor? » (posée par `746a4406`, 09/09), agentivité « How much control… », human-likeness « How human-like… ». Les défauts « (a definir) » du code restent des fallbacks jamais montrés |
| **F7** | Pas de boot hors-ligne | 🔁 | *(MàJ 09/09)* **Clos par décision (DEC-036)** : aucun mode hors-ligne ne sera développé — « pas un problème pour nous », dev/test avec backend accessible, et le cas n'existe pas pour un participant (lancement par URL + `sessionId`, DEC-009) |

### Couverture de la donnée (revue de couverture)

| Réf | Sujet | Verdict | État au 08/09 |
| :-- | :-- | :-: | :-- |
| **N1-A** | Colonnes `*_match_advice` | ❌ | Toujours zéro occurrence. Recalculabilité **améliorée** : `advisor_path_config` + `map_config.cells` couvrent désormais le proximal-chemin ; le motor reste non recalculable tant que N1-B est ouvert |
| **N1-B** | Motor advice sans donnée de résultat | 🟡 | *(MàJ 09/09)* **Partiellement fermé par `a919a683`** (DEC-030, post-revue) : `motor_advice_visible` et `motor_advice_reliable` sont désormais exportés (`FlowDataModels.cs`, `TrialManager.ApplyMotorAdviceState`). **Restent absents : le set actif et le set affiché** (`TrialDrawResolver.MotorAdviceDraw.active_set`/`displayed_set` ne remontent pas) — sur un trial free, le set actif n'est reconstructible que par re-simulation du RNG. Résidu porté par Q-MOTOR-1 |
| **N1-C** | Stimulus distal réalisé non loggé | ❌ | `LeftScanData`/`RightScanData` toujours lus nulle part hors `DistalChoiceUI.cs` (Q-DISTAL-1) |
| **N1-D** | QuestionnaireScene inatteignable | ❌ | Voir E1 — triple verrou intact |
| **N1-F / N2-J** | `participant-notes` hors codebook | 🔁 | **Clos par retrait** : `ParticipantNoteUI` et l'endpoint supprimés (`7a86e064`, DEC-024). ⚠️ La colonne client 76 `final_comments` redevient ❌ — à faire acter par le chercheur si le template CSV V1 fait toujours foi |
| **N2-A §9.1** | Ordre des options advisor non seedé, non loggé | 🟡 | *(MàJ 09/09)* **Volet seed fermé par `88899e9a`** (DEC-029, post-revue) : tirage 1×/bloc via `BlockDrawResolver.DrawAdvisorDisplayOrder` (salt 7), consommé par `AdvisorChoiceUI`. **Volet log toujours ouvert** : `advisor_display_order` vit dans `PlayerSessionState`, pas dans `TrialResponseRow` — le tirage réalisé n'est pas enregistré. L'export est **requis par le pilotage** (MàJ DEC-029 du 09/09) ; le non-export initial n'est pas validé |
| **N2-A §9.2** | Genre du badge humain incohérent | ✅ | `AdvisorBadgeUtility` — tiré **1×/bloc, seedé** (salt 6), consommé par les 3 UI, champ `advisor_display_is_male`. *(Correctif 09/09 : ce champ vit dans `PlayerSessionState` — **il n'est pas exporté** dans `TrialResponseRow`, contrairement à ce qu'affirmait cette ligne. L'incohérence intra-trial est bien réglée ; le genre affiché reste recalculable depuis la seed mais absent des données — même question d'export que §9.1, cf. Q-RANDOM-1 volet apparence)* |
| **N2-B** | `human_likeness` répliqué sur toutes les lignes | ❌ | `ApiClient.cs:142-178` inchangé — comportement toujours non acté (Q-HL-1) |
| **N2-C** | `display_probability` non documenté | 📘 | Le mécanisme demeure (et s'est étendu : `content_variant_long_probability`), mais il est désormais **documenté** (spec-fonc explanations R9, TDD, dictionnaire). L'asymétrie distale est commentée dans le code |
| **N2-D** | `block_template_id` hors codebook | ✅ | Dictionnaire et matrice tenus à jour depuis le 28/07 |
| **N2-E** | Pénalité touche invalide non comptée | ❌ | `GameManager.cs:292-303` applique la pénalité, aucun compteur en base — `green_bugs_collected` toujours non décomposable |
| **N2-F** | Pénalité budget de pas hors GDD | ❌ | Toujours présente (`GameManager.cs:262-271`) — attend le sign-off Q-007 |
| **N2-G** | Overlay « Equipment failure » non spécifié | ❌ | Code présent (`FlowController.cs:102`), toujours aucune spec |
| **N2-H** | Deux conventions de départage | 🟡 | Génération : `GenerateOrderedPair` produit la paire ordonnée par construction (l'ancien tie-break a disparu). Runtime : `GameManager.GetBestCloud` (`GameManager.cs:418`) garde le `>` strict — à égalité le nuage de **droite** gagne (Q-TIE-1 toujours ouvert) |
| **N2-I** | Input physique vs affichage OS | 🟡 | Désormais commenté dans le code (`MotorAdviceController.cs:123-138`) — toujours pas documenté côté Docs |
| **N2-K** | `CLAUDE.md` périmé (parsing CLI) | ✅ | Corrigé — décrit maintenant le vrai flux (`sessionId=` seul argument, `SessionManager` ne parse rien) |
| **N2-L** | Résidus « −2 » dans TDD | ✅ | Corrigés |
| **N2-M** | Inventaire packages TDD incomplet | ❓ | Non re-vérifié — à traiter dans la passe TDD (cf. §5). HDRP toujours dans `Packages/manifest.json:15` à côté d'URP |
| **N2-N** | `readme.md` racine périmé | ✅* | Réécrit dans cette passe (08/09) |

### Questions trial-wise (§4 revue de couverture)

Le protocole a **changé** depuis le 28/07 : jusqu'à **3 questions par trial** (2 acceptabilité en
slider 1-7 + 1 agentivité en 7 radios) **+ 1** human-likeness au dernier trial du bloc.
À 36 trials/bloc, ~**108 interruptions Likert par bloc** (contre 72 estimées en juillet) —
Q-FREQ-1 reste ouverte et se durcit. L'échelle 5 points est morte (DEC-025) ; l'ordre des
questions reste mélangé (Fisher-Yates **seedé** sur `CurrentTrialSeed` — reproductible) mais
l'ordre présenté n'est toujours pas loggé ; l'acceptabilité reste non posée si
`advisor_choice == None` (le trial part quand même désormais, cf. D1).

---

## 3. Couverture CSV client — delta depuis le 28/07

`TrialResponseRow` comptait **87 champs** au moment de la revue (82 au 28/07) : +`acceptability_question_1`/`_2`
(remplacent `acceptability_question`), +`advisor_path_config`, +`green_bugs_session_total`,
+`proximal_advice_reliable`, +`proximal_advice_reliable_probability`.

> *(MàJ 09/09)* Le total est passé à **93 champs** avec `a919a683` (DEC-030) : +`proximal_forced`,
> +`proximal_forced_value`, +`proximal_forced_was_optimal`, +`motor_forced`, +`motor_advice_visible`,
> +`motor_advice_reliable` — les tirages **réalisés** du forçage proximal/motor, à côté des
> probabilités configurées (migration Supabase `20260908150000`).

Sur les **29 colonnes client manquantes** du 28/07 :

| Mouvement | Colonnes |
| :-- | :-- |
| ✅ Fermées | 22 `proximal_advice_reliability` · 58 `proximal_advice_path` (`advisor_path_config`) · 55 `map_layout` complété (`map_config.cells` — murs + pièges, ferme PC-8) |
| 🔁 Retirées par décision | 76 `final_comments` (DEC-024 — **à faire acter côté chercheur**) · *(MàJ 09/09)* les 7 `visibility_noise` (12-14, 32, 35, 47, 51 — DEC-031, F1/Q-013 clos : difficulté portée par ratio/gap) |
| ❌ Toujours manquantes | les 6 du niveau moteur (N1-B) · les 5 du stimulus distal (N1-C) · les 3 `*_match_advice` (N1-A) · ~~les 7 `visibility_noise`~~ *(→ retirées, DEC-031)* · `screen_type`/`screen_id`/`username` · `explanation_given_frequency` (réalisé) |
| ✅ *(MàJ 09/09)* Fermées par `a919a683` | 64 `motor_advice_given` (→ `motor_advice_visible`) · 23 `motor_advice_reliability` réalisé (→ `motor_advice_reliable`) — sur les 6 colonnes moteur, restent 63 `motor_choice_active_config`, 65 `motor_advice` (set affiché), 67 `motor_choice`, 68 `motor_choice_match_advice` |

Endpoints actuels : `GET api/sessions/{id}` · `POST api/trial-responses` · `PATCH
api/trial-responses/{id}` · `GET` images publiques. (`api/participant-notes` supprimé.)

---

## 4. Ce qui bloque encore la livraison — ordre proposé

1. **Arbitrages chercheur** (rien de neuf ne peut se passer sans eux) :
   ~~textes réels des 4 questions~~ *(MàJ 09/09 : réglé — les 4 textes sont en scène, DEC-035 +
   `746a4406`, F6 clos)* · ~~pattern de fiabilité des advisors (Q-010)~~ *(MàJ 09/09 : résolu —
   DEC-037, probabiliste configuré chercheur)* ·
   sign-off du « null assumé » pour l'acceptabilité sans advisor (Q-DATA-1, tranché en code
   le 26/08) · acter le retrait de `final_comments` (DEC-024) et la rupture d'échelle 5→7 ·
   noms/formats des colonnes motor (Q-MOTOR-1).
2. **Lot E résiduel — mesure** (le refacto a baissé le coût) :
   E1' export motor advice réalisé — *(MàJ 09/09)* **partiellement fait** (`a919a683`, DEC-030 :
   visible + fiable exportés) ; **reste** l'export du set actif et du set affiché (N1-B, Q-MOTOR-1) ·
   E2' log du stimulus distal (`LeftScanData`/`RightScanData`, N1-C) ·
   E5' ordre des options advisor — *(MàJ 09/09)* **seed ✅ fait** (`88899e9a`, DEC-029) ;
   **reste** l'export du tirage réalisé, requis par le pilotage (N2-A §9.1) ·
   E4' compteur de touches invalides (N2-E) ·
   E3' colonnes `*_match_advice` ou acter « recalculables » (N1-A).
3. **Lot A résiduel — résilience** : A2 persistance de la file (`ApiClient.cs:58`) ·
   A3 `break` → dépilage/backoff + retry sur `OnApplicationPause`/`Quit` (`:295-301`) ·
   A4 log d'erreur sur `ToMotorKeySet` inconnu.
4. **Quick wins visibles participant** : E3 (une ligne de texte) · décision
   QuestionnaireScene : retirer du build ou câbler (N1-D — rappel : câbler = extension du
   modèle de données **et** du contrat PATCH).
5. **Campagne de validation** : ouvrir G0 ; exécuter en premier le check
   « hypothesis-readiness » (échouerait aujourd'hui sur toute hypothèse motrice) ;
   vérifier le schéma réel des colonnes Supabase (D-003) **avant** toute passation.
6. **Dette doc et hygiène** : cf. §5.

---

## 5. État documentaire après la passe du 08/09

### Mis à jour dans cette passe (sans toucher au code)

- `avancement.md` — Lot A1 reclassé corrigé, livraisons d'août-septembre intégrées, E2 requalifié
- `questions-client.md` — Q-DATA-1 requalifiée en sign-off, Q-011/Q-FREQ-1 réalignées sur les 4 questions
- `decisions.md` — DEC-026/027/028 actées rétroactivement (correctif pipeline, dashboard source unique + `BuildInfo`, 2ᵉ acceptabilité)
- `validation/journal-divergences.md` — D-001 partiellement clos, D-005 clos, divergences questionnaire ajoutées
- `validation/dictionnaire-donnees.md` — `map_config.cells`, nullables, avertissement 5→7
- `validation/matrice-tracabilite.md` — recompte des champs, lignes récentes vérifiées
- `validation/campagne-test-validation.md`, `config-session-test.md`, `gates-et-questions.md` — errata `acceptability_question_1/_2`, 4 questions
- `specs/multi-screen-flow/spec-tech.md` — encart d'errata sur le schéma SQL (D-003) et DEC-024
- `readme.md` racine — réécrit sur l'architecture actuelle
- Bandeaux de remplacement posés sur les deux revues du 28/07

### Dette documentaire restante

| Sujet | Détail |
| :-- | :-- |
| **TDD.md** | ~~Changelog gelé à v3.1 (27/08)~~ *(MàJ 09/09 : journal passé en v3.2 par `a919a683`, en-tête réaligné 3.0→3.2 lors de la passe de réconciliation)* ; **section 7 toujours manquante** (saute de 6.7 à 8) ; `BreakSceneController`, `BlockOrderRandomizer`, classes Audio, tutoriels, `SettingsPanelUI` absents ; nouvelles classes du refacto (`BlockDrawResolver`, `AdvisorBadgeUtility`, `ModalPanelUIBase`) non documentées ; §6.7 QuestionnaireUI à réviser vs N1-D ; inventaire packages §11.2 (N2-M) à refaire. **À traiter via le protocole `!doc`** (diff soumis à validation) — non fait dans cette passe, conformément aux règles du projet |
| **Spec-techs manquantes** | `free-forced-choices` et `explanations-short-long` — toujours « à produire » ; les tie-breakers R6/R15 renvoient toujours à un document fantôme (le tie-break vallée 50/50 est maintenant explicite dans `BlockDrawResolver`, salt 1 — matière à rétro-documentation) |
| **Sans aucune doc** | Murs organiques (`CorridorWallsGenerator`, `624f9a99`) · `SliderValueLabel` · panneaux forced de `RoundUI` (mentions éparses seulement) |
| **Hygiène repo** | `mono_crash.*.json` (334 Ko) toujours versionnés · 3 scènes `[LEGACY]`, `ProximalSceneFLO.unity`, 3 prefabs `_DRAFT`, `EndOfBlockPanel_TODO.prefab` · branches `feature/bug-cloud-feedback` et `scene/mission-dashboard` non mergées ni closes · HDRP installé à côté d'URP · toujours aucun test ni `.asmdef` |

---

## Index des verdicts

**Corrigés depuis le 28/07 :** D1, D5, E2, N2-A §9.2, N2-C (doc), N2-D, N2-K, N2-L, N2-N,
PC-8, colonnes client 22 et 58.
**Clos par retrait :** N1-F, N2-J (DEC-024 — sign-off chercheur à obtenir) ; *(MàJ 09/09)*
**F1** (DEC-031 — difficulté portée par ratio/gap, Q-013 fermée, 7 colonnes client abandonnées) ;
**F2** (DEC-032 — budget de pas remplace l'interdiction de retour arrière, Q-BACK-1 fermée) ;
**F4** (DEC-033 — QUIT_OVERLAY abandonné, re-scope WebGL : conservation des données par Lot A2/A3,
`session_abandoned` dérivé côté serveur) ;
**F5** (DEC-034 — BreakScene = écran de repos validé chercheur, BLOCK_RECAP §2.6 abandonné,
DEC-003 Mountain UI annulée) ;
**F7** (DEC-036 — pas de mode hors-ligne, assumé).
**Partiels :** D4, F8, N2-H, N2-I — et, depuis le 08/09 au soir *(MàJ 09/09)* :
**N1-B** (`a919a683`/DEC-030 — reste set actif/affiché) et **N2-A §9.1** (`88899e9a`/DEC-029 —
reste l'export du tirage réalisé).
**Corrigé le 09/09 :** **F6** (DEC-035 — les 4 textes réels en scène, dernier libellé via `746a4406`).
**Toujours ouverts :** E1/N1-D, E3, D2, D3, N1-A, N1-C,
N2-B, N2-E, N2-F, N2-G, Q-TIE-1.
