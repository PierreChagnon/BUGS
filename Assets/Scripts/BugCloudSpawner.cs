using UnityEngine;
using System.Collections.Generic;

public class BugCloudSpawner : MonoBehaviour
{
    [Header("Références")]
    public Transform player;
    public GameObject bugCloudPrefab;

    [Header("Grille (origine = 0,0 ; cellSize = 1)")]
    public Vector2Int gridSize = new(10, 10); // X (colonnes), Z (lignes)

    [Header("Placement")]
    [Tooltip("Distance Manhattan minimale (en cases) depuis le joueur.")]
    public int minDistance = 3;
    [Tooltip("Hauteur Y pour instancier les nuages (pivot au centre du prefab).")]
    public float spawnY = 0.5f;

    // À l'Awake, on place les 2 nuages (permet d'acceder à leurs positions dans Start du TrapSpawner)
    void Awake()
    {
        if (bugCloudPrefab == null) { Debug.LogError("[BugCloudSpawner] bugCloudPrefab manquant."); return; }
        if (player == null) { Debug.LogError("[BugCloudSpawner] player manquant."); return; }

        // Case du joueur
        Vector2Int playerCell = new(Mathf.RoundToInt(player.position.x), Mathf.RoundToInt(player.position.z));

        // Bornes pour la distance au joueur (D)
        int Dmin = Mathf.Max(1, minDistance);
        int Dmax = gridSize.x + gridSize.y; // borne large, on filtrera par InBounds

        // Liste des distances D qui ont ≥ 2 cases valides dans la grille
        List<int> candidateDs = new();
        for (int D = Dmin; D <= Dmax; D++)
        {
            var ring = GetRingCells(playerCell, D);                // On regarde une couronne pour un D donné
            ring.RemoveAll(c => !InBounds(c) || c == playerCell);  // On enlève les cases hors-grille et la case du joueur
            if (ring.Count >= 2) candidateDs.Add(D);               // Si au moins 2 cases valides, on garde ce D
        }

        if (candidateDs.Count == 0)
        {
            Debug.LogWarning("[BugCloudSpawner] Aucune couronne valide (vérifie gridSize/minDistance).");
            return;
        }

        // Choisir une couronne au hasard
        int chosenD = candidateDs[Random.Range(0, candidateDs.Count)];
        var validRing = GetRingCells(playerCell, chosenD);
        validRing.RemoveAll(c => !InBounds(c) || c == playerCell); // On enlève les cases hors-grille et la case du joueur

        // Choisir 2 cases distinctes au hasard dans la couronne valide
        // Il faut une case plutot à gauche et une plutot à droite pour éviter qu'elles soient trop proches
        int i = Random.Range(0, (validRing.Count / 2) - 1);               // Indice dans la moitié gauche (-1 pour décaler)
        int j = Random.Range((validRing.Count / 2) + 1, validRing.Count); // Indice dans la moitié droite (+1 pour décaler)

        // On récupère les coordonnées des cases choisies
        Vector2Int cellA = validRing[i];
        Vector2Int cellB = validRing[j];

        // Instancier (origine=0,0 ; cellSize=1)
        var goA = Instantiate(bugCloudPrefab, new Vector3(cellA.x, spawnY, cellA.y), Quaternion.identity, transform);
        var goB = Instantiate(bugCloudPrefab, new Vector3(cellB.x, spawnY, cellB.y), Quaternion.identity, transform);

        // Randomise le nombre de bugs dans chaque nuage
        var cloudA = goA.GetComponent<BugCloud>();
        cloudA.totalBugs = Random.Range(5, 11); // 5..10 inclus
        var cloudB = goB.GetComponent<BugCloud>();
        // cloudB ne doit pas être égal à cloudA
        do { cloudB.totalBugs = Random.Range(5, 11); } while (cloudB.totalBugs == cloudA.totalBugs);

        // Enregistrer les nuages dans le LevelRegistry
        if (LevelRegistry.Instance != null)
        {
            LevelRegistry.Instance.RegisterBugCloud(cellA);
            LevelRegistry.Instance.RegisterBugCloud(cellB);
        }

        // On informe le GameManager
        if (GameManager.Instance != null)
        {
            BugCloud left = (goA.transform.position.x <= goB.transform.position.x) ? cloudA : cloudB;
            BugCloud right = (left == cloudA) ? cloudB : cloudA;
            GameManager.Instance.RegisterClouds(left, right);
        }

        Debug.Log($"[BugCloudSpawner] D={chosenD}  A={cellA}  B={cellB}");
    }


    // Retourne les coordonnées des cases à distance Manhattan D d'une case centrale (l'anneau)
    List<Vector2Int> GetRingCells(Vector2Int center, int D)
    {
        var list = new List<Vector2Int>();
        for (int dx = -D; dx <= D; dx++) // On parcourt dx, puis on déduit dz
        {
            int dz = D - Mathf.Abs(dx);
            // On ajoute (dx, dz),
            list.Add(new Vector2Int(center.x + dx, center.y + dz));
            // On ne rajoute pas (dx, -dz) car dz > 0. On ne parcours que la moitié supérieure de l'anneau.
        }
        return list;
    }

    // Vérifie si une case est dans les limites de la grille
    bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < gridSize.x && c.y >= 0 && c.y < gridSize.y;
}
