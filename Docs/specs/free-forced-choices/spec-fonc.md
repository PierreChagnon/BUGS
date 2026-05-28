# Spec Fonctionnelle — Free / Forced choices

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date initiale :** 2026-05-20
**Dernière mise à jour :** 2026-05-26 (consolidation post-arbitrage chercheur — Q-FF-1 à Q-FF-11 résolues, Notes 1/2/3 intégrées)
**Statut :** `validé`
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
- [x] **Granularités différenciées** selon le niveau (révisé 2026-05-26, Note 3) :
  - meta, distal → pilotés au **bloc** (toggle déterministe),
  - proximal, motor → pilotés au **trial** par tirage probabiliste (proportion de trials forced configurable par bloc).
- [x] **Configuration depuis le session config panel** (chercheur) — tous les paramètres free/forced sont des champs `BlockConfig` édités via l'interface chercheur (cf. récap des 8 champs ci-dessous).
- [x] **Comportement UI cohérent** quand le choix est imposé : grisage des options non sélectionnables sur les écrans choice, masquage par fog of war permanent du cloud non imposé en proximal forced.
- [x] Articulation entre **forced × advice** (révisée 2026-05-26, Note 1) : l'advice est subordonné au forced (« advisor follows forced », TR3 réécrite). Pas de divergence possible advice/forced sur les trials forced, même en mode unreliable.
- [x] Articulation entre **forced × reliability** (depuis 2026-05-26, Note 1) : sur les trials forced, l'advice **suit** obligatoirement le forced (« advisor follows forced », TR3 réécrite). La reliability ne s'exprime que sur les trials free. L'optimalité des choix imposés est configurable par bloc : `distal_forced_optimal_probability ∈ {0, 1}` et `proximal_forced_optimal_probability ∈ {0, 1}`.
- [x] **Tracé data** dans la table `trial_responses` (DEC-011) avec 14 colonnes dédiées par niveau (`*_is_forced` + valeur forcée + paramètres d'optimalité, cf. §4).

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

- **Arbitrages chercheur (2026-05-26)** — résolvent Q-FF-1 à Q-FF-11 et précisent le modèle. Synthèse :
  - **Note 1 — Advisor follows forced (hard recommendation)** : quand un trial est forced, le choix du système forced **domine et l'advisor suit**. L'advice est subordonné au forced aux 3 niveaux (distal, proximal, motor), strict **même en mode unreliable**. La reliability ne s'exprime que sur les trials free. **Remplace** l'orthogonalité forced × advice posée initialement par TR3.
  - **Note 2 — Equipment failure overlay** : un overlay visuel diégétique « Equipment failure » est affiché à l'écran **uniquement** quand un trial est forced ET que l'advisor est `none` (no advisor choisi ou imposé). Justifie narrativement la contrainte en l'absence d'advisor pour la contextualiser.
  - **Note 3 — Modèle de configuration par bloc** :
    - **meta** : toggle déterministe `advisor_forced` + dropdown explicite `advisor_forced_value ∈ {none, human, robot}`.
    - **distal** : toggle déterministe `distal_forced` + valeur binaire `distal_forced_optimal_probability ∈ {0, 1}` (0 → vallée imposée toujours sous-optimale, 1 → toujours optimale).
    - **proximal** : **proba trial-wise** `proximal_forced_probability ∈ [0, 1]` (proportion des trials forced dans le bloc, tirage Bernoulli par trial) + valeur binaire `proximal_forced_optimal_probability ∈ {0, 1}` (0 → cloud imposé toujours sous-optimal, 1 → toujours optimal). Mix free/forced autorisé dans un bloc.
    - **motor** : **proba trial-wise** `motor_forced_probability ∈ [0, 1]` (proportion des trials forced dans le bloc) + `motor_forced_set ∈ {QZD, FTH, KOM}` fixé par le chercheur. Le set imposé s'applique **uniquement aux trials forced** ; les trials free du même bloc conservent le tirage aléatoire par trial (comportement actuel). Mix free/forced autorisé dans un bloc.

- **Récap config `BlockConfig` (8 champs) :**

  | Niveau | Champs |
  |:---|:---|
  | meta | `advisor_forced: bool`, `advisor_forced_value ∈ {none, human, robot}` |
  | distal | `distal_forced: bool`, `distal_forced_optimal_probability ∈ [0, 1]` |
  | proximal | `proximal_forced_probability ∈ [0, 1]`, `proximal_forced_optimal_probability ∈ [0, 1]` |
  | motor | `motor_forced_probability ∈ [0, 1]`, `motor_forced_set ∈ {QZD, FTH, KOM}` |

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
  - **Validation par clic explicite obligatoire** sur l'option imposée pour passer à la suite (Q-FF-7 résolu : option A, 2026-05-26). Pas d'auto-validation par timer.
  - **Cas spécial `advisor_forced_value = none`** : si le trial du bloc est forced (selon les flags distal/proximal/motor du bloc), l'overlay diégétique « **Equipment failure** » est affiché à l'écran à l'entrée du trial concerné (cf. TR6 réécrite, Q-FF-8 résolu).

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Clic sur l'option imposée | Sélection acceptée, transition vers DistalChoiceScene | — |
| Clic sur une option grisée | Aucun effet (bouton non interactif) | Pas de feedback "tentative refusée" requis. |
| Tentative de skip / navigation arrière | Bloqué par le flow standard | — |

#### Règles métier

- **R1** : `advisor_forced = true` **exige** un `advisor_forced_value ∈ {none, human, robot}` non vide dans la même `BlockConfig`. Le chercheur sélectionne explicitement la valeur via un dropdown dans le session config panel (Q-FF-1 résolu : option A, 2026-05-26). Si `advisor_forced = true` et `advisor_forced_value` vide → erreur de validation côté config (config invalide, refus de chargement).
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
  - L'autre est seule sélectionnable, **validation par clic explicite obligatoire** (Q-FF-7 résolu).
  - **La vallée imposée est définie par `distal_forced_optimal_probability ∈ {0, 1}`** (Q-FF-2 / Note 3 résolus : valeur binaire) :
    - si `= 1` → la vallée imposée est l'**optimale** (celle qui rendra le plus de green bugs en moyenne sur le bloc),
    - si `= 0` → la vallée imposée est la **sous-optimale**.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Clic sur la vallée imposée | Sélection acceptée, transition vers ProximalScene (premier trial) | — |
| Clic sur la vallée grisée | Aucun effet | Pas de feedback "tentative refusée" requis. |

#### Règles métier

- **R5** : `distal_forced_optimal_probability ∈ {0, 1}` est lu depuis `BlockConfig` (valeur binaire). Lecture effectuée une seule fois à l'entrée de scène. La sémantique est déterministe : 1 = optimal, 0 = sous-optimal.
- **R6** : la notion de "vallée optimale" est dérivée des paramètres de génération de chaque vallée (`MapGenConfig` de `valley_a` vs `valley_b`) — la vallée avec l'espérance de green bugs la plus haute. Si les deux vallées ont la même espérance → tie-breaker à définir (à traiter côté spec tech).
- **R7** : `State.valley_choice` est écrit avec la vallée imposée à la validation de la scène (après clic explicite).
- **R8** : pendant le bloc tutorial (`is_tutorial = true`), `distal_forced` est ignoré (cf. TR4).
- **R9** : si `meta_choice_forced_value = none` (forced "no advisor") **OU** si le participant a choisi `no advisor` en mode free, le distal advice n'est pas affiché (état existant). Le mode forced sur le distal-choice **reste actif** indépendamment de la présence d'un advisor (Q-FF-5 résolu : option A, 2026-05-26). Dans ce cas, l'overlay « Equipment failure » est affiché (cf. TR6).

#### Modes

- **free** vs **forced** (block-wise).
- Quand forced : vallée imposée selon `distal_forced_optimal_probability ∈ {0, 1}` (résultat ∈ `{optimal, suboptimal}` × `{A, B}`).

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode distal forced ce bloc | `distal_choice_is_forced` | `true` / `false` | À l'entrée de la scène |
| Vallée imposée (si forced) | `distal_choice_forced_value` | `A` / `B` / `null` (si free) | À l'entrée de la scène |
| Optimalité de la vallée imposée (si forced) | `distal_choice_forced_was_optimal` | `true` / `false` / `null` (si free) | À l'entrée de la scène |
| Paramètre d'optimalité utilisé (audit) | `distal_choice_forced_optimal_probability` | `0` / `1` / `null` (si free) | À l'entrée de la scène |

---

### 2.3 Proximal-choice forced (ProximalScene)

**Rôle :** Imposer au participant le cloud à atteindre sur un trial donné (un seul cloud affiché, pas de choix). Permet d'étudier l'effet de subir le choix de cible × le niveau d'advice donné, trial par trial (H4, H5, H6).
**Quand :** À chaque trial, à l'entrée de la `ProximalScene`. Décision tirée trial-by-trial.

#### Ce qui est affiché

- Si **mode = free** (issue du tirage) : les **deux clouds** sont affichés normalement, le participant choisit en navigant vers l'un ou l'autre.
- Si **mode = forced** (issue du tirage) :
  - Les deux clouds restent **instanciés** dans la map (génération inchangée),
  - Le cloud non imposé est **masqué visuellement** par un fog of war permanent (Q-FF-3 résolu : option B, 2026-05-26). Il n'est pas révélable par les mécaniques habituelles du fog (le brouillard sur cette zone est verrouillé), et sa collecte reste désactivée tant que le trial est forced.
  - Le cloud imposé est visible et collectable comme en mode free.
  - Le participant a une seule cible visuellement perceptible et navigue vers elle.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Navigation vers le cloud imposé | Comportement standard, fin de trial à l'atteinte | — |
| Tentative de partir dans la direction opposée | Comportement standard du forest screen (le joueur peut s'éloigner mais ne peut ni voir ni collecter l'autre cloud, masqué par fog of war permanent) | — |

#### Règles métier

- **R10** : `proximal_forced_probability ∈ [0, 1]` est lu depuis `BlockConfig` (proportion attendue de trials forced dans le bloc). Tirage Bernoulli de paramètre `p` effectué **au début de chaque trial** indépendamment des autres trials. Mix free/forced dans un même bloc autorisé. Les valeurs 0 et 1 sont autorisées (bloc entièrement free ou entièrement forced).
- **R11** : quand le trial est forced, **quel cloud est imposé** est défini par `proximal_forced_optimal_probability ∈ {0, 1}` (Q-FF-4 résolu : option D, 2026-05-26) :
  - si `= 1` → cloud optimal toujours imposé (celui avec la meilleure espérance de green bugs),
  - si `= 0` → cloud sous-optimal toujours imposé.
- **R12** : pendant le bloc tutorial, `proximal_forced_probability` est ignoré (mode free systématique, cf. TR4).
- **R13** : `target_choice_match_advice` (colonne existante) est **calculée comme en mode free** même quand le trial est forced (Q-FF-10 résolu : option C, 2026-05-26) — comparaison entre la valeur imposée et l'advice donné. Note : avec Note 1 (advisor follows forced), cette colonne sera mécaniquement `true` sur les trials forced où un advice est présent.
- **R14** : interaction avec le proximal-advice — si trial forced ET advice présent, le proximal-advice est **toujours affiché** mais son contenu **suit obligatoirement le forced** (Note 1 / TR3 réécrite : « advisor follows forced », strict même en unreliable). Pas de divergence possible entre advice et cloud imposé sur les trials forced. La reliability ne s'exprime que sur les trials free du bloc.
- **R15** : la notion de "cloud optimal" est dérivée des paramètres de génération des deux clouds dans la map (espérance de green bugs). Tie-breaker à définir côté spec tech si les deux clouds ont la même espérance.
- **R16** : **cas `advisor_forced_value = none` (ou no advisor en mode free)** + trial forced : l'overlay « **Equipment failure** » est affiché (cf. TR6 réécrite). Le proximal-advice n'est pas affiché (cohérent avec R1 du chantier explanations : pas d'advice sans advisor).

#### Modes

- **free** vs **forced** (tirage trial-wise selon `proximal_forced_probability`).
- Quand forced : cloud imposé ∈ `{left, right}` selon `proximal_forced_optimal_probability ∈ {0, 1}`.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode proximal forced ce trial | `proximal_choice_is_forced` | `true` / `false` | Au début du trial |
| Cloud imposé (si forced) | `proximal_choice_forced_value` | `left` / `right` / `null` (si free) | Au début du trial |
| Optimalité du cloud imposé (si forced) | `proximal_choice_forced_was_optimal` | `true` / `false` / `null` (si free) | Au début du trial |
| Proportion forced du bloc (audit) | `proximal_choice_forced_probability` | `[0, 1]` | Au début du trial (recopie config bloc) |
| Paramètre d'optimalité utilisé (audit) | `proximal_choice_forced_optimal_probability` | `0` / `1` / `null` (si free) | Au début du trial |

---

### 2.4 Motor-choice forced (ProximalScene — Forest screen)

**Rôle :** Imposer un set de touches fixé sur certains trials d'un bloc (selon une proportion configurée), en laissant les autres trials utiliser le tirage aléatoire par trial. Permet d'isoler l'effet "apprendre/découvrir les touches" de l'effet "exécuter un set stable" sur le coût moteur, avec possibilité de mixer les deux conditions dans le même bloc.
**Quand :** Au début de chaque trial, le système tire si ce trial est forced ou free. Si forced, le set imposé est utilisé ; sinon, tirage aléatoire standard.

**Évolution Note 3 (2026-05-26) :** le motor forced est passé d'un toggle block-wise à un mécanisme **trial-wise** via une proportion `motor_forced_probability ∈ [0, 1]`, en symétrie avec le proximal forced.

#### Ce qui est affiché

- Si **trial = free** (issue du tirage) : le set actif est **tiré aléatoirement parmi `{QZD, FTH, KOM}`** (comportement actuel inchangé). Le participant doit découvrir le set actif ou se reposer sur le motor-advice.
- Si **trial = forced** (issue du tirage) : le set actif est `motor_forced_set ∈ {QZD, FTH, KOM}` (fixé par le chercheur dans `BlockConfig`).

L'UI ne change pas entre free et forced (le set actif n'est pas affiché de base ; seul le motor-advice peut le révéler).

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Appui sur une touche du set actif (free ou forced) | Mouvement standard | — |
| Appui sur une touche hors set actif | Aucun mouvement (comportement existant) | Aucune pénalité explicite (hors scope motor advice) |

#### Règles métier

- **R17** : `motor_forced_probability ∈ [0, 1]` est lu depuis `BlockConfig` (proportion attendue de trials forced dans le bloc). Tirage Bernoulli de paramètre `p` effectué **au début de chaque trial**. Mix free/forced autorisé. Valeurs 0 et 1 autorisées (bloc entièrement free ou entièrement forced).
- **R18** : `motor_forced_set ∈ {QZD, FTH, KOM}` est fixé par le chercheur via dropdown dans le session config panel (Q-FF-9 résolu : option A, 2026-05-26). Le set s'applique **uniquement aux trials forced**. Sur les trials free du même bloc, le tirage aléatoire par trial reste actif. Si `motor_forced_probability > 0` et `motor_forced_set` non spécifié → erreur de validation côté config.
- **R19** : pendant le bloc tutorial, `motor_forced_probability` est ignoré (tirage par trial systématique, cf. TR4).
- **R20** : interaction avec le motor-advice — sur les trials forced, le motor-advice (s'il est affiché) **suit obligatoirement le set imposé** (Note 1 / TR3 réécrite, strict même en unreliable). Sur les trials free, le motor-advice suit ses propres règles de visibilité/fiabilité. La reliability ne s'exprime que sur les trials free.

#### Modes

- **free** vs **forced** (tirage trial-wise selon `motor_forced_probability`).
- Quand forced : set imposé = `motor_forced_set`.
- Quand free : set tiré aléatoirement par trial parmi `{QZD, FTH, KOM}`.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode motor forced ce trial | `motor_choice_is_forced` | `true` / `false` | Au début du trial |
| Set imposé (si forced) | `motor_choice_forced_set` | `QZD` / `FTH` / `KOM` / `null` (si free) | Au début du trial |
| Proportion forced du bloc (audit) | `motor_choice_forced_probability` | `[0, 1]` | Au début du trial (recopie config bloc) |

> La colonne existante `motor_choice_active_config` reste le **set actif sur ce trial** (égale à `motor_forced_set` quand forced, et au tirage aléatoire par trial en free).

---

## 3. Règles transversales

- **TR1 — Indépendance entre les 4 niveaux** : les quatre paramètres de forçage (`advisor_forced`, `distal_forced`, `proximal_forced_probability`, `motor_forced_probability`) sont pilotés indépendamment dans `BlockConfig`. Un bloc peut avoir n'importe quelle combinaison (ex : meta forced + distal free + proximal forced 50% + motor forced 30%).
- **TR2 — Indépendance free/forced × reliability** : la mécanique forced **n'altère pas** la fiabilité de l'advisor en tant que variable indépendante. **Précision Note 1** : la reliability ne s'exprime **que sur les trials free**. Sur les trials forced, l'advice suit le forced (TR3 ci-dessous) et la reliability n'a pas d'effet observable. Le paramètre `distal_forced_optimal_probability` (et son équivalent proximal) joue un rôle distinct de la reliability — articulation conceptuelle à documenter (cf. Q-FF-12, ouverte).
- **TR3 — Advisor FOLLOWS forced (hard recommendation)** : ⚠️ règle **réécrite 2026-05-26** suite à Note 1. Quand un trial est forced (à n'importe quel niveau distal/proximal/motor), l'advice correspondant **est subordonné au forced** : son contenu est obligatoirement aligné sur le choix imposé par le système. La règle est **stricte aux 3 niveaux** (distal, proximal, motor) et **stricte même en mode unreliable** : un advisor unreliable ne peut **pas** mentir contre un forced. L'orthogonalité forced × advice qui était posée initialement (permettant la dissonance) est abandonnée.
- **TR4 — Neutralité tutorial** : pendant un bloc `is_tutorial = true` (DEC-014), tous les paramètres de forçage sont ignorés et le mode free est appliqué (tirage aléatoire des sets motor, libre choix advisor/distal, libre choix de cloud proximal). Aucune colonne `*_is_forced` n'est envoyée à l'API (cohérent avec DEC-014).
- **TR5 — Édition centralisée via session config panel** : tous les paramètres free/forced (`advisor_forced`, `advisor_forced_value`, `distal_forced`, `distal_forced_optimal_probability`, `proximal_forced_probability`, `proximal_forced_optimal_probability`, `motor_forced_probability`, `motor_forced_set`) sont édités par le chercheur depuis le **session config panel** côté dashboard web et chargés au boot via l'API (cohérent avec DEC-009).
- **TR6 — Pas de feedback textuel "imposé" par défaut, exception "Equipment failure"** : ⚠️ règle **réécrite 2026-05-26** suite à Note 2 + Q-FF-8. Par défaut, le participant **n'est pas averti textuellement** que son choix est imposé ; seule la sémantique UI (options grisées sur les écrans choice, fog of war permanent sur le cloud non imposé) le signale visuellement. **Exception unique** : quand un trial est forced (à n'importe quel niveau) ET que `advisor_forced_value = none` (ou que le participant a choisi `no advisor` en mode free), un overlay diégétique « **Equipment failure** » est affiché à l'écran. Cet overlay justifie narrativement la contrainte en l'absence d'advisor pour la contextualiser.
- **TR7 — Cohérence avec free/forced × explanations** : les explanations restent affichables selon leurs propres règles (chantier `explanations-short-long`) quelle que soit la modalité du choix associé. Quand l'advice est subordonné au forced (TR3), l'explanation qui l'accompagne reste cohérente avec le contenu de l'advice (donc avec le forced).

---

## 4. Matrice de traçabilité

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `meta_choice_is_forced` | AdvisorChoiceScene | Enregistrer si le meta-choice est imposé ce bloc | `true` / `false` |
| `meta_choice_forced_value` | AdvisorChoiceScene | Enregistrer la valeur imposée (si forced) | `none` / `human` / `robot` / `null` |
| `distal_choice_is_forced` | DistalChoiceScene | Enregistrer si le distal-choice est imposé ce bloc | `true` / `false` |
| `distal_choice_forced_value` | DistalChoiceScene | Enregistrer la vallée imposée (si forced) | `A` / `B` / `null` |
| `distal_choice_forced_was_optimal` | DistalChoiceScene | Enregistrer si la vallée imposée est l'optimale | `true` / `false` / `null` |
| `distal_choice_forced_optimal_probability` | DistalChoiceScene | Enregistrer le paramètre d'optimalité utilisé (audit) | `0` / `1` / `null` |
| `proximal_choice_is_forced` | ProximalScene | Enregistrer si le proximal-choice est imposé ce trial | `true` / `false` |
| `proximal_choice_forced_value` | ProximalScene | Enregistrer le cloud imposé (si forced) | `left` / `right` / `null` |
| `proximal_choice_forced_was_optimal` | ProximalScene | Enregistrer si le cloud imposé est l'optimal | `true` / `false` / `null` |
| `proximal_choice_forced_probability` | ProximalScene | Enregistrer la proportion forced du bloc (audit) | `[0, 1]` |
| `proximal_choice_forced_optimal_probability` | ProximalScene | Enregistrer le paramètre d'optimalité utilisé (audit) | `0` / `1` / `null` |
| `motor_choice_is_forced` | ProximalScene (motor) | Enregistrer si le motor-choice est imposé ce trial | `true` / `false` |
| `motor_choice_forced_set` | ProximalScene (motor) | Enregistrer le set imposé (si forced) | `QZD` / `FTH` / `KOM` / `null` |
| `motor_choice_forced_probability` | ProximalScene (motor) | Enregistrer la proportion forced du bloc (audit) | `[0, 1]` |

**Correspondance avec le GDD V1** : aucune des **14 nouvelles colonnes** ci-dessus n'existe dans le CSV V1 actuel. Elles sont **nouvelles**, motivées par les hypothèses H1–H8 du GDD qui requièrent de tracer la modalité free/forced indépendamment du résultat du choix (déjà tracé dans `advisor_type`, `valley_choice`, `proximal_choice_target`, `motor_choice_active_config`, etc.).

**Granularité** : `meta_choice_*` et `distal_choice_*` sont block-wise (mêmes valeurs sur tous les trials du bloc). `proximal_choice_*` et `motor_choice_*` sont trial-wise (peuvent varier d'un trial à l'autre dans un même bloc). Les colonnes `proximal_choice_forced_probability` et `motor_choice_forced_probability` sont techniquement block-wise (paramètres config) mais recopiées sur chaque trial pour audit.

**Colonnes `*_match_advice`** (existantes : `valley_advisor_choice_match`, `target_choice_match_advice`, `motor_choice_match_advice`) : calculées comme en mode free (Q-FF-10 résolu : option C, 2026-05-26). Sur les trials forced, ces colonnes seront mécaniquement `true` quand l'advice est présent (conséquence directe de TR3 « advisor follows forced »).

**Pas de colonne consolidée `any_choice_is_forced`** (Q-FF-11 résolu : option B, 2026-05-26) — le OR éventuel se fait côté analyse.

---

## 5. Questions ouvertes

### Côté chercheur (résolues 2026-05-26)

| # | Question | Décision retenue |
|:---|:---|:---|
| Q-FF-1 | Advisor forced : qui choisit l'option imposée ? | ✅ **Option A** — Le chercheur sélectionne explicitement l'advisor imposé via dropdown `{none, human, robot}` dans le session config panel, par bloc. Cf. R1 §2.1. |
| Q-FF-2 | Distal forced : granularité du toggle | ✅ **Option A** — Flag binaire au bloc (`distal_forced`). Pas de proba trial-wise (un bloc n'a qu'un distal-choice). Paramètre séparé `distal_forced_optimal_probability ∈ {0, 1}` pour l'optimalité (Note 3). |
| Q-FF-3 | Proximal forced : neutralisation visuelle de l'autre cloud | ✅ **Option B** — Cloud non imposé instancié mais masqué par fog of war permanent. Cf. §2.3 et R16. |
| Q-FF-4 | Proximal forced : quel cloud est imposé | ✅ **Option D** — Paramètre `proximal_forced_optimal_probability ∈ {0, 1}` (Note 3 : valeur binaire) en symétrie avec distal. Cf. R11. |
| Q-FF-5 | Distal forced sans advisor : pertinent ? | ✅ **Option A** — Distal forced ET proximal forced restent actifs même sans advisor. Overlay « Equipment failure » affiché dans ce cas (TR6). Cf. R9 §2.2. |
| Q-FF-6 | Motor forced × motor-advice : cohérence | ✅ **Option A étendue par Note 1** — Motor-advice cohérent avec le set imposé sur les trials forced, strict même en unreliable. La reliability ne s'exprime que sur les trials free. Cf. R20 §2.4 et TR3. |
| Q-FF-7 | Validation choix imposés (advisor/distal) | ✅ **Option A** — Clic explicite requis sur l'option imposée. Pas d'auto-validation par timer. Cf. §2.1 et §2.2. |
| Q-FF-8 | Feedback explicite "imposé" | ✅ **Hybride** — Pas de bandeau textuel générique. **Exception** : si trial forced + advisor=none → overlay diégétique « Equipment failure ». Cf. TR6 réécrite. |
| Q-FF-9 | Motor forced : comment définir le set imposé | ✅ **Option A étendue par Note 3** — `motor_forced_set` fixé par chercheur via dropdown `{QZD, FTH, KOM}`, appliqué uniquement aux trials forced du bloc. Motor passe block-wise → trial-wise via `motor_forced_probability`. Cf. R18 §2.4. |

### Côté équipe (résolues 2026-05-26)

| # | Question | Décision retenue |
|:---|:---|:---|
| Q-FF-10 | Colonnes `*_match_advice` en mode forced | ✅ **Option C** — Calculées comme en mode free (comparaison entre la valeur imposée et l'advice). Conséquence avec TR3 : trivialement `true` sur trials forced avec advice (l'advice suit le forced). Cf. R13 §2.3 et matrice §4. |
| Q-FF-11 | Colonne consolidée `any_choice_is_forced` | ✅ **Option B** — Non, les 4 colonnes `*_is_forced` suffisent. OR à faire côté analyse. Cf. matrice §4. |

### Encore ouvertes

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-FF-12 | Articulation **`distal_forced_optimal_probability` vs `distal_advice_reliability`** — les deux paramètres décrivent une notion proche. Faut-il les unifier conceptuellement dans la doc, ou les garder strictement séparés ? | A: strictement séparés / B: unifiés conceptuellement (forced = cas particulier de reliability poussée à 100% d'exposition) / C: documenter la relation sans changer les paramètres | Clarté scientifique de la spec | 🟡 |
| Q-FF-13 | Tutorial — doit-il **présenter au participant l'existence du mode forced** ou rester strictement en mode free ? | A: tutorial 100% free (TR4 actuelle) / B: tutorial inclut un exemple de chaque mode forced pour familiariser le participant / C: configurable côté chercheur | Préparation du participant, biais d'apprentissage | 🟡 |
