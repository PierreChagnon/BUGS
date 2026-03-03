# Spec Fonctionnelle — [Nom du chantier]

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** <!-- YYYY-MM-DD -->
**Statut :** `draft` | `validé` | `en cours` | `terminé`
**Chantier :** <!-- ex: Multi-écran, Advisors, Score -->
**Spec tech associée :** `Docs/specs/[chantier]/spec-tech.md`

---

## 1. Contexte et scope

### Objectif
<!-- Quel besoin du chercheur cette spec couvre. En 2-3 phrases. -->

### Périmètre IN
<!-- Ce qui est couvert par cette spec -->
- [ ] [Comportement 1]
- [ ] [Comportement 2]

### Périmètre OUT
| Exclu | Raison | Reporté à |
|:---|:---|:---|
| [Feature X] | [Raison] | [Quand] |

### Décisions déjà prises
<!-- Référencer Docs/project-state/decisions.md si applicable -->

### Dépendances
- **Requiert :** [specs ou chantiers prérequis]
- **Est requis par :** [ce qui dépend de cette spec]

---

## 2. Catalogue des comportements

### 2.1 [Écran / Composant 1]

**Rôle :** <!-- Pourquoi cet écran existe dans le protocole -->
**Quand :** <!-- À quel moment du flow il apparaît -->

#### Ce qui est affiché
<!-- Description visuelle du contenu, sans choix techniques -->

#### Interactions utilisateur
| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| [Action] | [Résultat] | [Que se passe-t-il si…] |

#### Règles métier
<!-- Règles logiques qui gouvernent ce comportement -->

#### Modes
<!-- Si applicable : free vs forced, visible vs hidden, etc. -->

#### Données collectées
| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| [Donnée] | [Colonne] | [Valeurs] | [Moment] |

### 2.2 [Écran / Composant 2]
<!-- Même structure -->

---

## 3. Règles transversales

<!-- Règles qui s'appliquent à plusieurs écrans/composants -->

---

## 4. Matrice de traçabilité

| Colonne CSV V1 | Écran / Composant | Comportement source | Valeurs possibles |
|:---|:---|:---|:---|
| [Colonne] | [Écran] | [Comportement] | [Valeurs] |

---

## 5. Questions ouvertes

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| 1 | [Question] | A: … / B: … | [Impact] | 🔴/🟡 |
