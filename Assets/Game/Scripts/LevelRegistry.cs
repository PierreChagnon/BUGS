using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Gère l’état de la grille (nuages, pièges, chemins…)
// -----------------------------

[DefaultExecutionOrder(-300)]
public class LevelRegistry : MonoBehaviour
{
    public static LevelRegistry Instance { get; private set; }

    [Header("Grille")]
    public Vector2Int gridSize = new(10, 10);

    [Header("Monde ↔ Grille")]
    [Tooltip("Taille d'une case en unités monde.")]
    [Min(0.0001f)]
    public float cellSize = 1f;

    [Tooltip("Position monde (X,Z) de la case (0,0).")]
    public Vector3 originWorld = Vector3.zero;

    [HideInInspector]
    public int optimalPathLength;


    // --- Etat par case (flags) ---
    [Flags]
    public enum CellFlags
    {
        None = 0,
        BugCloud = 1 << 0,  // un nuage occupe la case
        Trap = 1 << 1,  // un piège est sur la case
        PathLeft = 1 << 2,  // la case appartient au chemin gauche
        PathRight = 1 << 3,  // la case appartient au chemin droit
        Reserved = 1 << 4,  // case réservée (exclure spawn piège)
        Visited = 1 << 5,  // le joueur a déjà marché ici
        Wall = 1 << 6,  // case bloquante (mur / non-praticable)
    }

    // Store “truth” here
    readonly Dictionary<Vector2Int, CellFlags> _cells = new();

    // Optionnel : HUD / debug peut s’abonner
    public event Action<Vector2Int, CellFlags> OnCellChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // --- API publique (utilisable par tes autres scripts) ---

    public bool InBounds(Vector2Int c)
        => c.x >= 0 && c.x < gridSize.x && c.y >= 0 && c.y < gridSize.y;

    public CellFlags GetFlags(Vector2Int c)
        => _cells.TryGetValue(c, out var f) ? f : CellFlags.None;

    public void MarkVisited(Vector2Int c) => AddFlags(c, CellFlags.Visited);

    public void RegisterBugCloud(Vector2Int c)
    {
        // Un nuage “réserve” naturellement sa case (pas de piège dessus)
        AddFlags(c, CellFlags.BugCloud | CellFlags.Reserved);
    }
    public void UnregisterBugCloud(Vector2Int c)
    {
        var f = GetFlags(c);
        f &= ~CellFlags.BugCloud; // retire seulement BugCloud
                                  // On ne retire Reserved que si la case n'appartient à aucun chemin
        if ((f & (CellFlags.PathLeft | CellFlags.PathRight)) == 0)
            f &= ~CellFlags.Reserved;
        SetFlags(c, f);
    }

    public bool RegisterTrap(Vector2Int c)
    {
        if (!InBounds(c)) return false;
        if (IsReserved(c)) return false;        // refuse si réservé (nuages / paths)
        if (HasTrap(c)) return false;           // évite doublon
        AddFlags(c, CellFlags.Trap);
        return true;
    }
    public void RegisterOptimalPath(List<Vector2Int> path)
    {
        optimalPathLength = path != null ? path.Count : 0;
        Debug.Log($"[LevelRegistry] Chemin optimal enregistré ({optimalPathLength} cases).");
    }

    public void UnregisterTrap(Vector2Int c) => RemoveFlags(c, CellFlags.Trap);

    public void ReservePathLeft(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells) AddFlags(c, CellFlags.PathLeft | CellFlags.Reserved);
    }
    public void ReservePathRight(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells) AddFlags(c, CellFlags.PathRight | CellFlags.Reserved);
    }
    public void ClearPathReservations()
    {
        // Retire PathLeft/PathRight/Reserved (mais laisse BugCloud/Trap/Visited intacts)
        var keys = new List<Vector2Int>(_cells.Keys);
        foreach (var k in keys)
        {
            var f = _cells[k];
            var nf = f & ~(CellFlags.PathLeft | CellFlags.PathRight | CellFlags.Reserved);
            if ((f & (CellFlags.PathLeft | CellFlags.PathRight)) != 0)
                SetFlags(k, nf);
        }
    }

    // Helpers lecture
    public bool HasBugCloud(Vector2Int c) => (GetFlags(c) & CellFlags.BugCloud) != 0;
    public bool HasTrap(Vector2Int c) => (GetFlags(c) & CellFlags.Trap) != 0;
    public bool IsOnAnyPath(Vector2Int c) => (GetFlags(c) & (CellFlags.PathLeft | CellFlags.PathRight)) != 0;
    public bool IsReserved(Vector2Int c) => (GetFlags(c) & CellFlags.Reserved) != 0;
    public bool IsVisited(Vector2Int c) => (GetFlags(c) & CellFlags.Visited) != 0;
    public bool IsWall(Vector2Int c) => (GetFlags(c) & CellFlags.Wall) != 0;

    // Murs : bloquent le déplacement et interdisent le spawn de pièges.
    public void RegisterWall(Vector2Int c)
    {
        if (!InBounds(c)) return;
        AddFlags(c, CellFlags.Wall);
    }
    public void UnregisterWall(Vector2Int c) => RemoveFlags(c, CellFlags.Wall);

    // Déplacement : une case est praticable si elle est dans la grille et non-mur.
    public bool IsWalkable(Vector2Int c) => InBounds(c) && !IsWall(c);

    // Pour le spawn de pièges : simple et clair
    public bool IsFreeForTrap(Vector2Int c)
        => InBounds(c) && !IsReserved(c) && !IsWall(c) && !HasTrap(c);

    // Conversions grille <-> monde (utilise originWorld + cellSize)
    public Vector2Int WorldToCell(Vector3 world)
    {
        float size = Mathf.Max(0.0001f, cellSize);
        float x = (world.x - originWorld.x) / size;
        float z = (world.z - originWorld.z) / size;
        return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(z));
    }

    public Vector3 CellToWorld(Vector2Int cell, float y = 0f)
    {
        float size = Mathf.Max(0.0001f, cellSize);
        return new Vector3(originWorld.x + cell.x * size, y, originWorld.z + cell.y * size);
    }

    public Vector3 SnapWorldToCellCenter(Vector3 world)
    {
        var cell = WorldToCell(world);
        return CellToWorld(cell, world.y);
    }

    // --- internes ---
    void AddFlags(Vector2Int c, CellFlags add)
    {
        var f = GetFlags(c);
        var nf = f | add;
        if (nf != f) SetFlags(c, nf);
    }
    void RemoveFlags(Vector2Int c, CellFlags rem)
    {
        var f = GetFlags(c);
        var nf = f & ~rem;
        if (nf != f) SetFlags(c, nf);
    }
    void SetFlags(Vector2Int c, CellFlags f)
    {
        _cells[c] = f;
        OnCellChanged?.Invoke(c, f);
    }
}
