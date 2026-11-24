# Spec Fonctionnelle — Motor Advice

> ⚠️ **Errata 09/09/2026, complété le 06/10/2026** — les tableaux « Données collectées » (§2) et
> « Matrice de traçabilité » (§4) sont désormais à jour : les 4 colonnes existent dans `trial_responses`.
> « Advice affiché » → **`motor_advice_visible`** et « Advice fiable » → **`motor_advice_reliable`**
> depuis le 08/09/26 (commit `a919a683`, DEC-030) ; « Set actif » → **`motor_active_set`** et
> « Set affiché » → **`motor_advised_set`** depuis le 06/10/26 (build 1.5.0, DEC-050, réponse de Mark
> du 04/10/26 à Q-MOTOR-1). S'y ajoute **`player_press_log`**, le log de toutes les touches pressées
> pendant l'essai, qui permet de mesurer l'adhérence au conseil moteur touche par touche.

> 🆕 **Amendement 20/11/2026 — sets multiples (DEC-054, build 1.9.0)** : un essai peut avoir
> 2 ou 3 sets actifs en même temps. Voir §6. Les règles « un seul set actif » des sections 1 à 4
> restent vraies pour tout essai mono-set (cas par défaut, `motor_multi_set_count = 1`).

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Decrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** 2026-03-12
**Statut :** draft
**Chantier :** Motor Advice
**Spec tech associee :** Docs/specs/MotorAdvice/spec-tech.md

---

## 1. Contexte et scope

### Objectif
Mettre en place un motor advice qui peut indiquer au participant le set de touches actif pour se deplacer sur la grille. Le set actif est tire aleatoirement avant chaque essai. L advice peut etre absent selon une probabilite configurable et peut etre non fiable selon une probabilite configurable.

### Perimetre IN
- [x] Tirage du set actif avant chaque essai parmi WASD, TFGH, IJKL (equiprobable ; sets nommes sur le layout QWERTY, DEC-051).
- [x] Tirage de l apparition de l advice selon une probabilite configurable.
- [x] Si l advice apparait, tirage de sa fiabilite selon une probabilite configurable.
- [x] Affichage du set de touches (advice) en bas a gauche pendant tout l essai.
- [x] Le set actif reste fixe pendant tout l essai.
- [x] Les touches hors set actif sont ignorees (aucun mouvement).

### Perimetre OUT
| Exclu | Raison | Reporte a |
|:---|:---|:---|
| Penalites en cas de mauvaise touche | Decoupage | Chantiers penalites / feedback |
| Mise en page UI finale, style graphique | Design UI non defini | Chantier UI advisors |
| Conseils proximal / distal | Hors scope | Chantier Advisors |

### Decisions deja prises
- Le set actif change a chaque essai, tire equiprobablement parmi 3 sets.
- L advice a deux probabilites distinctes : apparition et fiabilite.
- L advice s affiche en bas de l ecran pendant tout l essai.

### Dependances
- **Requiert :** Forest Screen, systeme d input (deplacement), pipeline de parametres recherche.
- **Est requis par :** advisor system, logging research (CSV).

---

## 2. Catalogue des comportements

### 2.1 Forest Screen — Motor Advice

**Role :** Aider (ou tromper) le participant sur le set de touches actif pour se deplacer.
**Quand :** Au debut de chaque essai, puis visible pendant tout l essai si l advice est actif.

#### Ce qui est affiche
Si l advice est actif, un bloc de texte en bas a gauche indique le set complet, dans l ordre Haut/Gauche/Bas/Droite.
Exemples de sets possibles :
- Haut: Z, Gauche: Q, Bas: S, Droite: D
- Haut: T, Gauche: F, Bas: G, Droite: H
- Haut: I, Gauche: J, Bas: K, Droite: L

Si l advice n est pas actif, aucun indicateur n est affiche a cet emplacement.

#### Interactions utilisateur
| Action utilisateur | Resultat | Cas limite |
|:---|:---|:---|
| Appuyer sur une touche du set actif | Deplacement valide (si la case est marchable) | Si cible non marchable, aucun mouvement (regle existante) |
| Appuyer sur une touche hors set actif | Aucun mouvement | Aucune penalite appliquee (hors scope) |

#### Regles metier
- Avant chaque essai, tirer le set actif equiprobablement parmi les 3 sets.
- Avant chaque essai, tirer si l advice apparait selon une probabilite configurable.
- Si l advice apparait, tirer sa fiabilite selon une probabilite configurable.
- Advice fiable : l advice affiche le set actif.
- Advice non fiable : l advice affiche un set incorrect parmi les 2 autres (equiprobable).
- Le set actif reste constant pendant tout l essai.

#### Modes
- **Advice visible** vs **Advice cache**.
- **Advice fiable** vs **Advice non fiable** (uniquement si visible).

#### Donnees collectees
| Donnee | Colonne CSV V1 | Valeurs possibles | Quand enregistree |
|:---|:---|:---|:---|
| Set actif | `motor_active_set` *(DEC-050)* | AWD / FTH / JIL *(nomenclature QWERTY, DEC-051)* | Debut d essai |
| Advice affiche | `motor_advice_visible` *(errata 09/09/26)* | true / false | Debut d essai |
| Advice fiable | `motor_advice_reliable` *(errata 09/09/26)* | true / false | Debut d essai |
| Set affiche | `motor_advised_set` *(DEC-050)* | AWD / FTH / JIL / NA (legende cachee) | Debut d essai |
| Touches pressees | `player_press_log` *(DEC-050)* | `[{key, set, t}]` — key = etiquette QWERTY de la position physique (W/A/S/D, T/F/G/H, I/J/K/L — un participant AZERTY qui presse Z est logge W, DEC-051) ou nom Unity ; set = AWD / FTH / JIL / null | Chaque pression, inputs deverrouilles |

---

## 3. Regles transversales

- Le set actif ne change pas en cours d essai.
- Les touches hors set actif sont ignorees, sans penalite (penalites hors scope).
- L advice n affiche jamais un set partiel : toujours le set complet.

---

## 4. Matrice de tracabilite

| Colonne CSV V1 | Ecran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| `motor_active_set` *(DEC-050)* | Forest Screen / Motor Advice | Enregistrer set actif | AWD / FTH / JIL |
| `motor_advice_visible` *(errata 09/09/26)* | Forest Screen / Motor Advice | Enregistrer advice affiche | true / false |
| `motor_advice_reliable` *(errata 09/09/26)* | Forest Screen / Motor Advice | Enregistrer advice fiable | true / false |
| `motor_advised_set` *(DEC-050)* | Forest Screen / Motor Advice | Enregistrer set affiche | AWD / FTH / JIL / NA |
| `player_press_log` *(DEC-050)* | Forest Screen / GridMover | Enregistrer chaque touche pressee (set actif ou non, cooldown inclus) | `[{key, set, t}]` |

---

## 5. Questions ouvertes

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| 1 | ~~Quelles sont les colonnes CSV V1 exactes pour le motor advice (set actif, advice affiche, fiabilite, set affiche) ?~~ **Resolue le 06/10/26** (DEC-050, option A : 4 colonnes dediees + `player_press_log`) | A: Definir 4 colonnes / B: Reutiliser colonnes existantes | Traçabilite et pipeline data | ✅ |
| 2 | Faut il afficher un texte d explanation (short/long) en plus du set ? | A: Oui / B: Non | UI et contenu | 🟡 |
| 3 | Format d affichage exact du set (ordre, separators, labels) est il impose par le protocole ? | A: Libre / B: Template fixe | UI et comprehension | 🟡 |
| 4 | ~~Sets multiples : conseil non fiable avec 3 sets actifs, essais forcés, format CSV (Q-MOTOR-2)~~ **Résolue le 20/11/26** (validé par Mark) | Voir §6.3 | Analyse de l advice-following | ✅ |

---

## 6. Amendement 20/11/2026 — Sets multiples (DEC-054)

**Source :** mail de Mark du 18/11/2026 (fil « BUGS Status + CSV export reference », point 2).
**Pourquoi :** avec un seul set actif, le participant finit forcément par suivre un conseil moteur fiable
pour progresser. Avec plusieurs sets actifs, il peut ignorer le conseil et progresser quand même : on
peut alors mesurer s il suit le conseil par choix.

### 6.1 Réglages de bloc (dashboard, section Motor Choice)

| Réglage | Valeurs | Défaut | Effet |
|:---|:---|:---|:---|
| `motor_multi_set_count` | 1, 2 ou 3 | 1 | Nombre de sets actifs sur un essai multi-sets. À 1, aucun essai n est multi-sets (comportement d avant). |
| `motor_multi_set_probability` | 0 – 1 | 0 | Probabilité, tirée à chaque essai, que l essai soit multi-sets. Sans effet quand le compte vaut 1. |

Comme tous les réglages du jeu, c est une probabilité par essai (proportion attendue), jamais une proportion exacte.

### 6.2 Règles métier

- Un **essai forcé** (`motor_forced`) reste mono-set : seul le set imposé est actif. Le tirage multi-sets ne concerne que les essais libres.
- Sur un **essai libre multi-sets** : le set principal est tiré comme avant ; les autres sets actifs sont tirés au hasard parmi les sets restants, jusqu au nombre réglé.
- **Déplacement** : n importe quelle touche d un set actif déplace le joueur ; le participant peut mélanger les sets pendant l essai.
- **Touches invalides** (`invalid_key_presses`) : une touche est invalide si elle n appartient à **aucun** set actif.
- **Légende** : elle montre toujours un seul set. Fiable → le set principal (actif). Non fiable → un set **inactif**.
- **3 sets actifs** : il n existe aucun set inactif à montrer, la légende est donc **toujours fiable** sur ces essais (`motor_advice_reliable = true`), quel que soit le tirage de fiabilité.

### 6.3 Questions posées à Mark le 20/11 (Q-MOTOR-2), choix validés par Mark le 20/11

| # | Question | Choix retenu |
|:---|:---|:---|
| 1 | Conseil non fiable quand les 3 sets sont actifs | Toujours fiable sur ces essais |
| 2 | Essais forcés | Restent mono-set |
| 3 | Format CSV | `motor_active_set` liste les sets actifs dans l ordre fixe AWD, FTH, JIL, joints par `+` (`AWD`, `AWD+JIL`, `AWD+FTH+JIL`) |

### 6.4 Données collectées (ajouts)

| Donnee | Colonne CSV | Valeurs possibles | Quand enregistree |
|:---|:---|:---|:---|
| Sets actifs | `motor_active_set` *(domaine élargi)* | AWD / FTH / JIL / AWD+FTH / AWD+JIL / FTH+JIL / AWD+FTH+JIL | Debut d essai |
| Nombre de sets d un essai multi-sets (config) | `motor_multi_set_count` | 1 / 2 / 3 | Écho de la config du bloc, chaque essai |
| Probabilité multi-sets (config) | `motor_multi_set_probability` | 0 – 1 | Écho de la config du bloc, chaque essai |

Un essai est multi-sets quand `motor_active_set` contient `+` : pas de colonne dédiée.
