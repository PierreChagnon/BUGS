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

    // Les 2 nuages du round (références données par le spawner)
    BugCloud leftCloud, rightCloud;

    // Chemin conseillé (BestPath nous l’enverra)
    readonly HashSet<Vector2Int> advisorPath = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UpdateScoreUI();
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
        if (cloud != null) bugsCollected += Mathf.Max(0, cloud.totalBugs);
        UpdateScoreUI();
        EndRound();
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
