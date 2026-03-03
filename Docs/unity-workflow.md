# Unity Workflow — Protocole IA / Développeur

## Principe fondamental

L'IA pilote le développement mais ne peut pas voir l'éditeur Unity.
Le développeur est les yeux et les mains de l'IA dans l'éditeur.
Ce protocole définit qui fait quoi, quand, et comment on se synchronise.

---

## 1. Taxonomie des tâches

Chaque tâche est classifiée dès le PLAN initial.

| Symbole | Type                | Responsable      | Exemples                                                 |
| ------- | ------------------- | ---------------- | -------------------------------------------------------- |
| 🤖      | Code pur            | IA               | Scripts C#, Editor tools, ScriptableObjects, shaders     |
| 👤      | Editor setup        | Développeur      | Créer prefab, configurer composants, assigner références |
| 👁️      | Validation visuelle | Développeur      | "Est-ce que X se comporte comme attendu ?"               |
| 🎛️      | Tuning de valeurs   | Développeur + IA | L'IA propose, le développeur teste et rapporte           |
| 🧠      | Décision de design  | Développeur      | Choix entre 2 approches, priorité feature                |
| 🔴      | Bloquant            | Développeur      | Sans ça l'IA ne peut pas continuer                       |
| 🟡      | Non-bloquant        | Développeur      | Peut être fait plus tard sans bloquer                    |

---

## 2. Structure d'une feature

> Chaque feature suit deux phases : **SPEC** puis **IMPL**.
> Elles peuvent s'enchaîner dans la même conversation ou se faire en sessions séparées.
> Le fichier spec est **toujours écrit avant que le PLAN commence** — c'est le contrat entre les deux phases.

---

### PHASE 1 — SPEC

#### Étape 0-A — Dialogue de cadrage

L'IA joue le rôle de tech consultant. Elle interviewe le développeur pour comprendre la feature avant de la spécifier.

**Quand faire une SPEC :**

| Cas                                            | Approche                           |
| ---------------------------------------------- | ---------------------------------- |
| Feature nouvelle avec plusieurs sous-systèmes  | SPEC complète                      |
| Feature avec des choix de design non évidents  | SPEC courte                        |
| Extension d'une feature existante              | SPEC optionnelle — l'IA le signale |
| Tâche technique précise ("ajoute un cooldown") | Pas de SPEC, direct au PLAN        |

**Règles du dialogue :**

- Maximum 3 questions par round, jamais tout en une fois
- L'IA converge en 2-3 rounds maximum
- Toujours terminer par une synthèse soumise à validation avant d'écrire le fichier

**Format :**

```
📝 SPEC — [Nom de la feature]

Quelques questions pour cadrer avant de spécifier.

─── Round 1 / Comportement ───
1. [Question sur le comportement principal]
2. [Question sur les cas limites ou le scope OUT]
3. [Question sur le contexte joueur / usage]
```

Le développeur répond. L'IA continue si nécessaire :

```
─── Round 2 / Technique ───
4. [Question sur les contraintes techniques]
5. [Question sur ce qui existe déjà dans le projet]
```

#### Étape 0-B — Validation de la synthèse

Avant d'écrire le fichier, l'IA soumet la synthèse pour validation :

```
─── SYNTHÈSE — à valider ───

✅ IN : ce qui sera implémenté
  - [item 1]
  - [item 2]

❌ OUT : explicitement exclu ou reporté
  - [item A → pourquoi exclu ou reporté]

🔧 Hypothèses techniques
  - [choix que l'IA va faire]
  - [ce qu'elle suppose exister dans le projet]

🎯 Critères d'acceptation
  - [comment on sait que c'est "done"]

────────────────────────────────────
Synthèse correcte ? → "go" pour écrire le fichier spec
À modifier ? → dis-moi quoi
```

#### Étape 0-C — Écriture du fichier spec

Après validation, l'IA crée le fichier :
`Docs/specs/[feature-name].md`

Elle utilise le template `Docs/specs/_spec-template.md`.

Puis elle annonce :

```
✅ Spec écrite → Docs/specs/[feature-name].md

On passe au PLAN maintenant ? → "go"
Tu préfères t'arrêter là et implémenter plus tard ? → dis-le moi
```

---

### PHASE 2 — IMPL

#### Étape 1 — PLAN

Le PLAN est toujours généré depuis le fichier spec.
Si la session reprend plus tard, l'IA relit `Docs/specs/[feature-name].md` avant de planifier.
Les fonctionnalités OUT du spec n'apparaissent jamais dans le PLAN.

```
📋 PLAN — [Nom de la feature]
Basé sur : Docs/specs/[feature-name].md

Tâches IA 🤖
  [ ] Tâche 1
  [ ] Tâche 2

Tâches développeur 👤
  [ ] Tâche A (🔴 bloquante avant tâche 2)
  [ ] Tâche B (🟡 non-bloquante)

Validations prévues 👁️
  [ ] Validation 1 — après tâche 1 + 2
  [ ] Validation 2 — après tâche A

Hypothèses que je fais sur ton projet :
  - Hypothèse 1 (ex: "tu as un Canvas dans la scène")
  - Hypothèse 2

⚠️ Anticipe dès maintenant : [tâches développeur à préparer en parallèle]

Je commence par [tâche non-bloquante]. Go ?
```

#### Étape 2 — Implémentation

L'IA code et livre les fichiers. Elle signale systématiquement :

- Les composants Unity supposés présents sur les GameObjects
- Les layers, tags, ou noms de scène qu'elle utilise
- Les références à assigner dans l'Inspector

#### Étape 3 — HUMAN GATE

Quand le développeur doit agir avant que l'IA puisse continuer :

```
⏸️ HUMAN GATE #N (🔴 Bloquant / 🟡 Non-bloquant)

Actions requises :
  [ ] Action précise 1 (ex: "Créer un prefab 'Enemy' avec Rigidbody2D + Collider2D")
  [ ] Action précise 2 (ex: "Assigner ce prefab dans EnemyManager > enemyPrefab dans l'Inspector")
  [ ] Action précise 3 (ex: "Vérifier que le Layer 'Enemies' existe dans Project Settings > Tags and Layers")

📍 Où faire ça dans Unity : [indication de navigation si nécessaire]

✅ Réponds "gate ok" quand c'est fait.
```

#### Étape 4 — VALIDATION

Après chaque étape compilable :

```
👁️ VALIDATION #N

Lance le jeu et vérifie :
  [ ] Comportement attendu 1 (ex: "Le panel s'ouvre avec Tab")
  [ ] Comportement attendu 2 (ex: "Les items apparaissent sous forme de grille")

Si quelque chose ne correspond pas, décris exactement :
  - Ce que tu vois
  - À quel moment ça se passe (au démarrage, après une action, etc.)
  - S'il y a des erreurs dans la Console Unity
```

---

## 3. Gestion des erreurs de compilation

Quand le développeur colle des erreurs de compilation :

1. L'IA identifie la cause (missing reference, namespace, type mismatch...)
2. Elle livre le fix ciblé
3. Elle vérifie si d'autres fichiers sont impactés
4. Elle demande confirmation avant de passer à la suite si le fix est incertain

---

## 4. Approche "Code-First Unity"

Pour maximiser l'autonomie de l'IA, le projet favorise :

- **Scènes bootstrappées par code** — une scène minimaliste, les GameObjects sont spawnés par des managers
- **Prefabs générés par code** quand possible — des classes `Builder` construisent les objets programmatiquement
- **UI** uGUI.
- **ScriptableObjects** comme interface de configuration — l'IA crée les SOs, le développeur renseigne les données visuelles
- **Editor scripts** pour les tâches répétitives — l'IA écrit des outils que le développeur exécute

---

## 5. Règles de communication

- L'IA ne suppose jamais silencieusement — elle déclare ses hypothèses
- L'IA anticipe les gates à venir : _"Dans 2 étapes j'aurai besoin de X, commence à le préparer"_
- L'IA ne continue jamais après un gate sans confirmation explicite
- Le développeur utilise des réponses courtes : `"go"`, `"gate ok"`, `"erreur : [coller]"`, `"je vois : [décrire]"`
- En cas de doute sur une décision de design 🧠, l'IA propose 2 options max avec les trade-offs, le développeur choisit

---

## 6. Fin de session

Avant de clore une session, l'IA génère un résumé :

```
📦 FIN DE SESSION

✅ Complété
  - [liste des tâches finies]

⏸️ En cours / suspendu
  - [tâche + état exact]

👤 À faire par le développeur avant la prochaine session
  - [liste]

📝 Contexte à retenir pour la prochaine session
  - [hypothèses validées, décisions prises, état du système]
```
