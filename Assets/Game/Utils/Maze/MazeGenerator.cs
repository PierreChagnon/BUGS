using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Générateur de maze simple (DFS backtracker).
// - Les cellules walkable forment un "maze" classique.
// - extraOpenings permet de créer des boucles (maze non parfait).
// -----------------------------

public static class MazeGenerator
{
    public static MazeGrid Generate(int width, int height, int seed, int extraOpenings)
    {
        var grid = new MazeGrid(width, height);
        if (width <= 1 || height <= 1)
        {
            grid.Fill(true);
            return grid;
        }

        var rng = new System.Random(seed);
        var start = PickRandomOddCell(width, height, rng);
        if (!start.HasValue)
        {
            grid.Fill(true);
            return grid;
        }

        var visited = new HashSet<Vector2Int>();
        var stack = new Stack<Vector2Int>();

        visited.Add(start.Value);
        stack.Push(start.Value);
        grid.SetWalkable(start.Value, true);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = CollectUnvisitedOddNeighbors(current, width, height, visited);
            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var next = neighbors[rng.Next(neighbors.Count)];
            var between = new Vector2Int((current.x + next.x) / 2, (current.y + next.y) / 2);

            grid.SetWalkable(between, true);
            grid.SetWalkable(next, true);
            visited.Add(next);
            stack.Push(next);
        }

        if (extraOpenings > 0)
            AddExtraOpenings(grid, rng, extraOpenings);

        return grid;
    }

    static Vector2Int? PickRandomOddCell(int width, int height, System.Random rng)
    {
        var odds = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x % 2 == 1 && y % 2 == 1)
                    odds.Add(new Vector2Int(x, y));
            }
        }

        if (odds.Count == 0) return null;
        return odds[rng.Next(odds.Count)];
    }

    static List<Vector2Int> CollectUnvisitedOddNeighbors(Vector2Int c, int width, int height, HashSet<Vector2Int> visited)
    {
        var result = new List<Vector2Int>(4);
        TryAddNeighbor(c.x + 2, c.y, width, height, visited, result);
        TryAddNeighbor(c.x - 2, c.y, width, height, visited, result);
        TryAddNeighbor(c.x, c.y + 2, width, height, visited, result);
        TryAddNeighbor(c.x, c.y - 2, width, height, visited, result);
        return result;
    }

    static void TryAddNeighbor(int x, int y, int width, int height, HashSet<Vector2Int> visited, List<Vector2Int> result)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        var c = new Vector2Int(x, y);
        if (visited.Contains(c)) return;
        result.Add(c);
    }

    static void AddExtraOpenings(MazeGrid grid, System.Random rng, int extraOpenings)
    {
        int attempts = 0;
        int added = 0;
        int maxAttempts = extraOpenings * 8;

        while (added < extraOpenings && attempts < maxAttempts)
        {
            attempts++;
            var c = new Vector2Int(rng.Next(grid.Width), rng.Next(grid.Height));
            if (grid.IsWalkable(c)) continue;
            if (CountWalkableNeighbors(grid, c) < 2) continue;

            grid.SetWalkable(c, true);
            added++;
        }
    }

    static int CountWalkableNeighbors(MazeGrid grid, Vector2Int c)
    {
        int count = 0;
        if (grid.IsWalkable(new Vector2Int(c.x + 1, c.y))) count++;
        if (grid.IsWalkable(new Vector2Int(c.x - 1, c.y))) count++;
        if (grid.IsWalkable(new Vector2Int(c.x, c.y + 1))) count++;
        if (grid.IsWalkable(new Vector2Int(c.x, c.y - 1))) count++;
        return count;
    }
}
