using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class LevelRegistryHUD : MonoBehaviour
{
    [Header("Références")]
    public Transform player;
    public TMP_Text output;

    [Header("Options d’affichage")]
    [Tooltip("Scanner la grille et afficher toutes les cases non vides.")]
    public bool scanGridEachFrame = true;

    [Tooltip("Afficher la liste des cases visitées (en temps réel).")]
    public bool showVisitedPath = true;

    [Tooltip("Nombre max de cases visitées à afficher (depuis le début).")]
    public int maxVisitedShown = 200;

    [Tooltip("Nombre max de lignes dans le log des changements récents.")]
    public int maxLogLines = 40;

    // Log des changements (comme avant)
    private readonly Queue<string> _recent = new();

    // Chemin visité (ordre + set pour éviter les doublons)
    private readonly List<Vector2Int> _visitedOrder = new();
    private Vector2Int? _lastCell = null;   // dernière cellule enregistrée

    void OnEnable()
    {
        if (LevelRegistry.Instance != null)
            LevelRegistry.Instance.OnCellChanged += OnCellChanged;
    }

    void OnDisable()
    {
        if (LevelRegistry.Instance != null)
            LevelRegistry.Instance.OnCellChanged -= OnCellChanged;
    }

    void OnCellChanged(Vector2Int cell, LevelRegistry.CellFlags flags)
    {
        _recent.Enqueue($"{cell}: {Pretty(flags)}");
        while (_recent.Count > maxLogLines) _recent.Dequeue();
    }

    void Update()
    {
        // Mise à jour temps réel de la liste "Visited"
        var reg = LevelRegistry.Instance;
        if (showVisitedPath && player != null && reg != null)
        {
            var cell = reg.WorldToCell(player.position);
            if (_lastCell != cell)     // on ajoute à chaque changement de case
            {
                _visitedOrder.Add(cell);
                _lastCell = cell;
            }
        }

        RefreshUI();
    }

    void RefreshUI()
    {
        if (output == null) return;

        var sb = new StringBuilder(2048);
        var reg = LevelRegistry.Instance;

        // 1) Ligne "joueur" (comme avant)
        if (player != null && reg != null)
        {
            var cell = reg.WorldToCell(player.position);
            var flags = reg.GetFlags(cell);
            sb.AppendLine($"Player @ {cell}   Flags: {Pretty(flags)}");
        }

        // 2) Liste “Visited” en temps réel (nouveau)
        if (showVisitedPath && _visitedOrder.Count > 0)
        {
            sb.AppendLine();
            sb.Append("Visited (").Append(_visitedOrder.Count).Append("): ");
            int start = Mathf.Max(0, _visitedOrder.Count - maxVisitedShown);
            for (int i = start; i < _visitedOrder.Count; i++)
            {
                sb.Append(_visitedOrder[i]);
                if (i < _visitedOrder.Count - 1) sb.Append(" -> ");
            }
            sb.AppendLine();
        }

        // 3) Snapshot des cases non vides (comme avant)
        if (scanGridEachFrame && reg != null)
        {
            sb.AppendLine();
            sb.AppendLine("Cells (non vides):");
            for (int z = 0; z < reg.gridSize.y; z++)
            {
                for (int x = 0; x < reg.gridSize.x; x++)
                {
                    var c = new Vector2Int(x, z);
                    var f = reg.GetFlags(c);
                    if (f != LevelRegistry.CellFlags.None)
                        sb.AppendLine($"{c}: {Pretty(f)}");
                }
            }
        }

        // 4) Derniers changements (comme avant)
        if (_recent.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recent changes:");
            foreach (var line in _recent)
                sb.AppendLine(line);
        }

        output.text = sb.ToString();
    }

    static string Pretty(LevelRegistry.CellFlags f)
        => f == LevelRegistry.CellFlags.None ? "None" : f.ToString();
}
