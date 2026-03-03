# Registre des décisions

> Ce fichier est la source de vérité pour les décisions prises sur le projet.
> Mis à jour par tous les rôles en fin de session. Consulté par tous les rôles en début de session.
> Append-only : ne jamais supprimer une entrée. Marquer `ANNULÉE` si une décision est invalidée.

---

## Format

```
### DEC-[NNN] — [Titre court]
- **Date :** YYYY-MM-DD
- **Tag :** [SCOPE] / [FONC] / [TECH] / [PLANNING] / [CLIENT]
- **Décision :** [Ce qui a été décidé, en 1-2 phrases]
- **Raison :** [Pourquoi]
- **Impact :** [Ce que ça change pour la suite]
- **Statut :** ACTIF / EN ATTENTE / ANNULÉE
```

---

## Décisions

### DEC-001 — Meta-choice une fois par bloc
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** Le participant fait un meta-choice (confirmer ou changer de stratégie) une seule fois au début de chaque bloc, pas à chaque trial.
- **Raison :** Évite la fatigue décisionnelle. Le chercheur veut mesurer la stratégie globale, pas les micro-ajustements.
- **Impact :** L'écran meta-choice n'apparaît qu'en début de bloc. Le flow intra-bloc est simplifié.
- **Statut :** ACTIF

### DEC-002 — Transitions par fade noir
- **Date :** 2026-03-03
- **Tag :** [TECH]
- **Décision :** Toutes les transitions entre écrans utilisent un fade to black / fade from black.
- **Raison :** Simple, universel, pas de risque d'état visible incohérent pendant la transition.
- **Impact :** Un composant FadeTransition sera nécessaire. Durée configurable.
- **Statut :** ACTIF

### DEC-003 — UI montagne pour progression dans le bloc
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** La progression dans un bloc est visualisée par une métaphore de montagne (ascension).
- **Raison :** Cohérence thématique avec le jeu d'exploration. Feedback visuel motivant.
- **Impact :** Nécessite un écran dédié ou un overlay. Design visuel à préciser.
- **Statut :** ACTIF

### DEC-004 — Consent screen dans le scope
- **Date :** 2026-03-03
- **Tag :** [SCOPE]
- **Décision :** L'écran de consentement éclairé est dans le périmètre du jeu, pas géré par une plateforme externe.
- **Raison :** Contrôle total du flow. Données de consentement traçables dans le pipeline.
- **Impact :** Un écran consent sera nécessaire avec checkbox obligatoire + bouton continuer.
- **Statut :** ACTIF

### DEC-005 — Questionnaires hors scope
- **Date :** 2026-03-03
- **Tag :** [SCOPE]
- **Décision :** Les questionnaires pré/post expérience sont hors scope du jeu Unity. Gérés par un outil externe (Qualtrics ou similaire).
- **Raison :** Réutiliser des outils spécialisés plutôt que recoder un moteur de questionnaire.
- **Impact :** Le flow du jeu démarre après le questionnaire pré et se termine avant le questionnaire post. Redirection URL à prévoir.
- **Statut :** ACTIF

### DEC-006 — Score cumulé entre trials
- **Date :** 2026-03-03
- **Tag :** [FONC]
- **Décision :** EN ATTENTE — le score se cumule-t-il entre les trials d'un même bloc, ou est-il remis à zéro ?
- **Raison :** Impacte la mécanique de feedback et la motivation du participant.
- **Impact :** Si cumulé → besoin d'un ScoreManager persistant. Si reset → le score est local au trial.
- **Statut :** EN ATTENTE — question à poser au client

### DEC-007 — Claude Code comme environnement unique pour tous les rôles
- **Date :** 2026-03-03
- **Tag :** [TECH]
- **Décision :** Les 5 rôles de gestion de projet (pilotage, analyse fonc, archi tech, planification, support dev) sont intégrés dans Claude Code via des fichiers de rôle dans le repo, plutôt que dans des projets Claude séparés via le navigateur.
- **Raison :** Évite la duplication de documents entre projets, permet la traçabilité git, et unifie l'environnement de travail.
- **Impact :** Structure `Docs/roles/` + `Docs/project-state/` ajoutée au repo. `CLAUDE.md` enrichi avec un système d'aiguillage.
- **Statut :** ACTIF

---

## Index par tag

- **[SCOPE]** : DEC-004, DEC-005
- **[FONC]** : DEC-001, DEC-003, DEC-006
- **[TECH]** : DEC-002, DEC-007
- **[PLANNING]** : _(aucune pour l'instant)_
- **[CLIENT]** : _(aucune pour l'instant)_
