# Spec Technique — [Nom du chantier]

> Produit par le Rôle 3 (Architecture Technique).
> Traduit la spec fonctionnelle en plan d'implémentation Unity/C#.

**Date :** <!-- YYYY-MM-DD -->
**Statut :** `draft` | `validé` | `en cours` | `terminé`
**Chantier :** <!-- ex: Multi-écran, Advisors, Score -->
**Spec fonc en entrée :** `Docs/specs/[chantier]/spec-fonc.md`
**Tickets associés :** `Docs/specs/[chantier]/tickets.md`

---

## 1. Contexte technique

### Spec fonctionnelle couverte
<!-- Résumé en 2-3 lignes de ce que la spec fonc demande -->

### Contraintes identifiées
<!-- WebGL, patterns existants, séquencement, etc. -->

### Alertes sur la spec fonctionnelle
<!-- Points où la spec fonc est irréaliste, risquée, ou sous-spécifiée -->

---

## 2. Architecture

### Nouveaux composants
| Composant | Responsabilité | Type |
|:---|:---|:---|
| [Nom] | [Rôle] | MonoBehaviour / SO / static / etc. |

### Modifications de l'existant
| Composant existant | Modification | Impact |
|:---|:---|:---|
| [Nom] | [Ce qui change] | [Effet sur les autres systèmes] |

### Diagramme d'architecture
```mermaid
<!-- Diagramme montrant les nouveaux composants et leur intégration -->
```

---

## 3. Contrats d'interface

### [Composant 1]

```csharp
// Pseudo-code — interface publique uniquement
// Pas d'implémentation, pas de code final

public class NomComposant : MonoBehaviour
{
    // Précondition : [ce qui doit être vrai avant l'appel]
    // Postcondition : [ce qui est garanti après l'appel]
    public void MethodePublique(params);
    
    // Event émis quand [condition]
    public event Action<Type> OnEvenement;
}
```

**Dépendances :**
- Nécessite : [...]
- Est utilisé par : [...]

---

## 4. Structures de données

### Enums
```csharp
public enum NomEnum
{
    Valeur1,    // [description]
    Valeur2,    // [description]
    // TOUTES les valeurs listées — pas de "etc."
}
```

### DTOs / Data classes
```csharp
[Serializable]
public class NomData
{
    public type champ;    // [description] — défaut : [valeur]
}
```

### Mapping CSV
| Champ C# | Colonne CSV V1 | Transformation |
|:---|:---|:---|
| [champ] | [colonne] | [directe / calculée / formatée] |

---

## 5. Flux de données et séquences

```mermaid
sequenceDiagram
    %% Diagramme de séquence pour les interactions complexes
```

### DefaultExecutionOrder
<!-- Si applicable : quel ordre pour les nouveaux composants et pourquoi -->

---

## 6. Gestion des erreurs

| Situation | Stratégie | Fallback |
|:---|:---|:---|
| [API timeout] | [Retry 3x] | [Log + continue] |
| [Quit en transition] | [Abandon propre] | [Données sauvées] |

---

## 7. Nettoyage et cycle de vie

### Entre trials
| Composant | Ce qui est détruit | Ce qui est reset | Ordre |
|:---|:---|:---|:---|
| [Composant] | [GameObjects] | [Champs] | [N] |

### Pattern recommandé
<!-- Ex: ICleanable interface, méthode ResetForNewTrial(), etc. -->

---

## 8. Stratégie de test

### Scénarios de validation
| Scénario | Vérification | Données CSV attendues |
|:---|:---|:---|
| [Scénario] | [Comportement attendu] | [Colonnes et valeurs] |

---

## 9. Conventions

### Nommage
<!-- Préfixes, suffixes, namespaces pour ce chantier -->

### Organisation fichiers
<!-- Où dans Assets/ les nouveaux fichiers vont -->

### Patterns à utiliser / éviter
<!-- Patterns du projet à suivre, anti-patterns à éviter -->
