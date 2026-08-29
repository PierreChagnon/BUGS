using UnityEngine;
using System.Collections.Generic;
using System;

[DefaultExecutionOrder(-200)]
public class BugCloudSpawner : MonoBehaviour
{
    [Header("Références")]

    public GameObject bugCloudPrefab;

    // Source de vérité: LevelRegistry (gridSize/cellSize/originWorld + paramètres recherche)

    [Header("Placement (visuel)")]
    // readonly int minZ = 5; // Z minimale pour placer un nuage (évite les nuages trop proches du joueur en Y)
    [Tooltip("Hauteur Y pour instancier les nuages (pivot au centre du prefab).")]
    public float spawnY = 0.5f;

    // Au Start, on place les 2 nuages (permet d'acceder à leurs positions dans Start du TrapSpawner)
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

        // Lecture de la config expérimentale via SessionManager.Map (source de vérité unique)
        var session = SessionManager.Instance;
        if (session == null)
        {
            Debug.LogError("[BugCloudSpawner] SessionManager manquant dans la scène.");
            return;
        }
        MapGenConfig map = session.Map;

        // Bornes pour la distance au joueur (D)
        int Dmin = Mathf.Max(1, map.min_distance);
        int gridMax = registry.gridSize.x + registry.gridSize.y; // borne absolue imposée par la map
        int maxDistance = map.max_distance;
        // Si le chercheur a défini une maxDistance > 0, on l'utilise bornée par la taille de la map.
        // Sinon (0 = pas de limite), on utilise la taille de la map comme fallback.
        int Dmax = (maxDistance > 0) ? Mathf.Min(maxDistance, gridMax) : gridMax;

        // Liste des distances D qui ont ≥ 2 cases valides dans la grille
        List<int> candidateDs = new();
        for (int D = Dmin; D <= Dmax; D++)
        {
            var ring = GetRingCells(playerCell, D);                // On regarde une couronne pour un D donné
            ring.RemoveAll(c => !registry.InBounds(c) || c == playerCell);  // On enlève les cases hors-grille et la case du joueur
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
        validRing.RemoveAll(c => !registry.InBounds(c) || c == playerCell); // On enlève les cases hors-grille et la case du joueur

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

        var cloudA = goA.GetComponent<BugCloud>();
        var cloudB = goB.GetComponent<BugCloud>();
        var cloudPair = BugCloudGenerationUtility.GenerateGameplayPair(map, rng);

        bool cloudAIsLeft = goA.transform.position.x <= goB.transform.position.x;
        BugCloudSample leftSample = cloudPair.firstCloud;
        BugCloudSample rightSample = cloudPair.secondCloud;
        BugCloudSample sampleA = cloudAIsLeft ? leftSample : rightSample;
        BugCloudSample sampleB = cloudAIsLeft ? rightSample : leftSample;

        cloudA.totalBugs = sampleA.totalBugs;
        cloudA.greenRatio = sampleA.greenRatio;
        cloudB.totalBugs = sampleB.totalBugs;
        cloudB.greenRatio = sampleB.greenRatio;

        string bestCloudSide = leftSample.GreenBugCount >= rightSample.GreenBugCount
            ? DistalScanSide.Left
            : DistalScanSide.Right;
        FlowController.Instance?.ResolveProximalForcedCloud(bestCloudSide);

        // ─────────────────────────────────────────────────────────────────────────────────
        // INITIALISATION DES SYSTÈMES DE PARTICULES
        // ─────────────────────────────────────────────────────────────────────────────────
        // Configure le nombre de particules vertes et rouges selon les ratios calculés.
        // Exemple : totalBugs=50, greenRatio=0.6 → 30 bugs verts, 20 bugs rouges
        cloudA.InitializeParticlesQty();
        cloudB.InitializeParticlesQty();

        // ─────────────────────────────────────────────────────────────────────────────────
        // LOG DE DEBUG : Affichage des valeurs tirées pour validation
        // ─────────────────────────────────────────────────────────────────────────────────
        Debug.Log($"[BugCloudSpawner] Trial setup: totalBugs={cloudA.totalBugs}, " +
                  $"cloudA greenRatio={cloudA.greenRatio:F2} ({cloudA.greenBugs} verts), " +
                  $"cloudB greenRatio={cloudB.greenRatio:F2} ({cloudB.greenBugs} verts), " +
                  $"gap={cloudPair.ratioGap:F2}");


        // Enregistrer les nuages dans le LevelRegistry
        registry.RegisterBugCloud(cellA);
        registry.RegisterBugCloud(cellB);

        // Enregistrer le budget de pas (distance Manhattan joueur → nuages)
        // Les deux nuages sont sur le même anneau, donc chosenD est la distance pour les deux.
        registry.RegisterStepBudget(chosenD);

        // On informe le GameManager
        if (GameManager.Instance != null)
        {
            BugCloud left = (goA.transform.position.x <= goB.transform.position.x) ? cloudA : cloudB;
            BugCloud right = (left == cloudA) ? cloudB : cloudA;
            GameManager.Instance.RegisterClouds(left, right);
        }

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

}
