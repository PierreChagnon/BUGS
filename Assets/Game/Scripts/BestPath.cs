using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class BestPath : MonoBehaviour
{
    [Header("Références")]
    public GameObject quadPrefab;

    [Header("Visibility")]
    public bool visible = true;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var reg = LevelRegistry.Instance;
        if (reg == null)
        {
            Debug.LogError("[BestPath] LevelRegistry manquant dans la scène.");
            return;
        }

        if (quadPrefab == null)
        {
            Debug.LogError("[BestPath] quadPrefab manquant (assigne le prefab de quad dans l'inspecteur).");
            return;
        }

        // Récupérer les positions des nuages
        GameObject[] clouds = GameObject.FindGameObjectsWithTag("BugCloud");
        if (clouds.Length < 2)
        {
            Debug.LogWarning($"[BestPath] Moins de 2 nuages trouvés (count={clouds.Length}). Vérifie BugCloudSpawner + tag 'BugCloud'.");
            return;
        }

        if (!reg.TryGetPlayerStartCell(out var playerCell))
        {
            Debug.LogError("[BestPath] player start non enregistré (vérifie PlayerSpawner / LevelRegistry).");
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
            if (currentPos.x != leftCloudCell.x && (currentPos.y == leftCloudCell.y || Random.value < 0.5f))
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
            if (currentPos.x != rightCloudCell.x && (currentPos.y == rightCloudCell.y || Random.value < 0.5f))
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

        // Si visible est false, on ne fait rien
        if (!visible)
        {
            Debug.Log("[BestPath] visible=false => instanciation des quads ignorée.");
            return;
        }

        // On tire au sort quel chemin on affiche, par défaut
        Vector2Int[] chosenPath = (Random.value < 0.5f) ? pathToLeftCloud : pathToRightCloud;
        // Si GameManager connaît un nuage "meilleur", on force le chemin correspondant
        if (GameManager.Instance != null)
        {
            var best = GameManager.Instance.GetBestCloud();
            if (best != null)
                chosenPath = (reg.WorldToCell(best.transform.position).x == reg.WorldToCell(leftCloud.transform.position).x)
                             ? pathToLeftCloud
                             : pathToRightCloud;

            // Publier la liste des cellules du chemin conseillé
            var advisorCells = new List<Vector2Int>(chosenPath.Length);
            foreach (var c in chosenPath)
                advisorCells.Add(c);

            GameManager.Instance.SetChosenPath(advisorCells);

            // Enregistrer le chemin optimal dans le LevelRegistry
            reg.RegisterOptimalPath(advisorCells);

        }


        // Instancier les quads le long des chemins
        int spawned = 0;
        foreach (var cell in chosenPath)
        {
            var pos = reg.CellToWorld(cell, 0.11f);

            // Important: on instancie sans parent puis on parent en conservant la transform monde.
            // Cela évite les surprises si le GO BestPath a une scale non-1.
            var quad = Instantiate(quadPrefab, pos, quadPrefab.transform.rotation);
            quad.transform.SetParent(transform, true);

            if (!quad.activeSelf) quad.SetActive(true);
            spawned++;
        }

        Debug.Log($"[BestPath] Quads instanciés: {spawned} (originWorld={reg.originWorld}, cellSize={reg.cellSize}).");







        // ------ REVELER LES CASES DU CHEMIN DANS LE FOG OF WAR ------
        if (FogController.Instance != null)
        {
            var cells = new List<Vector2Int>(chosenPath.Length);
            foreach (var c in chosenPath)
                cells.Add(c);

            // Révèle aussi la case de départ si tu veux
            cells.Add(playerCell);

            FogController.Instance.RevealCells(cells);
        }
    }
}
