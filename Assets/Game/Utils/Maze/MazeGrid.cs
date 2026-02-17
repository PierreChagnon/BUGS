using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Grille simple pour un maze (walkable ou mur par case).
// -----------------------------

public class MazeGrid
{
    public int Width { get; }
    public int Height { get; }

    readonly bool[,] _walkable;

    public MazeGrid(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        _walkable = new bool[Width, Height];
    }

    public bool InBounds(Vector2Int c)
        => c.x >= 0 && c.x < Width && c.y >= 0 && c.y < Height;

    public bool IsWalkable(Vector2Int c)
        => InBounds(c) && _walkable[c.x, c.y];

    public void SetWalkable(Vector2Int c, bool value)
    {
        if (!InBounds(c)) return;
        _walkable[c.x, c.y] = value;
    }

    public void Fill(bool value)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _walkable[x, y] = value;
    }

    public IEnumerable<Vector2Int> AllCells()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return new Vector2Int(x, y);
    }
}
