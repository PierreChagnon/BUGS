using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

    public static ApiClient Instance { get; private set; }

    [Header("Config")]
    public string backendRootUrl;
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

    public void PatchHumanLikenessQuestion(
        string trialResponseId,
        string humanLikenessQuestion,
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
            human_likeness_question = humanLikenessQuestion
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
        string url = CombineUrl(backendRootUrl, _trialResponsesPath);
        string payload = ToJsonObjectSkippingNullStrings(row);

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
        string body = ToJsonObjectSkippingNullStrings(payload);

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

        Match match = Regex.Match(responseText, "\"id\"\\s*:\\s*\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    static void MergeQuestionnairePatchPayload(QuestionnairePatchPayload target, QuestionnairePatchPayload source)
    {
        if (target == null || source == null)
            return;

        target.human_likeness_question = source.human_likeness_question;
    }

    static string ToJsonObjectSkippingNullStrings(object source)
    {
        if (source == null)
            return "{}";

        var builder = new StringBuilder();
        builder.Append('{');

        FieldInfo[] fields = source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        bool hasPreviousField = false;

        for (int i = 0; i < fields.Length; i++)
        {
            object value = fields[i].GetValue(source);
            bool shouldIncludeNull = Attribute.IsDefined(fields[i], typeof(IncludeNullInJsonAttribute));
            if (value == null && !shouldIncludeNull)
                continue;

            if (hasPreviousField)
                builder.Append(',');

            AppendJsonString(builder, fields[i].Name);
            builder.Append(':');
            if (value == null)
                builder.Append("null");
            else
                AppendJsonValue(builder, value);

            hasPreviousField = true;
        }

        builder.Append('}');
        return builder.ToString();
    }

    static void AppendJsonValue(StringBuilder builder, object value)
    {
        switch (value)
        {
            case string stringValue:
                AppendJsonString(builder, stringValue);
                break;
            case bool boolValue:
                builder.Append(boolValue ? "true" : "false");
                break;
            case IFormattable formattable:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                break;
            default:
                builder.Append(JsonUtility.ToJson(value));
                break;
        }
    }

    static void AppendJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        builder.Append('"');
    }

    static string BuildRequestError(string context, UnityWebRequest request)
    {
        string body = request?.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        return $"[{context}] {(request != null ? request.responseCode : 0)} {request?.error} {body}".Trim();
    }
}
