using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class LevelRegistry : MonoBehaviour
{
    public static LevelRegistry Instance { get; private set; }

    [Header("Grille (origine 0,0 ; cellSize 1)")]
    public Vector2Int gridSize = new(10, 10);

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

    // Pour le spawn de pièges : simple et clair
    public bool IsFreeForTrap(Vector2Int c)
        => InBounds(c) && !IsReserved(c) && !HasTrap(c);

    // Conversions grille <-> monde (origine 0,0 ; cellSize 1)
    public Vector2Int WorldToCell(Vector3 world)
        => new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));

    public Vector3 CellToWorld(Vector2Int cell, float y = 0f)
        => new(cell.x, y, cell.y);

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
