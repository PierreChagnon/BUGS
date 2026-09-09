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
    [SerializeField] private string _breakSceneName = "BreakScene";
    [SerializeField] private string _endSessionSceneName = "EndSessionScene";

    [Header("Editor Test")]
    [SerializeField] private string _editorSessionId;

    bool _isBootstrapping;
    double _breakResumeAllowedAt;
    bool _breakCountdownStarted;
    Coroutine _sceneTransitionCoroutine;

    public SessionConfig Config { get; private set; }
    public PlayerSessionState State { get; private set; }
    public long CurrentBlockSeed { get; private set; }
    public long CurrentTrialSeed { get; private set; }
    public int BlockScore { get; private set; }
    // Total de bugs verts collectes sur la session, blocs tutoriels exclus : jamais remis a zero entre les blocs.
    public int SessionScore { get; private set; }
    public string PlatformUrl => Config != null ? Config.platform_url : null;
    public bool WasConsentDeclined { get; private set; }
    public ExplanationRuntimeState DistalAdviceExplanation { get; private set; } = ExplanationRuntimeState.None();
    public ExplanationRuntimeState ProximalAdviceExplanation { get; private set; } = ExplanationRuntimeState.None();
    public ExplanationRuntimeState MotorAdviceExplanation { get; private set; } = ExplanationRuntimeState.None();

    public BlockConfig CurrentBlock
    {
        get
        {
            if (!HasLoadedConfig || State == null)
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
    public bool AreBreaksEnabled =>
        Config?.break_every_trials > 0 &&
        Config?.break_duration_seconds > 0;
    public int BreakRemainingSeconds
    {
        get
        {
            if (State == null || State.current_phase != GamePhase.Break)
                return 0;

            if (!_breakCountdownStarted)
                return Config?.break_duration_seconds ?? 0;

            return Mathf.Max(
                0,
                Mathf.CeilToInt((float)(_breakResumeAllowedAt - Time.realtimeSinceStartupAsDouble)));
        }
    }
    public bool IsCurrentBlockTutorial => CurrentBlock != null && CurrentBlock.is_tutorial;
    public bool IsLastBlock => HasLoadedConfig && State.current_block_index >= Config.blocks.Count - 1;
    public bool IsAnyChoiceForcedThisTrial => State != null && (
        State.meta_choice_is_forced ||
        State.distal_choice_is_forced ||
        State.proximal_choice_is_forced ||
        State.motor_choice_is_forced);
    public bool ShouldShowEquipmentFailureOverlay =>
        IsAnyChoiceForcedThisTrial && State != null && State.advisor_choice == AdvisorType.None;

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
        if (_isBootstrapping || HasLoadedConfig)
            return;

        _isBootstrapping = true;
        StartCoroutine(BootstrapFlow());
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
            completed_non_tutorial_trials = 0,
            break_pending = false,
            advisor_choice = AdvisorType.None,
            valley_choice = ValleyChoice.None,
            meta_choice_is_forced = false,
            meta_choice_forced_value = null,
            distal_choice_is_forced = false,
            distal_choice_forced_scan_side = null,
            distal_choice_forced_value = null,
            distal_choice_forced_was_optimal = null,
            distal_choice_forced_optimal_probability = null,
            proximal_choice_is_forced = false,
            proximal_choice_forced_value = null,
            proximal_choice_forced_was_optimal = null,
            proximal_choice_forced_probability = 0f,
            proximal_choice_forced_optimal_probability = null,
            motor_choice_is_forced = false,
            motor_choice_forced_set = null,
            motor_choice_forced_probability = 0f,
            advisor_display_is_male = false,
            advisor_display_order = null,
            distal_advice_visible = false,
            distal_advice_reliable = false,
            distal_advice_choice = null,
            distal_best_valley = null,
            distal_scan_choice = null
        };

        BlockScore = 0;
        SessionScore = 0;
        CurrentBlockSeed = 0;
        CurrentTrialSeed = 0;
        _breakResumeAllowedAt = 0;
        _breakCountdownStarted = false;
        WasConsentDeclined = false;
        ResetAllExplanationStates();
    }

    public void OnConsentGiven()
    {
        if (State == null || State.current_phase != GamePhase.Consent)
            return;

        WasConsentDeclined = false;
        AdvanceToPhase(GamePhase.Intro);
    }

    public void OnConsentDeclined()
    {
        if (State == null || State.current_phase != GamePhase.Consent)
            return;

        WasConsentDeclined = true;
        AdvanceToPhase(GamePhase.Welcome);
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

        if (State.meta_choice_is_forced && type != FlowValueConverters.ToAdvisorType(State.meta_choice_forced_value))
        {
            Debug.Log($"[FlowController] Advisor ignore: choix forced={State.meta_choice_forced_value}, recu={type}.");
            return;
        }

        State.advisor_choice = type;
        AdvanceToPhase(GamePhase.DistalChoice);
    }

    public void OnValleyChosen(string scanChoice)
    {
        if (State == null || State.current_phase != GamePhase.DistalChoice)
            return;

        if (State.distal_choice_is_forced && scanChoice != State.distal_choice_forced_scan_side)
        {
            Debug.Log($"[FlowController] Distal choice ignore: choix forced={State.distal_choice_forced_scan_side}, recu={scanChoice}.");
            return;
        }

        RecordExplanationHidden(AdviceLevel.Distal);
        State.distal_scan_choice = scanChoice;
        State.valley_choice = ResolveValleyFromDistalScanChoice(scanChoice);
        CurrentBlockSeed = ResolveBlockSeedForCurrentBlock();
        CurrentTrialSeed = SeedUtility.DeriveTrialSeed(CurrentBlockSeed, State.current_trial_index);
        Debug.Log(
            $"[FlowController] distalScanChoice={scanChoice}, " +
            $"distalBestScan={State.distal_best_valley}, " +
            $"valleyChoice={FlowValueConverters.ToApiValue(State.valley_choice)}, " +
            $"blockSeed={CurrentBlockSeed}, trialIndex={State.current_trial_index + 1}, trialSeed={CurrentTrialSeed}");
        AdvanceToPhase(GamePhase.Proximal);
    }

    public void OnTrialComplete(int trialScore)
    {
        if (State == null || State.current_phase != GamePhase.Proximal || CurrentBlock == null)
            return;

        bool completedLastTrial = State.current_trial_index >= CurrentBlock.trial_count - 1;

        BlockScore += trialScore;

        if (!CurrentBlock.is_tutorial)
        {
            SessionScore += trialScore;
            State.completed_non_tutorial_trials++;
            if (AreBreaksEnabled &&
                State.completed_non_tutorial_trials % Config.break_every_trials.Value == 0)
            {
                State.break_pending = true;
            }
        }

        State.current_trial_index++;

        if (completedLastTrial)
        {
            AdvanceToNextBlockOrEnd();
            return;
        }

        if (CurrentBlockSeed == 0)
            CurrentBlockSeed = ResolveBlockSeedForCurrentBlock();

        CurrentTrialSeed = SeedUtility.DeriveTrialSeed(CurrentBlockSeed, State.current_trial_index);
        Debug.Log($"[FlowController] blockSeed={CurrentBlockSeed}, trialIndex={State.current_trial_index + 1}, trialSeed={CurrentTrialSeed}");
        AdvanceToPhase(GamePhase.Proximal);
    }

    public void OnBreakComplete()
    {
        if (State == null || State.current_phase != GamePhase.Break)
            return;

        if (!_breakCountdownStarted || BreakRemainingSeconds > 0)
        {
            Debug.LogWarning(
                $"[FlowController] Reprise refusee: pause obligatoire encore active " +
                $"({BreakRemainingSeconds}s restantes).");
            return;
        }

        State.break_pending = false;
        AdvanceToPhase(GamePhase.AdvisorChoice);
    }

    public void StartBreakCountdown()
    {
        if (State == null ||
            State.current_phase != GamePhase.Break ||
            !AreBreaksEnabled ||
            _breakCountdownStarted)
        {
            return;
        }

        _breakCountdownStarted = true;
        _breakResumeAllowedAt =
            Time.realtimeSinceStartupAsDouble +
            Config.break_duration_seconds.Value;
    }

    public int GetAccumulatedScoreAfterTrial(int trialScore)
    {
        return BlockScore + trialScore;
    }

    // Le trial courant n'est pas encore comptabilise dans SessionScore au moment ou le
    // TrialManager assemble la ligne : on projette la valeur post-trial ici.
    public int GetSessionScoreAfterTrial(int trialScore)
    {
        return IsCurrentBlockTutorial ? SessionScore : SessionScore + trialScore;
    }

    public BugCloudPairData GenerateCurrentDistalScans()
    {
        return BugCloudGenerationUtility.GenerateDistalScanPair(
            CurrentBlock.distal_scene,
            State.distal_best_valley,
            CreateCurrentBlockRandom(2));
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
        State.current_trial_index = 0;
        State.advisor_choice = AdvisorType.None;
        State.valley_choice = ValleyChoice.None;
        State.distal_scan_choice = null;
        ResetForcedChoiceState();
        ResetDistalAdviceState();
        ResetAllExplanationStates();
        CurrentBlockSeed = 0;
        CurrentTrialSeed = 0;

        if (!IsLastBlock)
        {
            State.current_block_index++;

            if (State.break_pending && AreBreaksEnabled)
            {
                _breakCountdownStarted = false;
                _breakResumeAllowedAt = 0;
                AdvanceToPhase(GamePhase.Break);
                return;
            }

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

        if (next == GamePhase.AdvisorChoice)
        {
            RollMetaForcedForCurrentBlock();
            // Salt 7 : les salts 0 a 6 sont deja pris par les autres tirages de bloc.
            State.advisor_display_order = BlockDrawResolver.DrawAdvisorDisplayOrder(CreateCurrentBlockRandom(7));
        }

        if (next == GamePhase.DistalChoice)
            RollDistalAdviceForCurrentBlock();

        if (next == GamePhase.Proximal)
            RollTrialForcedChoicesForCurrentTrial();

        State.current_phase = next;
        OnPhaseChanged?.Invoke(next);

        string sceneName = GetSceneName(next);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"[FlowController] Aucune scene configuree pour la phase {next}.");
            return;
        }

        // Deux transitions rapprochees ne doivent pas se chevaucher : la
        // nouvelle remplace l'ancienne.
        if (_sceneTransitionCoroutine != null)
            StopCoroutine(_sceneTransitionCoroutine);

        _sceneTransitionCoroutine = StartCoroutine(TransitionToScene(sceneName));
    }

    IEnumerator TransitionToScene(string sceneName)
    {
        if (FadeTransition.Instance != null)
            yield return FadeTransition.Instance.FadeOut();

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        yield return null;

        if (FadeTransition.Instance != null)
            yield return FadeTransition.Instance.FadeIn();

        _sceneTransitionCoroutine = null;
    }

    SessionConfig PrepareConfig(SessionConfig source)
    {
        var config = source != null
            ? source.DeepClone()
            : new SessionConfig
            {
                blocks = new List<BlockConfig>()
            };

        if (config.blocks == null)
            config.blocks = new List<BlockConfig>();

        config.blocks.RemoveAll(block => block == null);
        if (string.IsNullOrWhiteSpace(config.session_template_id))
            config.session_template_id = "debug-session-template";

        for (int i = 0; i < config.blocks.Count; i++)
            SanitizeBlockConfig(config.blocks[i]);

        config.blocks = BlockOrderRandomizer.BuildPlayedOrder(
            config.blocks,
            config.randomize_blocks,
            new System.Random(Guid.NewGuid().GetHashCode()));

        if (config.randomize_blocks &&
            BlockOrderRandomizer.HasIdenticalConsecutiveBlocks(config.blocks))
        {
            Debug.LogWarning(
                "[FlowController] Aucune permutation sans fingerprints consecutives identiques " +
                $"trouvee apres {BlockOrderRandomizer.MAX_RANDOMIZATION_ATTEMPTS} tentatives. " +
                "Utilisation de la permutation fallback.");
        }

        Debug.Log(
            $"[FlowController] Ordre des blocs prepare une seule fois " +
            $"(randomize={config.randomize_blocks}): " +
            string.Join(", ", config.blocks.ConvertAll(
                block => $"{block.block_template_id ?? "<sans-id>"}@{block.block_order}")));

        return config;
    }

    void SanitizeBlockConfig(BlockConfig block)
    {
        if (block == null)
            return;

        block.distal_forced_optimal_probability = Mathf.Clamp01(block.distal_forced_optimal_probability);
        block.proximal_forced_probability = Mathf.Clamp01(block.proximal_forced_probability);
        block.proximal_forced_optimal_probability = Mathf.Clamp01(block.proximal_forced_optimal_probability);
        block.motor_forced_probability = Mathf.Clamp01(block.motor_forced_probability);
        SanitizeExplanationConfig(block.explanations?.distal);
        SanitizeExplanationConfig(block.explanations?.proximal);
        SanitizeExplanationConfig(block.explanations?.motor);

        block.advisor_forced_value = FlowValueConverters.ToApiValue(FlowValueConverters.ToAdvisorType(block.advisor_forced_value));
        block.motor_forced_set = FlowValueConverters.ToApiValue(FlowValueConverters.ToMotorKeySet(block.motor_forced_set));
    }

    static void SanitizeExplanationConfig(AdviceExplanationConfig config)
    {
        if (config == null)
            return;

        config.display_probability = Mathf.Clamp01(config.display_probability);
        config.display_mode_forced_probability = Mathf.Clamp01(
            config.display_mode_forced_probability);
        config.content_variant_long_probability = Mathf.Clamp01(
            config.content_variant_long_probability);
    }

    void RollMetaForcedForCurrentBlock()
    {
        ResetForcedChoiceState();

        var block = CurrentBlock;
        if (State == null || block == null || !block.advisor_forced)
            return;

        State.meta_choice_is_forced = true;
        State.meta_choice_forced_value = FlowValueConverters.ToApiValue(
            FlowValueConverters.ToAdvisorType(block.advisor_forced_value));

        Debug.Log($"[FlowController] Meta forced: advisor={State.meta_choice_forced_value}.");
    }

    void RollTrialForcedChoicesForCurrentTrial()
    {
        ResetTrialForcedChoiceState();
        ResetTrialExplanationStates();

        var block = CurrentBlock;
        if (State == null || block == null)
            return;

        var draw = TrialDrawResolver.DrawTrialForcedChoices(
            block,
            CreateCurrentTrialRandom(nameof(RollTrialForcedChoicesForCurrentTrial)));

        State.proximal_choice_forced_probability = draw.proximal_probability;
        State.proximal_choice_is_forced = draw.proximal_is_forced;
        if (draw.proximal_is_forced)
        {
            State.proximal_choice_forced_optimal_probability = draw.proximal_optimal_probability;
            State.proximal_choice_forced_was_optimal = draw.proximal_was_optimal;
        }

        State.motor_choice_forced_probability = draw.motor_probability;
        State.motor_choice_is_forced = draw.motor_is_forced;
        State.motor_choice_forced_set = draw.motor_is_forced
            ? FlowValueConverters.ToApiValue(FlowValueConverters.ToMotorKeySet(block.motor_forced_set))
            : null;

        Debug.Log(
            $"[FlowController] Forced trial state: proximal={State.proximal_choice_is_forced}, " +
            $"proximalWasOptimal={State.proximal_choice_forced_was_optimal}, " +
            $"motor={State.motor_choice_is_forced}, motorSet={State.motor_choice_forced_set}.");
    }

    public void ResolveProximalForcedCloud(string bestCloudSide)
    {
        if (State == null || !State.proximal_choice_is_forced || string.IsNullOrWhiteSpace(bestCloudSide))
            return;

        bool forcedWasOptimal = State.proximal_choice_forced_was_optimal == true;
        State.proximal_choice_forced_value = forcedWasOptimal
            ? bestCloudSide
            : DistalScanSide.Opposite(bestCloudSide);

        Debug.Log($"[FlowController] Proximal forced: cloud={State.proximal_choice_forced_value}, optimal={forcedWasOptimal}.");
    }

    public void ResolveProximalExplanationForCurrentTrial(bool adviceVisible)
    {
        bool showExplanation = adviceVisible && RollExplanationProbability(
            nameof(ResolveProximalExplanationForCurrentTrial),
            CurrentBlock?.explanations?.proximal != null
                ? CurrentBlock.explanations.proximal.display_probability
                : 0f);

        ProximalAdviceExplanation = ExplanationResolver.Resolve(
            CurrentBlock,
            AdviceLevel.Proximal,
            State != null ? State.advisor_choice : AdvisorType.None,
            showExplanation,
            CreateCurrentTrialRandom(ExplanationMixScope(nameof(ResolveProximalExplanationForCurrentTrial))));
    }

    public void ResolveMotorExplanationForCurrentTrial(bool adviceVisible)
    {
        bool showExplanation = adviceVisible && RollExplanationProbability(
            nameof(ResolveMotorExplanationForCurrentTrial),
            CurrentBlock?.explanations?.motor != null
                ? CurrentBlock.explanations.motor.display_probability
                : 0f);

        MotorAdviceExplanation = ExplanationResolver.Resolve(
            CurrentBlock,
            AdviceLevel.Motor,
            State != null ? State.advisor_choice : AdvisorType.None,
            showExplanation,
            CreateCurrentTrialRandom(ExplanationMixScope(nameof(ResolveMotorExplanationForCurrentTrial))));
    }

    // Tirage independant a chaque trial (remplace l'ancienne restriction "1er trial du bloc uniquement").
    bool RollExplanationProbability(string scope, float probability)
    {
        return ExplanationResolver.DrawShouldDisplay(probability, CreateCurrentTrialRandom(scope));
    }

    // Scope distinct de celui du tirage d'apparition : les valeurs mixtes de
    // display_mode et content_variant consomment leur propre flux aleatoire, pour
    // que les sessions sans mode mixte restent rejouables a l'identique.
    static string ExplanationMixScope(string scope)
    {
        return $"{scope}:mix";
    }

    public ExplanationRuntimeState GetExplanationState(AdviceLevel level)
    {
        return level switch
        {
            AdviceLevel.Distal => DistalAdviceExplanation,
            AdviceLevel.Proximal => ProximalAdviceExplanation,
            AdviceLevel.Motor => MotorAdviceExplanation,
            _ => ExplanationRuntimeState.None()
        };
    }

    public void RecordExplanationDisplayed(AdviceLevel level)
    {
        GetExplanationState(level)?.MarkDisplayed(Time.realtimeSinceStartup);
    }

    public void RecordExplanationHidden(AdviceLevel level)
    {
        GetExplanationState(level)?.MarkHidden(Time.realtimeSinceStartup);
    }

    public void FinalizeCurrentExplanationTimers()
    {
        RecordExplanationHidden(AdviceLevel.Distal);
        RecordExplanationHidden(AdviceLevel.Proximal);
        RecordExplanationHidden(AdviceLevel.Motor);
    }

    public void ApplyCurrentExplanationStatesToRow(TrialResponseRow row)
    {
        if (row == null)
            return;

        FlowSerializationUtility.ApplyExplanationState(row, AdviceLevel.Distal, DistalAdviceExplanation);
        FlowSerializationUtility.ApplyExplanationState(row, AdviceLevel.Proximal, ProximalAdviceExplanation);
        FlowSerializationUtility.ApplyExplanationState(row, AdviceLevel.Motor, MotorAdviceExplanation);
    }

    public bool IsAdvisorChoiceAllowed(AdvisorType type)
    {
        return State == null ||
               !State.meta_choice_is_forced ||
               type == FlowValueConverters.ToAdvisorType(State.meta_choice_forced_value);
    }

    public bool IsDistalScanChoiceAllowed(string scanChoice)
    {
        return State == null ||
               !State.distal_choice_is_forced ||
               scanChoice == State.distal_choice_forced_scan_side;
    }

    // Le conseil distal est determine aleatoirement a chaque fois que le joueur arrive sur la scene de choix distal, en fonction du bloc en cours et de l'advisor choisi.
    void RollDistalAdviceForCurrentBlock()
    {
        ResetDistalAdviceState();

        var block = CurrentBlock;
        if (State == null || block == null)
        {
            ResolveDistalExplanationForCurrentBlock();
            return;
        }

        State.distal_best_valley = BlockDrawResolver.DrawBestValleySide(CreateCurrentBlockRandom(0));
        // Salt 6 : les salts 0 a 5 sont deja pris par les autres tirages de bloc.
        State.advisor_display_is_male = BlockDrawResolver.DrawAdvisorDisplayIsMale(CreateCurrentBlockRandom(6));

        RollDistalForcedForCurrentBlock(block);

        if (State.advisor_choice == AdvisorType.None)
        {
            Debug.Log("[FlowController] Distal advice masque car advisor_choice=none.");
            ResolveDistalExplanationForCurrentBlock();
            return;
        }

        var advice = BlockDrawResolver.DrawDistalAdvice(
            block,
            State.distal_choice_is_forced,
            State.distal_choice_forced_scan_side,
            State.distal_choice_forced_was_optimal == true,
            State.distal_best_valley,
            CreateCurrentBlockRandom(4));

        State.distal_advice_visible = advice.visible;
        State.distal_advice_reliable = advice.reliable;
        State.distal_advice_choice = advice.choice;

        float visibleProbability = Mathf.Clamp01(block.distal_advice_visible_probability);
        if (!advice.visible)
        {
            Debug.Log($"[FlowController] Distal advice visible=false (prob={visibleProbability:0.###}).");
            ResolveDistalExplanationForCurrentBlock();
            return;
        }

        Debug.Log(
            $"[FlowController] Distal advice visible=true (prob={visibleProbability:0.###}), " +
            $"reliable={State.distal_advice_reliable} (prob={Mathf.Clamp01(block.distal_advice_reliable_probability):0.###}), " +
            $"bestScan={State.distal_best_valley}, " +
            $"advisor_choice={State.distal_advice_choice}.");
        ResolveDistalExplanationForCurrentBlock();
    }

    void ResolveDistalExplanationForCurrentBlock()
    {
        // Salt 5 : les salts 0 a 4 sont deja pris par les autres tirages de bloc.
        DistalAdviceExplanation = ExplanationResolver.Resolve(
            CurrentBlock,
            AdviceLevel.Distal,
            State != null ? State.advisor_choice : AdvisorType.None,
            State != null && State.distal_advice_visible,
            CreateCurrentBlockRandom(5));
    }

    void RollDistalForcedForCurrentBlock(BlockConfig block)
    {
        State.distal_choice_is_forced = false;
        State.distal_choice_forced_scan_side = null;
        State.distal_choice_forced_value = null;
        State.distal_choice_forced_was_optimal = null;
        State.distal_choice_forced_optimal_probability = null;

        if (block == null || !block.distal_forced)
            return;

        var forced = BlockDrawResolver.DrawDistalForced(
            block,
            State.distal_best_valley,
            CreateCurrentBlockRandom(3));

        State.distal_choice_is_forced = true;
        State.distal_choice_forced_scan_side = forced.forced_scan_side;
        State.distal_choice_forced_was_optimal = forced.forced_was_optimal;
        State.distal_choice_forced_optimal_probability = forced.optimal_probability;
        State.distal_choice_forced_value = FlowValueConverters.ToApiValue(
            ResolveValleyFromDistalScanChoice(forced.forced_scan_side));

        Debug.Log(
            $"[FlowController] Distal forced: scan={forced.forced_scan_side}, " +
            $"valley={State.distal_choice_forced_value}, optimal={forced.forced_was_optimal}.");
    }

    void ResetDistalAdviceState()
    {
        if (State == null)
            return;

        State.distal_advice_visible = false;
        State.distal_advice_reliable = false;
        State.distal_advice_choice = null;
        State.distal_best_valley = null;
        State.distal_scan_choice = null;
        DistalAdviceExplanation = ExplanationRuntimeState.None();
    }

    void ResetForcedChoiceState()
    {
        if (State == null)
            return;

        State.meta_choice_is_forced = false;
        State.meta_choice_forced_value = null;
        State.distal_choice_is_forced = false;
        State.distal_choice_forced_scan_side = null;
        State.distal_choice_forced_value = null;
        State.distal_choice_forced_was_optimal = null;
        State.distal_choice_forced_optimal_probability = null;
        ResetTrialForcedChoiceState();
    }

    void ResetTrialForcedChoiceState()
    {
        if (State == null)
            return;

        State.proximal_choice_is_forced = false;
        State.proximal_choice_forced_value = null;
        State.proximal_choice_forced_was_optimal = null;
        State.proximal_choice_forced_probability = CurrentBlock != null
            ? Mathf.Clamp01(CurrentBlock.proximal_forced_probability)
            : 0f;
        State.proximal_choice_forced_optimal_probability = null;
        State.motor_choice_is_forced = false;
        State.motor_choice_forced_set = null;
        State.motor_choice_forced_probability = CurrentBlock != null
            ? Mathf.Clamp01(CurrentBlock.motor_forced_probability)
            : 0f;
    }

    void ResetAllExplanationStates()
    {
        DistalAdviceExplanation = ExplanationRuntimeState.None();
        ResetTrialExplanationStates();
    }

    void ResetTrialExplanationStates()
    {
        ProximalAdviceExplanation = ExplanationRuntimeState.None();
        MotorAdviceExplanation = ExplanationRuntimeState.None();
    }

    ValleyChoice ResolveValleyFromDistalScanChoice(string scanChoice)
    {
        ValleyChoice bestValley = BlockDrawResolver.DrawMostRewardingValley(CurrentBlock, CreateCurrentBlockRandom(1));
        bool choseBestScan = scanChoice == State.distal_best_valley;

        if (choseBestScan)
            return bestValley;

        return GetOppositeValley(bestValley);
    }

    System.Random CreateCurrentBlockRandom(int salt)
    {
        long seed = ResolveDistalSeedForCurrentBlock();
        return new System.Random((int)SeedUtility.DeriveTrialSeed(seed, salt));
    }

    System.Random CreateCurrentTrialRandom(string scope)
    {
        long seed = CurrentTrialSeed != 0
            ? CurrentTrialSeed
            : SeedUtility.DeriveTrialSeed(ResolveBlockSeedForCurrentBlock(), State?.current_trial_index ?? 0);

        return new System.Random(SeedUtility.DeriveScopedSeed(seed, scope));
    }

    static ValleyChoice GetOppositeValley(ValleyChoice valley)
    {
        return valley == ValleyChoice.A ? ValleyChoice.B : ValleyChoice.A;
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
            GamePhase.Break => _breakSceneName,
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
            return SeedUtility.GenerateSeed();

        MapGenConfig map = State != null && State.valley_choice == ValleyChoice.B
            ? block.valley_b
            : block.valley_a;

        if (map == null)
            return SeedUtility.GenerateSeed();

        if (map.seed == 0)
            map.seed = SeedUtility.GenerateSeed();

        return map.seed;
    }

    long ResolveDistalSeedForCurrentBlock()
    {
        var block = CurrentBlock;
        if (block == null)
            return SeedUtility.GenerateSeed();

        if (block.valley_a != null && block.valley_a.seed != 0)
            return block.valley_a.seed;

        if (block.valley_b != null && block.valley_b.seed != 0)
            return block.valley_b.seed;

        long seed = SeedUtility.GenerateSeed();
        if (block.valley_a != null)
            block.valley_a.seed = seed;

        return seed;
    }
}
