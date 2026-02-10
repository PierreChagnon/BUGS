using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Génère des couloirs (zones praticables) et transforme le reste en murs.
// Objectif: guider le joueur vers les BugClouds, limiter les cul-de-sac,
// garantir au moins un chemin sans pièges (via BestPath + réservations).
// -----------------------------

[DefaultExecutionOrder(-50)]
public class CorridorWallsGenerator : MonoBehaviour
{
    [Header("Références")]
    public Transform player;
    public LevelRegistry registry; // optionnel: sinon LevelRegistry.Instance

    [Header("Couloirs")]
    [Min(1)]
    public int corridorWidth = 2;

    [Header("Visuel / Mur")]
    [Tooltip("Prefab optionnel de mur. Si null, un Cube est créé.")]
    public GameObject wallPrefab;

    [Tooltip("Material optionnel pour teinter les tiles de mur (tag=Tile).")]
    public Material wallMaterial;

    [Tooltip("Hauteur Y du mur (pivot au centre si Cube).")]
    public float wallY = 0.5f;

    [Tooltip("Hauteur du mur (Cube).")]
    public float wallHeight = 1.0f;

    [Tooltip("Largeur/profondeur du mur (Cube) en unités monde.")]
    public float wallThickness = 1.0f;

    [Tooltip("Détruit les murs enfants existants (utile si on relance en Play sans reload).")]
    public bool clearPreviousChildren = true;

    readonly Dictionary<Vector2Int, GameObject> _tilesByCell = new();

    void Start()
    {
        var reg = registry != null ? registry : LevelRegistry.Instance;
        if (reg == null)
        {
            Debug.LogError("[CorridorWallsGenerator] LevelRegistry manquant dans la scène.");
            return;
        }

        if (clearPreviousChildren)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        CacheTilesByCell();

        var walkable = BuildWalkableCells(reg);
        if (walkable.Count == 0)
        {
            Debug.LogWarning("[CorridorWallsGenerator] Aucun couloir généré (walkable vide).\n" +
                             "Vérifie que BestPath réserve des cases ou que les BugClouds sont bien enregistrés.");
            return;
        }

        // Tout ce qui n'est pas couloir devient un mur.
        int wallsPlaced = 0;
        for (int y = 0; y < reg.gridSize.y; y++)
        {
            for (int x = 0; x < reg.gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (walkable.Contains(c))
                {
                    // Par sécurité: si une case était précédemment marquée mur, on la retire.
                    if (reg.IsWall(c)) reg.UnregisterWall(c);
                    continue;
                }

                reg.RegisterWall(c);
                SpawnWallVisual(c);
                wallsPlaced++;
            }
        }

        Debug.Log($"[CorridorWallsGenerator] Walkable={walkable.Count}  Walls={wallsPlaced}");
    }

    void CacheTilesByCell()
    {
        _tilesByCell.Clear();

        GameObject[] tiles;
        try
        {
            tiles = GameObject.FindGameObjectsWithTag("Tile");
        }
        catch
        {
            // Tag absent -> pas de recoloration des tiles
            return;
        }

        foreach (var t in tiles)
        {
            if (t == null) continue;
            var cell = new Vector2Int(Mathf.RoundToInt(t.transform.position.x), Mathf.RoundToInt(t.transform.position.z));
            if (!_tilesByCell.ContainsKey(cell))
                _tilesByCell.Add(cell, t);
        }
    }

    HashSet<Vector2Int> BuildWalkableCells(LevelRegistry reg)
    {
        // Version ultra minimale + plus organique :
        // - On prend la base (BestPath si présent)
        // - On relie la case du joueur à chaque BugCloud via une marche aléatoire biaisée
        // - On "roughen" un peu les bords
        // - On élargit avec Inflate

        var baseCells = new HashSet<Vector2Int>();

        // 1) Ossature éventuelle via BestPath (si votre système de réservation est actif)
        for (int y = 0; y < reg.gridSize.y; y++)
        {
            for (int x = 0; x < reg.gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (reg.IsOnAnyPath(c))
                    baseCells.Add(c);
            }
        }

        // 2) Start (joueur)
        var start = player != null ? reg.WorldToCell(player.position) : new Vector2Int(0, 0);
        if (!reg.InBounds(start)) start = new Vector2Int(0, 0);
        baseCells.Add(start);

        // 3) Récupérer les cibles (BugClouds + pièges si déjà posés)
        var goals = CollectBugCloudCells(reg);
        goals.UnionWith(CollectTrapCells(reg));

        // 4) Carving organique: start -> chaque objectif
        foreach (var goal in goals)
        {
            baseCells.Add(goal); // garantit au minimum "pas de mur" sur l'objectif
            CarveOrganicPath(start, goal, reg, baseCells);
        }

        // 5) Légère irrégularité des bords (organique sans paramètres)
        baseCells = RoughenEdges(baseCells, reg);

        // 6) Largeur de couloir
        return Inflate(baseCells, corridorWidth, reg);
    }

    static IEnumerable<Vector2Int> Neighbors4(Vector2Int c)
    {
        yield return new Vector2Int(c.x + 1, c.y);
        yield return new Vector2Int(c.x - 1, c.y);
        yield return new Vector2Int(c.x, c.y + 1);
        yield return new Vector2Int(c.x, c.y - 1);
    }

    static HashSet<Vector2Int> CollectBugCloudCells(LevelRegistry reg)
    {
        var result = new HashSet<Vector2Int>();

        // 1) Source principale: registry (plus fiable, pas de Find par défaut)
        for (int y = 0; y < reg.gridSize.y; y++)
        {
            for (int x = 0; x < reg.gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (reg.HasBugCloud(c))
                    result.Add(c);
            }
        }

        if (result.Count > 0)
            return result;

        // 2) Fallback minimal: tag (si le LevelRegistry ne track pas les nuages)
        GameObject[] clouds;
        try
        {
            clouds = GameObject.FindGameObjectsWithTag("BugCloud");
        }
        catch
        {
            return result;
        }

        if (clouds == null) return result;
        foreach (var cloud in clouds)
        {
            if (cloud == null) continue;
            var cell = reg.WorldToCell(cloud.transform.position);
            if (reg.InBounds(cell)) result.Add(cell);
        }

        return result;
    }

    static HashSet<Vector2Int> CollectTrapCells(LevelRegistry reg)
    {
        var result = new HashSet<Vector2Int>();
        for (int y = 0; y < reg.gridSize.y; y++)
        {
            for (int x = 0; x < reg.gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (reg.HasTrap(c))
                    result.Add(c);
            }
        }
        return result;
    }

    static void CarveOrganicPath(Vector2Int start, Vector2Int goal, LevelRegistry reg, HashSet<Vector2Int> into)
    {
        if (!reg.InBounds(start) || !reg.InBounds(goal)) return;

        var cur = start;
        into.Add(cur);
        if (cur == goal) return;

        int manhattan = Mathf.Abs(goal.x - start.x) + Mathf.Abs(goal.y - start.y);
        int maxSteps = Mathf.Clamp(manhattan * 4 + 32, 32, 4096);

        for (int i = 0; i < maxSteps && cur != goal; i++)
        {
            var next = PickOrganicNextStep(cur, goal);
            if (!reg.InBounds(next))
            {
                if (!TryPickAnyInBoundsNeighbor(cur, reg, out next))
                    break;
            }

            cur = next;
            into.Add(cur);
        }

        into.Add(goal);
    }

    static Vector2Int PickOrganicNextStep(Vector2Int cur, Vector2Int goal)
    {
        // 75%: on se rapproche de la cible (mais en choisissant X/Y aléatoirement)
        // 25%: on part dans une direction aléatoire ("organic")
        if (UnityEngine.Random.value < 0.75f)
        {
            int dx = goal.x - cur.x;
            int dy = goal.y - cur.y;

            bool moveX;
            if (dx == 0) moveX = false;
            else if (dy == 0) moveX = true;
            else moveX = UnityEngine.Random.value < 0.5f;

            if (moveX)
                return new Vector2Int(cur.x + Math.Sign(dx), cur.y);
            return new Vector2Int(cur.x, cur.y + Math.Sign(dy));
        }

        // Random step
        int r = UnityEngine.Random.Range(0, 4);
        return r switch
        {
            0 => new Vector2Int(cur.x + 1, cur.y),
            1 => new Vector2Int(cur.x - 1, cur.y),
            2 => new Vector2Int(cur.x, cur.y + 1),
            _ => new Vector2Int(cur.x, cur.y - 1),
        };
    }

    static bool TryPickAnyInBoundsNeighbor(Vector2Int cur, LevelRegistry reg, out Vector2Int next)
    {
        // On mélange un peu l'ordre pour éviter les biais visibles
        var dirs = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            int j = UnityEngine.Random.Range(i, dirs.Length);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        foreach (var d in dirs)
        {
            var c = cur + d;
            if (reg.InBounds(c))
            {
                next = c;
                return true;
            }
        }

        next = default;
        return false;
    }

    static HashSet<Vector2Int> RoughenEdges(HashSet<Vector2Int> cells, LevelRegistry reg)
    {
        // Très léger “flou” pour casser les bords carrés.
        // Gardé volontairement minimal, sans paramètre.
        const float addChance = 0.22f;

        var outSet = new HashSet<Vector2Int>(cells);
        foreach (var c in cells)
        {
            foreach (var n in Neighbors4(c))
            {
                if (!reg.InBounds(n)) continue;
                if (UnityEngine.Random.value < addChance)
                    outSet.Add(n);
            }
        }
        return outSet;
    }

    static HashSet<Vector2Int> Inflate(HashSet<Vector2Int> cells, int width, LevelRegistry reg)
    {
        if (cells == null) return new HashSet<Vector2Int>();
        if (width <= 1) return new HashSet<Vector2Int>(cells);

        // Largeur exacte (supporte pair/impair). Exemple: width=2 => offsets [0..1] (décalé), width=3 => [-1..1].
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
                    if (reg.InBounds(cc)) outSet.Add(cc);
                }
            }
        }
        return outSet;
    }

    void SpawnWallVisual(Vector2Int c)
    {
        if (wallPrefab != null)
        {
            var pos = new Vector3(c.x, wallY, c.y);
            Instantiate(wallPrefab, pos, Quaternion.identity, transform);
            return;
        }

        // Fallback simple: cube
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Wall ({c.x},{c.y})";
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(c.x, wallY, c.y);
        go.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);
    }
}
