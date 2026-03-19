using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    [Serializable]
    class PendingTrialRequest
    {
        public TrialResponseRow row;
        public Action<string> onSuccess;
        public Action<string> onError;
    }

    [Serializable]
    class QuestionnairePatchPayload
    {
        public string q1_text;
        public string q1_response;
        public string q2_text;
        public string q2_response;
        public string q3_text;
        public string q3_response;
    }

    class PendingQuestionnairePatch
    {
        public QuestionnairePatchPayload payload;
        public Action onSuccess;
        public Action<string> onError;
    }

    public static ApiClient Instance { get; private set; }

    [Header("Config")]
    public string supabaseUrl;
    public string supabaseAnonKey;
    [SerializeField] private string _sessionConfigPath = "api/sessions";
    [SerializeField] private string _trialResponsesPath = "api/trial-responses";
    [SerializeField] private int _maxImmediateRetries = 3;
    [SerializeField] private float _retryDelaySeconds = 1f;

    readonly Queue<PendingTrialRequest> _pendingTrialRequests = new();
    readonly Dictionary<string, string> _storedTrialIdsByKey = new();
    readonly Dictionary<string, PendingQuestionnairePatch> _pendingQuestionnairePatches = new();

    bool _isProcessingTrialQueue;
    bool _isFlushingQuestionnairePatches;

    public event Action<TrialResponseRow, string> OnTrialResponseStored;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void FetchSessionConfig(string sessionId, Action<SessionConfig> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            onError?.Invoke("sessionId vide");
            return;
        }

        StartCoroutine(FetchSessionConfigCoroutine(sessionId, onSuccess, onError));
    }

    public void SendTrialResponse(TrialResponseRow row, Action<string> onSuccess, Action<string> onError)
    {
        if (row == null)
        {
            onError?.Invoke("TrialResponseRow null");
            return;
        }

        _pendingTrialRequests.Enqueue(new PendingTrialRequest
        {
            row = row,
            onSuccess = onSuccess,
            onError = onError
        });

        RetryPendingTrialUploads();
    }

    public void PatchQuestionnaireResponses(
        string trialResponseId,
        string q1Text,
        string q1Response,
        string q2Text,
        string q2Response,
        string q3Text,
        string q3Response,
        Action onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(trialResponseId))
        {
            onError?.Invoke("trialResponseId vide");
            return;
        }

        var payload = new QuestionnairePatchPayload
        {
            q1_text = q1Text,
            q1_response = q1Response,
            q2_text = q2Text,
            q2_response = q2Response,
            q3_text = q3Text,
            q3_response = q3Response
        };

        StartCoroutine(PatchQuestionnaireCoroutine(trialResponseId, payload, onSuccess, onError));
    }

    public void QueueQuestionnairePatchForTrial(
        string participantId,
        int blockIndex,
        int trialIndex,
        IReadOnlyList<QuestionResponse> responses,
        Action onSuccess,
        Action<string> onError)
    {
        var tempRow = new TrialResponseRow();
        FlowSerializationUtility.ApplyQuestionnaireResponses(tempRow, responses);

        string key = BuildTrialKey(participantId, blockIndex, trialIndex);
        _pendingQuestionnairePatches[key] = new PendingQuestionnairePatch
        {
            payload = new QuestionnairePatchPayload
            {
                q1_text = tempRow.q1_text,
                q1_response = tempRow.q1_response,
                q2_text = tempRow.q2_text,
                q2_response = tempRow.q2_response,
                q3_text = tempRow.q3_text,
                q3_response = tempRow.q3_response
            },
            onSuccess = onSuccess,
            onError = onError
        };

        RetryPendingTrialUploads();
        FlushPendingQuestionnairePatches();
    }

    public void RetryPendingTrialUploads()
    {
        if (_isProcessingTrialQueue || _pendingTrialRequests.Count == 0)
            return;

        StartCoroutine(ProcessPendingTrialRequests());
    }

    public void CompleteSession(string participantId)
    {
        RetryPendingTrialUploads();
        FlushPendingQuestionnairePatches();
        Debug.Log($"[ApiClient] Session complete pour participant={participantId}");
    }

    IEnumerator FetchSessionConfigCoroutine(string sessionId, Action<SessionConfig> onSuccess, Action<string> onError)
    {
        string url = CombineUrl(supabaseUrl, _sessionConfigPath, sessionId);
        Debug.Log($"[ApiClient] Fetching session config from {url}");
        using var request = UnityWebRequest.Get(url);
        ApplyCommonHeaders(request);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("FetchSessionConfig", request));
            yield break;
        }

        SessionConfig config = JsonUtility.FromJson<SessionConfig>(request.downloadHandler.text);
        if (config == null)
        {
            onError?.Invoke("JSON de SessionConfig invalide");
            yield break;
        }

        onSuccess?.Invoke(config);
    }

    IEnumerator ProcessPendingTrialRequests()
    {
        _isProcessingTrialQueue = true;

        while (_pendingTrialRequests.Count > 0)
        {
            PendingTrialRequest request = _pendingTrialRequests.Peek();
            bool success = false;
            string responseText = null;
            string errorMessage = null;

            for (int attempt = 1; attempt <= Mathf.Max(1, _maxImmediateRetries); attempt++)
            {
                yield return SendTrialResponseCoroutine(
                    request.row,
                    text =>
                    {
                        success = true;
                        responseText = text;
                    },
                    error => errorMessage = error);

                if (success)
                    break;

                if (attempt < _maxImmediateRetries)
                    yield return new WaitForSeconds(_retryDelaySeconds);
            }

            if (!success)
            {
                request.onError?.Invoke(errorMessage ?? "Echec envoi trial");
                break;
            }

            _pendingTrialRequests.Dequeue();

            string rowId = ExtractIdFromResponse(responseText);
            string trialKey = BuildTrialKey(request.row);
            if (!string.IsNullOrWhiteSpace(rowId))
                _storedTrialIdsByKey[trialKey] = rowId;

            request.onSuccess?.Invoke(rowId);
            OnTrialResponseStored?.Invoke(request.row, rowId);

            if (_pendingQuestionnairePatches.ContainsKey(trialKey))
                FlushPendingQuestionnairePatches();
        }

        _isProcessingTrialQueue = false;
    }

    IEnumerator SendTrialResponseCoroutine(TrialResponseRow row, Action<string> onSuccess, Action<string> onError)
    {
        string url = CombineUrl(supabaseUrl, _trialResponsesPath);
        string payload = JsonUtility.ToJson(row);

        using var request = BuildJsonRequest(url, "POST", payload);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("SendTrialResponse", request));
            yield break;
        }

        onSuccess?.Invoke(request.downloadHandler.text);
    }

    IEnumerator PatchQuestionnaireCoroutine(
        string trialResponseId,
        QuestionnairePatchPayload payload,
        Action onSuccess,
        Action<string> onError)
    {
        string url = CombineUrl(supabaseUrl, _trialResponsesPath, trialResponseId);
        string body = JsonUtility.ToJson(payload);

        using var request = BuildJsonRequest(url, "PATCH", body);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("PatchQuestionnaireResponses", request));
            yield break;
        }

        onSuccess?.Invoke();
    }

    void FlushPendingQuestionnairePatches()
    {
        if (_isFlushingQuestionnairePatches || _pendingQuestionnairePatches.Count == 0)
            return;

        StartCoroutine(FlushPendingQuestionnairePatchesCoroutine());
    }

    IEnumerator FlushPendingQuestionnairePatchesCoroutine()
    {
        _isFlushingQuestionnairePatches = true;

        var keys = new List<string>(_pendingQuestionnairePatches.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (!_pendingQuestionnairePatches.TryGetValue(key, out var pendingPatch))
                continue;

            if (!_storedTrialIdsByKey.TryGetValue(key, out var rowId) || string.IsNullOrWhiteSpace(rowId))
                continue;

            bool success = false;
            string errorMessage = null;

            yield return PatchQuestionnaireCoroutine(
                rowId,
                pendingPatch.payload,
                () => success = true,
                error => errorMessage = error);

            if (success)
            {
                pendingPatch.onSuccess?.Invoke();
                _pendingQuestionnairePatches.Remove(key);
            }
            else
            {
                pendingPatch.onError?.Invoke(errorMessage ?? "Echec patch questionnaire");
            }
        }

        _isFlushingQuestionnairePatches = false;
    }

    UnityWebRequest BuildJsonRequest(string url, string method, string jsonBody)
    {
        byte[] body = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
        var request = new UnityWebRequest(url, method)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Content-Type", "application/json");
        ApplyCommonHeaders(request);
        return request;
    }

    void ApplyCommonHeaders(UnityWebRequest request)
    {
        if (request == null)
            return;

        if (!string.IsNullOrWhiteSpace(supabaseAnonKey))
        {
            request.SetRequestHeader("apikey", supabaseAnonKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseAnonKey}");
        }
    }

    static string BuildTrialKey(TrialResponseRow row)
    {
        if (row == null)
            return string.Empty;

        return BuildTrialKey(row.participant_id, row.block_index, row.trial_index);
    }

    static string BuildTrialKey(string participantId, int blockIndex, int trialIndex)
    {
        return $"{participantId}|{blockIndex}|{trialIndex}";
    }

    static string CombineUrl(string root, params string[] parts)
    {
        string current = (root ?? string.Empty).TrimEnd('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
                continue;

            current += "/" + parts[i].Trim('/');
        }

        return current;
    }

    static string ExtractIdFromResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return null;

        Match match = Regex.Match(responseText, "\"id\"\\s*:\\s*\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    static string BuildRequestError(string context, UnityWebRequest request)
    {
        string body = request?.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        return $"[{context}] {(request != null ? request.responseCode : 0)} {request?.error} {body}".Trim();
    }
}
