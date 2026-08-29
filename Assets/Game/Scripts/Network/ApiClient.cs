using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
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
        public string human_likeness_question;
    }

    class PendingQuestionnairePatch
    {
        public QuestionnairePatchPayload payload;
        public Action onSuccess;
        public Action<string> onError;
    }

    [Serializable]
    class IdResponse
    {
        public string id;
    }

    // Lecture et ecriture JSON partagent la meme strategie (Newtonsoft) : les
    // champs null sont omis du payload, sauf ceux marques
    // [JsonProperty(NullValueHandling = Include)] dans les DTO.
    static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Culture = CultureInfo.InvariantCulture
    };

    public static ApiClient Instance { get; private set; }

    [Header("Config")]
    public string backendRootUrl;
    public string supabaseAnonKey;
    [SerializeField] private string _sessionConfigPath = "api/sessions";
    [SerializeField] private string _trialResponsesPath = "api/trial-responses";
    [SerializeField] private string _participantNotesPath = "api/participant-notes";
    [SerializeField] private int _maxImmediateRetries = 3;
    [SerializeField] private float _retryDelaySeconds = 1f;

    readonly Queue<PendingTrialRequest> _pendingTrialRequests = new();
    readonly Dictionary<string, string> _storedTrialIdsByKey = new();
    readonly Dictionary<string, PendingQuestionnairePatch> _pendingQuestionnairePatches = new();

    // Handles des deux boucles d'envoi : un handle non-null = boucle en cours
    // (empeche tout chevauchement de coroutines sur la meme file).
    Coroutine _trialQueueCoroutine;
    Coroutine _questionnaireFlushCoroutine;

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

    // Telecharge une image depuis une URL publique (bucket Supabase, sans auth) et la convertit en Sprite.
    public void FetchImage(string imageUrl, Action<Sprite> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            onError?.Invoke("imageUrl vide");
            return;
        }

        StartCoroutine(FetchImageCoroutine(imageUrl, onSuccess, onError));
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

        var payload = new QuestionnairePatchPayload
        {
            human_likeness_question = tempRow.human_likeness_question
        };

        if (string.IsNullOrWhiteSpace(payload.human_likeness_question))
            return;

        QueueQuestionnairePatchPayloadForTrial(participantId, blockIndex, trialIndex, payload, onSuccess, onError);
    }

    public void QueueHumanLikenessPatchForBlock(
        string participantId,
        int blockIndex,
        int trialCount,
        string humanLikenessQuestion,
        Action onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(humanLikenessQuestion))
            return;

        int pendingCount = Mathf.Max(1, trialCount);
        int successCount = 0;
        bool hasError = false;

        for (int trialIndex = 1; trialIndex <= pendingCount; trialIndex++)
        {
            QueueQuestionnairePatchPayloadForTrial(
                participantId,
                blockIndex,
                trialIndex,
                new QuestionnairePatchPayload
                {
                    human_likeness_question = humanLikenessQuestion
                },
                () =>
                {
                    successCount++;
                    if (successCount >= pendingCount && !hasError)
                        onSuccess?.Invoke();
                },
                error =>
                {
                    hasError = true;
                    onError?.Invoke(error);
                });
        }
    }

    void QueueQuestionnairePatchPayloadForTrial(
        string participantId,
        int blockIndex,
        int trialIndex,
        QuestionnairePatchPayload payload,
        Action onSuccess,
        Action<string> onError)
    {
        string key = BuildTrialKey(participantId, blockIndex, trialIndex);
        if (_pendingQuestionnairePatches.TryGetValue(key, out var existingPatch))
        {
            MergeQuestionnairePatchPayload(existingPatch.payload, payload);
            existingPatch.onSuccess += onSuccess;
            existingPatch.onError += onError;
        }
        else
        {
            _pendingQuestionnairePatches[key] = new PendingQuestionnairePatch
            {
                payload = payload,
                onSuccess = onSuccess,
                onError = onError
            };
        }

        RetryPendingTrialUploads();
        FlushPendingQuestionnairePatches();
    }

    public void RetryPendingTrialUploads()
    {
        if (_trialQueueCoroutine != null || _pendingTrialRequests.Count == 0)
            return;

        _trialQueueCoroutine = StartCoroutine(ProcessPendingTrialRequests());
    }

    public void CompleteSession(string participantId)
    {
        RetryPendingTrialUploads();
        FlushPendingQuestionnairePatches();
        Debug.Log($"[ApiClient] Session complete pour participant={participantId}");
    }

    public void SendParticipantNote(string participantId, string sessionTemplateId, string note, Action onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            onError?.Invoke("note vide");
            return;
        }

        StartCoroutine(SendParticipantNoteCoroutine(participantId, sessionTemplateId, note, onSuccess, onError));
    }

    IEnumerator SendParticipantNoteCoroutine(string participantId, string sessionTemplateId, string note, Action onSuccess, Action<string> onError)
    {
        string url = CombineUrl(backendRootUrl, _participantNotesPath);
        var payload = new ParticipantNotePayload
        {
            participant_id = participantId,
            session_template_id = sessionTemplateId,
            note = note
        };
        string body = ToJson(payload);

        using var request = BuildJsonRequest(url, "POST", body);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("SendParticipantNote", request));
            yield break;
        }

        onSuccess?.Invoke();
    }

    [Serializable]
    class ParticipantNotePayload
    {
        public string participant_id;
        public string session_template_id;
        public string note;
    }

    IEnumerator FetchSessionConfigCoroutine(string sessionId, Action<SessionConfig> onSuccess, Action<string> onError)
    {
        string url = CombineUrl(backendRootUrl, _sessionConfigPath, sessionId);
        Debug.Log($"[ApiClient] Fetching session config from {url}");
        using var request = UnityWebRequest.Get(url);
        ApplyCommonHeaders(request);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("FetchSessionConfig", request));
            yield break;
        }

        SessionConfig config = JsonConvert.DeserializeObject<SessionConfig>(request.downloadHandler.text);
        if (config == null)
        {
            onError?.Invoke("JSON de SessionConfig invalide");
            yield break;
        }

        onSuccess?.Invoke(config);
    }

    IEnumerator FetchImageCoroutine(string imageUrl, Action<Sprite> onSuccess, Action<string> onError)
    {
        using var request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("FetchImage", request));
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        onSuccess?.Invoke(sprite);
    }

    IEnumerator ProcessPendingTrialRequests()
    {
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

            if (_pendingQuestionnairePatches.ContainsKey(trialKey))
                FlushPendingQuestionnairePatches();
        }

        _trialQueueCoroutine = null;
    }

    IEnumerator SendTrialResponseCoroutine(TrialResponseRow row, Action<string> onSuccess, Action<string> onError)
    {
        string url = CombineUrl(backendRootUrl, _trialResponsesPath);
        string payload = ToJson(row);

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
        string url = CombineUrl(backendRootUrl, _trialResponsesPath, trialResponseId);
        string body = ToJson(payload);

        using var request = BuildJsonRequest(url, "PATCH", body);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildRequestError("PatchHumanLikenessQuestion", request));
            yield break;
        }

        onSuccess?.Invoke();
    }

    void FlushPendingQuestionnairePatches()
    {
        // HasFlushableQuestionnairePatch garantit au moins un yield dans la
        // coroutine : le handle ne peut pas etre ecrase par une execution
        // entierement synchrone.
        if (_questionnaireFlushCoroutine != null || !HasFlushableQuestionnairePatch())
            return;

        _questionnaireFlushCoroutine = StartCoroutine(FlushPendingQuestionnairePatchesCoroutine());
    }

    IEnumerator FlushPendingQuestionnairePatchesCoroutine()
    {
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

        _questionnaireFlushCoroutine = null;

        if (HasFlushableQuestionnairePatch())
            FlushPendingQuestionnairePatches();
    }

    bool HasFlushableQuestionnairePatch()
    {
        foreach (string key in _pendingQuestionnairePatches.Keys)
        {
            if (_storedTrialIdsByKey.TryGetValue(key, out var rowId) && !string.IsNullOrWhiteSpace(rowId))
                return true;
        }

        return false;
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

        try
        {
            var response = JsonConvert.DeserializeObject<IdResponse>(responseText, JsonSettings);
            return string.IsNullOrWhiteSpace(response?.id) ? null : response.id;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static void MergeQuestionnairePatchPayload(QuestionnairePatchPayload target, QuestionnairePatchPayload source)
    {
        if (target == null || source == null)
            return;

        target.human_likeness_question = source.human_likeness_question;
    }

    static string ToJson(object source)
    {
        return source == null ? "{}" : JsonConvert.SerializeObject(source, JsonSettings);
    }

    static string BuildRequestError(string context, UnityWebRequest request)
    {
        string body = request?.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        return $"[{context}] {(request != null ? request.responseCode : 0)} {request?.error} {body}".Trim();
    }
}
