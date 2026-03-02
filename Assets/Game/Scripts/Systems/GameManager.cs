using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// -----------------------------
// Orchestre l'état d'un round : score, cycle de vie, arbitrage.
//
// Responsabilités :
//   - État du round (steps, trapsHit, bugsCollected, followedBestPath)
//   - Références aux nuages L/R, détermination du "meilleur"
//   - Cycle de vie (inputLocked, roundOver, restart)
//   - Orchestration TrialManager (record moves, end trial, send)
//
// Ce qui n'est PAS ici :
//   - UI -> RoundUI écoute OnRoundEnded
//   - Détection piège -> Trap.OnTriggerEnter signale OnTrapTriggered
//   - Construction JSON map config -> TrialManager.SetMapConfig
// -----------------------------

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Références")]
    public TrialManager trialManager;

    [Header("Session")]
    public int blockId = 1;
    int _screenCounter = 0;

    [Header("Round / Score")]
    public int steps = 0;
    public int trapsHit = 0;
    public int bugsCollected = 0;
    public bool followedBestPath = true;

    // État de la manche
    public bool inputLocked { get; private set; } = false;
    bool _roundOver = false;

    // Les 2 nuages du round (références données par le spawner)
    BugCloud _leftCloud, _rightCloud;

    // Chemin conseillé (PathSpawner nous l'enverra)
    readonly HashSet<Vector2Int> _advisorPath = new();

    // --- Événements pour l'UI et les systèmes externes ---

    /// <summary>Données transmises à la fin d'un round.</summary>
    [Serializable]
    public struct RoundEndInfo
    {
        public int bugsCollected;
        public int trapsHit;
        public int steps;
        public bool followedBestPath;
        public int leftCloudBugs;
        public int rightCloudBugs;
    }

    /// <summary>Émis quand le round se termine (nuage collecté). RoundUI s'y abonne.</summary>
    public event Action<RoundEndInfo> OnRoundEnded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[GameManager] Awake");
    }

    // ══════════════════════════════════════════════════════════════
    //  CYCLE DE VIE DU ROUND
    // ══════════════════════════════════════════════════════════════

    /// <summary>Appelée par SessionManager quand la session est prête.</summary>
    public void BeginFirstRound()
    {
        StartNewRound("forest");
    }

    public void StartNewRound(string screenType)
    {
        _screenCounter++;

        long seed = 0;
        if (LevelRegistry.Instance != null && LevelRegistry.Instance.TryGetRoundSeed(out var s))
            seed = s;

        trialManager.StartNewTrial(blockId, _screenCounter, screenType, seed);
    }

    /// <summary>Restart de la scène (appelé par le bouton UI).</summary>
    public void RestartRound()
    {
        Debug.Log("[GameManager] Redémarrage de la manche...");
        inputLocked = false;
        _roundOver = false;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // ══════════════════════════════════════════════════════════════
    //  ÉVÉNEMENTS REÇUS DES ENTITÉS ET SPAWNERS
    // ══════════════════════════════════════════════════════════════

    /// <summary>BugCloudSpawner nous fournit les 2 nuages.</summary>
    public void RegisterClouds(BugCloud a, BugCloud b)
    {
        if (a == null || b == null) return;
        if (a.transform.position.x <= b.transform.position.x)
        { _leftCloud = a; _rightCloud = b; }
        else
        { _leftCloud = b; _rightCloud = a; }

        // Transmettre la config map au TrialManager (lui construit le JSON)
        if (trialManager != null && LevelRegistry.Instance != null)
        {
            var reg = LevelRegistry.Instance;
            trialManager.SetMapConfig(
                reg.gridSize,
                reg.WorldToCell(_leftCloud.transform.position), _leftCloud.totalBugs,
                reg.WorldToCell(_rightCloud.transform.position), _rightCloud.totalBugs
            );
        }
    }

    /// <summary>PathSpawner nous donne le chemin conseillé (pour détecter une déviation).</summary>
    public void SetChosenPath(IEnumerable<Vector2Int> cells)
    {
        _advisorPath.Clear();
        if (cells == null) return;
        foreach (var c in cells) _advisorPath.Add(c);
        followedBestPath = true;
    }

    /// <summary>
    /// Appelé par GridMover à chaque pas terminé.
    /// Orchestre : fog, visited, score, trial log.
    /// </summary>
    public void OnPlayerStep(Vector2Int cell)
    {
        if (_roundOver) return;

        steps++;

        // Marquer la cellule dans le registre et révéler le brouillard
        LevelRegistry.Instance?.MarkVisited(cell);
        FogController.Instance?.RevealCell(cell);

        // Vérifier l'adhérence au chemin conseillé
        if (_advisorPath.Count > 0 && !_advisorPath.Contains(cell))
            followedBestPath = false;

        // Enregistrer le mouvement dans le pipeline de données
        trialManager?.RecordMove(cell);
    }

    /// <summary>
    /// Appelé par Trap.OnTriggerEnter quand le joueur marche sur un piège.
    /// Applique la pénalité : -1 bug sur chaque nuage.
    /// </summary>
    public void OnTrapTriggered()
    {
        if (_roundOver) return;

        trapsHit++;
        if (_leftCloud != null) _leftCloud.AddBugs(-1);
        if (_rightCloud != null) _rightCloud.AddBugs(-1);

        Debug.Log($"[GameManager] Piège ! trapsHit={trapsHit}");
    }

    /// <summary>Appelé par BugCloud.OnTriggerEnter quand on collecte un nuage.</summary>
    public void OnCloudCollected(BugCloud cloud)
    {
        if (_roundOver) return;
        _roundOver = true;
        inputLocked = true;

        if (cloud != null) bugsCollected += Mathf.Max(0, cloud.totalBugs);

        // --- Finaliser les données de trial ---
        if (trialManager != null)
        {
            string choice = (cloud == _leftCloud) ? "left"
                          : (cloud == _rightCloud) ? "right"
                          : "unknown";

            var best = GetBestCloud();
            bool correct = best != null && cloud == best;

            if (LevelRegistry.Instance != null)
                trialManager.SetOptimalPathLength(LevelRegistry.Instance.optimalPathLength);

            trialManager.EndCurrentTrial(choice, correct);
            trialManager.SendTrials();
        }

        // --- Émettre l'événement pour l'UI ---
        OnRoundEnded?.Invoke(new RoundEndInfo
        {
            bugsCollected = bugsCollected,
            trapsHit = trapsHit,
            steps = steps,
            followedBestPath = followedBestPath,
            leftCloudBugs = _leftCloud ? _leftCloud.totalBugs : 0,
            rightCloudBugs = _rightCloud ? _rightCloud.totalBugs : 0,
        });
    }

    // ══════════════════════════════════════════════════════════════
    //  UTILITAIRES
    // ══════════════════════════════════════════════════════════════

    /// <summary>Quel nuage a le plus de bugs ? null si égalité.</summary>
    public BugCloud GetBestCloud()
    {
        if (_leftCloud == null || _rightCloud == null) return null;
        if (_leftCloud.totalBugs == _rightCloud.totalBugs) return null;
        return (_leftCloud.totalBugs > _rightCloud.totalBugs) ? _leftCloud : _rightCloud;
    }
}
