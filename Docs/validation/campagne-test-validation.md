# Campagne de test & validation — Pipeline de données de recherche

> **But** : garantir, avant livraison au chercheur, que le CSV de sortie est **complet, correct, traçable et exploitable**, et que la collecte ne perd pas de données.
> **Périmètre** : côté Unity (jusqu'au JSON envoyé) + confrontation d'un **CSV échantillon** exporté depuis Supabase. Le backend n'est pas modifié.
> **Source de vérité du contrat** : le **code** (`TrialResponseRow`, `Assets/Game/Scripts/Data/FlowDataModels.cs:439-521`). Le CSV doit s'y aligner ; toute divergence est remontée.
> **Statut** : méthodologie — l'équipe exécute. Ce document + les annexes de ce dossier sont les outils de la campagne.

📌 Rôle : **Analyse fonctionnelle** (données CSV) + **Pilotage** (pré-livraison).

---

## 0. Pourquoi cette campagne (les 2 risques qui la structurent)

1. **Dérive du contrat de données.** Les noms émis par le code ≠ noms de la doc/décisions.
   Ex. code : `advisor_forced`, `advisor_forced_value`, `distal_forced`, `proximal_forced_probability`, `acceptability_question_1` / `acceptability_question_2` / `sens_of_agency_question` / `human_likeness_question`.
   Doc/décisions : `meta_choice_is_forced`, `proximal_choice_is_forced`, `q1_response` / `q2_response` / `q3_response`.
   → C'est exactement ce qui casse l'analyse côté chercheur. **Le code fait foi**, mais il faut vérifier ce que reçoit réellement le chercheur (colonnes CSV) et lui livrer un dictionnaire aligné.

2. **Perte de données silencieuse.** Aucune persistance locale : crash, questionnaire incomplet, fermeture navigateur, reload de scène, ou 3 échecs réseau ⇒ trial perdu sans trace. Pour une expé (participants recrutés/payés), un trial perdu = un participant potentiellement corrompu.

---

## 1. Comment mener la campagne (principe)

Pyramide de tests « data », dans l'ordre. **On ne monte pas d'un axe tant que le précédent n'est pas vert.**

| # | Axe | Question à laquelle il répond | Annexe(s) |
| :- | :-- | :-- | :-- |
| 0 | Gel des références (GATES) | « Les colonnes ont-elles un sens acté ? » | `gates-et-questions.md` |
| 1 | Traçabilité / contrat | « Tous les liens sont-ils faits ? » | `matrice-tracabilite.md` |
| 2 | Sémantique par champ | « Les valeurs sont-elles justes ? » | `matrice-tracabilite.md` |
| 3 | Couverture expérimentale | « Toutes les conditions apparaissent-elles ? » | `config-session-test.md`, `protocole-sessions-test.md` |
| 4 | Intégrité & résilience | « Perd-on des données ? » | `protocole-sessions-test.md`, `journal-divergences.md` |
| 5 | Bout-en-bout | « La chaîne complète tient-elle ? » | `protocole-sessions-test.md` |
| 6 | Reproductibilité | « Même seed ⇒ même trial ? » | `protocole-sessions-test.md` |
| 7 | Prêt pour l'analyse | « Le chercheur peut-il analyser seul ? » | `dictionnaire-donnees.md` |

---

## 2. Détail des axes (checklists exécutables)

### Axe 0 — Gel des références *(GATES bloquants)*
- [ ] **G0.1** — Trancher avec le chercheur les questions qui donnent du **sens** aux colonnes :
  - **Q-007** modèle de perte de bugs (−1 green/piège/nuage vs décrément total — divergence GDD/code)
  - **Q-010** pattern de fiabilité advisor (*non spécifié*)
  - **Q-011** mapping des 4 questions ↔ `acceptability_question_1` / `acceptability_question_2` / `sens_of_agency_question` / `human_likeness_question` (2 questions d'acceptabilité depuis le 08/09/26)
- [ ] **G0.2** — Geler la liste des champs de `TrialResponseRow` = contrat opposable (cf. `matrice-tracabilite.md`).
- [ ] **G0.3** — Définir la/les session(s) de test qui exercent toutes les conditions (cf. `config-session-test.md`).

### Axe 1 — Traçabilité / contrat *(le cœur)*
- [ ] Renseigner, dans `matrice-tracabilite.md`, les colonnes **« Colonne CSV présente »** et **« Statut »** à partir du CSV échantillon exporté.
- [ ] Lister dans `journal-divergences.md` : colonnes **manquantes**, **en trop**, **renommées**, **types incohérents**, **null mal géré**.
- **Critère de sortie** : 0 divergence non expliquée entre champs émis (code) et colonnes CSV.

### Axe 2 — Sémantique par champ
- [ ] Pour chaque champ : valeur/plage/format attendus vérifiés sur des **trials déterministes** (seed connu ⇒ map/distances/chemin prédictibles).
- [ ] Colonnes composites : `map_config` (JSON), `player_path_log` (array JSON `{x,y,t}`), `distal_scene` (objet JSON), timestamps ISO 8601 **UTC** (`"o"`).
- [ ] **Règles de cohérence inter-champs** (à passer sur chaque ligne) :
  - `choice_correct` ⇔ `proximal_choice == true_cloud`
  - `followed_advisor_path` cohérent avec `player_path_log` vs chemin affiché
  - `green_bugs_accumulated` monotone croissant dans le bloc
  - `green_bugs_session_total` monotone croissant sur toute la session, jamais remis à 0 entre blocs, et plat sur les lignes `is_tutorial = true`
  - `block_index`/`trial_index` continus ; nb de lignes/bloc == `trial_count`
  - `overtime_steps` cohérent avec `steps` et `cloud_distance`
  - `started_at` ≤ 1er `t` de `player_path_log` ≤ `ended_at`

### Axe 3 — Couverture expérimentale
- [ ] Remplir la matrice de couverture (cf. `config-session-test.md`) : chaque manipulation × niveau apparaît ≥ 1 fois et est loggée correctement.
  - Free vs **forced** (advisor/distal/proximal/motor) × `*_forced_value`
  - **Explanations** : `display_mode` {forced, opt-in, none} × `content_variant` {short, long} × type advisor ; tracking opt-in (`*_explanation_clicked`, `*_explanation_display_duration_ms`)
  - Reliability/visibilité (distal/motor)
  - Motor sets {QZD, FTH, JIL} — ⚠️ `JIL` (IJKL) remplace `KOM` (OKLM) depuis le commit `7b3a9e86`. Vérifier qu'aucune config de session Supabase ne porte encore `KOM` : `FlowValueConverters.ToMotorKeySet` la convertirait **silencieusement en ZQSD**.
  - Suboptimal path / detour / fog / suboptimal traps
- [ ] **Tutorial** : `is_tutorial=true` ⇒ trial **NON envoyé** (DEC-014) — vérifier l'absence de ligne et qu'elle ne crée pas de « trou » mal interprété.
- [ ] Vérifier la **granularité** (blockwise vs trialwise) conforme aux décisions.

### Axe 4 — Intégrité & résilience *(perte de données)*
- [ ] Rejouer chaque scénario de perte (cf. `protocole-sessions-test.md` §Résilience) et **mesurer** ce qui manque dans le CSV.
  - Réseau coupé pendant/après trial
  - Questionnaire incomplet ⇒ **envoi annulé** (`TrialManager.cs:119-124`)
  - Fermeture navigateur en cours de session
  - Reload de scène
  - 3 retries épuisés — ⚠️ `ProcessPendingTrialRequests` fait `break` au 1er échec (`ApiClient.cs:349-353`) ⇒ **toute la file bloquée**
- [ ] Vérifier que le **PATCH human-likeness** (`QueueHumanLikenessPatchForBlock`) atterrit sur la **bonne ligne** du bloc.
- [ ] Faire **acter** au chercheur le comportement acceptable + consigner les mitigations recommandées dans `journal-divergences.md`.

### Axe 5 — Bout-en-bout
- [ ] Session complète factice (1 participant, tous les blocs), seed fixe : `sessionId=…` → fetch config → blocs/trials → POST/PATCH → lignes Supabase → **export CSV**.
- [ ] Vérifier : 1 ligne / trial non-tutorial ; comptes corrects ; clés `participant_id`/`block_index`/`trial_index` jointables ; timestamps parsables ; **survie des colonnes JSON à l'échappement CSV** (virgules/guillemets/retours-ligne dans `map_config` & `player_path_log`) ; UTF-8 ; pas de troncature.
- [ ] Rejouer avec **≥ 2 participants** : unicité `participant_id`, pas de contamination croisée.

### Axe 6 — Reproductibilité
- [ ] Même seed 2× ⇒ `map_config`, distances, `optimal_path_length`, tirages identiques (diff hors timestamps & input humain).
- [ ] ⚠️ **Vérifier `trial_seed`** : loggé = `session.randomizationSeed` si `SessionManager` présent (`TrialManager.cs:269`), potentiellement **constant**, et non le seed per-trial (`FlowController.CurrentTrialSeed`). Confirmer que le seed loggé **régénère bien le trial**.
- [ ] Vérifier la **précédence de config** dans `BuildBaseRow` (`SessionManager` vs `FlowController.ActiveMapConfig`) : laquelle pilote réellement le trial.

### Axe 7 — Prêt pour l'analyse
- [ ] Valider le **dictionnaire de données** (`dictionnaire-donnees.md`) avec le chercheur.
- [ ] **Hypothesis-readiness** : pour chaque H1–H8, lister les colonnes nécessaires et confirmer qu'elles existent, non ambiguës, suffisantes.
- [ ] Transmettre les **caveats d'analyse** (cf. `dictionnaire-donnees.md` §Limites connues).

---

## 3. Human gates (jalons humains / hors capacité Unity-blind)

| Gate | Contenu | Qui |
| :-- | :-- | :-- |
| G0 | Sign-off chercheur Q-007/010/011 + gel du contrat | Chercheur + Florian |
| G1 | Matrice de traçabilité renseignée ⇒ divergences listées | Équipe |
| G2 | Config session de test prête dans Supabase | Équipe (backend) |
| G3 | Runs de test exécutés (build Unity + play / WebGL) | Florian / équipe |
| G4 | CSV échantillon exporté & confronté | Équipe |
| G5 | Codebook validé et signé | Chercheur |

Détail et suivi : `gates-et-questions.md`.

---

## 4. Critères de sortie (la campagne est « réussie » quand…)

1. **0 divergence non expliquée** code ↔ colonnes CSV
2. **100 % de couverture** de la matrice de conditions ; tutorial correctement exclu
3. Toutes les **règles de cohérence inter-champs** passent sur l'échantillon
4. Tous les **scénarios de perte** caractérisés, comportement **acté** par le chercheur
5. **Reproductibilité** confirmée + point `trial_seed` levé
6. **Codebook validé** + H1–H8 mappées à des colonnes existantes et non ambiguës
7. **Caveats d'analyse** documentés et transmis

---

## 5. Notes

- Aucun code applicatif n'est modifié par cette campagne. Les corrections éventuelles (persistance locale, alignement des noms, seed) = chantiers séparés issus des divergences remontées.
- En fin de campagne, mettre à jour `Docs/project-state/` (decisions / avancement / questions-client).
- Emplacement de ce dossier : `Docs/validation/` (QA transverse). À déplacer sous `Docs/specs/validation-donnees/` si on préfère la convention chantier.
