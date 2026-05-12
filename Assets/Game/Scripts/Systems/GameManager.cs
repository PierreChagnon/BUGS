using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// -----------------------------
// Orchestre l'état d'un trial de gameplay.
//
// Responsabilités :
//   - Etat runtime du round (steps, traps, score, chemin suivi)
//   - Recevoir les signaux des entités (player, trap, bug cloud)
//   - Transmettre un résultat complet au TrialManager
//   - Laisser FlowController piloter la transition vers le trial suivant
// -----------------------------

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Références")]
    public TrialManager trialManager;
    [SerializeField] private GameObject _getReadyPanel;
    [SerializeField] float getReadyDuration = 3f;

    [Header("Round / Score")]
    public int steps;
    public int trapsHit;
    public int overtimeSteps;
    public int bugsCollected;
    public bool followedAdvisorPath = true;

    [Header("Audio")]
    [Tooltip("SFX joué quand un piège est déclenché (couvre trigger physique + fallback grille).")]
    [SerializeField] private SoundEffect _sfxTrap;
    [Tooltip("SFX joué à la collecte d'un nuage — partie bugs verts.")]
    [SerializeField] private SoundEffect _sfxBugGreen;
    [Tooltip("SFX joué à la collecte d'un nuage — partie bugs rouges.")]
    [SerializeField] private SoundEffect _sfxBugRed;

    bool _roundOver;
    bool _pathIsSuboptimal;
    bool _advisorPathVisible = true;

    public bool inputLocked { get; private set; }

    BugCloud _leftCloud;
    BugCloud _rightCloud;
    readonly HashSet<Vector2Int> _advisorPath = new();

    [Serializable]
    public struct RoundEndInfo
    {
        public int bugsCollected;
        public int trapsHit;
        public int overtimeSteps;
        public int steps;
        public bool followedAdvisorPath;
        public int leftCloudGreenBugs;
        public int rightCloudGreenBugs;
        public bool optimalPathVisible;
    }

    public event Action<RoundEndInfo> OnRoundEnded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[GameManager] Awake");
    }

    public void BeginFirstRound()
    {
        inputLocked = true;
        trialManager.StartNewTrial();
        StartCoroutine(GetReadySequence());
    }

    private IEnumerator GetReadySequence()
    {
        if (_getReadyPanel != null)
        {
            _getReadyPanel.SetActive(true);
            yield return new WaitForSeconds(getReadyDuration);
            _getReadyPanel.SetActive(false);
        }

        inputLocked = false;
    }

    public void ContinueAfterRound()
    {
        if (!_roundOver)
            return;

        if (FlowController.Instance != null)
        {
            FlowController.Instance.OnTrialComplete(bugsCollected);
            return;
        }

        RestartRound();
    }

    public void RestartRound()
    {
        Debug.Log("[GameManager] Redemarrage du round en mode debug.");
        inputLocked = false;
        _roundOver = false;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void RegisterClouds(BugCloud a, BugCloud b)
    {
        if (a == null || b == null)
            return;

        if (a.transform.position.x <= b.transform.position.x)
        {
            _leftCloud = a;
            _rightCloud = b;
        }
        else
        {
            _leftCloud = b;
            _rightCloud = a;
        }

        if (trialManager != null && LevelRegistry.Instance != null)
        {
            var registry = LevelRegistry.Instance;
            trialManager.SetMapConfig(
                registry.gridSize,
                registry.WorldToCell(_leftCloud.transform.position), _leftCloud.totalBugs, _leftCloud.greenRatio,
                registry.WorldToCell(_rightCloud.transform.position), _rightCloud.totalBugs, _rightCloud.greenRatio);
        }
    }

    public void SetChosenPath(IEnumerable<Vector2Int> cells)
    {
        _advisorPath.Clear();
        if (cells != null)
        {
            foreach (var cell in cells)
                _advisorPath.Add(cell);
        }

        followedAdvisorPath = true;
    }

    public void SetPathIsSuboptimal(bool isSuboptimal)
    {
        _pathIsSuboptimal = isSuboptimal;
    }

    public void SetAdvisorPathVisible(bool isVisible)
    {
        _advisorPathVisible = isVisible;
    }

    public void OnPlayerStep(Vector2Int cell)
    {
        if (_roundOver)
            return;

        steps++;

        LevelRegistry.Instance?.MarkVisited(cell);
        FogController.Instance?.RevealCell(cell);

        if (_advisorPath.Count > 0 && !_advisorPath.Contains(cell))
            followedAdvisorPath = false;

        int movesMade = steps - 1;
        var registry = LevelRegistry.Instance;
        if (registry != null && registry.stepBudget > 0 && movesMade > registry.stepBudget)
            OnStepBudgetExceeded();

        trialManager?.RecordMove(cell);

        // Fallbacks déterministes si les triggers physiques ratent.
        if (LevelRegistry.Instance != null && LevelRegistry.Instance.HasTrap(cell))
        {
            OnTrapTriggered();
            LevelRegistry.Instance.UnregisterTrap(cell);
        }

        TryCollectCloudAtCell(cell);
    }

    void TryCollectCloudAtCell(Vector2Int playerCell)
    {
        if (_roundOver)
            return;

        var registry = LevelRegistry.Instance;
        if (registry == null)
            return;

        BugCloud cloudToCollect = null;

        if (_leftCloud != null && registry.WorldToCell(_leftCloud.transform.position) == playerCell)
            cloudToCollect = _leftCloud;
        else if (_rightCloud != null && registry.WorldToCell(_rightCloud.transform.position) == playerCell)
            cloudToCollect = _rightCloud;

        if (cloudToCollect == null)
            return;

        Debug.Log("[GameManager] Nuage détecté sur la cellule du joueur : collecte forcée (fallback grille).");
        OnCloudCollected(cloudToCollect);
    }

    void OnStepBudgetExceeded()
    {
        if (_roundOver)
            return;

        overtimeSteps++;
        _leftCloud?.AddBugs(-2);
        _rightCloud?.AddBugs(-2);

        Debug.Log($"[GameManager] Depassement du budget de pas ! overtimeSteps={overtimeSteps}");
    }

    public void OnTrapTriggered()
    {
        if (_roundOver)
            return;

        trapsHit++;
        _leftCloud?.AddBugs(-2);
        _rightCloud?.AddBugs(-2);

        if (_sfxTrap != null)
            AudioManager.Instance?.PlaySfx(_sfxTrap);

        Debug.Log($"[GameManager] Piege ! trapsHit={trapsHit}");
    }

    public void OnInvalidMoveKeyPressed()
    {
        if (_roundOver)
            return;

        _leftCloud?.AddBugs(-2);
        _rightCloud?.AddBugs(-2);

        Debug.Log("[GameManager] Touche invalide ! Penalite -2 bugs sur chaque nuage.");
    }

    public void OnCloudCollected(BugCloud cloud)
    {
        if (_roundOver)
            return;

        _roundOver = true;
        inputLocked = true;

        if (_sfxBugGreen != null)
            AudioManager.Instance?.PlaySfx(_sfxBugGreen);
        if (_sfxBugRed != null)
            AudioManager.Instance?.PlaySfx(_sfxBugRed);

        FogController.Instance?.RevealAll();

        bugsCollected = cloud != null
            ? Mathf.Max(0, Mathf.RoundToInt(cloud.totalBugs * cloud.greenRatio))
            : 0;

        if (trialManager != null)
        {
            string choice = cloud == _leftCloud
                ? "left"
                : cloud == _rightCloud
                    ? "right"
                    : "unknown";

            BugCloud bestCloud = GetBestCloud();
            bool correct = bestCloud != null && cloud == bestCloud;
            string trueCloud = bestCloud == _leftCloud
                ? "left"
                : bestCloud == _rightCloud
                    ? "right"
                    : "none";

            if (LevelRegistry.Instance != null)
            {
                trialManager.SetOptimalPathLength(LevelRegistry.Instance.optimalPathLength);
                trialManager.SetCloudDistance(LevelRegistry.Instance.stepBudget);
            }

            trialManager.EndCurrentTrial(
                choice,
                correct,
                trueCloud,
                bugsCollected,
                trapsHit,
                steps,
                overtimeSteps,
                followedAdvisorPath,
                _advisorPathVisible,
                _pathIsSuboptimal);
        }

        OnRoundEnded?.Invoke(new RoundEndInfo
        {
            bugsCollected = bugsCollected,
            trapsHit = trapsHit,
            overtimeSteps = overtimeSteps,
            steps = steps,
            followedAdvisorPath = followedAdvisorPath,
            leftCloudGreenBugs = _leftCloud ? Mathf.RoundToInt(_leftCloud.totalBugs * _leftCloud.greenRatio) : 0,
            rightCloudGreenBugs = _rightCloud ? Mathf.RoundToInt(_rightCloud.totalBugs * _rightCloud.greenRatio) : 0,
            optimalPathVisible = _advisorPathVisible
        });
    }

    public BugCloud GetBestCloud()
    {
        if (_leftCloud == null || _rightCloud == null)
            return null;

        if (Mathf.Approximately(_leftCloud.greenRatio, _rightCloud.greenRatio))
            return null;

        return _leftCloud.greenRatio > _rightCloud.greenRatio ? _leftCloud : _rightCloud;
    }
}
