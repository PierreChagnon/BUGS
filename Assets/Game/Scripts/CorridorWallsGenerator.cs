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

    [Tooltip("Ajoute quelques connexions supplémentaires pour réduire les cul-de-sac.")]
    [Range(0, 100)]
    public int extraConnections = 10;

    [Tooltip("Si aucun chemin n'est réservé (BestPath absent), on connecte quand même le joueur aux nuages.")]
    public bool fallbackConnectToClouds = true;

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

        CacheTilesByCell(reg);

        var walkable = BuildWalkableCells(reg);
        if (walkable.Count == 0 && fallbackConnectToClouds)
            walkable = BuildFallbackWalkable(reg);

        if (walkable.Count == 0)
        {
            Debug.LogWarning("[CorridorWallsGenerator] Aucun couloir généré (walkable vide).\n" +
                             "Vérifie que BestPath réserve des cases ou active fallbackConnectToClouds.");
            return;
        }

        AddExtraConnections(reg, walkable);

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
                PaintTileAsWall(c);
                SpawnWallVisual(reg, c);
                wallsPlaced++;
            }
        }

        Debug.Log($"[CorridorWallsGenerator] Walkable={walkable.Count}  Walls={wallsPlaced}");
    }

    void CacheTilesByCell(LevelRegistry reg)
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
            var cell = reg.WorldToCell(t.transform.position);
            if (!_tilesByCell.ContainsKey(cell))
                _tilesByCell.Add(cell, t);
        }
    }

    HashSet<Vector2Int> BuildWalkableCells(LevelRegistry reg)
    {
        var baseCells = new HashSet<Vector2Int>();

        // 1) Les chemins réservés (BestPath) définissent l'ossature des couloirs.
        for (int y = 0; y < reg.gridSize.y; y++)
        {
            for (int x = 0; x < reg.gridSize.x; x++)
            {
                var c = new Vector2Int(x, y);
                if (reg.IsOnAnyPath(c) || reg.HasBugCloud(c))
                    baseCells.Add(c);
            }
        }

        // 2) Ajouter la case du joueur pour éviter de l'enfermer.
        if (player != null)
            baseCells.Add(reg.WorldToCell(player.position));

        // 3) Élargissement pour obtenir des couloirs de largeur > 1.
        return Inflate(baseCells, corridorWidth, reg);
    }

    HashSet<Vector2Int> BuildFallbackWalkable(LevelRegistry reg)
    {
        var result = new HashSet<Vector2Int>();

        var start = player != null ? reg.WorldToCell(player.position) : new Vector2Int(0, 0);
        if (!reg.InBounds(start)) start = new Vector2Int(0, 0);
        result.Add(start);

        // Essayer de récupérer les nuages par tag.
        var clouds = GameObject.FindGameObjectsWithTag("BugCloud");
        if (clouds == null || clouds.Length == 0) return Inflate(result, corridorWidth, reg);

        foreach (var cloud in clouds)
        {
            if (cloud == null) continue;
            var goal = reg.WorldToCell(cloud.transform.position);
            CarveLPath(start, goal, result);
        }

        return Inflate(result, corridorWidth, reg);
    }

    void AddExtraConnections(LevelRegistry reg, HashSet<Vector2Int> walkable)
    {
        if (extraConnections <= 0) return;

        // Collecter des cul-de-sac (degré <= 1) puis tenter de les connecter.
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

            // Chercher une cible walkable proche (hors voisins immédiats).
            if (!TryFindNearbyTarget(reg, walkable, from, out var to))
                continue;

            var carved = new HashSet<Vector2Int>();
            CarveLPath(from, to, carved);
            var inflated = Inflate(carved, corridorWidth, reg);
            foreach (var c in inflated) walkable.Add(c);
            added++;
        }

        if (added > 0)
            Debug.Log($"[CorridorWallsGenerator] Extra connections added: {added}");
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
        // L simple (avec un coude aléatoire) - pas un A* (suffisant pour une grille sans obstacles en fallback).
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

    bool TryFindNearbyTarget(LevelRegistry reg, HashSet<Vector2Int> walkable, Vector2Int from, out Vector2Int to)
    {
        // Recherche brute: on regarde dans un carré croissant et on prend la 1ère case walkable différente.
        for (int r = 2; r <= 6; r++)
        {
            var candidates = new List<Vector2Int>();
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    var c = new Vector2Int(from.x + dx, from.y + dy);
                    if (!reg.InBounds(c)) continue;
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

    void SpawnWallVisual(LevelRegistry reg, Vector2Int c)
    {
        if (wallPrefab != null)
        {
            var pos = reg.CellToWorld(c, wallY);
            Instantiate(wallPrefab, pos, Quaternion.identity, transform);
            return;
        }

        // Fallback simple: cube
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Wall ({c.x},{c.y})";
        go.transform.SetParent(transform, false);
        go.transform.position = reg.CellToWorld(c, wallY);
        go.transform.localScale = new Vector3(wallThickness, wallHeight, wallThickness);

        if (wallMaterial != null)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = wallMaterial;
        }
    }
}
