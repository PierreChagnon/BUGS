using System;
using System.Collections.Generic;
using UnityEngine;

// Génère une map complète directement dans l'éditeur (positions only).
// Objectif: visualiser rapidement la cohérence des placements sans lancer le jeu.
// NOTE: ce script COPIE volontairement la logique actuelle des spawners runtime
// (BugCloudSpawner / BestPath / CorridorWallsGenerator / TrapSpawner) afin d'éviter
// de modifier ces scripts et de limiter l'impact sur le repo.

public class MapGenerator : MonoBehaviour
{
    [Header("Références (Scene)")]
    public LevelRegistry registry;

    [Tooltip("Si null, on utilise 'playerInScene' comme référence (déjà placé dans la scène).")]
    public GameObject playerPrefab;

    [Tooltip("Optionnel: un player déjà présent dans la scène.")]
    public Transform playerInScene;

    [Header("Grille")]
    public Vector2Int gridSize = new(11, 11);
    public float cellSize = 1f;

    [Header("Tiles")]
    public GameObject tilePrefab;

    [Header("Bug Clouds")]
    public GameObject bugCloudPrefab;
    [Tooltip("Distance Manhattan minimale (en cases) depuis le joueur.")]
    public int minDistance = 3;
    [Tooltip("Z minimale (cell.y) pour placer un nuage. Reprend la logique actuelle.")]
    public int minCloudZ = 5;
    public float cloudY = 0.5f;

    [Header("Best Path")]
    public GameObject pathQuadPrefab;
    public bool showBestPath = true;

    [Header("Corridors / Walls")]
    [Min(1)]
    public int corridorWidth = 2;
    [Range(0, 100)]
    public int extraConnections = 10;
    public bool fallbackConnectToClouds = true;

    public GameObject wallPrefab;
    public Material wallMaterial;
    public float wallY = 0.5f;
    public float wallHeight = 1.0f;
    public float wallThickness = 1.0f;

    [Header("Traps")]
    public GameObject trapPrefab;
    public int trapCount = 10;
    public float trapY = 0.5f;

    [Header("Preview Root")]
    public Transform root;

    readonly Dictionary<Vector2Int, GameObject> _tilesByCell = new();

    [ContextMenu("Generate (Editor Preview)")]
    public void GenerateEditorPreview()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MapGenerator] Cette génération est prévue pour l'éditeur (hors Play Mode).");
            return;
        }

        ResolveRefs();
        if (registry == null)
        {
            Debug.LogError("[MapGenerator] LevelRegistry manquant dans la scène.");
            return;
        }

        // En preview éditeur, la source de vérité est l'inspecteur de ce MapGenerator.
        // On pousse donc la valeur dans le LevelRegistry pour que toute la génération
        // (réservations, murs, traps) soit cohérente sur la même taille de grille.
        registry.gridSize = gridSize;

        EnsureRoot();
        ClearEditorPreview();

        registry.ClearPathReservations();

        GenerateTiles();

        var player = EnsurePlayer();
        if (player == null)
        {
            Debug.LogError("[MapGenerator] Aucun player (prefab ou transform) n'est assigné.");
            return;
        }

        var playerCell = WorldToCell(player.position);

        // 1) Nuages (réservent leurs cases dans le registre)
        var clouds = GenerateBugClouds(playerCell);

        // 2) BestPath (réserve les chemins)
        if (clouds.Count >= 2)
            GenerateBestPath(player, clouds);
        else
            Debug.LogWarning("[MapGenerator] Moins de 2 nuages générés: best path ignoré.");

        // 3) Couloirs + Murs
        GenerateCorridorWalls(player);

        // 4) Traps (respecte IsFreeForTrap => pas sur mur / pas sur réservé / pas sur trap)
        GenerateTraps(playerCell);

        Debug.Log("[MapGenerator] Preview générée.");
    }

    [ContextMenu("Clear (Editor Preview)")]
    public void ClearEditorPreview()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MapGenerator] Clear prévu hors Play Mode.");
            return;
        }

        if (root != null)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                DestroyImmediate(root.GetChild(i).gameObject);
        }

        _tilesByCell.Clear();

        if (registry != null)
        {
            // On garde la grille, mais on nettoie les flags liés à la génération.
            // (Les clouds/traps/walls/path/reserved sont propres au layout.)
            // La classe n'a pas d'API publique de ClearAll: on remet au minimum.
            // On peut recréer le composant si besoin, mais on évite de toucher aux scripts existants.
            registry.ClearPathReservations();

            // Walls/Traps/BugCloud ne sont pas exposés en bulk: on les retire cellule par cellule.
            for (int y = 0; y < registry.gridSize.y; y++)
            {
                for (int x = 0; x < registry.gridSize.x; x++)
                {
                    var c = new Vector2Int(x, y);
                    registry.UnregisterWall(c);
                    registry.UnregisterTrap(c);
                    registry.UnregisterBugCloud(c);
                }
            }
        }
    }

    void ResolveRefs()
    {
        if (registry == null) registry = FindFirstObjectByType<LevelRegistry>();
        if (playerInScene == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) playerInScene = go.transform;
        }
    }

    void EnsureRoot()
    {
        if (root != null) return;
        var go = new GameObject("MapPreviewRoot");
        go.transform.SetParent(transform, false);
        root = go.transform;
    }

    Transform EnsurePlayer()
    {
        if (playerPrefab != null)
        {
            // En preview éditeur, on place le joueur au milieu de la première ligne (z=0)
            // pour reproduire la logique runtime "nuages devant le joueur".
            var playerCell = new Vector2Int(Mathf.Clamp(gridSize.x / 2, 0, Mathf.Max(0, gridSize.x - 1)), 0);
            var playerWorld = CellToWorld(playerCell, 0f);
            var playerGo = InstantiateEditor(playerPrefab, playerWorld, Quaternion.identity, root);
            playerGo.name = "PlayerPreview";
            return playerGo.transform;
        }

        return playerInScene;
    }

    void GenerateTiles()
    {
        if (tilePrefab == null)
        {
            Debug.LogWarning("[MapGenerator] tilePrefab manquant: tiles ignorées.");
            return;
        }

        var tilesRoot = new GameObject("Tiles");
        tilesRoot.transform.SetParent(root, false);

        _tilesByCell.Clear();

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                var pos = CellToWorld(new Vector2Int(x, y), 0f);
                var tile = InstantiateEditor(tilePrefab, pos, tilePrefab.transform.rotation, tilesRoot.transform);
                tile.name = $"Tile_{x}_{y}";
                if (!_tilesByCell.ContainsKey(new Vector2Int(x, y)))
                    _tilesByCell.Add(new Vector2Int(x, y), tile);
            }
        }
    }

    List<GameObject> GenerateBugClouds(Vector2Int playerCell)
    {
        var result = new List<GameObject>(2);

        if (bugCloudPrefab == null)
        {
            Debug.LogWarning("[MapGenerator] bugCloudPrefab manquant: nuages ignorés.");
            return result;
        }

        // Logique reprise de BugCloudSpawner
        int Dmin = Mathf.Max(1, minDistance);
        int Dmax = gridSize.x + gridSize.y;

        var candidateDs = new List<int>();
        for (int D = Dmin; D <= Dmax; D++)
        {
            var ring = GetRingCells(playerCell, D);
            ring.RemoveAll(c => !InBounds(c) || c == playerCell);
            if (ring.Count >= 2) candidateDs.Add(D);
        }

        if (candidateDs.Count == 0)
        {
            Debug.LogWarning("[MapGenerator] Aucune couronne valide pour placer les nuages.");
            return result;
        }

        // Runtime: on choisit un D au hasard puis on filtre par minZ.
        // Ici on garde la même logique, mais on borne les tentatives et on retente avec d'autres D
        // pour éviter les cas "couronne devient trop petite après filtrage".
        Vector2Int cellA = default;
        Vector2Int cellB = default;
        bool found = false;

        // Shuffle léger des distances candidates
        for (int s = 0; s < candidateDs.Count; s++)
        {
            int swap = UnityEngine.Random.Range(s, candidateDs.Count);
            (candidateDs[s], candidateDs[swap]) = (candidateDs[swap], candidateDs[s]);
        }

        int maxDRetries = Mathf.Min(candidateDs.Count, 8);
        for (int dTry = 0; dTry < maxDRetries && !found; dTry++)
        {
            int chosenD = candidateDs[dTry];
            var validRing = GetRingCells(playerCell, chosenD);
            validRing.RemoveAll(c => !InBounds(c) || c == playerCell || c.y < minCloudZ);

            if (validRing.Count < 2)
                continue;

            // Choix de 2 cases distinctes dans la couronne valide, en respectant le pattern runtime:
            // une "plutôt à gauche" et une "plutôt à droite" sur la même ligne (y).
            int halfStart = validRing.Count / 2;
            if (halfStart <= 0 || halfStart >= validRing.Count)
                continue;

            int i = -1;
            int j = -1;
            int attempts = 0;
            int maxAttempts = 50;

            while (j == -1 && attempts < maxAttempts)
            {
                attempts++;

                // Comme le runtime: i dans la moitié gauche, en évitant le "milieu".
                int leftMaxExclusive = Mathf.Max(1, halfStart - 1);
                i = UnityEngine.Random.Range(0, leftMaxExclusive);
                j = validRing.FindIndex(halfStart, c => c.y == validRing[i].y);
            }

            if (j < 0 || i < 0 || i >= validRing.Count || j >= validRing.Count)
                continue;

            cellA = validRing[i];
            cellB = validRing[j];
            found = cellA != cellB;

            if (found)
                Debug.Log($"[MapGenerator] Couronne D={chosenD} a {validRing.Count} cases valides après filtrage.");
        }

        if (!found)
        {
            Debug.LogWarning("[MapGenerator] Impossible de placer 2 nuages (vérifie gridSize/minDistance/minCloudZ)." );
            return result;
        }

        var cloudsRoot = GetOrCreateChildRoot("BugClouds");

        var goA = InstantiateEditor(bugCloudPrefab, CellToWorld(cellA, cloudY), Quaternion.identity, cloudsRoot);
        var goB = InstantiateEditor(bugCloudPrefab, CellToWorld(cellB, cloudY), Quaternion.identity, cloudsRoot);

        SafeSetTag(goA, "BugCloud");
        SafeSetTag(goB, "BugCloud");

        result.Add(goA);
        result.Add(goB);

        registry.RegisterBugCloud(cellA);
        registry.RegisterBugCloud(cellB);

        return result;
    }

    void GenerateBestPath(Transform player, List<GameObject> clouds)
    {
        if (player == null) return;
        if (clouds == null || clouds.Count < 2) return;

        // Logique reprise de BestPath (sans GameManager/Fog)
        GameObject leftCloud = null;
        GameObject rightCloud = null;
        foreach (var cloud in clouds)
        {
            if (cloud == null) continue;
            if (leftCloud == null || cloud.transform.position.x < leftCloud.transform.position.x)
                leftCloud = cloud;
            if (rightCloud == null || cloud.transform.position.x > rightCloud.transform.position.x)
                rightCloud = cloud;
        }

        if (leftCloud == null || rightCloud == null) return;

        // Utiliser des cellules (entiers) évite les soucis d'égalité flottante et garantit la terminaison.
        var playerCell = WorldToCell(player.position);
        var leftCloudCell = WorldToCell(leftCloud.transform.position);
        var rightCloudCell = WorldToCell(rightCloud.transform.position);

        var leftCells = BuildShortestRandomManhattanPath(playerCell, leftCloudCell);
        var rightCells = BuildShortestRandomManhattanPath(playerCell, rightCloudCell);

        registry.ReservePathLeft(leftCells);
        registry.ReservePathRight(rightCells);

        // Choix du best path (sans GameManager => aléatoire 50/50 comme fallback)
        var chosen = (UnityEngine.Random.value < 0.5f) ? leftCells : rightCells;
        registry.RegisterOptimalPath(chosen);

        if (!showBestPath) return;
        if (pathQuadPrefab == null)
        {
            Debug.LogWarning("[MapGenerator] pathQuadPrefab manquant: best path non visible.");
            return;
        }

        var pathRoot = GetOrCreateChildRoot("BestPath");
        foreach (var c in chosen)
        {
            var pos = CellToWorld(c, 0.01f);
            InstantiateEditor(pathQuadPrefab, pos, pathQuadPrefab.transform.rotation, pathRoot);
        }
    }

    void GenerateCorridorWalls(Transform player)
    {
        var wallsRoot = GetOrCreateChildRoot("Walls");

        var walkable = BuildWalkableCells(player);
        if (walkable.Count == 0 && fallbackConnectToClouds)
            walkable = BuildFallbackWalkable(player);

        if (walkable.Count == 0)
        {
            Debug.LogWarning("[MapGenerator] Aucun couloir généré (walkable vide).");
            return;
        }

        AddExtraConnections(walkable);

        int wallsPlaced = 0;
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (walkable.Contains(c))
                {
                    if (registry.IsWall(c)) registry.UnregisterWall(c);
                    continue;
                }

                registry.RegisterWall(c);
                PaintTileAsWall(c);
                SpawnWallVisual(c, wallsRoot);
                wallsPlaced++;
            }
        }

        Debug.Log($"[MapGenerator] Walkable={walkable.Count} Walls={wallsPlaced}");
    }

    void GenerateTraps(Vector2Int playerCell)
    {
        if (trapPrefab == null)
        {
            Debug.LogWarning("[MapGenerator] trapPrefab manquant: traps ignorés.");
            return;
        }

        var trapsRoot = GetOrCreateChildRoot("Traps");

        var candidates = new List<Vector2Int>();
        for (int x = 0; x < gridSize.x; x++)
            for (int z = 0; z < gridSize.y; z++)
                candidates.Add(new Vector2Int(x, z));

        candidates.Remove(playerCell);
        candidates.RemoveAll(c => !registry.IsFreeForTrap(c));

        // Shuffle
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < trapCount; i++)
        {
            var cell = candidates[i];
            if (!registry.RegisterTrap(cell)) continue;
            InstantiateEditor(trapPrefab, CellToWorld(cell, trapY), Quaternion.identity, trapsRoot);
            placed++;
        }

        Debug.Log($"[MapGenerator] Traps posés: {placed}/{trapCount}");
    }

    // -----------------
    // Corridor logic (copie CorridorWallsGenerator)
    // -----------------

    HashSet<Vector2Int> BuildWalkableCells(Transform player)
    {
        var baseCells = new HashSet<Vector2Int>();

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (registry.IsOnAnyPath(c) || registry.HasBugCloud(c))
                    baseCells.Add(c);
            }
        }

        if (player != null)
            baseCells.Add(WorldToCell(player.position));

        return Inflate(baseCells, corridorWidth);
    }

    HashSet<Vector2Int> BuildFallbackWalkable(Transform player)
    {
        var result = new HashSet<Vector2Int>();
        var start = player != null ? WorldToCell(player.position) : new Vector2Int(0, 0);
        if (!InBounds(start)) start = new Vector2Int(0, 0);
        result.Add(start);

        // Cherche les nuages générés dans notre root (plus robuste qu'un FindWithTag global)
        var cloudsRoot = root != null ? root.Find("BugClouds") : null;
        if (cloudsRoot == null || cloudsRoot.childCount == 0)
            return Inflate(result, corridorWidth);

        for (int i = 0; i < cloudsRoot.childCount; i++)
        {
            var cloud = cloudsRoot.GetChild(i);
            if (cloud == null) continue;
            var goal = WorldToCell(cloud.position);
            CarveLPath(start, goal, result);
        }

        return Inflate(result, corridorWidth);
    }

    void AddExtraConnections(HashSet<Vector2Int> walkable)
    {
        if (extraConnections <= 0) return;

        var deadEnds = new List<Vector2Int>();
        foreach (var c in walkable)
        {
            int deg = 0;
            foreach (var n in Neighbors4(c))
                if (walkable.Contains(n)) deg++;

            if (deg <= 1) deadEnds.Add(c);
        }

        int attempts = 0;
        int added = 0;
        while (attempts < extraConnections && deadEnds.Count > 0)
        {
            attempts++;
            var from = deadEnds[UnityEngine.Random.Range(0, deadEnds.Count)];

            if (!TryFindNearbyTarget(walkable, from, out var to))
                continue;

            var carved = new HashSet<Vector2Int>();
            CarveLPath(from, to, carved);
            var inflated = Inflate(carved, corridorWidth);
            foreach (var c in inflated) walkable.Add(c);
            added++;
        }

        if (added > 0)
            Debug.Log($"[MapGenerator] Extra connections added: {added}");
    }

    static IEnumerable<Vector2Int> Neighbors4(Vector2Int c)
    {
        yield return new Vector2Int(c.x + 1, c.y);
        yield return new Vector2Int(c.x - 1, c.y);
        yield return new Vector2Int(c.x, c.y + 1);
        yield return new Vector2Int(c.x, c.y - 1);
    }

    static void CarveLPath(Vector2Int a, Vector2Int b, HashSet<Vector2Int> into)
    {
        bool horizontalFirst = UnityEngine.Random.value < 0.5f;

        var cur = a;
        into.Add(cur);

        if (horizontalFirst)
        {
            while (cur.x != b.x)
            {
                cur.x += Math.Sign(b.x - cur.x);
                into.Add(cur);
            }
            while (cur.y != b.y)
            {
                cur.y += Math.Sign(b.y - cur.y);
                into.Add(cur);
            }
        }
        else
        {
            while (cur.y != b.y)
            {
                cur.y += Math.Sign(b.y - cur.y);
                into.Add(cur);
            }
            while (cur.x != b.x)
            {
                cur.x += Math.Sign(b.x - cur.x);
                into.Add(cur);
            }
        }
    }

    HashSet<Vector2Int> Inflate(HashSet<Vector2Int> cells, int width)
    {
        if (cells == null) return new HashSet<Vector2Int>();
        if (width <= 1) return new HashSet<Vector2Int>(cells);

        int left = (width - 1) / 2;
        int right = (width - 1) - left;

        var outSet = new HashSet<Vector2Int>();
        foreach (var c in cells)
        {
            for (int dy = -left; dy <= right; dy++)
            {
                for (int dx = -left; dx <= right; dx++)
                {
                    var cc = new Vector2Int(c.x + dx, c.y + dy);
                    if (InBounds(cc)) outSet.Add(cc);
                }
            }
        }
        return outSet;
    }

    bool TryFindNearbyTarget(HashSet<Vector2Int> walkable, Vector2Int from, out Vector2Int to)
    {
        for (int r = 2; r <= 6; r++)
        {
            var candidates = new List<Vector2Int>();
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    var c = new Vector2Int(from.x + dx, from.y + dy);
                    if (!InBounds(c)) continue;
                    if (!walkable.Contains(c)) continue;
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) < 2) continue;
                    candidates.Add(c);
                }
            }

            if (candidates.Count > 0)
            {
                to = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                return true;
            }
        }

        to = default;
        return false;
    }

    void PaintTileAsWall(Vector2Int c)
    {
        if (wallMaterial == null) return;
        if (!_tilesByCell.TryGetValue(c, out var tile) || tile == null) return;

        var renderers = tile.GetComponentsInChildren<Renderer>();
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.sharedMaterial = wallMaterial;
        }
    }

    void SpawnWallVisual(Vector2Int c, Transform parent)
    {
        if (wallPrefab != null)
        {
            InstantiateEditor(wallPrefab, CellToWorld(c, wallY), Quaternion.identity, parent);
            return;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Wall ({c.x},{c.y})";
        go.transform.SetParent(parent, false);
        go.transform.position = CellToWorld(c, wallY);
        go.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);

        if (wallMaterial != null)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = wallMaterial;
        }
    }

    // -----------------
    // Helpers
    // -----------------

    Transform GetOrCreateChildRoot(string name)
    {
        var t = root.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    static void SafeSetTag(GameObject go, string tag)
    {
        if (go == null) return;
        try { go.tag = tag; }
        catch { /* tag absent */ }
    }

    bool InBounds(Vector2Int c)
        => c.x >= 0 && c.x < gridSize.x && c.y >= 0 && c.y < gridSize.y;

    Vector2Int WorldToCell(Vector3 world)
        => new(Mathf.RoundToInt(world.x / Mathf.Max(0.0001f, cellSize)), Mathf.RoundToInt(world.z / Mathf.Max(0.0001f, cellSize)));

    Vector3 CellToWorld(Vector2Int cell, float y)
        => new(cell.x * cellSize, y, cell.y * cellSize);

    static List<Vector2Int> GetRingCells(Vector2Int center, int D)
    {
        var list = new List<Vector2Int>();
        for (int dx = -D; dx <= D; dx++)
        {
            int dz = D - Mathf.Abs(dx);
            list.Add(new Vector2Int(center.x + dx, center.y + dz));
        }
        return list;
    }

    static List<Vector2Int> BuildShortestRandomManhattanPath(Vector2Int start, Vector2Int goal)
    {
        int len = Mathf.Abs(start.x - goal.x) + Mathf.Abs(start.y - goal.y) + 1;
        var path = new List<Vector2Int>(len);

        var cur = start;
        path.Add(cur);

        // Safety: on ne devrait jamais dépasser cette limite, mais ça évite tout blocage éditeur.
        int maxSteps = Mathf.Max(1, len * 4);
        int steps = 0;

        while (cur != goal && steps < maxSteps)
        {
            steps++;

            bool canMoveX = cur.x != goal.x;
            bool canMoveY = cur.y != goal.y;

            // Choix aléatoire mais toujours dans la direction du but.
            if (canMoveX && (!canMoveY || UnityEngine.Random.value < 0.5f))
                cur.x += Math.Sign(goal.x - cur.x);
            else if (canMoveY)
                cur.y += Math.Sign(goal.y - cur.y);

            path.Add(cur);
        }

        if (cur != goal)
            Debug.LogWarning("[MapGenerator] BuildShortestRandomManhattanPath: limite de sécurité atteinte (chemin tronqué).");

        return path;
    }

    static GameObject InstantiateEditor(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, pos, rot, parent);
        return go;
    }
}
