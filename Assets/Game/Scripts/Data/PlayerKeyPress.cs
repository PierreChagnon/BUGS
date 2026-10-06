using System;

// -----------------------------
// Une pression de touche du joueur pour le log du trial courant (player_press_log).
// Même forme que PlayerStep : un objet plat par événement, timestamp ISO UTC.
//   key : étiquette de la touche dans la nomenclature des sets moteur
//         (Z/Q/S/D, T/F/G/H, I/J/K/L), sinon le nom Unity de la touche.
//   set : set moteur auquel la touche appartient (QZD / FTH / JIL),
//         null hors des trois sets.
// -----------------------------

[Serializable]
public class PlayerKeyPress
{
    public string key;
    public string set;
    public string t; // timestamp de la pression

    public PlayerKeyPress(string key, string set, string time)
    {
        this.key = key;
        this.set = set;
        t = time;
    }
}
