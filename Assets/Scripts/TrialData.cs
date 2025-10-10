using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Classe représentant UNE manche
// -----------------------------
[Serializable]
public class TrialData
{
    // Identifiants de session et de manche
    public string game_session_id;   // donné par l’API /api/session
    public int block_id;
    public int screen_id;

    // Infos de contexte
    public string screen_type;       // ex : "forest", "mountain"
    public string timestamp;         // date ISO du début de la manche
    public float base_reward;
    public string advisor_type;

    // Données de carte (simplifiées)
    public string map_config;        // tu pourras la convertir en JSON
    public string true_cloud;
    public int optimal_path_length;

    // Chemin du joueur
    public List<Vector2Int> player_path_log = new();

    // Résultats
    public string proximal_choice;
    public bool choice_correct;

    // Temps de réaction
    // public int rt_ms;

    // Fin de manche
    public string end_timestamp;

    // Constructeur (appelé au début de la manche)
    public TrialData(string sessionId, int block, int screen, string type)
    {
        game_session_id = sessionId;
        block_id = block;
        screen_id = screen;
        screen_type = type;
        timestamp = DateTime.UtcNow.ToString("o");  // ISO 8601 UTC
    }
}
