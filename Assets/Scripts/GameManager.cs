using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    // Spawner nous fournit les 2 nuages
    public void RegisterClouds(BugCloud a, BugCloud b)
    {
        if (a == null || b == null) return;
        if (a.transform.position.x <= b.transform.position.x) { leftCloud = a; rightCloud = b; }
        else { leftCloud = b; rightCloud = a; }
        UpdateScoreUI();
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
    }

    // Appelé par BugCloud quand on collecte un nuage
    public void OnCloudCollected(BugCloud cloud)
    {
        if (roundOver) return;
        roundOver = true;

        inputLocked = true; // plus d’input possible

        if (cloud != null) bugsCollected += Mathf.Max(0, cloud.totalBugs);

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
}
