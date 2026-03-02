# Script Execution Order (Runtime)

> Convention : **Awake = initialiser les données**, **Start = construire le niveau**.
> L'ordre est garanti par `[DefaultExecutionOrder(N)]`. Les scripts sans cet attribut s'exécutent à l'ordre 0.

---

## Phase 1 — Awake (infrastructure)

| Ordre | Script | Ce qu'il fait | Écrit vers |
|:------|:-------|:-------------|:-----------|
| -300 | LevelRegistry | Singleton. Crée la grille vierge (CellFlags, RNG). | — |
| -250 | FogController | Singleton. Lit `gridSize` depuis LevelRegistry, crée la texture masque RGBA32. | — |
| -240 | TilesSpawner | Calcule `originWorld` depuis la position du root. | `LevelRegistry.originWorld` |
| 0 | SessionManager | Parse args CLI (seed, trapCount, minDistance, totalBugs, greenRatio, gap, pathVisible, blockId, sessionId). Applique la seed et **tous les paramètres recherche** dans LevelRegistry. | `LevelRegistry.roundSeed`, `LevelRegistry.trapCount`, `LevelRegistry.minDistance`, `LevelRegistry.minTotalBugs`, `LevelRegistry.maxTotalBugs`, `LevelRegistry.minGreenBugsRatio`, `LevelRegistry.maxGreenBugsRatio`, `LevelRegistry.gapMin`, `LevelRegistry.gapMax`, `LevelRegistry.pathVisible`, `LevelRegistry.blockId`, `TrialManager.sessionId` |
| 0 | GameManager | Singleton. | — |

---

## Phase 2 — Start (construction du niveau, séquentiel)

| Ordre | Script | Ce qu'il fait | Dépendances requises | Écrit vers |
|:------|:-------|:-------------|:---------------------|:-----------|
| -250 | PlayerSpawner | Instancie le joueur à `spawnTransform`. | LevelRegistry (WorldToCell) | `LevelRegistry.RegisterPlayerStart(cell)` |
| -240 | TilesSpawner | Instancie les tuiles visuelles sur toute la grille. | LevelRegistry (originWorld, gridSize, cellSize) | — (lecture seule) |
| -200 | BugCloudSpawner | Place 2 nuages symétriques (L/R), lit les paramètres recherche depuis LevelRegistry, configure totalBugs + greenRatio. | LevelRegistry (playerStart, gridSize, CreateRng, **params recherche**) | `LevelRegistry.RegisterBugCloud(cell)`, `GameManager.RegisterClouds(left, right)` |
| -100 | PathSpawner | Calcule 2 chemins Manhattan optimaux, lit pathVisible depuis LevelRegistry, spawne les quads si visible, révèle le fog. | LevelRegistry (playerStart, paths, **pathVisible**), GameManager (GetBestCloud), FogController, tag "BugCloud" | `LevelRegistry.ReservePathLeft/Right(cells)`, `GameManager.SetChosenPath(cells)`, `FogController.RevealCells(cells)` |
| -50 | CorridorWallsGenerator | Maze procédural, force les couloirs le long des chemins, mure le reste. | LevelRegistry (paths, gridSize, playerStart), tag "Tile" | `LevelRegistry.RegisterWall(cell)` |
| -10 | TrapSpawner | Place N pièges sur les cases libres (Fisher-Yates shuffle). | LevelRegistry (trapCount, IsFreeForTrap, CreateRng) | `LevelRegistry.RegisterTrap(cell)` |
| 0 | SessionManager | Coroutine : yield 1 frame puis lance la partie. | GameManager | `GameManager.BeginFirstRound()` |

---

## Phase 3 — Update (gameplay)

| Script | Ordre | Ce qu'il fait | Signale vers |
|:-------|:------|:-------------|:-------------|
| GridMover | 0 | Lit les touches, valide via `IsWalkable`, interpole le déplacement. | `GameManager.OnPlayerStep(cell)` |
| BugCloud | 0 | Rotation visuelle. `OnTriggerEnter` détecte la collecte. | `GameManager.OnCloudCollected(this)` |
| Trap | 0 | `OnTriggerEnter` détecte le déclenchement (guard `_triggered`). | `GameManager.OnTrapTriggered()` |

---

## Scripts passifs (ni Awake ni Start)

| Script | Rôle |
|:-------|:-----|
| TrialManager | Reçoit les appels de GameManager, accumule les TrialData, envoie à l'API. |
| RoundUI | S'abonne à `GameManager.OnRoundEnded` dans son Start (ordre 0), affiche le panneau game over. |
| TrialData | Conteneur sérialisable pour une manche (pas un MonoBehaviour). |
| PlayerStep | Conteneur sérialisable pour un pas du joueur (pas un MonoBehaviour). |
