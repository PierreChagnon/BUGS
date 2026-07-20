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
    public int bugsEscaped;
    public bool followedAdvisorPath = true;

    [Header("Audio")]
    [Tooltip("SFX joué quand un piège est déclenché (couvre trigger physique + fallback grille).")]
    [SerializeField] private SoundEffect _sfxTrap;
    [Tooltip("SFX joué à la collecte d'un nuage — partie bugs verts.")]
    [SerializeField] private SoundEffect _sfxBugGreen;
    [Tooltip("SFX joué à la collecte d'un nuage — partie bugs rouges.")]
    [SerializeField] private SoundEffect _sfxBugRed;
    [Tooltip("SFX joué quand le joueur presse une touche hors du set actif (mauvais input).")]
    [SerializeField] private SoundEffect _sfxPlayerMissedKey;

    [Header("Mauvais input")]
    [Tooltip("Durée pendant laquelle les mauvais inputs suivants sont ignorés, en secondes.")]
    [SerializeField, Min(0f)] private float _wrongInputCooldownDuration = 1f;

    bool _roundOver;
    bool _pathIsSuboptimal;
    bool _advisorPathVisible = true;
    Coroutine _wrongInputCooldownCoroutine;

    public bool IsWrongInputCooldownActive { get; private set; }
    public event Action<bool> OnWrongInputCooldownChanged;

    public bool inputLocked { get; private set; }
    public void SetInputLocked(bool value) => inputLocked = value;

    // Verrou externe (ex : overlay tutorial) — GetReadySequence attend sa liberation avant de deverrouiller.
    private bool _inputLockHeld;
    public void HoldInputLock() => _inputLockHeld = true;
    public void ReleaseInputLock() => _inputLockHeld = false;

    BugCloud _leftCloud;
    BugCloud _rightCloud;
    BugCloud _forcedCollectableCloud;
    readonly HashSet<Vector2Int> _advisorPath = new();

    [Serializable]
    public struct RoundEndInfo
    {
        public int bugsCollected;
        public int bugsEscaped;
        public bool choiceCorrect;
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
    }

    public void BeginFirstRound()
    {
        inputLocked = true;
        bugsEscaped = 0;
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

        while (_inputLockHeld) yield return null;
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

        ApplyProximalForcedCloud();

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
        FlowController.Instance?.ResolveProximalExplanationForCurrentTrial(isVisible);
    }

    public void OnPlayerStep(Vector2Int cell)
    {
        if (_roundOver)
            return;

        steps++;

        LevelRegistry.Instance?.MarkVisited(cell);
        if (!IsHiddenForcedCloudCell(cell))
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

        if (_forcedCollectableCloud != null && cloudToCollect != _forcedCollectableCloud)
            return;

        Debug.Log("[GameManager] Nuage détecté sur la cellule du joueur : collecte forcée (fallback grille).");
        OnCloudCollected(cloudToCollect);
    }

    void OnStepBudgetExceeded()
    {
        if (_roundOver)
            return;

        overtimeSteps++;
        _leftCloud?.AddBugs(-1);
        _rightCloud?.AddBugs(-1);

        Debug.Log($"[GameManager] Depassement du budget de pas ! overtimeSteps={overtimeSteps}");
    }

    public void OnTrapTriggered()
    {
        if (_roundOver)
            return;

        trapsHit++;
        _leftCloud?.AddBugs(-1);
        _rightCloud?.AddBugs(-1);

        if (_sfxTrap != null)
            AudioManager.Instance?.PlaySfx(_sfxTrap);

        Debug.Log($"[GameManager] Piege ! trapsHit={trapsHit}");
    }

    // Retourne true si le mauvais input a ete pris en compte (penalite + cooldown declenches),
    // false s'il est ignore (round termine ou cooldown deja actif) — permet de synchroniser
    // un feedback (animation) avec la penalite et le son.
    public bool OnInvalidMoveKeyPressed()
    {
        if (_roundOver || IsWrongInputCooldownActive)
            return false;

        _leftCloud?.AddBugs(-1);
        _rightCloud?.AddBugs(-1);

        if (_sfxPlayerMissedKey != null)
            AudioManager.Instance?.PlaySfx(_sfxPlayerMissedKey);

        Debug.Log("[GameManager] Touche invalide ! Penalite -1 bug vert sur chaque nuage.");

        StartWrongInputCooldown();
        return true;
    }

    void StartWrongInputCooldown()
    {
        IsWrongInputCooldownActive = true;
        OnWrongInputCooldownChanged?.Invoke(true);

        if (_wrongInputCooldownCoroutine != null)
            StopCoroutine(_wrongInputCooldownCoroutine);

        _wrongInputCooldownCoroutine = StartCoroutine(WrongInputCooldownSequence());
    }

    IEnumerator WrongInputCooldownSequence()
    {
        if (_wrongInputCooldownDuration > 0f)
            yield return new WaitForSeconds(_wrongInputCooldownDuration);
        else
            yield return null;

        _wrongInputCooldownCoroutine = null;
        IsWrongInputCooldownActive = false;
        OnWrongInputCooldownChanged?.Invoke(false);
    }

    public bool OnCloudCollected(BugCloud cloud)
    {
        if (_roundOver)
            return false;

        if (_forcedCollectableCloud != null && cloud != _forcedCollectableCloud)
        {
            Debug.Log("[GameManager] Collecte ignoree: cloud non impose sur un trial proximal forced.");
            return false;
        }

        _roundOver = true;
        inputLocked = true;

        if (_sfxBugGreen != null)
            AudioManager.Instance?.PlaySfx(_sfxBugGreen);
        if (_sfxBugRed != null)
            AudioManager.Instance?.PlaySfx(_sfxBugRed);

        FogController.Instance?.RevealAll();

        bugsCollected = cloud != null
            ? cloud.greenBugs
            : 0;
        bugsEscaped = cloud != null ? cloud.bugsEscaped : 0;

        BugCloud bestCloud = GetBestCloud();
        bool correct = cloud == bestCloud;

        if (trialManager != null)
        {
            string choice = cloud == _leftCloud
                ? "left"
                : cloud == _rightCloud
                    ? "right"
                    : "unknown";

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
            bugsEscaped = bugsEscaped,
            choiceCorrect = correct,
            trapsHit = trapsHit,
            overtimeSteps = overtimeSteps,
            steps = steps,
            followedAdvisorPath = followedAdvisorPath,
            leftCloudGreenBugs = _leftCloud ? _leftCloud.greenBugs : 0,
            rightCloudGreenBugs = _rightCloud ? _rightCloud.greenBugs : 0,
            optimalPathVisible = _advisorPathVisible
        });

        return true;
    }

    public BugCloud GetBestCloud()
    {
        if (_leftCloud == null || _rightCloud == null)
            return null;

        return _leftCloud.greenBugs > _rightCloud.greenBugs ? _leftCloud : _rightCloud;
    }

    void ApplyProximalForcedCloud()
    {
        _forcedCollectableCloud = null;

        var flow = FlowController.Instance;
        if (flow == null || flow.State == null || !flow.State.proximal_choice_is_forced)
        {
            SetCloudVisibility(_leftCloud, true);
            SetCloudVisibility(_rightCloud, true);
            return;
        }

        _forcedCollectableCloud = flow.State.proximal_choice_forced_value == DistalScanSide.Right
            ? _rightCloud
            : _leftCloud;

        SetCloudVisibility(_leftCloud, _forcedCollectableCloud == _leftCloud);
        SetCloudVisibility(_rightCloud, _forcedCollectableCloud == _rightCloud);
    }

    bool IsHiddenForcedCloudCell(Vector2Int cell)
    {
        if (_forcedCollectableCloud == null || LevelRegistry.Instance == null)
            return false;

        BugCloud hiddenCloud = _forcedCollectableCloud == _leftCloud ? _rightCloud : _leftCloud;
        return hiddenCloud != null && LevelRegistry.Instance.WorldToCell(hiddenCloud.transform.position) == cell;
    }

    static void SetCloudVisibility(BugCloud cloud, bool visible)
    {
        if (cloud == null)
            return;

        cloud.SetVisible(visible);
        cloud.SetCollectable(visible);
    }
}
