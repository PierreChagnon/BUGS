using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// -----------------------------
// Gère la création et l’envoi des manches
// -----------------------------
public class TrialManager : MonoBehaviour
{
    [Header("API Settings")]
    public string apiBaseUrl = "http://localhost:3000"; // ton backend local
    public string studyToken = "ensstudytoken"; // même que .env

    [Header("Session Info")]
    public string gameSessionId;  // récupéré après POST /api/session

    private List<TrialData> trials = new(); // stocke les manches locales 
    private TrialData currentTrial;
    private bool isSending = false;
    private string pendingMapConfigJson; // tampon pour la config de la map

    // Permet de définir l’ID de session (appelé par SessionManager)
    public void SetSessionId(string id)
    {
        gameSessionId = id;
        Debug.Log("[TrialManager] Session ID set: " + id);
    }

    // Appelé au début de chaque manche
    public void StartNewTrial(int blockId, int screenId, string screenType, long trialSeed)
    {
        if (string.IsNullOrEmpty(gameSessionId))
        {
            Debug.LogError("[TrialManager] Cannot StartNewTrial: gameSessionId is empty. Ensure session is created first.");
            return;
        }

        // Crée une nouvelle manche et l’ajoute à la liste
        currentTrial = new TrialData(gameSessionId, blockId, screenId, screenType);
        currentTrial.trial_seed = trialSeed;
        trials.Add(currentTrial);

        // Applique la config de map en attente si elle existe
        if (!string.IsNullOrEmpty(pendingMapConfigJson))
        {
            currentTrial.map_config = pendingMapConfigJson;
            Debug.Log("[TrialManager] map_config appliquée depuis le tampon");
            pendingMapConfigJson = null;
        }

        Debug.Log($"Nouvelle manche : block {blockId}, screen {screenId}, seed {trialSeed}");
    }

    // Appelé à chaque déplacement du joueur
    public void RecordMove(Vector2Int position)
    {
        if (currentTrial != null)
        {
            currentTrial.player_path_log.Add(new PlayerStep(position, System.DateTime.UtcNow.ToString("o")));
        }
    }

    // Appelé quand le joueur attrape un nuage
    public void EndCurrentTrial(string playerChoice, bool correct, string trueCloud,
                                int greenBugsCollected, int trapsHit, int steps, bool optimalPathVisible, bool pathIsSuboptimal)
    {
        if (currentTrial == null) return;

        currentTrial.proximal_choice = playerChoice;
        currentTrial.choice_correct = correct;
        currentTrial.true_cloud = trueCloud;
        currentTrial.green_bugs_collected = greenBugsCollected;
        currentTrial.traps_hit = trapsHit;
        currentTrial.steps = steps;
        currentTrial.optimal_path_visible = optimalPathVisible;
        currentTrial.path_is_suboptimal = pathIsSuboptimal;
        // currentTrial.rt_ms = Mathf.RoundToInt(Time.timeSinceLevelLoad * 1000f);
        currentTrial.end_timestamp = System.DateTime.UtcNow.ToString("o");

        Debug.Log($"Manche terminée ! choix={playerChoice}, correct={correct}, trueCloud={trueCloud}, " +
                  $"greenBugs={greenBugsCollected}, pièges={trapsHit}, pas={steps}, optimalPathVisible={optimalPathVisible}, suboptimal={pathIsSuboptimal}");
    }

    // Setter appelé par le Gameplay pour fournir la longueur de chemin optimal
    public void SetOptimalPathLength(int value)
    {
        if (currentTrial == null)
        {
            Debug.LogWarning("[TrialManager] SetOptimalPathLength() ignoré: currentTrial est null");
            return;
        }

        currentTrial.optimal_path_length = value;
    }

    /// <summary>
    /// Enregistre la distance de Manhattan entre le joueur et les nuages (chosenD).
    /// Les chercheurs utilisent cette valeur + steps pour calculer le dépassement.
    /// </summary>
    public void SetCloudDistance(int value)
    {
        if (currentTrial == null)
        {
            Debug.LogWarning("[TrialManager] SetCloudDistance() ignoré: currentTrial est null");
            return;
        }

        currentTrial.cloud_distance = value;
    }

    // Envoi de toutes les manches accumulées vers ton API
    public void SendTrials()
    {
        if (!isSending) StartCoroutine(SendTrialsCoroutine());
    }

    private IEnumerator SendTrialsCoroutine()
    {
        if (trials.Count == 0)
        {
            Debug.Log("Aucune manche à envoyer");
            yield break;
        }

        isSending = true;

        // Sérialisation JSON
        string json = JsonHelper.ToJson(trials.ToArray(), true);
        Debug.Log("Payload JSON:\n" + json);
        Debug.Log($"Envoi de {trials.Count} manches à l’API...");

        // Envoi HTTP POST
        using UnityWebRequest req = new(apiBaseUrl + "/api/trials", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-study-token", studyToken);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Essais envoyés avec succès !");
            trials.Clear(); // vide la mémoire locale
        }
        else
        {
            Debug.LogError($"Erreur d’envoi: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
        }

        isSending = false;
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIG MAP — construction structurée du JSON
    // ══════════════════════════════════════════════════════════════

    [System.Serializable]
    private struct CloudInfo
    {
        public int x;
        public int y;
        public int totalBugs;
        public float greenRatio;
    }

    [System.Serializable]
    private struct MiniMapCfg
    {
        public int gridWidth;
        public int gridHeight;
        public CloudInfo leftCloud;
        public CloudInfo rightCloud;
    }

    /// <summary>
    /// Appelé par GameManager.RegisterClouds pour fournir la config de la map
    /// sous forme structurée. Construit le JSON en interne.
    /// </summary>
    public void SetMapConfig(Vector2Int gridSize,
                             Vector2Int leftCell, int leftBugs, float leftGreenRatio,
                             Vector2Int rightCell, int rightBugs, float rightGreenRatio)
    {
        var cfg = new MiniMapCfg
        {
            gridWidth = gridSize.x,
            gridHeight = gridSize.y,
            leftCloud = new CloudInfo { x = leftCell.x, y = leftCell.y, totalBugs = leftBugs, greenRatio = leftGreenRatio },
            rightCloud = new CloudInfo { x = rightCell.x, y = rightCell.y, totalBugs = rightBugs, greenRatio = rightGreenRatio }
        };
        SetMapConfigJson(JsonUtility.ToJson(cfg));
    }

    // Permet de stocker la config de la map (JSON) dans la manche courante
    public void SetMapConfigJson(string json)
    {
        Debug.Log($"[TrialManager] SetMapConfigJson()");
        if (string.IsNullOrEmpty(json) || json == "{}")
        {
            Debug.LogWarning("[TrialManager] map_config vide, ignorée");
            return;
        }

        if (currentTrial != null)
        {
            Debug.Log($"[TrialManager] currentTrial != null");
            currentTrial.map_config = json;
        }
        else
        {
            Debug.LogWarning($"[TrialManager] currentTrial is null, map_config mise en tampon");
            pendingMapConfigJson = json;
        }
    }

}

// Helper pour sérialiser un tableau en JSON Unity-friendly
public static class JsonHelper
{
    public static string ToJson<T>(T[] array, bool prettyPrint = false)
    {
        Wrapper<T> wrapper = new Wrapper<T> { Items = array };
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [System.Serializable]
    private class Wrapper<T> { public T[] Items; }
}
