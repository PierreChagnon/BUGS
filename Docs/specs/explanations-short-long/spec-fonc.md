# Spec Fonctionnelle — Explanations short/long

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** 2026-05-26 *(révisée — consolidation post-échange chercheurs)*
**Statut :** `draft`
**Chantier :** Explanations short/long
**Spec tech associée :** `Docs/specs/explanations-short-long/spec-tech.md` (à produire)

---

## 1. Contexte et scope

### Objectif

Permettre à chaque advice (distal, proximal, motor) d'être accompagné d'une **explication textuelle** affichée au participant. L'explication existe en **deux variantes éditoriales** (`short` et `long`), éditées par les chercheurs depuis le panneau de configuration de session. Deux dimensions orthogonales pilotent l'affichage : un **`display_mode`** (`forced` / `opt-in` / `none`) et un **`content_variant`** (`short` / `long`), tous deux configurés au niveau bloc. La feature est la brique nécessaire à l'hypothèse **H8** du GDD : *"Explanations with reference to higher levels of abstraction leads to higher levels of acceptability and human-likeness."*

### Périmètre IN

- [x] Affichage d'une explanation accompagnant un advice **distal**, **proximal** ou **motor**, conditionné à la précondition d'affichage (cf. règle R1).
- [x] Deux variantes textuelles **short** et **long** stockées et éditées indépendamment pour chaque advice.
- [x] Deux dimensions orthogonales par advice :
  - **`display_mode`** ∈ `forced` / `opt-in` / `forced/opt-in` / `none` — pilote comment et si l'explanation est présentée
  - **`content_variant`** ∈ `short` / `long` / `short/long` — pilote quelle variante éditoriale est utilisée quand affichée
- [x] Valeurs **mixtes** sur les deux dimensions : `forced/opt-in` et `short/long` ne fixent plus le réglage pour tout le bloc, le jeu tire la valeur réelle à chaque affichage (cf. règle R9).
- [x] Pilotage **blockwise** des deux dimensions via la configuration de session (`SessionConfig` / `BlockConfig`).
- [x] Édition des contenus short et long par les chercheurs dans le **session config panel**, avec **2 corpora distincts par bloc** indexés par `advisor_type` (`human-bot`, `bot-bot`) → 12 textes par bloc (3 advice × 2 variants × 2 advisor types).
- [x] Tracking du `display_mode=opt-in` : enregistrement du clic du participant sur le bouton « show explanation » et de la durée d'affichage de l'explication.
- [x] Tracé des données dans la table `trial_responses` (table plate dénormalisée, DEC-011) avec colonnes dédiées par advice (5 colonnes par advice, 15 au total).
- [x] Indépendance vis-à-vis de la fiabilité (`reliability`) de l'advice : un advice non fiable peut porter une explanation.

### Périmètre OUT

| Exclu | Raison | Reporté à |
|:---|:---|:---|
| Choix d'architecture de stockage (BDD vs fichier vs ScriptableObject) | Décision technique | Spec tech `explanations-short-long/spec-tech.md` |
| Design UI graphique (typographie, mise en page, animations) | Hors analyse fonctionnelle | Chantier UI Advisors |
| Mécanique `free/forced` sur les 4 choix | Chantier distinct | Spec free/forced (rattachée à Q-005) |
| Contenu littéral des textes explicatifs | À fournir par le chercheur | Validation chercheur, hors spec |
| Pattern de fréquence d'apparition de l'advice lui-même | Chantier reliability | Spec reliability (rattachée à Q-010) |
| Tutoriel : pas d'explanation pendant le bloc tutorial (cf. DEC-014) | Hors scope expérimental | — |

### Décisions déjà prises

- **DEC-001** — Le meta-choice est une fois par bloc.
- **DEC-008** — Le questionnaire post-bloc est in-game.
- **DEC-011** — Modèle de données = table plate `trial_responses`.
- **DEC-013** — Questionnaire envoyé par PATCH après le dernier trial.
- **DEC-014** — Tutorial modélisé comme `BlockConfig` avec flag `is_tutorial` (pas d'envoi API).
- **DEC-017** — Cadrage initial du présent chantier : périmètre D+P+M, deux textes alternatifs, précondition advisor choisi + advice donné, édition via session config panel. Résout Q-003 (motor advice avec explanation) et Q-006 (modèle d'édition).
- **DEC-018** — Consolidation post-échange chercheurs (Florian / Valerian / Mark). Introduction de `display_mode` orthogonal à `content_variant`, granularité blockwise, cross-level interdit, jusqu'à 3 explanations simultanées (layout L/R), corpus indexé par advisor type, format CSV enum + id, tracking opt-in obligatoire, contrainte UI no-overlap avec la map. Principe directeur : « keep it simple, adapt later ». Résout Q-EXP-1, 2, 3, 4, 5, 7, 8, 9.

### Dépendances

- **Requiert :** Advisors fonctionnels (DistalChoiceScene, ProximalScene, MotorAdvice forest UI), pipeline `trial_responses` (DEC-011), configuration de session chargée par URL + API (DEC-009), connaissance de l'`advisor_type` actif au moment d'afficher une explanation.
- **Est requis par :** validité de l'hypothèse H8, finalisation du chantier Motor Advice (Q-003), spec free/forced (les conditions `explanation × forced` font partie de la combinatoire GDD).
- **Doit rester cohérent avec :** `BlockConfig` (DEC-014), envoi PATCH questionnaire (DEC-013), neutralité du flag `is_tutorial`.
- **Dépendances UI :** non-overlap avec la map (TR10), layout L/R pour multi-advices simultanés (TR9). Détails à régler en spec tech et en UI test.

---

## 2. Catalogue des comportements

### 2.1 Distal Advice + Explanation (DistalChoiceScene)

**Rôle :** Accompagner le distal-advice (recommandation sur la zone à explorer) par une explication textuelle qui justifie ce conseil au niveau de l'intention distale.
**Quand :** Sur l'écran distal, en début de bloc lorsque le participant doit choisir la vallée à explorer. Affichée uniquement si la précondition R1 est satisfaite.

#### Ce qui est affiché

L'affichage est piloté par **2 dimensions blockwise orthogonales** :

| `display_mode` | `content_variant` | Résultat |
|:---|:---|:---|
| `none` | (ignoré) | Aucune explanation affichée. L'advice (s'il est donné) reste visible seul. |
| `forced` | `short` | Texte court accompagne l'advice d'office, sans interaction. |
| `forced` | `long` | Texte long accompagne l'advice d'office, sans interaction. |
| `opt-in` | `short` | Bouton « show explanation » ; au clic, le texte court s'affiche. |
| `opt-in` | `long` | Bouton « show explanation » ; au clic, le texte long s'affiche. |

Le texte affiché est sélectionné dans le corpus du bloc indexé par l'`advisor_type` du trial (cf. TR13).

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Le participant lit l'explanation (mode `forced`) | Pas d'action requise. L'explanation reste visible (cf. R7) | — |
| Le participant clique sur « show explanation » (mode `opt-in`) | Le texte s'affiche. Clic et durée d'affichage loggés. | Si pas de clic → `clicked = false`, `display_duration_ms = 0` |
| Le participant cache l'explanation | L'explanation est masquée (modalités UI : à régler en UI test, cf. Q-EXP-6) | — |
| Le participant fait son distal-choice | Le choix est enregistré normalement ; la lecture ou non de l'explanation n'influe pas sur la validité du choix | — |

#### Règles métier

- **R1 (Précondition d'affichage)** : l'explanation n'est affichée que si **(a)** un advisor a été choisi par le participant (meta-choice ≠ "no advisor") **ET (b)** un advice distal est effectivement donné sur ce bloc. Si l'une des deux conditions manque, `display_mode = none` forcé.
- **R2** : `display_mode` et `content_variant` sont lus depuis la config du bloc (granularité **blockwise**, cf. TR7).
- **R9 (Valeurs mixtes)** : `display_mode = forced/opt-in` et `content_variant = short/long` délèguent le choix à un tirage effectué **à chaque affichage**, pas une fois pour le bloc. Les probabilités `display_mode_forced_probability` (probabilité de tirer `forced`, sinon `opt-in`) et `content_variant_long_probability` (probabilité de tirer `long`, sinon `short`) sont lues uniquement quand la dimension correspondante vaut sa valeur mixte. Le tirage est déterministe (dérivé du seed du trial pour proximal/motor, du seed du bloc pour distal) afin que la session reste rejouable. Il intervient **après** `display_probability`, qui reste seul maître de l'apparition de l'explanation proximale ou motor sur le trial. Les valeurs mixtes n'existent qu'en config : les colonnes `trial_responses` enregistrent toujours la valeur tirée (`forced` / `opt-in`, `short` / `long`).
- **R3** : l'explanation est **indépendante de la fiabilité** de l'advice. Un advice non fiable peut être accompagné d'une explanation.
- **R4** : pendant le bloc tutorial (`is_tutorial = true`, DEC-014), aucune explanation n'est affichée et aucune ligne n'est envoyée à l'API.
- **R7 (Tracking opt-in)** : quand `display_mode = opt-in`, le système enregistre **(a)** si le participant a cliqué sur « show explanation » et **(b)** la durée d'affichage de l'explication. Quand `display_mode ≠ opt-in`, les colonnes de tracking sont à `null`.
- **R8 (Cross-level interdit)** : seule l'explanation **du même niveau** que l'advice peut être affichée. Pas de motor-explanation pour un distal-advice, etc. (cf. TR8).

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode d'affichage de l'explanation distale | `distal_advice_explanation_display_mode` | `forced` / `opt-in` / `none` | À l'affichage de la scène distale |
| Variante éditoriale active | `distal_advice_explanation_content_variant` | `short` / `long` / `null` (si `display_mode = none`) | À l'affichage de la scène distale |
| Id du texte effectivement affiché | `distal_advice_explanation_text_id` | id chercheur / `null` | À l'affichage de la scène distale |
| Clic sur « show explanation » | `distal_advice_explanation_clicked` | `true` / `false` / `null` (si `display_mode ≠ opt-in`) | À la sortie de la scène distale |
| Durée d'affichage de l'explication | `distal_advice_explanation_display_duration_ms` | int (ms) / `null` (si `display_mode ≠ opt-in`) | À la sortie de la scène distale |

---

### 2.2 Proximal Advice + Explanation (ProximalScene)

**Rôle :** Accompagner le proximal-advice (recommandation sur le chemin à emprunter / la cible à viser) par une explication textuelle qui justifie ce conseil au niveau de l'intention proximale.
**Quand :** Sur le forest screen, au début et pendant le trial, conditionné à la précondition R1.

#### Ce qui est affiché

- Mêmes 2 dimensions (`display_mode` × `content_variant`) que §2.1, mêmes valeurs.
- Contenus textuels propres à l'advice proximal, édités par le chercheur dans le session config panel, **indexés par advisor_type** (cf. TR13).
- Quand un proximal-advice et un motor-advice coexistent sur le même trial, leurs explanations respectent le layout **L/R** : motor à gauche, proximal à droite (cf. TR9).

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Le participant lit l'explanation (mode `forced`) | Pas d'action requise. L'explanation reste visible (cf. R7) | — |
| Le participant clique sur « show explanation » (mode `opt-in`) | Le texte s'affiche. Clic et durée loggés. | — |
| Le participant cache l'explanation | L'explanation est masquée sans recouvrir la map (cf. TR10) | — |
| Le participant se déplace sur la grille | Le mouvement reste indépendant de l'explanation | — |

#### Règles métier

- **R1, R2, R3, R4, R7, R8** s'appliquent à l'identique (cf. §2.1).
- **R5 (positionnement écran)** : l'explanation proximale partage le forest screen avec l'advice proximal lui-même (chemin coloré dans le fog of war, cf. specs-light §9) — l'emplacement de l'encart doit **ne pas recouvrir la map** (TR10) ni masquer le chemin/les nuages. Détails UI à régler en UI test (cf. Q-EXP-6).

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode d'affichage de l'explanation proximale | `proximal_advice_explanation_display_mode` | `forced` / `opt-in` / `none` | Au début du trial |
| Variante éditoriale active | `proximal_advice_explanation_content_variant` | `short` / `long` / `null` | Au début du trial |
| Id du texte affiché | `proximal_advice_explanation_text_id` | id / `null` | Au début du trial |
| Clic sur « show explanation » | `proximal_advice_explanation_clicked` | `true` / `false` / `null` | À la fin du trial |
| Durée d'affichage de l'explication | `proximal_advice_explanation_display_duration_ms` | int (ms) / `null` | À la fin du trial |

---

### 2.3 Motor Advice + Explanation (ProximalScene / Forest screen)

**Rôle :** Accompagner le motor-advice (indication du set de touches actif, cf. spec MotorAdvice) par une explication textuelle qui justifie ce conseil au niveau de l'intention motrice.
**Quand :** Sur le forest screen, en même temps que l'affichage du set de touches (cf. spec MotorAdvice §2.1).

#### Ce qui est affiché

- Mêmes 2 dimensions (`display_mode` × `content_variant`) que §2.1, mêmes valeurs.
- Le motor-advice lui-même reste l'affichage du set complet de touches (cf. spec MotorAdvice). L'explanation, si présente, vient justifier ce set ou expliquer le mécanisme.
- Quand un proximal-advice et un motor-advice coexistent sur le même trial, la motor-explanation est affichée à **gauche**, la proximal-explanation à **droite** (cf. TR9).

**Note** : cette section **résout Q-003** (le motor advice doit-il inclure une explanation ?). Réponse : **oui**, au même titre que distal et proximal.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Le participant lit l'explanation (mode `forced`) | Pas d'action requise. L'explanation reste visible (cf. R7) | — |
| Le participant clique sur « show explanation » (mode `opt-in`) | Le texte s'affiche. Clic et durée loggés. | — |
| Le participant appuie sur une touche | Comportement standard du motor-advice (cf. MotorAdvice spec) | L'explanation n'influe pas sur le mapping touches |

#### Règles métier

- **R1, R2, R3, R4, R7, R8** s'appliquent à l'identique.
- **R6 (cohérence avec MotorAdvice)** : l'explanation motor n'est affichée que si l'advice motor lui-même est affiché (probabilité d'apparition gérée par la spec MotorAdvice). Si motor advice = caché, `display_mode = none` forcé.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode d'affichage de l'explanation motor | `motor_advice_explanation_display_mode` | `forced` / `opt-in` / `none` | Au début du trial |
| Variante éditoriale active | `motor_advice_explanation_content_variant` | `short` / `long` / `null` | Au début du trial |
| Id du texte affiché | `motor_advice_explanation_text_id` | id / `null` | Au début du trial |
| Clic sur « show explanation » | `motor_advice_explanation_clicked` | `true` / `false` / `null` | À la fin du trial |
| Durée d'affichage de l'explication | `motor_advice_explanation_display_duration_ms` | int (ms) / `null` | À la fin du trial |

---

## 3. Règles transversales

- **TR1 — Indépendance fiabilité / explanation** : la fiabilité d'un advice ne conditionne pas l'affichage de son explanation. Les deux variables sont orthogonales (cohérent avec les variables indépendantes du GDD : `Explanation/No explanations` listée séparément de `Advice reliability`).
- **TR2 — Deux textes alternatifs** : pour chaque advice et chaque advisor_type, le chercheur édite **deux textes indépendants** (un court, un long). Aucune génération automatique d'un texte à partir de l'autre.
- **TR3 — Édition centralisée** : tous les contenus d'explanation sont édités par les chercheurs depuis le **session config panel** (DEC-017). Aucun texte n'est hardcodé côté Unity.
- **TR4 — Neutralité tutorial** : pendant un bloc `is_tutorial = true` (DEC-014), aucune explanation n'est affichée. Aucune colonne `*_explanation_*` n'est envoyée à l'API (cohérent avec DEC-014, pas d'envoi API en tuto).
- **TR5 — Indépendance entre advice** : les trois explanations (distal, proximal, motor) sont pilotées indépendamment. Un trial peut avoir distal `display_mode = forced/long`, proximal `display_mode = none`, motor `display_mode = opt-in/short`.
- **TR6 — Cohérence avec free/forced** : l'interaction entre `explanation` et le mécanisme `free/forced` (Q-005 partiellement résolue) doit être confirmée dans la spec free/forced. Hypothèse de travail : les deux paramètres sont orthogonaux.
- **TR7 — Granularité blockwise des deux dimensions** : `display_mode` et `content_variant` sont définis **par bloc** dans `BlockConfig`, pas par trial. Tous les trials d'un même bloc partagent la même configuration explanation. (Q-EXP-1, DEC-018)
- **TR8 — Cross-level interdit (1-to-1)** : une advice d'un niveau donné ne peut être accompagnée que d'une explanation **du même niveau**. Pas de motor-explanation pour un distal-advice, etc. (Q-EXP-4, DEC-018)
- **TR9 — Plusieurs explanations simultanées** : jusqu'à 3 explanations peuvent être affichées simultanément sur un même trial (1 par advice). Quand un motor-advice et un proximal-advice coexistent, leurs explanations respectent un layout **L/R** : motor à gauche, proximal à droite. (Q-EXP-7, DEC-018)
- **TR10 — Contrainte UI no-overlap** : aucune explanation, dans aucun mode d'affichage, ne doit recouvrir la map / le forest screen / les nuages / le chemin. La possibilité de cacher l'explanation et son positionnement précis sont à régler en UI test (Q-EXP-6).
- **TR11 — Tracking opt-in obligatoire** : quand `display_mode = opt-in` est configuré, le tracking (clic sur le bouton + durée d'affichage) est obligatoire et doit alimenter les colonnes `*_explanation_clicked` et `*_explanation_display_duration_ms`. (DEC-018)
- **TR12 — Principe « keep it simple, adapt later »** : sur les points de cadrage encore ambigus à la date de la spec, retenir la version la plus simple, quitte à ré-ouvrir après lancement de l'étude si les premiers résultats le justifient. (DEC-018)
- **TR13 — Corpus indexé par advisor type** : pour chaque bloc, le chercheur édite **2 corpora distincts** (advisor_type = `human-bot`, `bot-bot`), chacun contenant 3 advice × 2 variants = 6 textes. Soit **12 textes par bloc**. Le corpus utilisé sur un trial est sélectionné au runtime selon l'advisor effectivement choisi par le participant. Le `no-advisor` ne reçoit pas de corpus (cohérent avec R1). (Q-EXP-8, DEC-018)

---

## 4. Matrice de traçabilité

**15 colonnes** au total (5 par advice × 3 advices). Renommages depuis la version initiale de la spec : `*_advice_explanation_mode` → `*_advice_explanation_content_variant`. Ajout des colonnes `*_display_mode`, `*_clicked`, `*_display_duration_ms`.

### Distal

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `distal_advice_explanation_display_mode` | DistalChoiceScene | Mode d'affichage du bloc | `forced` / `opt-in` / `none` |
| `distal_advice_explanation_content_variant` | DistalChoiceScene | Variante éditoriale active | `short` / `long` / `null` |
| `distal_advice_explanation_text_id` | DistalChoiceScene | Id du texte chercheur effectivement affiché | id / `null` |
| `distal_advice_explanation_clicked` | DistalChoiceScene | Clic sur « show explanation » (opt-in uniquement) | `true` / `false` / `null` |
| `distal_advice_explanation_display_duration_ms` | DistalChoiceScene | Durée d'affichage en ms (opt-in uniquement) | int / `null` |

### Proximal

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `proximal_advice_explanation_display_mode` | ProximalScene | Mode d'affichage du bloc | `forced` / `opt-in` / `none` |
| `proximal_advice_explanation_content_variant` | ProximalScene | Variante éditoriale active | `short` / `long` / `null` |
| `proximal_advice_explanation_text_id` | ProximalScene | Id du texte chercheur effectivement affiché | id / `null` |
| `proximal_advice_explanation_clicked` | ProximalScene | Clic sur « show explanation » (opt-in uniquement) | `true` / `false` / `null` |
| `proximal_advice_explanation_display_duration_ms` | ProximalScene | Durée d'affichage en ms (opt-in uniquement) | int / `null` |

### Motor

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `motor_advice_explanation_display_mode` | ProximalScene (motor UI) | Mode d'affichage du bloc | `forced` / `opt-in` / `none` |
| `motor_advice_explanation_content_variant` | ProximalScene (motor UI) | Variante éditoriale active | `short` / `long` / `null` |
| `motor_advice_explanation_text_id` | ProximalScene (motor UI) | Id du texte chercheur effectivement affiché | id / `null` |
| `motor_advice_explanation_clicked` | ProximalScene (motor UI) | Clic sur « show explanation » (opt-in uniquement) | `true` / `false` / `null` |
| `motor_advice_explanation_display_duration_ms` | ProximalScene (motor UI) | Durée d'affichage en ms (opt-in uniquement) | int / `null` |

**Note** : le couple `display_mode + content_variant` couvre l'option C de Q-EXP-9 (enum + id, max traçabilité), enrichi par le tracking opt-in demandé par les chercheurs (DEC-018, R7).

**Correspondance avec le GDD V1** :
- `distal_advice_explanation_*` ↔ `ValleyAdviseExplanation` du GDD (« Did the advisor include explanation? Which one? »).
- `proximal_advice_explanation_*` ↔ `ForestAdviseExplanation` du GDD.
- `motor_advice_explanation_*` : **colonnes nouvelles** non présentes dans le GDD initial, ajoutées par DEC-017 pour couvrir H8 sur le niveau motor.

---

## 5. Questions ouvertes

### Résolues (échange chercheurs du 2026-05-26, DEC-018)

| # | Décision retenue | Source |
|:---|:---|:---|
| Q-EXP-1 | **A — Blockwise** : `display_mode` et `content_variant` pilotés par bloc | Valerian + synthèse Florian |
| Q-EXP-2 | **A — `none` activable** : couvert par `display_mode = none` (nouvelle dimension Q-EXP-5) | Valerian |
| Q-EXP-3 | **B — Par bloc** : corpus customisable bloc par bloc | Valerian (flexibilité demandée, low priority) |
| Q-EXP-4 | **A — 1-to-1 strict** : pas de cross-level | Valerian + synthèse Florian (cf. §6 pour l'alternative évaluée) |
| Q-EXP-5 | **Nouveau modèle** : introduction du `display_mode` ∈ `forced` / `opt-in` / `none` (orthogonal au `content_variant`). Remplace les options A/B/C/D originales. | Valerian + Mark + synthèse Florian |
| Q-EXP-7 | **A — 1 par advice (jusqu'à 3)** + layout L/R : motor à gauche, proximal à droite | Valerian |
| Q-EXP-8 | **B — 2 corpora (human-bot, bot-bot)** : indexation par advisor_type | Valerian + arbitrage Florian |
| Q-EXP-9 | **C — Enum + id** : maximum de traçabilité, enrichi du tracking opt-in (clic + durée) | Valerian + Mark (tracking) |

### Restant ouvertes

| # | Question | Options / Statut | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-EXP-6 | Modalités précises de hide / minimum exposure de l'explication | Reformulée : **UI test à mener** pour valider la possibilité de cacher l'explication et son positionnement no-overlap. Contrainte acquise : pas de recouvrement de la map (TR10) | UX, contrôle exposition | 🟡 |
| Q-EXP-10 | Localisation du corpus (FR seul vs multilingue) ? | A: FR seul / B: multilingue dès le départ | Structure corpus, taille config | 🟢 |

---

## 6. Alternatives évaluées

### A1 — Cross-level autorisé (Q-EXP-4) — proposition Mark

**Proposition** : autoriser le cross-level **dans un seul sens** : un advice de niveau inférieur peut être accompagné d'une explanation de niveau supérieur (ex : motor-advice + explanation distale), mais jamais l'inverse.

**Justification scientifique** : permettrait de tester si la présence d'explanations de haut niveau modifie l'advice-taking en général. Exemple cité par Mark : « it would be very interesting if people took more motor advice from robots that give higher-level explanations than they did when the same advice is given with lower-level explanations ».

**Statut** : non retenu dans DEC-018 (Valerian + synthèse Florian préfèrent 1-to-1 strict, principe « keep it simple »). Conservé ici comme alternative ré-ouvrable si les premiers résultats expérimentaux le justifient.

### A2 — Pilotage trial-wise (Q-EXP-1) — préférence Mark

**Proposition** : tirage indépendant short/long à chaque trial plutôt qu'au niveau bloc.

**Statut** : non retenu (Valerian + synthèse Florian préfèrent blockwise, principe « keep it simple »). Combinatoire plus riche pour H8 si ré-ouverture future.
