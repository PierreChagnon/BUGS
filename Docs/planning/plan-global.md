# Plan global de développement

> # ⚠️ DOCUMENT ARCHIVÉ — gelé au 03/03/2026
>
> **Ne reflète plus l'état du projet.** Ce plan décrit un découpage en 4 phases qui a été abandonné :
> aucune case n'a jamais été passée à ✅, et il ne mentionne aucun des 8 chantiers réellement menés
> (audio, free/forced, explanations, how-to-play, tutoriel in-context, motor advice, penalty feedback,
> randomisation des blocs & pauses).
>
> La convention `Docs/specs/[chantier]/tickets.md` annoncée ci-dessous n'a **jamais été appliquée** :
> aucun `tickets.md` n'existe dans le repo.
>
> **Source de vérité actuelle :**
> - `Docs/project-state/revue-completude-2026-07-28.md` — état réel code vs attendu
> - `Docs/project-state/avancement.md` — état condensé
>
> Conservé pour l'historique. Ne pas mettre à jour.

---

> Séquencement des chantiers du projet. Mis à jour par le Rôle 4.
> Chaque chantier a ses tickets détaillés dans `Docs/specs/[chantier]/tickets.md`.

**Dernière mise à jour :** 2026-03-03
**Statut :** ARCHIVÉ le 28/07/2026 — brouillon jamais complété

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
