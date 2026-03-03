# SPEC — [Nom de la feature]

> Généré par l'IA après dialogue de cadrage.
> Ce fichier est le contrat entre la phase SPEC et la phase IMPL.
> Ne pas modifier manuellement sans en informer l'IA en début de session IMPL.

**Date :** <!-- YYYY-MM-DD -->
**Statut :** `draft` | `validé` | `en cours` | `terminé`

---

## Contexte

<!-- Pourquoi cette feature ? Quel problème joueur / besoin de game design elle résout -->

---

## Fonctionnalités IN

<!-- Ce qui sera implémenté dans cette feature -->

- [ ] [Comportement 1]
- [ ] [Comportement 2]
- [ ] [Comportement 3]

---

## Fonctionnalités OUT

<!-- Explicitement exclu de cette feature — avec raison -->

| Item        | Raison                | Reporté à         |
| ----------- | --------------------- | ----------------- |
| [Feature X] | hors scope MVP        | future feature    |
| [Feature Y] | dépend de [système Z] | après [système Z] |

---

## Critères d'acceptation

<!-- Comment on sait que c'est "done" — observable par le développeur dans Unity -->

- [ ] [Critère 1 — ex: "Le joueur peut ouvrir l'inventaire avec Tab"]
- [ ] [Critère 2 — ex: "Les items s'affichent en grille 4x5"]
- [ ] [Critère 3 — ex: "Aucune erreur dans la Console au démarrage"]

---

## Hypothèses techniques

<!-- Ce que l'IA suppose sur le projet — à vérifier avant de commencer le PLAN -->

- [ ] [Hypothèse 1 — ex: "Un Canvas 'MainCanvas' existe dans la scène"]
- [ ] [Hypothèse 2 — ex: "Le Layer 'UI' est configuré"]
- [ ] [Hypothèse 3 — ex: "UI Toolkit est disponible dans le projet"]

---

## Choix techniques retenus

<!-- Décisions d'architecture prises pendant le dialogue de cadrage -->

| Sujet             | Décision               | Raison                              |
| ----------------- | ---------------------- | ----------------------------------- |
| [ex: Rendu UI]    | [ex: UI Toolkit]       | [ex: lisible par l'IA, maintenable] |
| [ex: Persistance] | [ex: ScriptableObject] | [ex: pas de save pour l'instant]    |

---

## Dépendances

<!-- Systèmes existants que cette feature utilise ou modifie -->

- **Utilise :** [ex: EventBus.cs, PlayerController.cs]
- **Modifie :** [ex: GameManager.cs — ajout d'un état InventoryOpen]
- **Crée :** [ex: Inventory.cs, Item.cs, InventoryUI.cs]

---

## Assets requis

<!-- Ce que le développeur doit préparer — peut être des placeholders -->

| Asset                          | Type         | Bloquant ? | Notes                      |
| ------------------------------ | ------------ | ---------- | -------------------------- |
| [ex: Sprite item générique]    | Sprite 64x64 | 🔴 Oui     | Placeholder acceptable     |
| [ex: Son ouverture inventaire] | Audio clip   | 🟡 Non     | Peut être ajouté plus tard |

---

## Notes

<!-- Décisions prises pendant le dialogue, points d'attention, questions ouvertes -->
