using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlowController : MonoBehaviour
{
    const string SessionIdArgumentName = "sessionId";

    public static FlowController Instance { get; private set; }

    [Header("Routing")]
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

    [Header("Editor Test")]
    [SerializeField] private string _editorSessionId;

    bool _isBootstrapping;

    public SessionConfig Config { get; private set; }
    public PlayerSessionState State { get; private set; }
    public long CurrentBlockSeed { get; private set; }
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

            return source != null ? source.DeepClone() : new MapGenConfig();
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
            distal_advice_visible = false,
            distal_advice_reliable = false,
            distal_advice_choice = ValleyChoice.None,
            distal_best_valley = ValleyChoice.None,
            green_bugs_accumulated = 0,
            last_trial_response_id = null
        };

        BlockScore = 0;
        CurrentBlockSeed = 0;
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
        CurrentBlockSeed = ResolveBlockSeedForCurrentBlock();
        CurrentTrialSeed = DeriveTrialSeed(CurrentBlockSeed, State.current_trial_index);
        Debug.Log($"[FlowController] blockSeed={CurrentBlockSeed}, trialIndex={State.current_trial_index + 1}, trialSeed={CurrentTrialSeed}");
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

        if (CurrentBlockSeed == 0)
            CurrentBlockSeed = ResolveBlockSeedForCurrentBlock();

        CurrentTrialSeed = DeriveTrialSeed(CurrentBlockSeed, State.current_trial_index);
        Debug.Log($"[FlowController] blockSeed={CurrentBlockSeed}, trialIndex={State.current_trial_index + 1}, trialSeed={CurrentTrialSeed}");
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

        string sessionId = ExtractSessionIdFromCommandLineArgs(Environment.GetCommandLineArgs());
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = _editorSessionId;
#endif

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Debug.LogError("[FlowController] Aucun sessionId recu. Le flow ne peut pas demarrer.");
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
        ResetDistalAdviceState();
        State.last_trial_response_id = null;
        CurrentBlockSeed = 0;
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

        if (next == GamePhase.DistalChoice)
            RollDistalAdviceForCurrentBlock();

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

    // Le conseil distal est determine aleatoirement a chaque fois que le joueur arrive sur la scene de choix distal, en fonction du bloc en cours et de l'advisor choisi.
    void RollDistalAdviceForCurrentBlock()
    {
        ResetDistalAdviceState();

        var block = CurrentBlock;
        if (State == null || block == null)
            return;

        var rng = CreateDistalAdviceRng();
        State.distal_best_valley = ResolveMostRewardingValley(block, rng);

        if (State.advisor_choice == AdvisorType.None)
        {
            Debug.Log("[FlowController] Distal advice masque car advisor_choice=none.");
            return;
        }

        float visibleProbability = Mathf.Clamp01(block.distal_advice_visible_probability);
        float reliableProbability = Mathf.Clamp01(block.distal_advice_reliable_probability);

        State.distal_advice_visible = rng.NextDouble() < visibleProbability;
        if (!State.distal_advice_visible)
        {
            Debug.Log($"[FlowController] Distal advice visible=false (prob={visibleProbability:0.###}).");
            return;
        }

        State.distal_advice_reliable = rng.NextDouble() < reliableProbability;
        State.distal_advice_choice = State.distal_advice_reliable
            ? State.distal_best_valley
            : GetOppositeValley(State.distal_best_valley);

        Debug.Log(
            $"[FlowController] Distal advice visible=true (prob={visibleProbability:0.###}), " +
            $"reliable={State.distal_advice_reliable} (prob={reliableProbability:0.###}), " +
            $"best={FlowValueConverters.ToApiValue(State.distal_best_valley)}, " +
            $"choice={FlowValueConverters.ToApiValue(State.distal_advice_choice)}.");
    }

    void ResetDistalAdviceState()
    {
        if (State == null)
            return;

        State.distal_advice_visible = false;
        State.distal_advice_reliable = false;
        State.distal_advice_choice = ValleyChoice.None;
        State.distal_best_valley = ValleyChoice.None;
    }

    System.Random CreateDistalAdviceRng()
    {
        unchecked
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offset;
            MixLong(ref hash, prime, State != null ? State.current_block_index : 0);
            MixLong(ref hash, prime, CurrentBlock != null ? CurrentBlock.block_order : 0);
            MixString(ref hash, prime, State != null ? State.participant_id : null);
            MixString(ref hash, prime, Config != null ? Config.session_template_id : null);
            MixString(ref hash, prime, "distal-advice");

            int seed = (int)(hash & 0x7FFFFFFF);
            if (seed == 0)
                seed = 1;

            return new System.Random(seed);
        }
    }

    static ValleyChoice ResolveMostRewardingValley(BlockConfig block, System.Random rng)
    {
        float valleyAExpectedGreenBugs = ComputeExpectedGreenBugs(block?.valley_a);
        float valleyBExpectedGreenBugs = ComputeExpectedGreenBugs(block?.valley_b);

        if (Mathf.Approximately(valleyAExpectedGreenBugs, valleyBExpectedGreenBugs))
            return rng.NextDouble() < 0.5 ? ValleyChoice.A : ValleyChoice.B;

        return valleyAExpectedGreenBugs > valleyBExpectedGreenBugs
            ? ValleyChoice.A
            : ValleyChoice.B;
    }

    static float ComputeExpectedGreenBugs(MapGenConfig map)
    {
        if (map == null)
            return 0f;

        float averageTotalBugs = (map.min_total_bugs + map.max_total_bugs) * 0.5f;
        float averageGreenRatio = (map.min_green_ratio + map.max_green_ratio) * 0.5f;
        return averageTotalBugs * averageGreenRatio;
    }

    static ValleyChoice GetOppositeValley(ValleyChoice valley)
    {
        return valley == ValleyChoice.A ? ValleyChoice.B : ValleyChoice.A;
    }

    static void MixLong(ref ulong hash, ulong prime, long value)
    {
        ulong raw = (ulong)value;
        for (int i = 0; i < 8; i++)
        {
            hash ^= (byte)(raw & 0xFF);
            hash *= prime;
            raw >>= 8;
        }
    }

    static void MixString(ref ulong hash, ulong prime, string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        for (int i = 0; i < value.Length; i++)
        {
            hash ^= (byte)value[i];
            hash *= prime;
        }
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

    static string ExtractSessionIdFromCommandLineArgs(string[] args)
    {
        if (args == null)
            return null;

        for (int i = 0; i < args.Length; i++)
        {
            string value = ExtractSessionIdFromArgument(args[i]);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    static string ExtractSessionIdFromArgument(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
            return null;

        string prefix = $"{SessionIdArgumentName}=";
        if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        string value = argument.Substring(prefix.Length);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value;
    }

    long ResolveBlockSeedForCurrentBlock()
    {
        var block = CurrentBlock;
        if (block == null)
            return GenerateSeed();

        MapGenConfig map = State != null && State.valley_choice == ValleyChoice.B
            ? block.valley_b
            : block.valley_a;

        if (map == null)
            return GenerateSeed();

        if (map.seed == 0)
            map.seed = GenerateSeed();

        return map.seed;
    }

    static long DeriveTrialSeed(long blockSeed, int trialIndex)
    {
        unchecked
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;

            ulong h = offset;
            ulong seed64 = (ulong)blockSeed;

            for (int i = 0; i < 8; i++)
            {
                h ^= (byte)(seed64 & 0xFF);
                h *= prime;
                seed64 >>= 8;
            }

            uint index = (uint)Mathf.Max(0, trialIndex);
            for (int i = 0; i < 4; i++)
            {
                h ^= (byte)(index & 0xFF);
                h *= prime;
                index >>= 8;
            }

            int seed = (int)(h & 0x7FFFFFFF);
            if (seed == 0)
                seed = 1;

            return seed;
        }
    }

    static long GenerateSeed()
    {
        unchecked
        {
            // Seed 31 bits positive: exact en JSON/JS (pas de perte de precision).
            int ticksHash = DateTime.UtcNow.Ticks.GetHashCode();
            int guidHash = Guid.NewGuid().GetHashCode();
            int seed = (ticksHash ^ guidHash) & 0x7FFFFFFF;
            if (seed == 0)
                seed = 1;

            return seed;
        }
    }
}
