# Spec Fonctionnelle — Motor Advice

> ⚠️ **Errata 09/09/2026, complété le 06/10/2026** — les tableaux « Données collectées » (§2) et
> « Matrice de traçabilité » (§4) sont désormais à jour : les 4 colonnes existent dans `trial_responses`.
> « Advice affiché » → **`motor_advice_visible`** et « Advice fiable » → **`motor_advice_reliable`**
> depuis le 08/09/26 (commit `a919a683`, DEC-030) ; « Set actif » → **`motor_active_set`** et
> « Set affiché » → **`motor_advised_set`** depuis le 06/10/26 (build 1.5.0, DEC-050, réponse de Mark
> du 04/10/26 à Q-MOTOR-1). S'y ajoute **`player_press_log`**, le log de toutes les touches pressées
> pendant l'essai, qui permet de mesurer l'adhérence au conseil moteur touche par touche.

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
