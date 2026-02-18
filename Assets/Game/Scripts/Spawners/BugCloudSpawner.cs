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

    [Header("Green Ratio Bounds")]
    [Tooltip("Borne minimale pour le tirage du premier ratio de bugs verts (ex: 0.4 = 40% de verts).")]
    [SerializeField]
    private float minGreenBugsRatio = 0.4f;
    [Tooltip("Borne maximale pour le tirage du premier ratio de bugs verts (ex: 0.8 = 80% de verts).")]
    [SerializeField]
    private float maxGreenBugsRatio = 0.8f;

    [Header("Discrimination Difficulty Control")]
    [Tooltip("Écart MINIMUM entre les ratios verts des deux nuages (ex: 0.05 = 5% d'écart minimum). Plus petit = discrimination difficile.")]
    [SerializeField]
    private float gapMin = 0.1f;
    [Tooltip("Écart MAXIMUM entre les ratios verts des deux nuages (ex: 0.3 = 30% d'écart maximum). Plus grand = discrimination facile.")]
    [SerializeField]
    private float gapMax = 0.3f;

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

        // ═════════════════════════════════════════════════════════════════════════════════
        // TIRAGE DES RATIOS VERTS AVEC CONTRÔLE DE DIFFICULTÉ DE DISCRIMINATION
        // ═════════════════════════════════════════════════════════════════════════════════
        // Objectif : Les deux nuages ont le même totalBugs, mais des ratios verts DIFFÉRENTS.
        // La meilleure récompense = le nuage avec le PLUS de bugs verts (totalBugs × greenRatio).
        // La difficulté de discrimination visuelle = l'écart entre les deux ratios (gap).
        //
        // Algorithme en 5 étapes :
        //   1) Tirer le totalBugs (partagé entre les 2 nuages)
        //   2) Tirer le premier ratio (ratio1) entre [minGreenBugsRatio, maxGreenBugsRatio]
        //   3) Tirer un gap (écart) entre [gapMin, gapMax]
        //   4) Calculer le deuxième ratio (ratio2) = ratio1 ± gap (direction aléatoire)
        //   5) Assigner aléatoirement ratio1 et ratio2 aux deux nuages (gauche/droite)
        // ═════════════════════════════════════════════════════════════════════════════════

        var cloudA = goA.GetComponent<BugCloud>();
        var cloudB = goB.GetComponent<BugCloud>();

        // ─────────────────────────────────────────────────────────────────────────────────
        // ÉTAPE 1 : Tirage du nombre total de bugs (partagé entre les deux nuages)
        // ─────────────────────────────────────────────────────────────────────────────────
        // Exemple : minTotalBugs=20, maxTotalBugs=80 → trialTotalBugs pourrait être 50
        int trialTotalBugs = rng.Next(minTotalBugs, maxTotalBugs + 1);
        cloudA.totalBugs = trialTotalBugs;
        cloudB.totalBugs = trialTotalBugs;

        // ─────────────────────────────────────────────────────────────────────────────────
        // ÉTAPE 2 : Premier tirage de ratio vert
        // ─────────────────────────────────────────────────────────────────────────────────
        // On tire un ratio entre les bornes configurées par le chercheur.
        // Exemple : minGreenBugsRatio=0.4, maxGreenBugsRatio=0.8 → ratio1 pourrait être 0.6
        float ratio1 = Mathf.Lerp(minGreenBugsRatio, maxGreenBugsRatio, (float)rng.NextDouble());

        // ─────────────────────────────────────────────────────────────────────────────────
        // ÉTAPE 3 : Tirage de l'écart (gap) entre les deux ratios
        // ─────────────────────────────────────────────────────────────────────────────────
        // Cet écart contrôle la difficulté de discrimination visuelle :
        //   - gap petit (proche de gapMin) → ratios très proches → difficile à distinguer
        //   - gap grand (proche de gapMax) → ratios éloignés → facile à distinguer
        // Exemple : gapMin=0.1, gapMax=0.3 → gap pourrait être 0.15
        float gap = Mathf.Lerp(gapMin, gapMax, (float)rng.NextDouble());

        // ─────────────────────────────────────────────────────────────────────────────────
        // ÉTAPE 4 : Calcul du deuxième ratio RELATIF au premier
        // ─────────────────────────────────────────────────────────────────────────────────
        // On ajoute OU soustrait le gap au premier ratio de manière aléatoire (50/50).
        // Ensuite on clamp entre [0, 1] pour garantir un ratio valide.
        //
        // Exemples :
        //   - Si ratio1=0.6 et gap=0.15 et direction=+ → ratio2 = 0.6 + 0.15 = 0.75
        //   - Si ratio1=0.6 et gap=0.15 et direction=- → ratio2 = 0.6 - 0.15 = 0.45
        //   - Si ratio1=0.75 et gap=0.3 et direction=+ → ratio2 = 0.75 + 0.3 = 1.05 → clamped à 1.0
        float ratio2 = (rng.NextDouble() < 0.5)
            ? ratio1 + gap   // Direction positive (ratio2 > ratio1)
            : ratio1 - gap;  // Direction négative (ratio2 < ratio1)

        // Clamp strict entre 0 et 1 (un ratio ne peut pas être négatif ou > 100%)
        ratio2 = Mathf.Clamp01(ratio2);

        // ─────────────────────────────────────────────────────────────────────────────────
        // ÉTAPE 5 : Assignment ALÉATOIRE des ratios aux deux nuages
        // ─────────────────────────────────────────────────────────────────────────────────
        // On ne sait pas à l'avance quel nuage (gauche ou droite) aura le meilleur ratio.
        // Cela évite un biais spatial (ex: "le nuage de gauche est toujours meilleur").
        //
        // Exemple :
        //   - Si ratio1=0.6, ratio2=0.75, et tirage=0.3 (<0.5) → cloudA=0.6, cloudB=0.75
        //   - Si ratio1=0.6, ratio2=0.75, et tirage=0.7 (≥0.5) → cloudA=0.75, cloudB=0.6
        if (rng.NextDouble() < 0.5)
        {
            cloudA.greenRatio = ratio1;
            cloudB.greenRatio = ratio2;
        }
        else
        {
            cloudA.greenRatio = ratio2;
            cloudB.greenRatio = ratio1;
        }

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
        Debug.Log($"[BugCloudSpawner] Trial setup: totalBugs={trialTotalBugs}, " +
                  $"cloudA greenRatio={cloudA.greenRatio:F2} ({Mathf.RoundToInt(trialTotalBugs * cloudA.greenRatio)} verts), " +
                  $"cloudB greenRatio={cloudB.greenRatio:F2} ({Mathf.RoundToInt(trialTotalBugs * cloudB.greenRatio)} verts), " +
                  $"gap={Mathf.Abs(cloudA.greenRatio - cloudB.greenRatio):F2}");


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
