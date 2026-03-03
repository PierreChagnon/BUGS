# Plan global de développement

> Séquencement des chantiers du projet. Mis à jour par le Rôle 4.
> Chaque chantier a ses tickets détaillés dans `Docs/specs/[chantier]/tickets.md`.

**Dernière mise à jour :** 2026-03-03
**Statut :** Brouillon — à compléter après production des specs fonc/tech des chantiers

---

## Séquencement proposé

```
Phase 0 : Infrastructure      [EN COURS]
  └─ Multi-screen flow (FlowController, transitions, lifecycle)
  
Phase 1 : Écrans de protocole  [À PLANIFIER]
  ├─ Consent screen
  ├─ Instructions & tutorial
  └─ Advisor system
  
Phase 2 : Gameplay enrichi     [À PLANIFIER]
  ├─ Score & feedback
  ├─ Meta-choice
  └─ Mountain UI (progression bloc)
  
Phase 3 : Finalisation         [À PLANIFIER]
  ├─ End-of-block summary
  ├─ Redirect questionnaire externe
  └─ Stabilisation & tests end-to-end
```

---

## Dépendances entre chantiers

```
Multi-screen flow ──► Tous les autres chantiers
                      (fondation requise)

Advisor system ──► Meta-choice
                   (le meta-choice référence la stratégie advisor)

Score & feedback ──► Mountain UI
                     (la montagne affiche la progression du score)

DEC-006 (score cumulé ?) ──► Score & feedback
                              Mountain UI
```

---

## État par chantier

| Chantier | Spec fonc | Spec tech | Tickets | Dev | Recette |
|:---|:---|:---|:---|:---|:---|
| Multi-screen flow | 🟡 Hybride existant | 🟡 Hybride existant | ⬜ | ⬜ | ⬜ |
| Advisor system | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Score & feedback | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Consent screen | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Instructions | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Meta-choice | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Mountain UI | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| End-of-block | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |
| Redirect | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ |

Légende : ⬜ À faire · 🟡 En cours / partiel · ✅ Terminé
