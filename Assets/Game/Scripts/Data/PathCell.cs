using System;
using UnityEngine;

// -----------------------------
// Classe représentant une case du chemin affiché par l'advisor,
// pour l'archivage du trial courant (advisor_path_config).
// -----------------------------

[Serializable]
public class PathCell
{
    public int x;
    public int y;

    public PathCell(Vector2Int pos)
    {
        x = pos.x;
        y = pos.y;
    }
}
