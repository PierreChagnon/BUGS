using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// -----------------------------
// Gère l’état global du jeu (score, fin de manche, UI…)
// -----------------------------

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public TrialManager trialManager;
    public int blockId = 1;
    int screenCounter = 0;


    [Header("UI Score")]
    public TMP_Text scoreText;

    [Header("Round / Score")]
    public int steps = 0;
    public int trapsHit = 0;
    public int bugsCollected = 0;
    public bool followedBestPath = true;

    [Header("Game Over UI")]
    public GameObject gameOverUI;   // Panel à afficher/masquer
    public TMP_Text gameOverStats;  // Texte pour le récap

    // Etat de la manche
    public bool inputLocked { get; private set; } = false;
    bool roundOver = false;

    // Les 2 nuages du round (références données par le spawner)
    BugCloud leftCloud, rightCloud;

    // Chemin conseillé (BestPath nous l’enverra)
    readonly HashSet<Vector2Int> advisorPath = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UpdateScoreUI();

        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

    // --- APIs appelées par d’autres scripts ---

    // appelée par SessionManager quand la session est prête
    public void BeginFirstRound()
    {
        StartNewRound("forest");
    }

    public void StartNewRound(string screenType)
    {
        screenCounter++;
        trialManager.StartNewTrial(blockId, screenCounter, screenType);
    }

    // Spawner nous fournit les 2 nuages
    public void RegisterClouds(BugCloud a, BugCloud b)
    {
        if (a == null || b == null) return;
        if (a.transform.position.x <= b.transform.position.x) { leftCloud = a; rightCloud = b; }
        else { leftCloud = b; rightCloud = a; }
        UpdateScoreUI();

        // Envoi de la config de la carte au TrialManager
        if (trialManager != null)
        {

            // Construire une mini config JSON (positions + bugs initiaux)
            var cfg = new MiniMapCfg
            {
                grid_w = LevelRegistry.Instance ? LevelRegistry.Instance.gridSize.x : 10,
                grid_h = LevelRegistry.Instance ? LevelRegistry.Instance.gridSize.y : 10,
                cloud_left = new CloudInfo
                {
                    x = Mathf.RoundToInt(leftCloud.transform.position.x),
                    y = Mathf.RoundToInt(leftCloud.transform.position.z),
                    bugs = leftCloud.totalBugs
                },
                cloud_right = new CloudInfo
                {
                    x = Mathf.RoundToInt(rightCloud.transform.position.x),
                    y = Mathf.RoundToInt(rightCloud.transform.position.z),
                    bugs = rightCloud.totalBugs
                }
            };

            // Envoi de la configuration de la carte au TrialManager
            trialManager.SetMapConfigJson(JsonUtility.ToJson(cfg));
            Debug.Log($"[GameManager] Map config sent to TrialManager: {JsonUtility.ToJson(cfg)}");
        }
    }

    // BestPath nous donne le chemin conseillé (pour détecter une déviation)
    public void SetChosenPath(IEnumerable<Vector2Int> cells)
    {
        advisorPath.Clear();
        if (cells == null) return;
        foreach (var c in cells) advisorPath.Add(c);
        followedBestPath = true; // reset pour ce round
        UpdateScoreUI();
    }

    // Appelé par le joueur à chaque pas terminé
    public void OnPlayerStep(Vector2Int cell)
    {
        steps++;
        if (advisorPath.Count > 0 && !advisorPath.Contains(cell))
            followedBestPath = false;

        // Si on marche sur un piège -> -1 bug dans CHAQUE nuage
        if (LevelRegistry.Instance != null && LevelRegistry.Instance.HasTrap(cell))
        {
            trapsHit++;
            if (leftCloud != null) leftCloud.AddBugs(-1);
            if (rightCloud != null) rightCloud.AddBugs(-1);
        }

        UpdateScoreUI();

        // --- LOGGAGE DU CHEMIN DANS TrialManager ---
        if (trialManager != null)
            trialManager.RecordMove(cell);

    }

    // Appelé par BugCloud quand on collecte un nuage
    public void OnCloudCollected(BugCloud cloud)
    {
        if (roundOver) return;
        roundOver = true;

        inputLocked = true; // plus d’input possible

        if (cloud != null) bugsCollected += Mathf.Max(0, cloud.totalBugs);

        // --- FIN DE MANCHE → informer TrialManager ---
        if (trialManager != null)
        {
            // "left" / "right" selon le nuage collecté
            string choice = (cloud == leftCloud) ? "left" : (cloud == rightCloud ? "right" : "unknown");

            // correct si c'était le nuage "optimal"
            var best = GetBestCloud(); // déjà fourni par ton GameManager
            bool correct = (best != null) ? (cloud == best) : false;

            // Fin de la manche, on envoie les résultats a TrialData
            trialManager.EndCurrentTrial(choice, correct);

            // Envoi des manches accumulées à l’API (async)
            trialManager.SendTrials();
        }

        ShowGameOver();
        UpdateScoreUI();
    }

    // Affichage du récap de fin de round
    void ShowGameOver()
    {
        if (gameOverUI != null) gameOverUI.SetActive(true);

        if (gameOverStats != null)
        {
            int left = leftCloud ? leftCloud.totalBugs : 0;
            int right = rightCloud ? rightCloud.totalBugs : 0;
            gameOverStats.text =
                $"Bugs récoltés : {bugsCollected}\n" +
                $"Pièges déclenchés : {trapsHit}\n" +
                $"Coups joués : {steps}\n" +
                $"Chemin conseillé suivi : {(followedBestPath ? "Oui" : "Non")}\n" +
                $"Bugs restants L/R : {left} / {right}";
        }
    }

    // Restart de la scène (appelé par le bouton UI)
    public void RestartRound()
    {
        // Débloquer si tu avais des locks globaux (optionnel)
        inputLocked = false;
        roundOver = false;

        // Recharger la scène => tout sera régénéré (nuages, chemins, pièges…)
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    // Utilitaire : qui est le “meilleur” nuage (pour BestPath)
    public BugCloud GetBestCloud()
    {
        if (leftCloud == null || rightCloud == null) return null;
        if (leftCloud.totalBugs == rightCloud.totalBugs) return null; // égalité -> au choix
        return (leftCloud.totalBugs > rightCloud.totalBugs) ? leftCloud : rightCloud;
    }

    // --- Fin de round + restart simple ---
    void EndRound()
    {
        // ici tu peux logguer/écrire un fichier/etc. puis relancer un round
        StartCoroutine(RestartSceneAfter(1.0f));
    }
    IEnumerator RestartSceneAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateScoreUI()
    {
        if (scoreText == null) return;
        int left = leftCloud ? leftCloud.totalBugs : 0;
        int right = rightCloud ? rightCloud.totalBugs : 0;
        scoreText.text =
            $"Bugs: {bugsCollected}\n" +
            $"Traps: {trapsHit}\n" +
            $"Steps: {steps}\n" +
            $"Best path: {(followedBestPath ? "YES" : "NO")}\n" +
            $"Clouds L/R: {left} / {right}";
    }

    // petits DTO internes à GameManager
    [System.Serializable]
    struct MiniMapCfg
    {
        public int grid_w, grid_h;
        public CloudInfo cloud_left, cloud_right;
    }
    [System.Serializable] struct CloudInfo { public int x, y, bugs; }
}
