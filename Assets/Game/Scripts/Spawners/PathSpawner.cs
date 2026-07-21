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
        bool bestKnown = false;
        if (GameManager.Instance != null)
        {
            var best = GameManager.Instance.GetBestCloud();
            if (best != null)
            {
                bestIsLeft = reg.WorldToCell(best.transform.position).x == reg.WorldToCell(leftCloud.transform.position).x;
                bestKnown = true;
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
        bool proximalForced = FlowController.Instance != null &&
                              FlowController.Instance.State != null &&
                              FlowController.Instance.State.proximal_choice_is_forced;
        bool forcedIsLeft = proximalForced &&
                            FlowController.Instance.State.proximal_choice_forced_value != DistalScanSide.Right;

        if (hasAdvisor && proximalForced)
        {
            displayPath = new List<Vector2Int>(forcedIsLeft ? pathToLeftCloud : pathToRightCloud);
            isSuboptimal = bestKnown && forcedIsLeft != bestIsLeft;
        }
        else if (hasAdvisor && session.suboptimalPathProbability > 0f && rng.NextDouble() < session.suboptimalPathProbability)
        {
            isSuboptimal = true;
            Vector2Int bestCloudCell = bestIsLeft ? leftCloudCell : rightCloudCell;

            // Détour (crochet) ou simple chemin Manhattan alternatif ?
            bool withDetour = session.detourProbability > 0f && rng.NextDouble() < session.detourProbability;

            if (withDetour)
                displayPath = BuildSuboptimalDetour(rng, reg, playerCell, bestCloudCell, out withDetour);
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
        // Toujours révéler la case joueur. En proximal forced, seul le cloud imposé est révélé.
        // Le chemin lui-même n'est révélé que si visible.
        if (FogController.Instance != null)
        {
            var reveal = new List<Vector2Int> { playerCell };
            if (proximalForced)
                reveal.Add(forcedIsLeft ? leftCloudCell : rightCloudCell);
            else
            {
                reveal.Add(leftCloudCell);
                reveal.Add(rightCloudCell);
            }

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
    /// Construit un chemin suboptimal en forme de « U » ouvert vers la cible :
    /// 1. Monte de quelques cases avant le crochet
    /// 2. Part horizontalement à l'opposé de la cible
    /// 3. Monte exactement de GAP cases
    /// 4. Revient horizontalement et continue jusqu'à l'axe X de la cible
    /// 5. Rejoint la cible uniquement à la verticale
    /// Les deux seules portions horizontales sont donc séparées par une rangée vide.
    /// Aucune troisième portion horizontale ne peut longer le retour près de la cible.
    /// </summary>
    List<Vector2Int> BuildSuboptimalDetour(System.Random rng, LevelRegistry reg,
                                            Vector2Int start, Vector2Int goal,
                                            out bool detourBuilt)
    {
        const int GAP = 2; // une rangée vide entre les deux portions horizontales
        detourBuilt = false;

        // Le crochet part du côté opposé au nuage. Son retour peut alors être prolongé
        // jusqu'à goal.x sans changer de sens ni créer une troisième portion horizontale.
        int goalDx = goal.x - start.x;
        if (goalDx == 0)
            return BuildRandomManhattanPath(rng, reg, start, goal);

        int hookDx = -System.Math.Sign(goalDx);
        int horizontalCapacity = hookDx > 0
            ? reg.gridSize.x - 1 - start.x
            : start.x;

        if (horizontalCapacity <= 0)
            return BuildRandomManhattanPath(rng, reg, start, goal);

        int safeDetourMin = Mathf.Max(1, detourMin);
        int safeDetourMax = Mathf.Max(safeDetourMin + 1, detourMax);
        int detourSize = Mathf.Min(rng.Next(safeDetourMin, safeDetourMax), horizontalCapacity);

        // Le crochet ne doit être ni sur la rangée des nuages (il pourrait traverser
        // l'autre nuage), ni GAP rangées dessous pour la même raison au retour.
        int maxPrefixSteps = Mathf.Min(2, reg.gridSize.y - 1 - GAP - start.y);
        var prefixCandidates = new List<int>();
        for (int steps = 0; steps <= maxPrefixSteps; steps++)
        {
            int lowerY = start.y + steps;
            int upperY = lowerY + GAP;
            if (lowerY != goal.y && upperY != goal.y)
                prefixCandidates.Add(steps);
        }

        if (prefixCandidates.Count == 0)
            return BuildRandomManhattanPath(rng, reg, start, goal);

        int normalSteps = prefixCandidates[rng.Next(prefixCandidates.Count)];

        var visited = new HashSet<Vector2Int>();
        var path = new List<Vector2Int>();
        var cur = start;
        path.Add(cur);
        visited.Add(cur);

        // Phase 1 : préfixe uniquement vertical. Il ne peut donc pas longer le crochet.
        if (!TryVertical(reg, ref cur, normalSteps, reg.gridSize.y - 1, visited, path))
            return BuildRandomManhattanPath(rng, reg, start, goal);

        // Phase 2 : crochet horizontal à l'opposé de la cible.
        TryHorizontal(reg, ref cur, hookDx, detourSize, visited, path);

        // Phase 3 : séparation garantie, jamais tronquée par la hauteur du nuage.
        if (!TryVertical(reg, ref cur, GAP, reg.gridSize.y - 1, visited, path))
            return BuildRandomManhattanPath(rng, reg, start, goal);

        // Phase 4 : retour puis prolongement sur la même ligne jusqu'à la cible.
        int horizontalToGoal = Mathf.Abs(goal.x - cur.x);
        TryHorizontal(reg, ref cur, System.Math.Sign(goal.x - cur.x), horizontalToGoal, visited, path);

        // Phase 5 : arrivée purement verticale, donc aucune nouvelle parallèle horizontale.
        if (!TryVerticalTowards(reg, ref cur, goal.y, visited, path))
            return BuildRandomManhattanPath(rng, reg, start, goal);

        int directDistance = Mathf.Abs(goal.x - start.x) + Mathf.Abs(goal.y - start.y);
        bool hasRealDetour = path.Count - 1 > directDistance;
        if (cur != goal || !hasRealDetour || HasNonConsecutiveAdjacentCells(path))
        {
            Debug.LogWarning("[PathSpawner] Crochet invalide ou visuellement ambigu : chemin Manhattan utilisé en fallback.");
            return BuildRandomManhattanPath(rng, reg, start, goal);
        }

        detourBuilt = true;
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
    static bool TryVertical(LevelRegistry reg, ref Vector2Int cur, int count, int maxY,
                             HashSet<Vector2Int> visited, List<Vector2Int> path)
    {
        for (int i = 0; i < count; i++)
        {
            var next = new Vector2Int(cur.x, cur.y + 1);
            if (!reg.InBounds(next) || visited.Contains(next) || next.y > maxY) return false;
            cur = next;
            path.Add(cur);
            visited.Add(cur);
        }

        return true;
    }

    /// <summary>Rejoint une rangée en ligne droite, vers le haut ou vers le bas.</summary>
    static bool TryVerticalTowards(LevelRegistry reg, ref Vector2Int cur, int targetY,
                                    HashSet<Vector2Int> visited, List<Vector2Int> path)
    {
        while (cur.y != targetY)
        {
            var next = new Vector2Int(cur.x, cur.y + System.Math.Sign(targetY - cur.y));
            if (!reg.InBounds(next) || visited.Contains(next)) return false;
            cur = next;
            path.Add(cur);
            visited.Add(cur);
        }

        return true;
    }

    /// <summary>
    /// Détecte les contacts entre deux cases du chemin qui ne se suivent pas.
    /// Un tel contact dessine un faux embranchement ou un raccourci visuel.
    /// </summary>
    static bool HasNonConsecutiveAdjacentCells(List<Vector2Int> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            for (int j = i + 2; j < path.Count; j++)
            {
                int distance = Mathf.Abs(path[i].x - path[j].x) + Mathf.Abs(path[i].y - path[j].y);
                if (distance == 1)
                    return true;
            }
        }

        return false;
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
