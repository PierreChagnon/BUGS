# Spec Fonctionnelle — Explanations short/long

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** 2026-05-20
**Statut :** `draft`
**Chantier :** Explanations short/long
**Spec tech associée :** `Docs/specs/explanations-short-long/spec-tech.md` (à produire)

---

## 1. Contexte et scope

### Objectif

Permettre à chaque advice (distal, proximal, motor) d'être accompagné d'une **explication textuelle** affichée au participant. L'explication existe en **deux variantes éditoriales** (`short` et `long`), éditées par les chercheurs depuis le panneau de configuration de session. Le mode actif sur un trial détermine quelle variante est affichée — ou aucune (`none`). La feature est la brique nécessaire à l'hypothèse **H8** du GDD : *"Explanations with reference to higher levels of abstraction leads to higher levels of acceptability and human-likeness."*

### Périmètre IN

- [x] Affichage d'une explanation accompagnant un advice **distal**, **proximal** ou **motor**, conditionné à la précondition d'affichage (cf. règle R1).
- [x] Deux variantes textuelles **short** et **long** stockées et éditées indépendamment pour chaque advice.
- [x] Un mode actif par advice et par trial parmi `none` | `short` | `long`.
- [x] Pilotage du mode via la configuration de session (`SessionConfig` / `BlockConfig`), au niveau bloc ou trial (granularité à trancher, cf. Q-EXP-1).
- [x] Édition des contenus short et long par les chercheurs dans le **session config panel**.
- [x] Tracé des données dans la table `trial_responses` (table plate dénormalisée, DEC-011) avec colonnes dédiées par advice.
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
- **DEC-017 (en cours)** — Cadrage du présent chantier : périmètre D+P+M, deux textes alternatifs, précondition advisor choisi + advice donné, édition via session config panel. Résout Q-003 (motor advice avec explanation) et Q-006 (modèle d'édition).

### Dépendances

- **Requiert :** Advisors fonctionnels (DistalChoiceScene, ProximalScene, MotorAdvice forest UI), pipeline `trial_responses` (DEC-011), configuration de session chargée par URL + API (DEC-009).
- **Est requis par :** validité de l'hypothèse H8, finalisation du chantier Motor Advice (Q-003), spec free/forced (les conditions `explanation × forced` font partie de la combinatoire GDD).
- **Doit rester cohérent avec :** `BlockConfig` (DEC-014), envoi PATCH questionnaire (DEC-013), neutralité du flag `is_tutorial`.

---

## 2. Catalogue des comportements

### 2.1 Distal Advice + Explanation (DistalChoiceScene)

**Rôle :** Accompagner le distal-advice (recommandation sur la zone à explorer) par une explication textuelle qui justifie ce conseil au niveau de l'intention distale.
**Quand :** Sur l'écran distal, en début de bloc lorsque le participant doit choisir la vallée à explorer. Affichée uniquement si la précondition R1 est satisfaite.

#### Ce qui est affiché

- Si **mode = `none`** : aucun texte d'explication n'est affiché. L'advice (s'il est donné) reste visible seul.
- Si **mode = `short`** : un encart texte court accompagne l'advice. Contenu défini par le chercheur.
- Si **mode = `long`** : un encart texte plus développé accompagne l'advice. Contenu défini par le chercheur.

L'emplacement et le timing précis (avant / simultané / après l'advice, encart fixe vs bouton "voir explication") restent à trancher — cf. Q-EXP-5.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Le participant lit l'explanation | Pas d'action requise dans le comportement par défaut | Si l'explanation est interactive (Q-EXP-6), comportement à préciser |
| Le participant fait son distal-choice | Le choix est enregistré normalement, l'explanation lue ou non n'influe pas sur la validité du choix | — |

#### Règles métier

- **R1 (Précondition d'affichage)** : l'explanation n'est affichée que si **(a)** un advisor a été choisi par le participant (meta-choice ≠ "no advisor") **ET (b)** un advice distal est effectivement donné sur ce bloc. Si l'une des deux conditions manque, mode = `none` forcé, aucune explanation affichée, et la colonne CSV est renseignée en `none`.
- **R2** : le mode (`none`/`short`/`long`) est lu depuis la config de session. La granularité (par bloc ou par trial) reste à trancher — cf. Q-EXP-1.
- **R3** : l'explanation est **indépendante de la fiabilité** de l'advice. Un advice non fiable peut être accompagné d'une explanation (short ou long).
- **R4** : pendant le bloc tutorial (`is_tutorial = true`, DEC-014), aucune explanation n'est affichée et aucune ligne n'est envoyée à l'API (cohérent avec DEC-014).

#### Modes

- **`none`** vs **`short`** vs **`long`** (mode d'affichage).
- **Précondition satisfaite** vs **précondition non satisfaite** (R1).

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode d'explanation distal sur ce trial | `distal_advice_explanation_mode` | `none` / `short` / `long` | À l'affichage de la scène distale |
| Texte effectivement affiché (id du texte source, si retenu) | `distal_advice_explanation_text_id` *(optionnel, cf. Q-EXP-9)* | id du contenu chercheur / `null` | À l'affichage de la scène distale |

---

### 2.2 Proximal Advice + Explanation (ProximalScene)

**Rôle :** Accompagner le proximal-advice (recommandation sur le chemin à emprunter / la cible à viser) par une explication textuelle qui justifie ce conseil au niveau de l'intention proximale.
**Quand :** Sur le forest screen, au début et pendant le trial, conditionné à la précondition R1.

#### Ce qui est affiché

- Mêmes 3 modes que 2.1 (`none` / `short` / `long`).
- Contenus textuels propres à l'advice proximal, édités par le chercheur dans le session config panel.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Le participant lit l'explanation | Pas d'action requise par défaut | Si interactive (Q-EXP-6), à préciser |
| Le participant se déplace sur la grille | Le mouvement reste indépendant de l'explanation | — |

#### Règles métier

- **R1, R2, R3, R4** s'appliquent à l'identique (cf. §2.1).
- **R5 (positionnement écran)** : l'explanation proximale partage le forest screen avec l'advice proximal lui-même (chemin coloré dans le fog of war, cf. specs-light §9) — l'emplacement de l'encart doit ne pas masquer le chemin ni les nuages. Détails UI hors scope (Q-EXP-5).

#### Modes

- Identiques à §2.1.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode d'explanation proximal | `proximal_advice_explanation_mode` | `none` / `short` / `long` | Au début du trial |
| Texte affiché (id, optionnel) | `proximal_advice_explanation_text_id` *(cf. Q-EXP-9)* | id / `null` | Au début du trial |

---

### 2.3 Motor Advice + Explanation (ProximalScene / Forest screen)

**Rôle :** Accompagner le motor-advice (indication du set de touches actif, cf. spec MotorAdvice) par une explication textuelle qui justifie ce conseil au niveau de l'intention motrice.
**Quand :** Sur le forest screen, en même temps que l'affichage du set de touches (cf. spec MotorAdvice §2.1).

#### Ce qui est affiché

- Mêmes 3 modes (`none` / `short` / `long`).
- Le motor-advice lui-même reste l'affichage du set complet de touches (cf. spec MotorAdvice). L'explanation, si présente, vient justifier ce set ou expliquer le mécanisme.

**Note** : cette section **résout Q-003** (le motor advice doit-il inclure une explanation ?). Réponse : **oui**, au même titre que distal et proximal.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Le participant lit l'explanation | Pas d'action requise par défaut | Si interactive (Q-EXP-6), à préciser |
| Le participant appuie sur une touche | Comportement standard du motor-advice (cf. MotorAdvice spec) | L'explanation n'influe pas sur le mapping touches |

#### Règles métier

- **R1, R2, R3, R4** s'appliquent à l'identique.
- **R6 (cohérence avec MotorAdvice)** : l'explanation motor n'est affichée que si l'advice motor lui-même est affiché (probabilité d'apparition gérée par la spec MotorAdvice). Si motor advice = caché, mode explanation = `none` forcé.

#### Modes

- Identiques à §2.1.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| Mode d'explanation motor | `motor_advice_explanation_mode` | `none` / `short` / `long` | Au début du trial |
| Texte affiché (id, optionnel) | `motor_advice_explanation_text_id` *(cf. Q-EXP-9)* | id / `null` | Au début du trial |

---

## 3. Règles transversales

- **TR1 — Indépendance fiabilité / explanation** : la fiabilité d'un advice ne conditionne pas l'affichage de son explanation. Les deux variables sont orthogonales (cohérent avec les variables indépendantes du GDD : `Explanation/No explanations` listée séparément de `Advice reliability`).
- **TR2 — Deux textes alternatifs** : pour chaque advice, le chercheur édite **deux textes indépendants** (un court, un long). Aucune génération automatique d'un texte à partir de l'autre.
- **TR3 — Édition centralisée** : tous les contenus d'explanation sont édités par les chercheurs depuis le **session config panel** (DEC-017). Aucun texte n'est hardcodé côté Unity.
- **TR4 — Neutralité tutorial** : pendant un bloc `is_tutorial = true` (DEC-014), aucune explanation n'est affichée. Aucune colonne `*_explanation_mode` n'est envoyée à l'API (cohérent avec DEC-014, pas d'envoi API en tuto).
- **TR5 — Indépendance entre advice** : les trois modes d'explanation (distal, proximal, motor) sont pilotés indépendamment. Un trial peut avoir distal = `long`, proximal = `none`, motor = `short`.
- **TR6 — Cohérence avec free/forced** : l'interaction entre `explanation` et le mécanisme `free/forced` (Q-005 en attente) doit être confirmée dans la spec free/forced. Hypothèse de travail : les deux paramètres sont orthogonaux.

---

## 4. Matrice de traçabilité

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `distal_advice_explanation_mode` | DistalChoiceScene | Enregistrer mode d'explanation distal du bloc | `none` / `short` / `long` |
| `distal_advice_explanation_text_id` *(optionnel)* | DistalChoiceScene | Enregistrer l'id du texte distal effectivement affiché | id chercheur / `null` |
| `proximal_advice_explanation_mode` | ProximalScene | Enregistrer mode d'explanation proximal du trial | `none` / `short` / `long` |
| `proximal_advice_explanation_text_id` *(optionnel)* | ProximalScene | Enregistrer l'id du texte proximal effectivement affiché | id chercheur / `null` |
| `motor_advice_explanation_mode` | ProximalScene (motor advice UI) | Enregistrer mode d'explanation motor du trial | `none` / `short` / `long` |
| `motor_advice_explanation_text_id` *(optionnel)* | ProximalScene (motor advice UI) | Enregistrer l'id du texte motor effectivement affiché | id chercheur / `null` |

**Note** : Les colonnes `*_text_id` ne sont conservées que si l'option C de Q-EXP-9 est retenue (enum + id côte à côte).

**Correspondance avec le GDD V1** :
- `distal_advice_explanation_mode` ↔ `ValleyAdviseExplanation` du GDD (« Did the advisor include explanation? Which one? »).
- `proximal_advice_explanation_mode` ↔ `ForestAdviseExplanation` du GDD.
- `motor_advice_explanation_mode` : **colonne nouvelle** non présente dans le GDD initial, ajoutée par DEC-017 pour couvrir H8 sur le niveau motor.

---

## 5. Questions ouvertes

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-EXP-1 | Comment piloter quelle version (short / long) est donnée ? | A: par bloc / B: par trial / C: mixte (bloc active, trial tire short/long) | Schéma de config et de log | 🔴 |
| Q-EXP-2 | Veut-on des blocs où l'explanation est **simplement non donnée** alors même qu'un advisor a été choisi et qu'un advice est donné (mode `none` activable explicitement) ? | A: oui, `none` est un mode pilotable au même titre que short/long / B: non, dès qu'il y a un advice il y a forcément une explanation | Validité expérimentale, design panneau session | 🔴 |
| Q-EXP-3 | Les contenus des explanations sont-ils **customizables par bloc** ou **identiques pour toute la session** ? | A: par session uniquement (un corpus global) / B: par bloc (un corpus par bloc) / C: hybride (corpus session + overrides par bloc) | Taille et structure du corpus, complexité du panneau session | 🔴 |
| Q-EXP-4 | Un advice peut-il porter une explanation d'un **autre niveau d'abstraction** (cross-level) ? Ex : motor-advice + explanation distale, comme H8 le sous-entend. | A: non, 1-to-1 / B: oui, n'importe quel level / C: matrice contrôlée | Combinatoire des conditions H8 | 🟡 |
| Q-EXP-5 | Quand s'affiche l'explanation par rapport à l'advice ? | A: avant l'advice / B: simultané / C: après / D: bouton "voir explication" | UI, timing perception | 🟡 |
| Q-EXP-6 | L'explanation est-elle skippable ou a-t-elle une durée minimale d'affichage / une validation requise ? | A: skippable libre / B: durée minimale puis skippable / C: lecture obligatoire jusqu'à validation | UX, contrôle exposition | 🟡 |
| Q-EXP-7 | Si plusieurs advice présents sur un même trial (D rappelé + P + M), combien d'explanations max simultanées ? | A: 1 par advice (jusqu'à 3) / B: 1 globale par trial / C: piloté par config | Surcharge UI, logging | 🟡 |
| Q-EXP-8 | Différencie-t-on le corpus par **advisor type** (human-bot vs bot-bot) ? Le no-advisor a-t-il des explanations ? | A: corpus unique / B: corpus ×2 (human/bot) / C: corpus ×3 (incl. no-advisor) | Taille du corpus, mapping config | 🟡 |
| Q-EXP-9 | Format des valeurs CSV `*_advice_explanation` ? | A: enum `none/short/long` / B: id du texte affiché / C: enum + id (deux colonnes) | Traçabilité analytique | 🟡 |
| Q-EXP-10 | Localisation du corpus (FR seul vs multilingue) ? | A: FR seul / B: multilingue dès le départ | Structure corpus, taille config | 🟢 |
