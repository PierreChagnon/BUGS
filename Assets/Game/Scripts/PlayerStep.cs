using System;
using UnityEngine;

// -----------------------------
// Classe représentant UN pas du joueur (pour TrialData)
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