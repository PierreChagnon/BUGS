using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class PathSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject quadPrefab;

    [Header("Chemin suboptimal (debug)")]
    [Tooltip("Taille min du détour en cases (ajoutera 2×N à la longueur).")]
    [Min(1)]
    public int detourMin = 2;

    [Tooltip("Taille max du détour en cases (exclusif). Doit être > detourMin.")]
    [Min(2)]
    public int detourMax = 5;

    void Start()
    {
        var reg = LevelRegistry.Instance;
        if (reg == null)
        {
            Debug.LogError("[PathSpawner] LevelRegistry manquant dans la scène.");
            return;
        }

        // Lecture du paramètre recherche depuis SessionManager (propriétaire de la config expérimentale)
        var session = SessionManager.Instance;
        if (session == null)
        {
            Debug.LogError("[PathSpawner] SessionManager manquant dans la scène.");
            return;
        }

        var rng = reg.CreateRng(nameof(PathSpawner));


        if (quadPrefab == null)
        {
            Debug.LogError("[PathSpawner] quadPrefab manquant (assigne le prefab de quad dans l'inspecteur).");
            return;
        }

        // Récupérer les positions des nuages
        GameObject[] clouds = GameObject.FindGameObjectsWithTag("BugCloud");
        if (clouds.Length < 2)
        {
            Debug.LogWarning($"[PathSpawner] Moins de 2 nuages trouvés (count={clouds.Length}). Vérifie BugCloudSpawner + tag 'BugCloud'.");
            return;
        }

        if (!reg.TryGetPlayerStartCell(out var playerCell))
        {
            Debug.LogError("[PathSpawner] player start non enregistré (vérifie PlayerSpawner / LevelRegistry).");
            return;
        }

        // Déterminer le nuage de gauche et celui de droite
        GameObject leftCloud = null;
        GameObject rightCloud = null;
        foreach (var cloud in clouds)
        {
            if (leftCloud == null || cloud.transform.position.x < leftCloud.transform.position.x)
                leftCloud = cloud;
            if (rightCloud == null || cloud.transform.position.x > rightCloud.transform.position.x)
                rightCloud = cloud;
        }





        // ------ DEFINITION DES DEUX CHEMINS (best paths) ------

        // Chemin 1 : Joueur -> Nuage gauche
        // Chemin 2 : Joueur -> Nuage droite
        // On détermine un chemin aléatoire mais avec la distance la plus courte possible entre le joueur et le nuage de gauche, 
        // puis entre le joueur de gauche et le nuage de droite

        // On travaille en cellules (source de vérité: LevelRegistry)
        Vector2Int leftCloudCell = reg.WorldToCell(leftCloud.transform.position);
        Vector2Int rightCloudCell = reg.WorldToCell(rightCloud.transform.position);

        // On crée les deux tableaux de cellules pour les deux chemins
        Vector2Int[] pathToLeftCloud = new Vector2Int[Mathf.Abs(playerCell.x - leftCloudCell.x) + Mathf.Abs(playerCell.y - leftCloudCell.y) + 1];
        Vector2Int[] pathToRightCloud = new Vector2Int[Mathf.Abs(playerCell.x - rightCloudCell.x) + Mathf.Abs(playerCell.y - rightCloudCell.y) + 1];

        // On remplit les deux tableaux avec les positions des cases du chemin
        int index = 0;
        Vector2Int currentPos = playerCell;
        pathToLeftCloud[index++] = currentPos;  // Ajouter la position initiale du joueur

        while (currentPos != leftCloudCell)  // Chemin vers le nuage gauche
        {
            if (currentPos.x != leftCloudCell.x && (currentPos.y == leftCloudCell.y || rng.NextDouble() < 0.5))
                currentPos.x--; // Se déplacer horizontalement
            else if (currentPos.y != leftCloudCell.y)
                currentPos.y++; // Se déplacer verticalement
            pathToLeftCloud[index++] = currentPos; // On ajoute la nouvelle position au chemin
        }

        index = 0;
        currentPos = playerCell;
        pathToRightCloud[index++] = currentPos;  // Ajouter la position initiale du nuage gauche

        while (currentPos != rightCloudCell) // Chemin vers le nuage droite
        {
            if (currentPos.x != rightCloudCell.x && (currentPos.y == rightCloudCell.y || rng.NextDouble() < 0.5))
                currentPos.x++; // Se déplacer horizontalement
            else if (currentPos.y != rightCloudCell.y)
                currentPos.y++; // Se déplacer verticalement
            pathToRightCloud[index++] = currentPos; // On ajoute la nouvelle position au chemin
        }







        // ------ ENREGISTREMENT DANS LE REGISTRE (réserver les deux chemins) ------
        var leftCells = new List<Vector2Int>(pathToLeftCloud.Length);
        var rightCells = new List<Vector2Int>(pathToRightCloud.Length);

        foreach (var c in pathToLeftCloud) leftCells.Add(c);
        foreach (var c in pathToRightCloud) rightCells.Add(c);

        reg.ReservePathLeft(leftCells);
        reg.ReservePathRight(rightCells);








        // ------ INSTANTIATION DES QUADS LE LONG DES CHEMINS ------

        // Déterminer le chemin optimal vers le meilleur nuage
        Vector2Int[] optimalPath;
        // Par défaut, on tire au sort
        optimalPath = (rng.NextDouble() < 0.5) ? pathToLeftCloud : pathToRightCloud;

        // Si GameManager connaît un nuage "meilleur", on force le chemin correspondant
        bool bestIsLeft = false;
        if (GameManager.Instance != null)
        {
            var best = GameManager.Instance.GetBestCloud();
            if (best != null)
            {
                bestIsLeft = reg.WorldToCell(best.transform.position).x == reg.WorldToCell(leftCloud.transform.position).x;
                optimalPath = bestIsLeft ? pathToLeftCloud : pathToRightCloud;
            }
        }

        // Publier le chemin optimal et l'enregistrer (toujours, même si on affiche un suboptimal)
        var optimalCells = new List<Vector2Int>(optimalPath.Length);
        foreach (var c in optimalPath) optimalCells.Add(c);

        if (GameManager.Instance != null)
        {
            // Enregistrer le chemin optimal dans le LevelRegistry
            reg.RegisterOptimalPath(optimalCells);
        }


        // ------ TIRAGE : CHEMIN SUBOPTIMAL ? ------
        bool isSuboptimal = false;
        List<Vector2Int> displayPath; // chemin qui sera affiché au joueur
        bool hasAdvisor = session != null && session.HasAdvisor;

        if (hasAdvisor && session.suboptimalPathProbability > 0f && rng.NextDouble() < session.suboptimalPathProbability)
        {
            isSuboptimal = true;
            Vector2Int bestCloudCell = bestIsLeft ? leftCloudCell : rightCloudCell;

            // Détour (crochet) ou simple chemin Manhattan alternatif ?
            bool withDetour = session.detourProbability > 0f && rng.NextDouble() < session.detourProbability;

            if (withDetour)
                displayPath = BuildSuboptimalDetour(rng, reg, playerCell, bestCloudCell);
            else
                displayPath = BuildRandomManhattanPath(rng, reg, playerCell, bestCloudCell);

            // Enregistrer dans LevelRegistry pour que les murs ne bloquent pas le chemin
            reg.RegisterSuboptimalPath(displayPath);

            Debug.Log($"[PathSpawner] Chemin SUBOPTIMAL généré : {displayPath.Count} cases (détour={withDetour}, optimal={optimalPath.Length}).");
        }
        else
        {
            displayPath = optimalCells;
        }

        // Publier le chemin affiché (advisor path) au GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetChosenPath(hasAdvisor ? displayPath : null);
            GameManager.Instance.SetPathIsSuboptimal(isSuboptimal);
        }

        bool visible = hasAdvisor && rng.NextDouble() < session.pathVisible;

        if (GameManager.Instance != null)
            GameManager.Instance.SetAdvisorPathVisible(visible);

        // ------ REVELER LES CASES DANS LE FOG OF WAR ------
        // Toujours révéler la case joueur et les deux nuages (même si le chemin est caché).
        // Le chemin lui-même n'est révélé que si visible.
        if (FogController.Instance != null)
        {
            var reveal = new List<Vector2Int> { playerCell, leftCloudCell, rightCloudCell };

            if (visible)
            {
                foreach (var c in displayPath)
                    reveal.Add(c);
            }

            FogController.Instance.RevealCells(reveal);
        }

        // Si le chemin n'est pas visible, pas de quads
        if (!visible)
        {
            Debug.Log("[PathSpawner] visible=false => instanciation des quads ignorée.");
            return;
        }

        // Instancier les quads le long du chemin affiché
        int spawned = 0;
        foreach (var cell in displayPath)
        {
            var pos = reg.CellToWorld(cell, 0.11f);

            var quad = Instantiate(quadPrefab, pos, quadPrefab.transform.rotation);
            quad.transform.SetParent(transform, true);

            if (!quad.activeSelf) quad.SetActive(true);
            spawned++;
        }

        Debug.Log($"[PathSpawner] Quads instanciés: {spawned} (suboptimal={isSuboptimal}, originWorld={reg.originWorld}, cellSize={reg.cellSize}).");
    }

    // ══════════════════════════════════════════════════════════════
    //  GÉNÉRATION DE CHEMINS SUBOPTIMAUX
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Construit un chemin Manhattan aléatoire vers le nuage cible.
    /// Même longueur que l'optimal, mais tracé différent (non réservé → pièges possibles).
    /// </summary>
    List<Vector2Int> BuildRandomManhattanPath(System.Random rng, LevelRegistry reg,
                                               Vector2Int start, Vector2Int goal)
    {
        var visited = new HashSet<Vector2Int>();
        var path = new List<Vector2Int>();
        var cur = start;
        path.Add(cur);
        visited.Add(cur);

        int safety = reg.gridSize.x + reg.gridSize.y + 20;
        while (cur != goal && safety-- > 0)
        {
            if (!TryStep(rng, reg, ref cur, goal, visited, path)) break;
        }

        return path;
    }

    /// <summary>
    /// Construit un chemin suboptimal en forme de « Z » :
    /// 1. Quelques pas normaux vers le nuage
    /// 2. Crochet horizontal (detourSize pas)
    /// 3. Montée verticale (≥ GAP, sépare les couloirs)
    /// 4. Retour horizontal en sens inverse (returnSize pas, tirage indépendant)
    /// 5. Montée verticale (≥ GAP, sépare du chemin de rejoint)
    /// 6. Rejoint le nuage en Manhattan
    /// Le retour (phase 4) garantit un vrai surplus de steps vs le chemin optimal.
    /// Le chemin ne repasse jamais sur une case déjà visitée.
    /// </summary>
    List<Vector2Int> BuildSuboptimalDetour(System.Random rng, LevelRegistry reg,
                                            Vector2Int start, Vector2Int goal)
    {
        int normalSteps = rng.Next(1, 3);
        int detourSize  = rng.Next(detourMin, detourMax);
        int returnSize  = rng.Next(detourMin, detourMax); // tirage indépendant
        int hookDx      = (rng.NextDouble() < 0.5) ? +1 : -1;
        const int GAP   = 2; // pas verticaux entre segments horizontaux (1 rangée d'écart)

        var visited = new HashSet<Vector2Int>();
        var path = new List<Vector2Int>();
        var cur = start;
        path.Add(cur);
        visited.Add(cur);

        // Phase 1 : quelques pas normaux vers le nuage
        for (int i = 0; i < normalSteps && cur != goal; i++)
            if (!TryStep(rng, reg, ref cur, goal, visited, path)) break;

        // Phase 2 : crochet horizontal
        // On force d'abord un deplacement vertical avant de commencer le crochet, pour éviter d'avoir des portions horizontales limitrophes
        TryVertical(reg, ref cur, GAP, goal.y, visited, path);
        TryHorizontal(reg, ref cur, hookDx, detourSize, visited, path);

        // Phase 3 : montée verticale (séparer les deux segments horizontaux)
        TryVertical(reg, ref cur, GAP, goal.y, visited, path);

        // Phase 4 : retour horizontal en sens inverse
        TryHorizontal(reg, ref cur, -hookDx, returnSize, visited, path);

        // Phase 5 : montée verticale (séparer du chemin de rejoint)
        TryVertical(reg, ref cur, GAP, goal.y, visited, path);

        // Phase 6 : rejoindre le nuage cible
        int safety = reg.gridSize.x + reg.gridSize.y + 20;
        while (cur != goal && safety-- > 0)
            if (!TryStep(rng, reg, ref cur, goal, visited, path)) break;

        return path;
    }

    /// <summary>Avance de <paramref name="count"/> pas horizontaux. Inverse la direction si bloqué.</summary>
    static void TryHorizontal(LevelRegistry reg, ref Vector2Int cur, int dx, int count,
                               HashSet<Vector2Int> visited, List<Vector2Int> path)
    {
        for (int i = 0; i < count; i++)
        {
            var next = new Vector2Int(cur.x + dx, cur.y);
            if (!reg.InBounds(next) || visited.Contains(next))
            {
                dx = -dx;
                next = new Vector2Int(cur.x + dx, cur.y);
                if (!reg.InBounds(next) || visited.Contains(next)) break;
            }
            cur = next;
            path.Add(cur);
            visited.Add(cur);
        }
    }

    /// <summary>Monte de <paramref name="count"/> pas verticaux (y+1) sans dépasser <paramref name="maxY"/>.</summary>
    static void TryVertical(LevelRegistry reg, ref Vector2Int cur, int count, int maxY,
                             HashSet<Vector2Int> visited, List<Vector2Int> path)
    {
        for (int i = 0; i < count; i++)
        {
            var next = new Vector2Int(cur.x, cur.y + 1);
            if (!reg.InBounds(next) || visited.Contains(next) || next.y > maxY) break;
            cur = next;
            path.Add(cur);
            visited.Add(cur);
        }
    }

    /// <summary>Avance d'un pas Manhattan vers la cible sans repasser sur une case visitée.</summary>
    static bool TryStep(System.Random rng, LevelRegistry reg, ref Vector2Int cur, Vector2Int goal,
                        HashSet<Vector2Int> visited, List<Vector2Int> path)
    {
        int dx = goal.x - cur.x;
        int dy = goal.y - cur.y;
        if (dx == 0 && dy == 0) return false;

        // Deux candidats vers la cible, ordre aléatoire
        Vector2Int a = (dx != 0) ? new Vector2Int(cur.x + System.Math.Sign(dx), cur.y) : cur;
        Vector2Int b = (dy != 0) ? new Vector2Int(cur.x, cur.y + System.Math.Sign(dy)) : cur;

        if (dx != 0 && dy != 0 && rng.NextDouble() < 0.5)
            (a, b) = (b, a);

        if (a != cur && reg.InBounds(a) && !visited.Contains(a))
        { cur = a; path.Add(cur); visited.Add(cur); return true; }

        if (b != cur && reg.InBounds(b) && !visited.Contains(b))
        { cur = b; path.Add(cur); visited.Add(cur); return true; }

        return false; // bloqué
    }
}
