using System.Collections;
using UnityEngine;

// -----------------------------
// Façade locale du ProximalScene.
//
// Responsabilités :
//   - Exposer aux spawners la config du trial courant
//   - Copier la config depuis FlowController
//   - Poser la seed du trial dans LevelRegistry
//
// Si FlowController est absent, SessionManager conserve simplement
// les valeurs deja presentes dans l'Inspector de la scene.
// -----------------------------

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Références")]
    public GameManager gameManager;

    [Header("Session")]
    public long randomizationSeed;
    public string buildVersion = "1.0.0";

    [Header("Recherche : Map")]
    public int trapCount = 10;
    public int minDistance = 3;
    public int maxDistance;

    [Header("Recherche : Discrimination")]
    public int minTotalBugs = 20;
    public int maxTotalBugs = 80;
    public float minGreenBugsRatio = 0.4f;
    public float maxGreenBugsRatio = 0.8f;
    public float gapMin = 0.1f;
    public float gapMax = 0.3f;

    [Header("Recherche : Advisor")]
    public float pathVisible = 1f;
    [Range(0f, 1f)] public float suboptimalPathProbability;
    [Range(0f, 1f)] public float detourProbability;
    [Range(0f, 1f)] public float proximalAdviceReliableProbability = 1f;
    [Range(0f, 1f)] public float motorAdviceVisibleProbability = 1f;
    [Range(0f, 1f)] public float motorAdviceReliableProbability = 1f;
    [Range(0f, 1f)] public float suboptimalTrapProbability;
    [Min(0)] public int minSuboptimalTraps = 1;
    [Min(0)] public int maxSuboptimalTraps = 3;

    [Header("Recherche : Fog of War")]
    [Range(0f, 1f)] public float fogProbability;

    [Header("Recherche : Protocole")]
    public int blockId = 1;

    public bool IsFlowDriven { get; private set; }
    public bool IsTutorialBlock { get; private set; }
    public bool HasAdvisor { get; private set; } = true;
    public bool ProximalChoiceIsForced { get; private set; }
    public string ProximalChoiceForcedValue { get; private set; }
    public bool MotorChoiceIsForced { get; private set; }
    public string MotorChoiceForcedSet { get; private set; }
    public bool ShouldShowEquipmentFailureOverlay { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        IsFlowDriven = CopyConfigFromFlowController();
        if (!IsFlowDriven)
            Debug.LogWarning("[SessionManager] FlowController absent: utilisation des valeurs deja presentes dans la scene.");

        ApplySeedForThisTrial();
    }

    IEnumerator Start()
    {
        yield return null;
        gameManager?.BeginFirstRound();
    }

    public void RefreshForcedValuesFromFlow()
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.State == null)
            return;

        ProximalChoiceIsForced = flow.State.proximal_choice_is_forced;
        ProximalChoiceForcedValue = flow.State.proximal_choice_forced_value;
        ShouldShowEquipmentFailureOverlay = flow.ShouldShowEquipmentFailureOverlay;
    }

    bool CopyConfigFromFlowController()
    {
        var flow = FlowController.Instance;
        if (flow == null || !flow.HasLoadedConfig || flow.ActiveMapConfig == null)
            return false;

        MapGenConfig map = flow.ActiveMapConfig;
        HasAdvisor = flow.State != null && flow.State.advisor_choice != AdvisorType.None;
        ProximalChoiceIsForced = flow.State != null && flow.State.proximal_choice_is_forced;
        ProximalChoiceForcedValue = flow.State != null ? flow.State.proximal_choice_forced_value : null;
        MotorChoiceIsForced = flow.State != null && flow.State.motor_choice_is_forced;
        MotorChoiceForcedSet = flow.State != null ? flow.State.motor_choice_forced_set : null;
        ShouldShowEquipmentFailureOverlay = flow.ShouldShowEquipmentFailureOverlay;

        randomizationSeed = flow.CurrentTrialSeed != 0 ? flow.CurrentTrialSeed : map.seed;
        buildVersion = flow.BuildVersion;

        trapCount = map.trap_count;
        minDistance = map.min_distance;
        maxDistance = map.max_distance;
        minTotalBugs = map.min_total_bugs;
        maxTotalBugs = map.max_total_bugs;
        minGreenBugsRatio = map.min_green_ratio;
        maxGreenBugsRatio = map.max_green_ratio;
        gapMin = map.gap_min;
        gapMax = map.gap_max;
        pathVisible = map.path_visible_probability;
        suboptimalPathProbability = map.suboptimal_path_probability;
        detourProbability = map.detour_probability;
        proximalAdviceReliableProbability = map.proximal_advice_reliable_probability;
        motorAdviceVisibleProbability = map.motor_advice_visible_probability;
        motorAdviceReliableProbability = map.motor_advice_reliable_probability;
        suboptimalTrapProbability = map.suboptimal_trap_probability;
        minSuboptimalTraps = map.min_suboptimal_traps;
        maxSuboptimalTraps = map.max_suboptimal_traps;
        fogProbability = map.fog_probability;

        if (!HasAdvisor)
        {
            pathVisible = 0f;
            motorAdviceVisibleProbability = 0f;
        }

        blockId = flow.State.current_block_index + 1;
        IsTutorialBlock = flow.IsCurrentBlockTutorial;

        Debug.Log($"[SessionManager] Config chargee depuis FlowController (block={blockId}, tutorial={IsTutorialBlock}, seed={randomizationSeed}).");
        return true;
    }

    void ApplySeedForThisTrial()
    {
        var registry = LevelRegistry.Instance;
        if (registry == null)
        {
            Debug.LogWarning("[SessionManager] LevelRegistry introuvable en Awake: seed non appliquee.");
            return;
        }

        if (randomizationSeed == 0)
            randomizationSeed = GenerateSeed();

        registry.SetRoundSeed(randomizationSeed);
    }

    static long GenerateSeed()
    {
        unchecked
        {
            int ticksHash = System.DateTime.UtcNow.Ticks.GetHashCode();
            int guidHash = System.Guid.NewGuid().GetHashCode();
            int seed = (ticksHash ^ guidHash) & 0x7FFFFFFF;
            if (seed == 0)
                seed = 1;

            return seed;
        }
    }
}
