# Architecture runtime — BUGS (Unity)

> **Principe fondateur** : *Une entité sait ce qu'elle EST et ce qu'elle FAIT. Un manager sait ce que ça SIGNIFIE pour le jeu.*
>
> **MapGenerator** (outil éditeur) est volontairement exclu — il ne tourne pas en production.

---

## 1. Les 5 couches

Le projet est organisé en **5 couches** qui ne communiquent que dans un seul sens (de bas en haut) :

| Couche | Dossier | Scripts | Rôle |
|:-------|:--------|:--------|:-----|
| **UI** | `UI/` | RoundUI | Affiche, ne décide rien |
| **Controllers** | `Controllers/` | FogController | Exécute des ordres visuels (fog of war) |
| **Systems** | `Systems/` | GameManager, SessionManager, TrialManager, LevelRegistry | Orchestrent le jeu, stockent l'état |
| **Spawners** | `Spawners/` | PlayerSpawner, TilesSpawner, BugCloudSpawner, PathSpawner, CorridorWallsGenerator, TrapSpawner | Construisent le niveau une seule fois |
| **Entities** | `Entities/` | GridMover, BugCloud, Trap | Vivent dans le monde, signalent au GameManager |

Dossiers complémentaires : `Data/` (TrialData, PlayerStep), `EditorMapGenerator/` (MapGenerator, éditeur uniquement).

---

## 2. Systèmes centraux

| Système | Rôle |
|:--------|:-----|
| **LevelRegistry** | Source de vérité unique de la grille. CellFlags bitwise par cellule, conversions World↔Cell, marchabilité, RNG déterministe. Hub central : tous les Spawners y inscrivent leurs réservations, tous les systèmes y lisent l'état. Aucun spawner ne communique directement avec un autre — tout transite par le registre. |
| **GameManager** | Orchestre l'état d'un round : steps, trapsHit, bugsCollected. Reçoit les signaux des entités, délègue aux sous-systèmes (FogController, LevelRegistry, TrialManager). Émet `OnRoundEnded` — ne contient aucun code d'affichage. |
| **FogController** | Brouillard de guerre par texture masque RGBA32. Révélation par brush avec rayon et feathering via shader. |
| **SessionManager** | Parse les args CLI (seed, trapCount, sessionId). Injecte la config dans LevelRegistry et TrialManager. Lance `BeginFirstRound()`. Ne tourne qu'une fois au démarrage. |
| **TrialManager** | Collecte les données de manche (chemin joueur, config map, choix L/R, justesse). Empaquette dans TrialData, envoie en POST à l'API REST `/api/trials`. |

---

## 3. Cycle de vie d'un round

### Phase 1 — Initialisation (Awake)

Les Awake posent l'infrastructure (grille, fog, seed). Ordre garanti par `DefaultExecutionOrder` :

| Ordre | Script | Action |
|:------|:-------|:-------|
| -300 | LevelRegistry | Singleton + état grille vierge |
| -250 | FogController | Singleton + crée la texture masque (lit gridSize depuis LevelRegistry) |
| -240 | TilesSpawner | Calcule `originWorld` et l'écrit dans LevelRegistry |
| 0 | SessionManager | Parse seed CLI, appelle `SetRoundSeed()` sur LevelRegistry, écrit trapCount et sessionId |
| 0 | GameManager | Singleton |

### Phase 2 — Génération de map (Start, séquentiel)

Chaque Spawner lit l'état courant de LevelRegistry et y inscrit ses propres réservations. La chaîne de dépendances construit la map :

| Ordre | Script | Action | Écriture dans LevelRegistry |
|:------|:-------|:-------|:----------------------------|
| -250 | PlayerSpawner | Instancie le joueur | `RegisterPlayerStart(cell)` |
| -240 | TilesSpawner | Instancie la grille visuelle de tuiles | — (lecture seule : originWorld, gridSize, cellSize) |
| -200 | BugCloudSpawner | Place 2 nuages symétriques (L/R), configure totalBugs + greenRatio | `RegisterBugCloud(cell)` + `RegisterClouds(left, right)` vers GameManager |
| -100 | PathSpawner | Calcule 2 chemins Manhattan optimaux, visualise le chemin conseillé, révèle le fog | `ReservePathLeft/Right(cells)` + `SetChosenPath(cells)` vers GameManager |
| -50 | CorridorWallsGenerator | Maze procédural, couloirs forcés le long des chemins, le reste → mur | `RegisterWall(cell)` après vérification `IsOnAnyPath()` |
| -10 | TrapSpawner | Place N pièges sur les cellules libres | `RegisterTrap(cell)` après vérification `IsFreeForTrap()` |
| 0 | SessionManager (Start) | Lance `GameManager.BeginFirstRound()` → `TrialManager.BeginTrial()` | — |

**Règle d'or** : chaque spawner lit LevelRegistry pour savoir ce qui existe déjà, et y écrit son propre contenu.

### Phase 3 — Gameplay (Update loop)

Le joueur se déplace case par case. Trois entités signalent au GameManager :

| Entité | Signal | Ce que GameManager en fait |
|:-------|:-------|:---------------------------|
| **GridMover** (joueur) | `OnPlayerStep(cell)` | Révèle le fog (`FogController.RevealCell`), marque la case visitée (`LevelRegistry.MarkVisited`), vérifie l'adhérence au chemin optimal, enregistre le mouvement (`TrialManager.RecordMove`) |
| **BugCloud** | `OnCloudCollected(this)` | Calcule le score final, termine le round |
| **Trap** | `OnTrapTriggered()` | Applique une pénalité (`-1` bug par nuage par piège touché) |

**L'entité ne sait pas ce que son action signifie.** Le piège ne sait pas qu'il enlève des bugs. Le nuage ne sait pas qu'il termine le round. Seul GameManager le sait. Les entités ne se connaissent pas entre elles.

### Phase 4 — Fin de manche et données

Déclenchée par la collecte d'un nuage :

1. **GameManager** verrouille les inputs, calcule `bugsCollected`, émet l'événement `OnRoundEnded`.
2. **RoundUI** écoute `OnRoundEnded` et affiche le panneau game over avec les stats du round.
3. **TrialManager.EndCurrentTrial** enregistre le choix (L/R), la justesse, le timestamp de fin.
4. **TrialManager.SendTrials** sérialise en JSON et POST vers `/api/trials` avec auth token.
5. **GameManager.RestartRound** recharge la scène entière (`SceneManager.LoadScene()`) → retour Phase 1.

---

## 4. Pipeline de données de recherche

`GameManager.RegisterClouds()` → `TrialManager.SetMapConfig()` → chaque pas est enregistré via `RecordMove()` → `EndCurrentTrial()` empaquette dans `TrialData` + `PlayerStep` → `SendTrials()` POST vers l'API REST.

---

## 5. Résumé par script

| Script | Rôle |
|:-------|:-----|
| **LevelRegistry** | Stocke l'état de chaque case de la grille (flags, positions, seed). |
| **GameManager** | Reçoit les signaux des entités et orchestre le score, le fog et les données. |
| **SessionManager** | Lit la config CLI et lance la partie. |
| **TrialManager** | Enregistre les données de recherche et les envoie à l'API. |
| **FogController** | Gère la texture masque du brouillard de guerre. |
| **PlayerSpawner** | Instancie le joueur et enregistre sa position de départ. |
| **TilesSpawner** | Calcule originWorld (Awake) et génère la grille visuelle (Start). |
| **BugCloudSpawner** | Place 2 nuages avec ratios de bugs aléatoires contrôlés. |
| **PathSpawner** | Calcule et réserve les chemins optimaux vers chaque nuage. |
| **CorridorWallsGenerator** | Creuse un labyrinthe et mure le reste. |
| **TrapSpawner** | Place les pièges sur les cases libres. |
| **GridMover** | Déplace le joueur case par case et signale chaque pas. |
| **BugCloud** | Détecte la collecte et signale au GameManager. |
| **Trap** | Détecte le déclenchement et signale au GameManager. |
| **RoundUI** | Affiche l'écran de fin de round (stats + bouton restart). |

