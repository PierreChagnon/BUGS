# Spec Fonctionnelle — Free / Forced choices

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** 2026-05-20
**Statut :** `draft`
**Chantier :** Free / Forced choices
**Spec tech associée :** `Docs/specs/free-forced-choices/spec-tech.md` (à produire)

---

## 1. Contexte et scope

### Objectif

Permettre que, pour certains blocs ou certains trials, chacun des **quatre choix** du protocole — meta-choice (advisor), distal-choice (vallée), proximal-choice (cloud), motor-choice (set de touches) — soit **imposé** au participant au lieu d'être libre. La mécanique constitue l'outil expérimental central qui permet de dissocier deux variables sinon confondues :

- l'effet de **recevoir une information** (advice à un niveau donné),
- l'effet d'**exercer une agency** (avoir choisi vs subir le choix).

Brique nécessaire à la validité des hypothèses **H1 à H8** du GDD (sense of agency, acceptabilité, advice-taking, human-likeness, et leurs interactions avec le niveau d'abstraction et la fiabilité de l'advisor).

### Périmètre IN

- [x] Mécanique free/forced sur les **quatre niveaux** : meta (advisor), distal (vallée), proximal (cloud), motor (set de touches).
- [x] **Granularités différenciées** selon le niveau :
  - meta, distal, motor → pilotés au **bloc**,
  - proximal → piloté au **trial** par tirage probabiliste.
- [x] **Configuration depuis le session config panel** (chercheur) — au niveau `BlockConfig` pour les trois niveaux block-wise, et au niveau `BlockConfig` également pour la probabilité proximal (tirage par trial).
- [x] **Comportement UI cohérent** quand le choix est imposé : grisage des options non sélectionnables, affichage unique des éléments forcés.
- [x] Articulation entre **forced × advice** (advice toujours affichable même quand le choix est forcé, sauf si conflit logique — cf. §3 et Q-FF-4/Q-FF-6).
- [x] Articulation entre **forced × reliability** (la fiabilité du distal forcé est elle-même probabiliste : `distal_forced_optimal_probability`).
- [x] **Tracé data** dans la table `trial_responses` (DEC-011) avec colonnes dédiées par niveau (`*_is_forced` + valeur forcée le cas échéant).

### Périmètre OUT

| Exclu | Raison | Reporté à |
|:---|:---|:---|
| Choix d'architecture des champs config (extension de `BlockConfig` vs nouvelle classe) | Décision technique | Spec tech `free-forced-choices/spec-tech.md` |
| Design UI graphique (style du grisage, animations de désactivation) | Hors analyse fonctionnelle | Chantier UI Advisors |
| Contenus textuels des advisors quand le choix est imposé | Hors spec, dépend du chantier explanations | Spec `explanations-short-long` |
| Articulation fine free/forced × explanations | Co-dépendance, à confirmer avec spec explanations | TR6 de `explanations-short-long` |
| Pattern de reliability advisor (fréquence d'apparition de l'advice) | Chantier distinct | Spec reliability (Q-010) |
| Tutorial : pas de mécanique free/forced dans le bloc tutorial (cf. DEC-014) | Hors scope expérimental | — |

### Décisions déjà prises

- **DEC-001** — Meta-choice une fois par bloc → cohérent avec une mécanique forced block-wise sur ce niveau.
- **DEC-011** — Table plate `trial_responses` → les colonnes free/forced sont ajoutées comme colonnes plates de cette table.
- **DEC-014** — Tutorial = `BlockConfig` avec `is_tutorial = true`, pas d'envoi API → le bloc tutorial n'active jamais le mode forced.
- **Notes orales chercheur (2026-05-20)** — répondent partiellement à Q-005 (granularité). Modèle retenu :
  - **Meta (advisor)** : toggle bloc ON/OFF, pas de proba. Le chercheur définit **laquelle** des 3 options est imposée quand ON.
  - **Distal (vallée)** : forced piloté au bloc, **pas un simple ON/OFF** — paramètre probabiliste `distal_forced_optimal_probability` qui contrôle si la zone imposée est l'optimale ou non.
  - **Proximal (cloud)** : pas de toggle bloc — paramètre `proximal_forced_probability` au niveau bloc, tirage par trial.
  - **Motor (set de touches)** : toggle bloc ON/OFF. Quand ON, **le même set de touches est utilisé pour tous les trials du bloc** (au lieu d'un tirage aléatoire par trial).

### Dépendances

- **Requiert :** `AdvisorChoiceScene` + `AdvisorChoiceUI` + `AdvisorOptionButton`, `DistalChoiceScene` + `DistalChoiceUI` + `DistalValleyScanView`, `ProximalScene` + `BugCloudSpawner`, `GridMoverNewInput` (motor sets), `BlockConfig` (`FlowDataModels.cs`), pipeline `trial_responses` (DEC-011).
- **Est requis par :** validité de H1–H8, finalisation cohérente du chantier `explanations-short-long` (TR6), spec reliability (Q-010) qui devra tenir compte du mode forced sur le distal.
- **Doit rester cohérent avec :** DEC-001 (meta-choice block-wise), DEC-011 (table plate), DEC-014 (neutralité tutorial), spec `MotorAdvice` (le motor advice reste affichable quand motor forced).

---

## 2. Catalogue des comportements

### 2.1 Meta-choice forced (AdvisorChoiceScene)

**Rôle :** Imposer au participant le type d'advisor pour le bloc à venir (no advisor / human-bot / bot-bot), au lieu de le laisser choisir. Permet d'isoler l'effet "avoir choisi son advisor" de l'effet "type d'advisor" sur acceptabilité, human-likeness, advice-taking (H1, H2, H3, H4, H7).
**Quand :** En entrée de chaque bloc, à l'affichage de la `AdvisorChoiceScene`.

#### Ce qui est affiché

- Si **mode = free** (par défaut, `advisor_forced = false`) : les trois boutons advisor sont actifs, le participant choisit.
- Si **mode = forced** (`advisor_forced = true`) :
  - Les **deux options non retenues** sont **grisées** (visibles mais non cliquables).
  - L'**option imposée** (`advisor_forced_value ∈ {none, human, robot}`, définie par le chercheur dans `BlockConfig`) est seule sélectionnable.
  - Le participant doit confirmer pour passer à la suite (le bouton "Continuer" reste actif sur l'option imposée uniquement). Voir Q-FF-7 sur l'auto-validation.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Clic sur l'option imposée | Sélection acceptée, transition vers DistalChoiceScene | — |
| Clic sur une option grisée | Aucun effet (bouton non interactif) | Pas de feedback "tentative refusée" requis (cf. Q-FF-8 sur feedback explicite "imposé") |
| Tentative de skip / navigation arrière | Bloqué par le flow standard | — |

#### Règles métier

- **R1** : `advisor_forced = true` est conditionné à un `advisor_forced_value` non vide dans la même `BlockConfig`. Si `advisor_forced = true` et `advisor_forced_value` non spécifié → comportement à définir (cf. Q-FF-1).
- **R2** : la valeur imposée est lue depuis la config bloc une seule fois en entrée de scène, n'est pas re-tirée en cours de bloc.
- **R3** : `State.advisor_choice` est écrit avec `advisor_forced_value` à la validation de la scène, exactement comme en mode free.
- **R4** : pendant le bloc tutorial (`is_tutorial = true`), `advisor_forced` est ignoré quoi qu'il dise (mode free de fait).

#### Modes

- **free** vs **forced** (block-wise).
- Quand forced : valeur imposée ∈ `{none, human, robot}`.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode meta forced ce bloc | `meta_choice_is_forced` | `true` / `false` | À l'entrée de la scène |
| Valeur imposée (si forced) | `meta_choice_forced_value` | `none` / `human` / `robot` / `null` (si free) | À l'entrée de la scène |

> La colonne existante `advisor_type` (= `advisor_choice` côté flow) reste le **résultat** du choix. Les deux nouvelles colonnes documentent la **modalité**.

---

### 2.2 Distal-choice forced (DistalChoiceScene)

**Rôle :** Imposer au participant la vallée à explorer pour le bloc, avec une probabilité que la vallée imposée soit l'optimale ou la sous-optimale. Permet d'étudier l'effet "subir une zone" × "qualité de la zone subie" sur sense of agency et performance (H5, H6, H8).
**Quand :** En entrée de chaque bloc, à l'affichage de la `DistalChoiceScene`, après le meta-choice.

#### Ce qui est affiché

- Si **mode = free** (`distal_forced = false`) : les deux vallées (A / B) sont actives, le participant choisit.
- Si **mode = forced** (`distal_forced = true`) :
  - Une vallée est **grisée** (visible mais non cliquable),
  - L'autre est seule sélectionnable.
  - **La vallée imposée est tirée selon `distal_forced_optimal_probability`** :
    - tirage Bernoulli de paramètre `p = distal_forced_optimal_probability`,
    - si succès → la vallée imposée est l'**optimale** (celle qui rendra le plus de green bugs en moyenne sur le bloc),
    - si échec → la vallée imposée est la **sous-optimale**.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Clic sur la vallée imposée | Sélection acceptée, transition vers ProximalScene (premier trial) | — |
| Clic sur la vallée grisée | Aucun effet | Pas de feedback "tentative refusée" requis (cf. Q-FF-8) |

#### Règles métier

- **R5** : `distal_forced_optimal_probability ∈ [0, 1]` est lu depuis `BlockConfig`. Tirage effectué une seule fois à l'entrée de scène (pas re-tiré en cours de bloc).
- **R6** : la notion de "vallée optimale" est dérivée des paramètres de génération de chaque vallée (`MapGenConfig` de `valley_a` vs `valley_b`) — la vallée avec l'espérance de green bugs la plus haute. Si les deux vallées ont la même espérance → tie-breaker à définir (cf. Q-FF-2).
- **R7** : `State.valley_choice` est écrit avec la vallée imposée à la validation de la scène.
- **R8** : pendant le bloc tutorial (`is_tutorial = true`), `distal_forced` est ignoré.
- **R9** : si `meta_choice_forced_value = none` (forced "no advisor") **OU** si le participant a choisi `no advisor` en mode free, le distal advice n'est pas affiché (état existant). Le mode forced sur le distal-choice reste indépendant de cette condition : on peut imposer une vallée même sans advisor (cf. Q-FF-5).

#### Modes

- **free** vs **forced** (block-wise).
- Quand forced : vallée imposée selon `distal_forced_optimal_probability` (résultat ∈ `{optimal, suboptimal}` × `{A, B}`).

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode distal forced ce bloc | `distal_choice_is_forced` | `true` / `false` | À l'entrée de la scène |
| Vallée imposée (si forced) | `distal_choice_forced_value` | `A` / `B` / `null` (si free) | À l'entrée de la scène |
| Optimalité de la vallée imposée (si forced) | `distal_choice_forced_was_optimal` | `true` / `false` / `null` (si free) | À l'entrée de la scène, après tirage |
| Probabilité utilisée (audit) | `distal_choice_forced_optimal_probability` | `[0, 1]` / `null` (si free) | À l'entrée de la scène |

---

### 2.3 Proximal-choice forced (ProximalScene)

**Rôle :** Imposer au participant le cloud à atteindre sur un trial donné (un seul cloud affiché, pas de choix). Permet d'étudier l'effet de subir le choix de cible × le niveau d'advice donné, trial par trial (H4, H5, H6).
**Quand :** À chaque trial, à l'entrée de la `ProximalScene`. Décision tirée trial-by-trial.

#### Ce qui est affiché

- Si **mode = free** (issue du tirage) : les **deux clouds** sont affichés normalement, le participant choisit en navigant vers l'un ou l'autre.
- Si **mode = forced** (issue du tirage) :
  - **Un seul cloud est affiché** sur la map (celui imposé),
  - L'autre n'est pas instancié (ou est masqué — décision UI à confirmer cf. Q-FF-3),
  - Le participant n'a qu'une cible disponible et navigue obligatoirement vers elle.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Navigation vers le cloud unique | Comportement standard, fin de trial à l'atteinte | — |
| Tentative de partir dans la direction opposée | Comportement standard du forest screen (le joueur peut s'éloigner mais n'a aucun cloud à atteindre) | Cf. Q-FF-3 sur la mécanique "pas d'autre cible" |

#### Règles métier

- **R10** : `proximal_forced_probability ∈ [0, 1]` est lu depuis `BlockConfig`. Tirage Bernoulli de paramètre `p` effectué **au début de chaque trial** indépendamment des autres trials du bloc.
- **R11** : quand le trial est forced, **quel cloud est affiché** est défini par une règle à confirmer (cf. Q-FF-4 — optimal / advisé / random / configurable).
- **R12** : pendant le bloc tutorial, `proximal_forced` est ignoré (mode free systématique).
- **R13** : `target_choice_match_advice` (colonne existante) doit avoir une convention claire en mode forced : `null` (pas applicable), `true` par défaut (le joueur n'a pas pu dévier), ou autre — cf. Q-FF-10.
- **R14** : interaction avec le proximal-advice : si trial forced ET advice présent, le proximal-advice est **toujours affiché** par défaut (orthogonalité advice/forced), sauf décision contraire — cf. Q-FF-4.

#### Modes

- **free** vs **forced** (tirage trial-wise).
- Quand forced : cloud imposé ∈ `{left, right}` (ou `{A, B}` selon convention).

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode proximal forced ce trial | `proximal_choice_is_forced` | `true` / `false` | Au début du trial |
| Cloud imposé (si forced) | `proximal_choice_forced_value` | `left` / `right` / `null` (si free) | Au début du trial |
| Probabilité utilisée (audit) | `proximal_choice_forced_probability` | `[0, 1]` | Au début du trial |

---

### 2.4 Motor-choice forced (ProximalScene — Forest screen)

**Rôle :** Fixer le set de touches actif sur l'ensemble du bloc (au lieu du tirage aléatoire par trial). Permet d'isoler l'effet "apprendre/découvrir les touches à chaque trial" de l'effet "exécuter un set stable" sur le coût moteur de chaque trial.
**Quand :** En entrée du premier trial du bloc, le set actif est fixé pour tout le bloc.

#### Ce qui est affiché

- Si **mode = free** (`motor_forced = false`, comportement actuel) : le set actif est **tiré aléatoirement à chaque trial** parmi `{QZD, FTH, KOM}`. Le participant doit découvrir le set actif ou se reposer sur le motor-advice.
- Si **mode = forced** (`motor_forced = true`) :
  - Le set actif est **le même pour tous les trials du bloc**,
  - La valeur du set imposé (`motor_forced_set`) est définie au niveau du bloc — soit fixée par le chercheur, soit tirée une seule fois au début du bloc (cf. Q-FF-9).
  - L'UI ne change pas par rapport au mode free (le set actif n'est pas affiché de base ; seul le motor-advice peut le révéler).

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Appui sur une touche du set imposé | Mouvement standard | — |
| Appui sur une touche hors set imposé | Aucun mouvement (comportement existant) | Aucune pénalité explicite (hors scope motor advice) |

#### Règles métier

- **R15** : `motor_forced ∈ {true, false}` et `motor_forced_set ∈ {QZD, FTH, KOM, null}` sont lus depuis `BlockConfig` à l'entrée du premier trial du bloc.
- **R16** : si `motor_forced = true` et `motor_forced_set` non spécifié → tirage aléatoire **une seule fois** au début du bloc (à confirmer cf. Q-FF-9).
- **R17** : pendant le bloc tutorial, `motor_forced` est ignoré (tirage par trial systématique).
- **R18** : interaction avec le motor-advice : le motor-advice **reste affichable** selon ses propres règles de visibilité/fiabilité, indépendamment de `motor_forced` (orthogonalité). Cf. Q-FF-6 si le set imposé doit être systématiquement reflété par l'advice.

#### Modes

- **free** vs **forced** (block-wise).
- Quand forced : set imposé ∈ `{QZD, FTH, KOM}`.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode motor forced ce bloc | `motor_choice_is_forced` | `true` / `false` | Au premier trial du bloc (recopié sur tous les trials du bloc) |
| Set imposé (si forced) | `motor_choice_forced_set` | `QZD` / `FTH` / `KOM` / `null` (si free) | Au premier trial du bloc |

> La colonne existante `motor_choice_active_config` reste le **set actif sur ce trial** (équivalente à `motor_forced_set` quand forced, et au tirage aléatoire en free).

---

## 3. Règles transversales

- **TR1 — Indépendance entre les 4 niveaux** : les quatre flags `*_is_forced` sont pilotés indépendamment dans `BlockConfig`. Un bloc peut avoir n'importe quelle combinaison (ex : meta forced + distal free + proximal forced 50% + motor forced).
- **TR2 — Indépendance free/forced × reliability** : la mécanique forced **n'altère pas** la fiabilité des advice (cohérent avec `Advice reliability` comme variable indépendante distincte dans le GDD). Exception assumée : le distal forced **intègre** un paramètre `distal_forced_optimal_probability` qui joue un rôle voisin de la reliability — articulation à clarifier (cf. Q-FF-12).
- **TR3 — Orthogonalité forced × advice** (hypothèse de travail) : un choix forced n'empêche **pas** l'affichage de l'advice correspondant (ni l'explanation, cf. TR6 de `explanations-short-long`). Le chercheur peut donc créer des conditions "imposé + advice donné" pour évaluer la dissonance entre conseil et choix imposé. Exceptions / cas limites à confirmer (cf. Q-FF-4, Q-FF-5, Q-FF-6).
- **TR4 — Neutralité tutorial** : pendant un bloc `is_tutorial = true` (DEC-014), tous les flags `*_is_forced` sont ignorés et le mode free est appliqué. Aucune colonne `*_is_forced` n'est envoyée à l'API (cohérent avec DEC-014).
- **TR5 — Édition centralisée via session config panel** : tous les paramètres free/forced (`advisor_forced`, `advisor_forced_value`, `distal_forced`, `distal_forced_optimal_probability`, `proximal_forced_probability`, `motor_forced`, `motor_forced_set`) sont édités par le chercheur depuis le **session config panel** côté dashboard web et chargés au boot via l'API (cohérent avec DEC-009).
- **TR6 — Pas de feedback explicite "imposé" par défaut** : le participant **n'est pas averti textuellement** que son choix est imposé ; seule l'UI (options grisées, cloud unique) le signale visuellement. Décision à confirmer pour ne pas biaiser le sense of agency (cf. Q-FF-8).
- **TR7 — Cohérence avec free/forced × explanations** : l'orthogonalité posée par TR6 de `explanations-short-long` est confirmée ici (les explanations restent affichables quelle que soit la modalité du choix associé).

---

## 4. Matrice de traçabilité

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `meta_choice_is_forced` | AdvisorChoiceScene | Enregistrer si le meta-choice est imposé ce bloc | `true` / `false` |
| `meta_choice_forced_value` | AdvisorChoiceScene | Enregistrer la valeur imposée (si forced) | `none` / `human` / `robot` / `null` |
| `distal_choice_is_forced` | DistalChoiceScene | Enregistrer si le distal-choice est imposé ce bloc | `true` / `false` |
| `distal_choice_forced_value` | DistalChoiceScene | Enregistrer la vallée imposée (si forced) | `A` / `B` / `null` |
| `distal_choice_forced_was_optimal` | DistalChoiceScene | Enregistrer si la vallée imposée est l'optimale | `true` / `false` / `null` |
| `distal_choice_forced_optimal_probability` | DistalChoiceScene | Enregistrer la proba utilisée (audit) | `[0, 1]` / `null` |
| `proximal_choice_is_forced` | ProximalScene | Enregistrer si le proximal-choice est imposé ce trial | `true` / `false` |
| `proximal_choice_forced_value` | ProximalScene | Enregistrer le cloud imposé (si forced) | `left` / `right` / `null` |
| `proximal_choice_forced_probability` | ProximalScene | Enregistrer la proba utilisée (audit) | `[0, 1]` |
| `motor_choice_is_forced` | ProximalScene (motor) | Enregistrer si le motor-choice est imposé ce bloc | `true` / `false` |
| `motor_choice_forced_set` | ProximalScene (motor) | Enregistrer le set imposé (si forced) | `QZD` / `FTH` / `KOM` / `null` |

**Correspondance avec le GDD V1** : aucune des 11 colonnes ci-dessus n'existe dans le CSV V1 actuel. Elles sont **nouvelles**, motivées par les hypothèses H1–H8 du GDD qui requièrent de tracer la modalité free/forced indépendamment du résultat du choix (déjà tracé dans `advisor_type`, `valley_choice`, `proximal_choice_target`, `motor_choice_active_config`, etc.).

---

## 5. Questions ouvertes

### Côté chercheur (haute priorité — bloquent la finalisation de la spec)

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-FF-1 | Quand `advisor_forced = true`, **qui choisit** laquelle des 3 options (none / human / robot) est imposée ? | A: fixée explicitement par le chercheur dans `BlockConfig` / B: tirée aléatoirement par le système au début du bloc (équiprobable ou pondérée) / C: chercheur peut choisir un sous-ensemble parmi lequel tirer | Structure config bloc, complexité panneau session | 🔴 |
| Q-FF-2 | Distal — **granularité du toggle** : est-ce que `distal_forced` est un flag bloc (tout le bloc forced ou tout le bloc free), ou bien y a-t-il aussi une proba `distal_forced_probability` au niveau bloc qui décide trial-by-trial ? | A: flag bloc uniquement (tout bloc-wide) / B: flag + proba trial-wise / C: proba uniquement (pas de flag binaire) | Modèle de config et de pilotage | 🔴 |
| Q-FF-3 | Proximal forced — **comment l'autre cloud est-il neutralisé** visuellement ? | A: l'autre cloud n'est pas instancié (1 seul cloud spawné) / B: l'autre cloud est instancié mais masqué (fog of war permanent dessus) / C: l'autre cloud est visible mais non collectable | Génération de map, mécanique de fin de trial | 🔴 |
| Q-FF-4 | Proximal forced — **quel cloud est imposé** ? | A: toujours l'optimal / B: toujours l'advisé (si advice donné) / C: random équiprobable / D: configurable par bloc via une proba `proximal_forced_optimal_probability` (analogue distal) | Combinatoire expérimentale, complexité config | 🔴 |
| Q-FF-5 | Si meta-choice forced = `none` (no advisor imposé) **OU** si le participant a choisi `no advisor` en mode free → **les advice distal/proximal/motor sont absents par construction**. Dans ce cas, le distal forced (avec sa proba d'optimalité) reste-t-il pertinent ? | A: oui, distal forced reste actif (la vallée est imposée mais sans advisor pour expliquer pourquoi) / B: non, distal forced est ignoré si pas d'advisor / C: distal forced reste actif mais l'optimalité forcée joue un rôle différent à clarifier | Validité scientifique de la condition "imposé sans advisor" | 🔴 |
| Q-FF-6 | Motor forced × motor-advice — **le motor-advice doit-il refléter le set imposé** quand motor forced est ON ? | A: oui, advice toujours cohérent avec le set imposé (sauf si advice unreliable) / B: oui, advice toujours montré, mais peut être unreliable et montrer un autre set / C: motor-advice est désactivé quand motor forced (redondant) | Combinatoire et lisibilité expérimentale | 🔴 |
| Q-FF-7 | Quand un choix est imposé (advisor/distal) — **faut-il une validation explicite** du participant (clic sur l'option imposée) ou la transition est-elle automatique au bout de X secondes ? | A: clic explicite requis / B: auto-validation après délai configurable / C: clic explicite + délai minimum d'exposition à la scène | Sense of agency, contrôle de l'exposition | 🔴 |
| Q-FF-8 | **Feedback explicite "imposé"** — le participant doit-il être averti textuellement que son choix est imposé (ex : bandeau "choix imposé pour ce bloc"), ou seule la sémantique UI (options grisées) doit le signaler ? | A: pas de bandeau, UI seule / B: bandeau systématique / C: bandeau configurable par le chercheur | Validité de la mesure de sense of agency (peut biaiser la réponse) | 🔴 |
| Q-FF-9 | Motor forced — **comment est défini `motor_forced_set`** quand `motor_forced = true` ? | A: fixé explicitement par le chercheur dans `BlockConfig` / B: tiré aléatoirement une fois au début du bloc / C: chercheur choisit un sous-ensemble parmi lequel tirer | Structure config bloc | 🔴 |

### Côté équipe (à arbitrer en interne, peuvent attendre les réponses chercheur)

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-FF-10 | Convention des colonnes "match" existantes en mode forced (`valley_advisor_choice_match`, `target_choice_match_advice`, `motor_choice_match_advice`) — comment les renseigner quand le choix est imposé ? | A: `null` (pas applicable) / B: `true` par défaut (le participant n'a pas pu dévier) / C: valeur calculée comme en mode free (en comparant la valeur imposée à l'advice) | Traçabilité analytique, cohérence du CSV | 🟡 |
| Q-FF-11 | Faut-il une **colonne consolidée par trial** `any_choice_is_forced` (= OR des 4 flags) pour faciliter les analyses ? | A: oui, colonne consolidée + 4 colonnes détaillées / B: non, 4 colonnes suffisent / C: colonne consolidée + 4 booléens fusionnés en une string `meta:0|distal:1|prox:0|motor:1` | Volume du CSV, ergonomie d'analyse | 🟡 |
| Q-FF-12 | Articulation **`distal_forced_optimal_probability` vs `distal_advice_reliability`** — les deux paramètres décrivent une notion proche (proba que la vallée recommandée/imposée soit la "bonne"). Faut-il les unifier conceptuellement dans la doc, ou les garder strictement séparés ? | A: strictement séparés (deux mécaniques distinctes, deux paramètres dans `BlockConfig`) / B: unifiés conceptuellement (le forced est un cas particulier de reliability poussée à 100% d'exposition) / C: documenter la relation sans changer les paramètres | Clarté scientifique de la spec | 🟡 |
| Q-FF-13 | Tutorial — le tutorial doit-il **présenter au participant l'existence du mode forced** (ex : un trial où on lui explique que parfois le choix est imposé) ou rester strictement en mode free ? | A: tutorial 100% free (comme proposé par TR4) / B: tutorial inclut un exemple de chaque mode forced pour familiariser le participant / C: configurable côté chercheur | Préparation du participant, biais d'apprentissage | 🟡 |
