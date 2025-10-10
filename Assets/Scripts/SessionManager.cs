using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// -----------------------------
// Gère la création et l’envoi de la session de jeu
// -----------------------------

public class SessionManager : MonoBehaviour
{
    [Header("Refs")]
    public TrialManager trialManager;
    public GameManager gameManager;

    [Header("API")]
    public string apiBaseUrl = "http://localhost:3000";
    public string studyToken = "change-moi-en-long-secret-aleatoire";

    [Header("Session meta (optionnel)")]
    public long randomizationSeed = 0;
    public string buildVersion = "1.0.0";

    IEnumerator Start()
    {
        yield return StartCoroutine(CreateSession());
        // Ici seulement, on démarre le jeu
        gameManager.BeginFirstRound();
    }


    IEnumerator CreateSession()
    {
        var payload = new
        {
            randomization_seed = randomizationSeed,
            build_version = buildVersion,
            consent_given_at = System.DateTime.UtcNow.ToString("o")
        };
        string json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest(apiBaseUrl + "/api/session", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-study-token", studyToken);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<SessionResp>(req.downloadHandler.text);
            trialManager.SetSessionId(resp.game_session_id); // informe TrialManager
            Debug.Log("Session créée: " + resp.game_session_id);
        }
        else
        {
            Debug.LogError($"CreateSession FAIL {req.responseCode} {req.error} {req.downloadHandler.text}");
        }
    }

    [System.Serializable] private struct SessionResp { public string game_session_id; }
}
