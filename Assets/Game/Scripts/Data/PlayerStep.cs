using System;
using UnityEngine;

// -----------------------------
// Classe représentant un pas du joueur pour le log du trial courant.
// -----------------------------

[Serializable]
public class PlayerStep
{
    public int x;
    public int y;
    public string t; // timestamp du step

    public PlayerStep(Vector2Int pos, string time)
    {
        x = pos.x;
        y = pos.y;
        t = time;
    }
}
