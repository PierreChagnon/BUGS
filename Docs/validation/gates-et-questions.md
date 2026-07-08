# Human gates & suivi des questions chercheur

> **But** : suivre les jalons humains (ce qui ne peut pas être fait sans décision/accès) et les questions chercheur qui conditionnent la validité des données.

## 1. Human gates

| Gate | Contenu | Responsable | Bloque | Statut | Date |
| :-- | :-- | :-- | :-- | :-- | :-- |
| **G0** | Sign-off chercheur Q-007 / Q-010 / Q-011 + gel du contrat (`TrialResponseRow`) | Chercheur + Florian | Axes 1→7 | ⬜ | |
| **G1** | Matrice de traçabilité renseignée (colonnes `CSV ?` + `Statut`) | Équipe | Axes 2→ | ⬜ | |
| **G2** | Sessions de test A/B/C prêtes dans Supabase | Équipe (backend) | Axes 3→ | ⬜ | |
| **G3** | Runs de test exécutés (build Unity + play / WebGL) | Florian / équipe | Axes 4,5,6 | ⬜ | |
| **G4** | CSV échantillon exporté & confronté au code | Équipe | Axe 1 (clôture) | ⬜ | |
| **G5** | Codebook validé et signé par le chercheur | Chercheur | Livraison | ⬜ | |

Statut : ⬜ à faire · 🟨 en cours · ✅ fait

## 2. Questions chercheur bloquantes (donnent du sens aux colonnes)

| Q | Sujet | Impact données | Colonnes concernées | Statut |
| :-- | :-- | :-- | :-- | :-- |
| **Q-007** | Modèle de perte de bugs (−1 green/piège/nuage vs décrément total) | Interprétation des scores | `green_bugs_collected`, `green_bugs_accumulated`, `traps_hit` | ⬜ |
| **Q-010** | Pattern de fiabilité advisor (fréquence) — *non spécifié* | Cœur scientifique | `*_advice_reliable*`, `*_visible*` | ⬜ |
| **Q-011** | Mapping des 3 questions ↔ dimensions | Validité questionnaire | `acceptability_question`, `sens_of_agency_question`, `human_likeness_question` | ⬜ |
| Q-002 | Colonnes CSV motor advice | Traçabilité motor | `motor_*` | ⬜ |
| Q-012 | Nombre de trials/bloc cible | Volumétrie / durée | `trial_count` | ⬜ |

> Référence : `Docs/project-state/questions-client.md`. Mettre à jour ce fichier **et** celui-ci quand une question est tranchée.

## 3. Décisions produites par la campagne (à répercuter)

| Réf | Décision | À reporter dans |
| :-- | :-- | :-- |
| | | `Docs/project-state/decisions.md` |

> En fin de campagne : mettre à jour `Docs/project-state/{decisions,avancement,questions-client}.md`.
