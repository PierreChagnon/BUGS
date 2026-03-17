using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowController : MonoBehaviour
{
    public static FlowController Instance { get; private set; }

    [Header("Routing")]
    [SerializeField] private string _sessionIdQueryParameter = "session";
    [SerializeField] private string _bootSceneName = "BootScene";
    [SerializeField] private string _welcomeSceneName = "WelcomeScene";
    [SerializeField] private string _consentSceneName = "ConsentScene";
    [SerializeField] private string _introSceneName = "IntroScene";
    [SerializeField] private string _advisorChoiceSceneName = "AdvisorChoiceScene";
    [SerializeField] private string _distalChoiceSceneName = "DistalChoiceScene";
    [SerializeField] private string _proximalSceneName = "ProximalScene";
    [SerializeField] private string _questionnaireSceneName = "QuestionnaireScene";
    [SerializeField] private string _endSessionSceneName = "EndSessionScene";

    [Header("Build")]
    [SerializeField] private string _buildVersion = "0.3.0-flow";

    bool _isBootstrapping;

    public SessionConfig Config { get; private set; }
    public PlayerSessionState State { get; private set; }
    public long CurrentTrialSeed { get; private set; }
    public int BlockScore { get; private set; }
    public string BuildVersion => _buildVersion;

    public BlockConfig CurrentBlock
    {
        get
        {
            if (!HasLoadedConfig)
                return null;

            if (State.current_block_index < 0 || State.current_block_index >= Config.blocks.Count)
                return null;

            return Config.blocks[State.current_block_index];
        }
    }

    public MapGenConfig ActiveMapConfig
    {
        get
        {
            var block = CurrentBlock;
            if (block == null)
                return null;

            MapGenConfig source = State != null && State.valley_choice == ValleyChoice.B
                ? block.valley_b
                : block.valley_a;

            var clone = source != null ? source.DeepClone() : new MapGenConfig();
            if (CurrentTrialSeed != 0)
                clone.seed = CurrentTrialSeed;

            return clone;
        }
    }

    public bool HasLoadedConfig => Config != null && Config.blocks != null && Config.blocks.Count > 0;
    public bool IsCurrentBlockTutorial => CurrentBlock != null && CurrentBlock.is_tutorial;
    public bool IsLastBlock => HasLoadedConfig && State.current_block_index >= Config.blocks.Count - 1;
    public bool IsLastTrial => CurrentBlock != null && State.current_trial_index >= CurrentBlock.trial_count - 1;

    public event Action<GamePhase> OnPhaseChanged;

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

    void Start()
    {
        SubscribeToApiClient();

        if (_isBootstrapping || HasLoadedConfig)
            return;

        _isBootstrapping = true;
        StartCoroutine(BootstrapFlow());
    }

    void OnDestroy()
    {
        if (ApiClient.Instance != null)
            ApiClient.Instance.OnTrialResponseStored -= HandleTrialResponseStored;
    }

    public void Initialize(SessionConfig config)
    {
        Config = PrepareConfig(config);
        if (!HasLoadedConfig)
        {
            Debug.LogError("[FlowController] Initialize a recu une config invalide.");
            return;
        }

        State = new PlayerSessionState
        {
            participant_id = Guid.NewGuid().ToString(),
            session_template_id = Config.session_template_id,
            current_phase = GamePhase.Boot,
            current_block_index = 0,
            current_trial_index = 0,
            advisor_choice = AdvisorType.None,
            valley_choice = ValleyChoice.None,
            green_bugs_accumulated = 0,
            last_trial_response_id = null
        };

        BlockScore = 0;
        CurrentTrialSeed = 0;
    }

    public void OnConsentGiven()
    {
        if (State == null || State.current_phase != GamePhase.Consent)
            return;

        AdvanceToPhase(GamePhase.Intro);
    }

    public void OnPhaseComplete()
    {
        if (State == null)
            return;

        switch (State.current_phase)
        {
            case GamePhase.Welcome:
                AdvanceToPhase(GamePhase.Consent);
                break;

            case GamePhase.Intro:
            case GamePhase.Tutorial:
                AdvanceToPhase(GamePhase.AdvisorChoice);
                break;

            case GamePhase.EndSession:
                ApiClient.Instance?.CompleteSession(State.participant_id);
                break;

            default:
                Debug.LogWarning($"[FlowController] OnPhaseComplete ignore pour phase={State.current_phase}");
                break;
        }
    }

    public void OnAdvisorChosen(AdvisorType type)
    {
        if (State == null || State.current_phase != GamePhase.AdvisorChoice)
            return;

        State.advisor_choice = type;
        AdvanceToPhase(GamePhase.DistalChoice);
    }

    public void OnValleyChosen(ValleyChoice valley)
    {
        if (State == null || State.current_phase != GamePhase.DistalChoice)
            return;

        State.valley_choice = valley;
        CurrentTrialSeed = GenerateTrialSeed();
        AdvanceToPhase(GamePhase.Proximal);
    }

    public void OnTrialComplete(int trialScore)
    {
        if (State == null || State.current_phase != GamePhase.Proximal || CurrentBlock == null)
            return;

        bool completedLastTrial = State.current_trial_index >= CurrentBlock.trial_count - 1;

        if (!CurrentBlock.is_tutorial)
        {
            BlockScore += trialScore;
            State.green_bugs_accumulated = BlockScore;
        }

        State.current_trial_index++;

        if (completedLastTrial)
        {
            if (CurrentBlock.is_tutorial || CurrentBlock.questions == null || CurrentBlock.questions.Count == 0)
                AdvanceToNextBlockOrEnd();
            else
                AdvanceToPhase(GamePhase.Questionnaire);

            return;
        }

        CurrentTrialSeed = GenerateTrialSeed();
        AdvanceToPhase(GamePhase.Proximal);
    }

    public void OnQuestionnaireComplete(List<QuestionResponse> responses)
    {
        if (State == null || State.current_phase != GamePhase.Questionnaire || CurrentBlock == null)
            return;

        if (!CurrentBlock.is_tutorial && ApiClient.Instance != null)
        {
            int blockIndex = State.current_block_index + 1;
            int lastTrialIndex = Mathf.Max(1, CurrentBlock.trial_count);

            ApiClient.Instance.QueueQuestionnairePatchForTrial(
                State.participant_id,
                blockIndex,
                lastTrialIndex,
                responses,
                () => Debug.Log($"[FlowController] Questionnaire bloque {blockIndex} patche."),
                error => Debug.LogWarning($"[FlowController] Questionnaire non patche tout de suite: {error}"));
        }

        AdvanceToNextBlockOrEnd();
    }

    public int GetAccumulatedScoreAfterTrial(int trialScore)
    {
        if (CurrentBlock != null && CurrentBlock.is_tutorial)
            return 0;

        return BlockScore + trialScore;
    }

    public void RegisterLastTrialResponse(string trialResponseId)
    {
        if (State == null)
            return;

        State.last_trial_response_id = trialResponseId;
    }

    IEnumerator BootstrapFlow()
    {
        yield return null;

        string sessionId = ExtractSessionIdFromAbsoluteUrl(Application.absoluteURL, _sessionIdQueryParameter);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Debug.LogError("[FlowController] Aucun sessionId dans l'URL. Le flow ne peut pas demarrer.");
            yield break;
        }

        if (ApiClient.Instance == null)
        {
            Debug.LogError("[FlowController] ApiClient introuvable au boot.");
            yield break;
        }

        bool completed = false;
        SessionConfig fetchedConfig = null;
        string fetchError = null;

        ApiClient.Instance.FetchSessionConfig(
            sessionId,
            config =>
            {
                fetchedConfig = config;
                completed = true;
            },
            error =>
            {
                fetchError = error;
                completed = true;
            });

        while (!completed)
            yield return null;

        if (fetchedConfig != null)
        {
            Initialize(fetchedConfig);
            AdvanceToPhase(GamePhase.Welcome);
            yield break;
        }

        Debug.LogError($"[FlowController] Impossible de charger la session '{sessionId}': {fetchError}");
    }

    void AdvanceToNextBlockOrEnd()
    {
        BlockScore = 0;
        State.green_bugs_accumulated = 0;
        State.current_trial_index = 0;
        State.advisor_choice = AdvisorType.None;
        State.valley_choice = ValleyChoice.None;
        State.last_trial_response_id = null;
        CurrentTrialSeed = 0;

        if (!IsLastBlock)
        {
            State.current_block_index++;
            AdvanceToPhase(GamePhase.AdvisorChoice);
            return;
        }

        ApiClient.Instance?.CompleteSession(State.participant_id);
        AdvanceToPhase(GamePhase.EndSession);
    }

    void AdvanceToPhase(GamePhase next)
    {
        if (State == null)
            return;

        State.current_phase = next;
        OnPhaseChanged?.Invoke(next);

        string sceneName = GetSceneName(next);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"[FlowController] Aucune scene configuree pour la phase {next}.");
            return;
        }

        StartCoroutine(TransitionToScene(sceneName));
    }

    IEnumerator TransitionToScene(string sceneName)
    {
        if (FadeTransition.Instance != null)
            yield return FadeTransition.Instance.FadeOut();

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        yield return null;

        if (FadeTransition.Instance != null)
            yield return FadeTransition.Instance.FadeIn();
    }

    void SubscribeToApiClient()
    {
        if (ApiClient.Instance == null)
            return;

        ApiClient.Instance.OnTrialResponseStored -= HandleTrialResponseStored;
        ApiClient.Instance.OnTrialResponseStored += HandleTrialResponseStored;
    }

    void HandleTrialResponseStored(TrialResponseRow row, string trialResponseId)
    {
        if (row == null || State == null)
            return;

        if (!string.Equals(row.participant_id, State.participant_id, StringComparison.OrdinalIgnoreCase))
            return;

        RegisterLastTrialResponse(trialResponseId);
    }

    SessionConfig PrepareConfig(SessionConfig source)
    {
        var config = TutorialSessionFactory.EnsureTutorialBlock(source);
        if (config.blocks == null)
            config.blocks = new List<BlockConfig>();

        config.blocks.RemoveAll(block => block == null);
        if (string.IsNullOrWhiteSpace(config.session_template_id))
            config.session_template_id = "debug-session-template";

        config.blocks.Sort((a, b) => a.block_order.CompareTo(b.block_order));
        return config;
    }

    string GetSceneName(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Boot => _bootSceneName,
            GamePhase.Welcome => _welcomeSceneName,
            GamePhase.Consent => _consentSceneName,
            GamePhase.Intro => _introSceneName,
            GamePhase.AdvisorChoice => _advisorChoiceSceneName,
            GamePhase.DistalChoice => _distalChoiceSceneName,
            GamePhase.Proximal => _proximalSceneName,
            GamePhase.Questionnaire => _questionnaireSceneName,
            GamePhase.EndSession => _endSessionSceneName,
            _ => _bootSceneName
        };
    }

    static string ExtractSessionIdFromAbsoluteUrl(string absoluteUrl, string queryParameter)
    {
        if (string.IsNullOrWhiteSpace(absoluteUrl) || string.IsNullOrWhiteSpace(queryParameter))
            return null;

        int queryIndex = absoluteUrl.IndexOf('?');
        if (queryIndex < 0 || queryIndex >= absoluteUrl.Length - 1)
            return null;

        string query = absoluteUrl.Substring(queryIndex + 1);
        string[] parts = query.Split('&');
        for (int i = 0; i < parts.Length; i++)
        {
            string[] keyValue = parts[i].Split('=');
            if (keyValue.Length != 2)
                continue;

            if (!string.Equals(keyValue[0], queryParameter, StringComparison.OrdinalIgnoreCase))
                continue;

            return Uri.UnescapeDataString(keyValue[1]);
        }

        return null;
    }

    static long GenerateTrialSeed()
    {
        unchecked
        {
            long ticks = DateTime.UtcNow.Ticks;
            int hash = Guid.NewGuid().GetHashCode();
            return (ticks << 1) ^ hash;
        }
    }
}
