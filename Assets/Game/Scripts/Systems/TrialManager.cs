using System;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Collecte les données du trial courant dans ProximalScene.
//
// Responsabilités :
//   - Ouvrir un nouveau trial local au démarrage du gameplay
//   - Enregistrer les pas du joueur et la config de map
//   - Assembler la TrialResponseRow complète en fin de trial
//   - Déléguer l'envoi à ApiClient (sauf tutorial)
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
    }

    readonly List<PlayerStep> _playerPathSteps = new();

    TrialResponseRow _currentTrialRow;
    string _startedAtIsoUtc;
    string _pendingMapConfigJson;
    bool _currentTrialSubmitted;

    public void StartNewTrial()
    {
        _playerPathSteps.Clear();
        _startedAtIsoUtc = DateTime.UtcNow.ToString("o");
        _currentTrialRow = BuildBaseRow();
        _currentTrialSubmitted = false;

        if (!string.IsNullOrWhiteSpace(_pendingMapConfigJson))
        {
            _currentTrialRow.map_config = _pendingMapConfigJson;
            _pendingMapConfigJson = null;
        }

        Debug.Log($"[TrialManager] Nouveau trial initialise: block={_currentTrialRow.block_index}, trial={_currentTrialRow.trial_index}, seed={_currentTrialRow.trial_seed}");
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
        bool optimalPathVisible,
        bool pathIsSuboptimal)
    {
        if (_currentTrialRow == null)
            _currentTrialRow = BuildBaseRow();

        _currentTrialRow.proximal_choice = playerChoice;
        _currentTrialRow.choice_correct = correct;
        _currentTrialRow.true_cloud = trueCloud;
        _currentTrialRow.green_bugs_collected = greenBugsCollected;
        _currentTrialRow.green_bugs_accumulated = FlowController.Instance != null
            ? FlowController.Instance.GetAccumulatedScoreAfterTrial(greenBugsCollected)
            : greenBugsCollected;
        _currentTrialRow.traps_hit = trapsHit;
        _currentTrialRow.steps = steps;
        _currentTrialRow.overtime_steps = overtimeSteps;
        _currentTrialRow.followed_advisor_path = followedAdvisorPath;
        _currentTrialRow.optimal_path_visible = optimalPathVisible;
        _currentTrialRow.path_is_suboptimal = pathIsSuboptimal;
        _currentTrialRow.player_path_log = FlowSerializationUtility.ToPlayerStepsJson(_playerPathSteps);
        _currentTrialRow.started_at = _startedAtIsoUtc;
        _currentTrialRow.ended_at = DateTime.UtcNow.ToString("o");

        if (FlowController.Instance != null && FlowController.Instance.IsCurrentBlockTutorial)
        {
            Debug.Log("[TrialManager] Trial tutorial termine: envoi reseau ignore.");
            return;
        }

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

        if (string.IsNullOrWhiteSpace(_currentTrialRow.acceptability_question) ||
            string.IsNullOrWhiteSpace(_currentTrialRow.sens_of_agency_question))
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
                if (FlowController.Instance != null)
                    FlowController.Instance.RegisterLastTrialResponse(trialResponseId);

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

        SetMapConfigJson(JsonUtility.ToJson(cfg));
    }

    public void SetMapConfigJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            Debug.LogWarning("[TrialManager] map_config vide, ignoree.");
            return;
        }

        if (_currentTrialRow != null)
        {
            _currentTrialRow.map_config = json;
            return;
        }

        _pendingMapConfigJson = json;
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

        return new TrialResponseRow
        {
            participant_id = participantId,
            session_template_id = flow != null && flow.State != null
                ? flow.State.session_template_id
                : "debug-session-template",
            build_version = session != null ? session.buildVersion : (flow != null ? flow.BuildVersion : "debug-build"),
            block_index = blockIndex,
            trial_index = trialIndex,
            trial_count = block != null ? block.trial_count : 1,
            advisor_choice = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.advisor_choice) : "none",
            valley_choice = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.valley_choice) : null,
            distal_advice_visible_probability = block != null ? block.distal_advice_visible_probability : 0f,
            distal_advice_reliable_probability = block != null ? block.distal_advice_reliable_probability : 0f,
            distal_advice_visible = flow != null && flow.State != null && flow.State.distal_advice_visible,
            distal_advice_reliable = flow != null && flow.State != null && flow.State.distal_advice_reliable,
            distal_advice_choice = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.distal_advice_choice) : null,
            distal_best_valley = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.distal_best_valley) : null,
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
            path_visible_probability = session != null ? session.pathVisible : map?.path_visible ?? 0f,
            suboptimal_path_probability = session != null ? session.suboptimalPathProbability : map?.suboptimal_path_probability ?? 0f,
            detour_probability = session != null ? session.detourProbability : map?.detour_probability ?? 0f,
            motor_advice_visible_probability = session != null ? session.motorAdviceVisibleProbability : map?.motor_advice_visible_probability ?? 0f,
            motor_advice_reliable_probability = session != null ? session.motorAdviceReliableProbability : map?.motor_advice_reliable_probability ?? 0f,
            suboptimal_trap_probability = session != null ? session.suboptimalTrapProbability : map?.suboptimal_trap_probability ?? 0f,
            min_suboptimal_traps = session != null ? session.minSuboptimalTraps : map?.min_suboptimal_traps ?? 0,
            max_suboptimal_traps = session != null ? session.maxSuboptimalTraps : map?.max_suboptimal_traps ?? 0,
            started_at = _startedAtIsoUtc
        };
    }

    void EnsureCurrentTrialRow()
    {
        if (_currentTrialRow == null)
            _currentTrialRow = BuildBaseRow();
    }
}
