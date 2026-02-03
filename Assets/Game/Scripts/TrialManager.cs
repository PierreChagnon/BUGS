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
    public void StartNewTrial(int blockId, int screenId, string screenType)
    {
        if (string.IsNullOrEmpty(gameSessionId))
        {
            Debug.LogError("[TrialManager] Cannot StartNewTrial: gameSessionId is empty. Ensure session is created first.");
            return;
        }

        // Crée une nouvelle manche et l’ajoute à la liste
        currentTrial = new TrialData(gameSessionId, blockId, screenId, screenType);
        trials.Add(currentTrial);

        // Applique la config de map en attente si elle existe
        if (!string.IsNullOrEmpty(pendingMapConfigJson))
        {
            currentTrial.map_config = pendingMapConfigJson;
            Debug.Log("[TrialManager] map_config appliquée depuis le tampon");
            pendingMapConfigJson = null;
        }

        Debug.Log($"Nouvelle manche : block {blockId}, screen {screenId}");
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
    public void EndCurrentTrial(string playerChoice, bool correct)
    {
        if (currentTrial == null) return;

        currentTrial.proximal_choice = playerChoice;
        currentTrial.choice_correct = correct;
        // currentTrial.rt_ms = Mathf.RoundToInt(Time.timeSinceLevelLoad * 1000f);
        currentTrial.end_timestamp = System.DateTime.UtcNow.ToString("o");

        Debug.Log("Manche terminée !");
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
