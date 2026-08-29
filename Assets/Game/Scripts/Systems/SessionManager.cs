using System.Collections;
using UnityEngine;

// -----------------------------
// Façade locale du ProximalScene.
//
// Responsabilités :
//   - Exposer aux spawners la config du trial courant (Map)
//   - Poser la seed du trial dans LevelRegistry
//   - Lancer le premier round
//
// Map EST la config active du FlowController (ActiveMapConfig) : plus aucune
// copie champ par champ susceptible de diverger. L'état de flux (advisor,
// forçages) est lu en direct dans FlowController. Les champs Inspector
// "Sandbox" ne servent qu'à construire Map quand la scène est lancée sans
// FlowController.
// -----------------------------

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Références")]
    public GameManager gameManager;

    [Header("Session")]
    public long randomizationSeed;

    [Header("Sandbox : Map")]
    public int trapCount = 10;
    public int minDistance = 3;
    public int maxDistance;

    [Header("Sandbox : Discrimination")]
    public int minTotalBugs = 20;
    public int maxTotalBugs = 80;
    public float minGreenBugsRatio = 0.4f;
    public float maxGreenBugsRatio = 0.8f;
    public float gapMin = 0.1f;
    public float gapMax = 0.3f;

    [Header("Sandbox : Advisor")]
    public float pathVisible = 1f;
    [Range(0f, 1f)] public float suboptimalPathProbability;
    [Range(0f, 1f)] public float detourProbability;
    [Range(0f, 1f)] public float proximalAdviceReliableProbability = 1f;
    [Range(0f, 1f)] public float motorAdviceVisibleProbability = 1f;
    [Range(0f, 1f)] public float motorAdviceReliableProbability = 1f;
    [Range(0f, 1f)] public float suboptimalTrapProbability;
    [Min(0)] public int minSuboptimalTraps = 1;
    [Min(0)] public int maxSuboptimalTraps = 3;

    [Header("Sandbox : Fog of War")]
    [Range(0f, 1f)] public float fogProbability;

    [Header("Sandbox : Protocole")]
    public int blockId = 1;

    // Config de map du trial courant : ActiveMapConfig en mode flow, valeurs
    // Inspector en mode sandbox. Source de vérité unique pour les spawners et
    // pour TrialManager.BuildBaseRow.
    public MapGenConfig Map { get; private set; }

    // État de flux lu en direct : pas de copie locale qui pourrait diverger.
    // Les valeurs de repli couvrent le mode sandbox (FlowController absent).
    public bool HasAdvisor =>
        FlowController.Instance == null ||
        FlowController.Instance.State == null ||
        FlowController.Instance.State.advisor_choice != AdvisorType.None;

    public bool ProximalChoiceIsForced =>
        FlowController.Instance != null &&
        FlowController.Instance.State != null &&
        FlowController.Instance.State.proximal_choice_is_forced;

    public bool MotorChoiceIsForced =>
        FlowController.Instance != null &&
        FlowController.Instance.State != null &&
        FlowController.Instance.State.motor_choice_is_forced;

    public string MotorChoiceForcedSet =>
        FlowController.Instance != null && FlowController.Instance.State != null
            ? FlowController.Instance.State.motor_choice_forced_set
            : null;

    public bool ShouldShowEquipmentFailureOverlay =>
        FlowController.Instance != null &&
        FlowController.Instance.ShouldShowEquipmentFailureOverlay;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        var flow = FlowController.Instance;
        MapGenConfig activeMap = flow != null && flow.HasLoadedConfig ? flow.ActiveMapConfig : null;

        if (activeMap != null)
        {
            Map = activeMap;
            randomizationSeed = flow.CurrentTrialSeed != 0 ? flow.CurrentTrialSeed : Map.seed;
            Debug.Log(
                $"[SessionManager] Config active lue depuis FlowController " +
                $"(block={flow.State.current_block_index + 1}, tutorial={flow.IsCurrentBlockTutorial}, seed={randomizationSeed}).");
        }
        else
        {
            Map = BuildSandboxMap();
            Debug.LogWarning("[SessionManager] FlowController absent: utilisation des valeurs sandbox de la scene.");
        }

        ApplySeedForThisTrial();
    }

    IEnumerator Start()
    {
        yield return null;
        gameManager?.BeginFirstRound();
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
            randomizationSeed = SeedUtility.GenerateSeed();

        registry.SetRoundSeed(randomizationSeed);
    }

    MapGenConfig BuildSandboxMap()
    {
        return new MapGenConfig
        {
            trap_count = trapCount,
            min_distance = minDistance,
            max_distance = maxDistance,
            min_total_bugs = minTotalBugs,
            max_total_bugs = maxTotalBugs,
            min_green_ratio = minGreenBugsRatio,
            max_green_ratio = maxGreenBugsRatio,
            gap_min = gapMin,
            gap_max = gapMax,
            path_visible_probability = pathVisible,
            suboptimal_path_probability = suboptimalPathProbability,
            detour_probability = detourProbability,
            proximal_advice_reliable_probability = proximalAdviceReliableProbability,
            motor_advice_visible_probability = motorAdviceVisibleProbability,
            motor_advice_reliable_probability = motorAdviceReliableProbability,
            suboptimal_trap_probability = suboptimalTrapProbability,
            min_suboptimal_traps = minSuboptimalTraps,
            max_suboptimal_traps = maxSuboptimalTraps,
            fog_probability = fogProbability,
            seed = randomizationSeed
        };
    }
}
