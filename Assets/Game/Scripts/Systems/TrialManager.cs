using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Collecte les données du trial courant dans ProximalScene.
//
// Responsabilités :
//   - Ouvrir un nouveau trial local au démarrage du gameplay
//   - Enregistrer les pas du joueur et la config de map
//   - Assembler la TrialResponseRow complète en fin de trial (blocs tutorial inclus)
//   - Déléguer l'envoi à ApiClient
// -----------------------------

public class TrialManager : MonoBehaviour
{
    [Serializable]
    struct CloudInfo
    {
        public int x;
        public int y;
        public int totalBugs;
        public float greenRatio;
    }

    [Serializable]
    struct MiniMapCfg
    {
        public int gridWidth;
        public int gridHeight;
        public CloudInfo leftCloud;
        public CloudInfo rightCloud;
        public List<CellState> cells;
    }

    [Serializable]
    struct CellState
    {
        public int x;
        public int y;
        public bool trap;
        public bool path;   // PathLeft ou PathRight (les deux chemins optimaux sont réservés avant les pièges)
        public bool wall;
        public bool cloud;
        public bool suboptimalPath; // tracé affiché quand le chemin proposé est un détour (ni PathLeft ni PathRight)
        public bool playerStart;
    }

    readonly List<PlayerStep> _playerPathSteps = new();

    TrialResponseRow _currentTrialRow;
    string _startedAtIsoUtc;
    MiniMapCfg? _pendingMapCfg;
    bool _currentTrialSubmitted;

    public void StartNewTrial()
    {
        _playerPathSteps.Clear();
        _startedAtIsoUtc = DateTime.UtcNow.ToString("o");
        _currentTrialRow = BuildBaseRow();
        _currentTrialSubmitted = false;

        if (_pendingMapCfg.HasValue)
        {
            _currentTrialRow.map_config = BuildMapConfigJson(_pendingMapCfg.Value);
            _pendingMapCfg = null;
        }
    }

    public void RecordMove(Vector2Int position)
    {
        if (_currentTrialRow == null)
            return;

        _playerPathSteps.Add(new PlayerStep(position, DateTime.UtcNow.ToString("o")));
    }

    public void EndCurrentTrial(
        string playerChoice,
        bool correct,
        string trueCloud,
        int greenBugsCollected,
        int trapsHit,
        int steps,
        int overtimeSteps,
        bool followedAdvisorPath,
        IReadOnlyList<Vector2Int> advisorPathCells,
        bool optimalPathVisible,
        bool pathIsSuboptimal,
        bool proximalAdviceReliable)
    {
        if (_currentTrialRow == null)
            _currentTrialRow = BuildBaseRow();

        FlowController.Instance?.FinalizeCurrentExplanationTimers();
        ApplyForcedChoiceState(_currentTrialRow);

        _currentTrialRow.proximal_choice = playerChoice;
        _currentTrialRow.choice_correct = correct;
        _currentTrialRow.true_cloud = trueCloud;
        _currentTrialRow.green_bugs_collected = greenBugsCollected;
        _currentTrialRow.green_bugs_accumulated = FlowController.Instance != null
            ? FlowController.Instance.GetAccumulatedScoreAfterTrial(greenBugsCollected)
            : greenBugsCollected;
        _currentTrialRow.green_bugs_session_total = FlowController.Instance != null
            ? FlowController.Instance.GetSessionScoreAfterTrial(greenBugsCollected)
            : greenBugsCollected;
        _currentTrialRow.traps_hit = trapsHit;
        _currentTrialRow.steps = steps;
        _currentTrialRow.overtime_steps = overtimeSteps;
        _currentTrialRow.followed_advisor_path = followedAdvisorPath;
        _currentTrialRow.optimal_path_visible = optimalPathVisible;
        _currentTrialRow.path_is_suboptimal = pathIsSuboptimal;
        _currentTrialRow.proximal_advice_reliable = proximalAdviceReliable;
        _currentTrialRow.player_path_log = FlowSerializationUtility.ToPlayerStepsJson(_playerPathSteps);
        _currentTrialRow.advisor_path_config = optimalPathVisible
            ? FlowSerializationUtility.ToPathCellsJson(advisorPathCells)
            : null;
        _currentTrialRow.started_at = _startedAtIsoUtc;
        _currentTrialRow.ended_at = DateTime.UtcNow.ToString("o");
        FlowController.Instance?.ApplyCurrentExplanationStatesToRow(_currentTrialRow);

        Debug.Log("[TrialManager] Trial termine: envoi en attente des reponses questionnaire.");
    }

    public void SubmitCurrentTrialResponses(IReadOnlyList<QuestionResponse> responses)
    {
        if (_currentTrialRow == null)
        {
            Debug.LogWarning("[TrialManager] Aucun trial courant a envoyer.");
            return;
        }

        if (_currentTrialSubmitted)
        {
            Debug.LogWarning("[TrialManager] Trial deja envoye, soumission ignoree.");
            return;
        }

        FlowSerializationUtility.ApplyQuestionnaireResponses(_currentTrialRow, responses);

        // L'acceptabilite n'est pas posee sans advisor (advisor_choice = none) :
        // le trial part quand meme, le champ reste null et est omis du payload.
        if (string.IsNullOrWhiteSpace(_currentTrialRow.sens_of_agency_question))
        {
            Debug.LogWarning("[TrialManager] Reponses questionnaire incompletes: envoi du trial annule.");
            return;
        }

        string humanLikenessQuestion = _currentTrialRow.human_likeness_question;
        _currentTrialRow.human_likeness_question = null;

        if (ApiClient.Instance == null)
        {
            Debug.LogWarning("[TrialManager] ApiClient introuvable: impossible d'envoyer le trial.");
            return;
        }

        string participantId = _currentTrialRow.participant_id;
        int blockIndex = _currentTrialRow.block_index;
        int trialCount = _currentTrialRow.trial_count;

        _currentTrialSubmitted = true;
        ApiClient.Instance.SendTrialResponse(
            _currentTrialRow,
            trialResponseId =>
            {
                Debug.Log($"[TrialManager] Trial envoye avec succes (id={trialResponseId}).");

                if (!string.IsNullOrWhiteSpace(humanLikenessQuestion))
                {
                    ApiClient.Instance.QueueHumanLikenessPatchForBlock(
                        participantId,
                        blockIndex,
                        trialCount,
                        humanLikenessQuestion,
                        () => Debug.Log($"[TrialManager] Human-likeness patche sur le bloc {blockIndex}."),
                        error => Debug.LogWarning($"[TrialManager] Echec patch human-likeness: {error}"));
                }
            },
            error => Debug.LogWarning($"[TrialManager] Echec envoi trial: {error}"));
    }

    public void SetOptimalPathLength(int value)
    {
        EnsureCurrentTrialRow();
        _currentTrialRow.optimal_path_length = value;
    }

    public void SetCloudDistance(int value)
    {
        EnsureCurrentTrialRow();
        _currentTrialRow.cloud_distance = value;
    }

    public void SetMapConfig(
        Vector2Int gridSize,
        Vector2Int leftCell,
        int leftBugs,
        float leftGreenRatio,
        Vector2Int rightCell,
        int rightBugs,
        float rightGreenRatio)
    {
        var cfg = new MiniMapCfg
        {
            gridWidth = gridSize.x,
            gridHeight = gridSize.y,
            leftCloud = new CloudInfo
            {
                x = leftCell.x,
                y = leftCell.y,
                totalBugs = leftBugs,
                greenRatio = leftGreenRatio
            },
            rightCloud = new CloudInfo
            {
                x = rightCell.x,
                y = rightCell.y,
                totalBugs = rightBugs,
                greenRatio = rightGreenRatio
            }
        };

        if (_currentTrialRow != null)
        {
            _currentTrialRow.map_config = BuildMapConfigJson(cfg);
            return;
        }

        _pendingMapCfg = cfg;
    }

    // Sérialise la config de map en y ajoutant l'état de chaque cellule de la grille.
    // Ne doit être appelé qu'une fois la génération terminée (paths, murs, pièges posés),
    // c'est-à-dire au plus tôt dans StartNewTrial (SessionManager.Start, ordre 0).
    string BuildMapConfigJson(MiniMapCfg cfg)
    {
        cfg.cells = new List<CellState>();

        var registry = LevelRegistry.Instance;
        if (registry != null)
        {
            for (int y = 0; y < registry.gridSize.y; y++)
            {
                for (int x = 0; x < registry.gridSize.x; x++)
                {
                    var flags = registry.GetFlags(new Vector2Int(x, y));
                    cfg.cells.Add(new CellState
                    {
                        x = x,
                        y = y,
                        trap = (flags & LevelRegistry.CellFlags.Trap) != 0,
                        path = (flags & (LevelRegistry.CellFlags.PathLeft | LevelRegistry.CellFlags.PathRight)) != 0,
                        wall = (flags & LevelRegistry.CellFlags.Wall) != 0,
                        cloud = (flags & LevelRegistry.CellFlags.BugCloud) != 0,
                        suboptimalPath = (flags & LevelRegistry.CellFlags.SuboptimalPath) != 0,
                        playerStart = (flags & LevelRegistry.CellFlags.PlayerStart) != 0,
                    });
                }
            }
        }

        return JsonUtility.ToJson(cfg);
    }

    TrialResponseRow BuildBaseRow()
    {
        var session = SessionManager.Instance;
        var flow = FlowController.Instance;
        var block = flow != null ? flow.CurrentBlock : null;
        var map = flow != null ? flow.ActiveMapConfig : null;

        int blockIndex = flow != null && flow.State != null ? flow.State.current_block_index + 1 : (session != null ? session.blockId : 1);
        int trialIndex = flow != null && flow.State != null ? flow.State.current_trial_index + 1 : 1;
        string participantId = flow != null && flow.State != null && !string.IsNullOrWhiteSpace(flow.State.participant_id)
            ? flow.State.participant_id
            : Guid.NewGuid().ToString();

        var row = new TrialResponseRow
        {
            participant_id = participantId,
            session_template_id = flow != null && flow.State != null
                ? flow.State.session_template_id
                : "debug-session-template",
            session_name = flow != null && flow.Config != null ? flow.Config.label : null,
            build_version = session != null ? session.buildVersion : (flow != null ? flow.BuildVersion : "debug-build"),
            block_template_id = block != null ? block.block_template_id : null,
            block_index = blockIndex,
            trial_index = trialIndex,
            trial_count = block != null ? block.trial_count : 1,
            is_tutorial = block != null && block.is_tutorial,
            advisor_choice = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.advisor_choice) : "none",
            valley_choice = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.valley_choice) : null,
            distal_advice_visible_probability = block != null ? block.distal_advice_visible_probability : 0f,
            distal_advice_reliable_probability = block != null ? block.distal_advice_reliable_probability : 0f,
            show_numerical_feedback = block == null || block.show_numerical_feedback,
            distal_advice_visible = flow != null && flow.State != null && flow.State.distal_advice_visible,
            distal_advice_reliable = flow != null && flow.State != null && flow.State.distal_advice_reliable,
            distal_advice_choice = flow != null && flow.State != null ? flow.State.distal_advice_choice : null,
            distal_best_valley = flow != null && flow.State != null ? flow.State.distal_best_valley : null,
            distal_scan_choice = flow != null && flow.State != null ? flow.State.distal_scan_choice : null,
            distal_scene = block != null && block.distal_scene != null ? block.distal_scene.DeepClone() : new DistalSceneConfig(),
            trap_count = session != null ? session.trapCount : map?.trap_count ?? 0,
            min_distance = session != null ? session.minDistance : map?.min_distance ?? 0,
            max_distance = session != null ? session.maxDistance : map?.max_distance ?? 0,
            min_total_bugs = session != null ? session.minTotalBugs : map?.min_total_bugs ?? 0,
            max_total_bugs = session != null ? session.maxTotalBugs : map?.max_total_bugs ?? 0,
            min_green_ratio = session != null ? session.minGreenBugsRatio : map?.min_green_ratio ?? 0f,
            max_green_ratio = session != null ? session.maxGreenBugsRatio : map?.max_green_ratio ?? 0f,
            gap_min = session != null ? session.gapMin : map?.gap_min ?? 0f,
            gap_max = session != null ? session.gapMax : map?.gap_max ?? 0f,
            fog_probability = session != null ? session.fogProbability : map?.fog_probability ?? 0f,
            trial_seed = session != null ? session.randomizationSeed : flow?.CurrentTrialSeed ?? 0,
            path_visible_probability = session != null ? session.pathVisible : map?.path_visible_probability ?? 0f,
            suboptimal_path_probability = session != null ? session.suboptimalPathProbability : map?.suboptimal_path_probability ?? 0f,
            detour_probability = session != null ? session.detourProbability : map?.detour_probability ?? 0f,
            proximal_advice_reliable_probability = session != null ? session.proximalAdviceReliableProbability : map?.proximal_advice_reliable_probability ?? 0f,
            motor_advice_visible_probability = session != null ? session.motorAdviceVisibleProbability : map?.motor_advice_visible_probability ?? 0f,
            motor_advice_reliable_probability = session != null ? session.motorAdviceReliableProbability : map?.motor_advice_reliable_probability ?? 0f,
            suboptimal_trap_probability = session != null ? session.suboptimalTrapProbability : map?.suboptimal_trap_probability ?? 0f,
            min_suboptimal_traps = session != null ? session.minSuboptimalTraps : map?.min_suboptimal_traps ?? 0,
            max_suboptimal_traps = session != null ? session.maxSuboptimalTraps : map?.max_suboptimal_traps ?? 0,
            started_at = _startedAtIsoUtc
        };

        ApplyForcedChoiceState(row);
        flow?.ApplyCurrentExplanationStatesToRow(row);
        return row;
    }

    static void ApplyForcedChoiceState(TrialResponseRow row)
    {
        if (row == null)
            return;

        var flow = FlowController.Instance;
        var block = flow != null ? flow.CurrentBlock : null;
        if (block != null)
        {
            row.advisor_forced = block.advisor_forced;
            row.advisor_forced_value = block.advisor_forced_value;
            row.distal_forced = block.distal_forced;
            row.distal_forced_optimal_probability = block.distal_forced_optimal_probability;
            row.proximal_forced_probability = block.proximal_forced_probability;
            row.proximal_forced_optimal_probability = block.proximal_forced_optimal_probability;
            row.motor_forced_probability = block.motor_forced_probability;
            row.motor_forced_set = block.motor_forced_set;
            return;
        }

        if (flow == null || flow.State == null)
            return;

        row.advisor_forced = flow.State.meta_choice_is_forced;
        row.advisor_forced_value = flow.State.meta_choice_forced_value;
        row.distal_forced = flow.State.distal_choice_is_forced;
        row.distal_forced_optimal_probability = flow.State.distal_choice_forced_optimal_probability ?? 0f;
        row.proximal_forced_probability = flow.State.proximal_choice_forced_probability;
        row.proximal_forced_optimal_probability = flow.State.proximal_choice_forced_optimal_probability ?? 0f;
        row.motor_forced_probability = flow.State.motor_choice_forced_probability;
        row.motor_forced_set = flow.State.motor_choice_forced_set;
    }

    void EnsureCurrentTrialRow()
    {
        if (_currentTrialRow == null)
            _currentTrialRow = BuildBaseRow();
    }

}
