using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class BestPath : MonoBehaviour
{
    [Header("Références")]
    public Transform player;
    public GameObject quadPrefab;

    [Header("Visibility")]
    public bool visible = true;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Récupérer les positions des nuages
        GameObject[] clouds = GameObject.FindGameObjectsWithTag("BugCloud");
        if (clouds.Length < 2)
        {
            Debug.LogWarning("[BestPath] Moins de 2 nuages trouvés (vérifie BugCloudSpawner).");
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

        // On récupère les positions arrondies des trois entités
        Vector3 playerPos = new Vector3(Mathf.Round(player.position.x), 0, Mathf.Round(player.position.z));
        Vector3 leftCloudPos = new Vector3(Mathf.Round(leftCloud.transform.position.x), 0, Mathf.Round(leftCloud.transform.position.z));
        Vector3 rightCloudPos = new Vector3(Mathf.Round(rightCloud.transform.position.x), 0, Mathf.Round(rightCloud.transform.position.z));

        // On crée les deux tableaux de positions pour les deux chemins
        Vector3[] pathToLeftCloud = new Vector3[(int)(Mathf.Abs(playerPos.x - leftCloudPos.x) + Mathf.Abs(playerPos.z - leftCloudPos.z)) + 1];
        Vector3[] pathToRightCloud = new Vector3[(int)(Mathf.Abs(playerPos.x - rightCloudPos.x) + Mathf.Abs(playerPos.z - rightCloudPos.z)) + 1];

        // On remplit les deux tableaux avec les positions des cases du chemin
        int index = 0;
        Vector3 currentPos = playerPos;
        pathToLeftCloud[index++] = currentPos;  // Ajouter la position initiale du joueur

        while (currentPos != leftCloudPos)  // Chemin vers le nuage gauche
        {
            if (currentPos.x != leftCloudPos.x && (currentPos.z == leftCloudPos.z || Random.value < 0.5f))
                currentPos.x--; // Se déplacer horizontalement
            else if (currentPos.z != leftCloudPos.z)
                currentPos.z++; // Se déplacer verticalement
            pathToLeftCloud[index++] = currentPos; // On ajoute la nouvelle position au chemin
        }

        index = 0;
        currentPos = playerPos;
        pathToRightCloud[index++] = currentPos;  // Ajouter la position initiale du nuage gauche

        while (currentPos != rightCloudPos) // Chemin vers le nuage droite
        {
            if (currentPos.x != rightCloudPos.x && (currentPos.z == rightCloudPos.z || Random.value < 0.5f))
                currentPos.x++; // Se déplacer horizontalement
            else if (currentPos.z != rightCloudPos.z)
                currentPos.z++; // Se déplacer verticalement
            pathToRightCloud[index++] = currentPos; // On ajoute la nouvelle position au chemin
        }







        // ------ ENREGISTREMENT DANS LE REGISTRE (réserver les deux chemins) ------
        if (LevelRegistry.Instance != null)
        {
            var leftCells = new List<Vector2Int>(pathToLeftCloud.Length);
            var rightCells = new List<Vector2Int>(pathToRightCloud.Length);

            foreach (var p in pathToLeftCloud)
                leftCells.Add(LevelRegistry.Instance.WorldToCell(p));
            foreach (var p in pathToRightCloud)
                rightCells.Add(LevelRegistry.Instance.WorldToCell(p));

            LevelRegistry.Instance.ReservePathLeft(leftCells);
            LevelRegistry.Instance.ReservePathRight(rightCells);
        }








        // ------ INSTANTIATION DES QUADS LE LONG DES CHEMINS ------

        // Si visible est false, on ne fait rien
        if (!visible) return;

        // On tire au sort quel chemin on affiche, par défaut
        Vector3[] chosenPath = (Random.value < 0.5f) ? pathToLeftCloud : pathToRightCloud;
        // Si GameManager connaît un nuage "meilleur", on force le chemin correspondant
        if (GameManager.Instance != null)
        {
            var best = GameManager.Instance.GetBestCloud();
            if (best != null)
                chosenPath = (Mathf.RoundToInt(best.transform.position.x) ==
                              Mathf.RoundToInt(leftCloud.transform.position.x))
                             ? pathToLeftCloud
                             : pathToRightCloud;

            // Publier la liste des cellules du chemin conseillé
            var advisorCells = new List<Vector2Int>(chosenPath.Length);
            foreach (var p in chosenPath)
                advisorCells.Add(LevelRegistry.Instance.WorldToCell(p));

            GameManager.Instance.SetChosenPath(advisorCells);

            // Enregistrer le chemin optimal dans le LevelRegistry
            LevelRegistry.Instance.RegisterOptimalPath(advisorCells);

        }


        // Instancier les quads le long des chemins
        foreach (var pos in chosenPath)
        {
            Instantiate(quadPrefab, new Vector3(pos.x, 0.01f, pos.z), quadPrefab.transform.rotation, transform);
        }







        // ------ REVELER LES CASES DU CHEMIN DANS LE FOG OF WAR ------
        if (FogController.Instance != null)
        {
            var cells = new List<Vector2Int>(chosenPath.Length);
            foreach (var p in chosenPath)
                cells.Add(new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z)));

            // Révèle aussi la case de départ si tu veux
            cells.Add(FogController.Instance.WorldToCell(player.position));

            FogController.Instance.RevealCells(cells);
        }
    }
}
