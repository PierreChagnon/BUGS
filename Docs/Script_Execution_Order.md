# Script Execution Order (Runtime)

| Ordre | Script | Moment | Dependances requises | Ce qu'il initialise / fournit | Remarques |
| :-- | :-- | :-- | :-- | :-- | :-- |
| -300 | LevelRegistry | Awake | — | Source de verite (grille, flags, originWorld) | Singleton |
| -250 | PlayerSpawner | Start | LevelRegistry | Instancie le player + enregistre playerStart | spawnTransform obligatoire |
| -240 | TilesSpawner | Awake | LevelRegistry + root | Initialise originWorld, instancie la grille de tiles | root obligatoire |
| -200 | BugCloudSpawner | Start | LevelRegistry (playerStart, grid) | Place 2 nuages + enregistre dans registry + informe GameManager | Dependance directe playerStart |
| -100 | BestPath | Start | LevelRegistry + nuages | Reserve chemins, genere quads, publie chemin conseille | Besoin de BugCloudSpawner deja passe |
| -50 | CorridorWallsGenerator | Start | LevelRegistry + BestPath | Genere couloirs, place murs | Utilise maze + reserved paths |
| -10 | TrapSpawner | Start | LevelRegistry | Place les pieges sur cases libres | |
| 0 | GameManager | Awake | — | Init score, UI, etat | Recoit nuages/chemin depuis spawners |
| 0 | SessionManager | Start | GameManager + TrialManager | Parse args, demarre le round | |
| 0 | GridMoverNewInput | Start/Update | LevelRegistry + GameManager | Mouvement, reveal fog, log steps | |
