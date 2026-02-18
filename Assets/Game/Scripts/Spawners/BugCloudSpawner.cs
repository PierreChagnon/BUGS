using UnityEngine;
using System.Collections.Generic;
using System;

[DefaultExecutionOrder(-200)]
public class BugCloudSpawner : MonoBehaviour
{
    [Header("Références")]

    public GameObject bugCloudPrefab;

    // Source de vérité: LevelRegistry (gridSize/cellSize/originWorld)

    [Header("Placement")]
    [Tooltip("Distance Manhattan minimale (en cases) depuis le joueur.")]
    public int minDistance = 3;
    readonly int minZ = 5; // Z minimale pour placer un nuage (évite les nuages trop proches du joueur en Y)
    [Tooltip("Hauteur Y pour instancier les nuages (pivot au centre du prefab).")]
    public float spawnY = 0.5f;

    //Parameters to set number of bugs in clouds
    [Header("BugsCloud Parameters : Researchers Input")]
    [SerializeField]
    private int minTotalBugs = 20;
    [SerializeField]
    private int maxTotalBugs = 80;
    [SerializeField]
    private float minGreenBugsRatio = 0.4f;
    [SerializeField]
    private float maxGreenBugsRatio = 0.8f;

    // À l'Awake, on place les 2 nuages (permet d'acceder à leurs positions dans Start du TrapSpawner)
    void Start()
    {
        if (bugCloudPrefab == null) { Debug.LogError("[BugCloudSpawner] bugCloudPrefab manquant."); return; }

        var registry = LevelRegistry.Instance;
        if (registry == null)
        {
            Debug.LogError("[BugCloudSpawner] LevelRegistry manquant dans la scène.");
            return;
        }

        if (!registry.TryGetPlayerStartCell(out var playerCell))
        {
            Debug.LogError("[BugCloudSpawner] player start non enregistré (vérifie PlayerSpawner / LevelRegistry).");
            return;
        }

        var rng = registry.CreateRng(nameof(BugCloudSpawner));

        // Bornes pour la distance au joueur (D)
        int Dmin = Mathf.Max(1, minDistance);
        int Dmax = registry.gridSize.x + registry.gridSize.y; // borne large, on filtrera par InBounds

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
        int chosenD = candidateDs[rng.Next(0, candidateDs.Count)];
        var validRing = GetRingCells(playerCell, chosenD);
        validRing.RemoveAll(c => !InBounds(c) || c == playerCell || c.y < minZ); // On enlève les cases hors-grille, la case du joueur et celles en dessous de minZ
        Debug.Log($"[BugCloudSpawner] Couronne D={chosenD} a {validRing.Count} cases valides après filtrage.");

        // Choisir 2 cases distinctes au hasard dans la couronne valide
        // Il faut une case plutot à gauche et une plutot à droite pour éviter qu'elles soient trop proches
        // Il faut aussi que les cases soient symétriques par rapport au joueur pour l'équilibrage
        int j = -1;
        int i = -1;
        while (j == -1)
        {
            i = rng.Next(0, validRing.Count / 2 - 1); // Indice dans la moitié gauche (-1 pour éviter le milieu)
            j = validRing.FindIndex(validRing.Count / 2, c => c.y == validRing[i].y);
        }

        // On récupère les coordonnées des cases choisies
        Vector2Int cellA = validRing[i];
        Vector2Int cellB = validRing[j];

        // Instancier (origine=0,0 ; cellSize=1)
        var goA = Instantiate(bugCloudPrefab, registry.CellToWorld(cellA, spawnY), Quaternion.identity, transform);
        var goB = Instantiate(bugCloudPrefab, registry.CellToWorld(cellB, spawnY), Quaternion.identity, transform);

        // Randomise le nombre de bugs dans chaque nuage, basé sur les paramètres min/maxTotalBugs et min/maxGreenBugsRatio
        // Cloud A et B on le même total de bugs mais des quantités différentes de verts et de rouges.
        // Ici on tire les valeurs dans le range : 1 totalBugs et 2 ratio (un pour chaque cloud). et on initialize les particles

        int trialTotalBugs = rng.Next(minTotalBugs, maxTotalBugs + 1);
        var cloudA = goA.GetComponent<BugCloud>();
        var cloudB = goB.GetComponent<BugCloud>();

        cloudA.totalBugs = trialTotalBugs;
        cloudB.totalBugs = trialTotalBugs;

        cloudA.greenRatio = Mathf.Lerp(minGreenBugsRatio, maxGreenBugsRatio, (float)rng.NextDouble());
        cloudB.greenRatio = Mathf.Lerp(minGreenBugsRatio, maxGreenBugsRatio, (float)rng.NextDouble());

        cloudA.InitializeParticlesQty();
        cloudB.InitializeParticlesQty();


        // Enregistrer les nuages dans le LevelRegistry
        registry.RegisterBugCloud(cellA);
        registry.RegisterBugCloud(cellB);

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
    bool InBounds(Vector2Int c)
    {
        var reg = LevelRegistry.Instance;
        if (reg == null) return false;
        return c.x >= 0 && c.x < reg.gridSize.x && c.y >= 0 && c.y < reg.gridSize.y;
    }
}
