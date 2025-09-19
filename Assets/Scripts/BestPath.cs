using UnityEngine;

public class BestPath : MonoBehaviour
{
    public Transform player;

    public GameObject bugCloudPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Récupérer les positions des nuages
        GameObject[] clouds = GameObject.FindGameObjectsWithTag("BugCloud");

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
        Debug.Log($"[BestPath] Nuage gauche: {leftCloud.transform.position}, Nuage droite: {rightCloud.transform.position}");

        // ------ DEFINITION DES DEUX CHEMINS (best paths) ------

        // Chemin 1 : Joueur -> Nuage gauche
        // Chemin 2 : Joueur -> Nuage droite
        // On détermine un chemin aléatoire mais avec la distance la plus courte possible entre le joueur et le nuage de gauche, 
        // puis entre le joueur de gauche et le nuage de droite

        // On récupère les positions arrondies des trois entités
        Vector3 playerPos = new Vector3(Mathf.Round(player.position.x), 0, Mathf.Round(player.position.z));
        Vector3 leftCloudPos = new Vector3(Mathf.Round(leftCloud.transform.position.x), 0, Mathf.Round(leftCloud.transform.position.z));
        Vector3 rightCloudPos = new Vector3(Mathf.Round(rightCloud.transform.position.x), 0, Mathf.Round(rightCloud.transform.position.z));
        Debug.Log($"[BestPath] Positions arrondies: Joueur {playerPos}, Nuage gauche {leftCloudPos}, Nuage droite {rightCloudPos}");

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
        // Afficher les deux chemins dans la console
        string path1 = "[BestPath] Chemin vers nuage gauche: ";
        foreach (var pos in pathToLeftCloud)
            path1 += pos + " ";
        Debug.Log(path1);

        string path2 = "[BestPath] Chemin vers nuage droite: ";
        foreach (var pos in pathToRightCloud)
            path2 += pos + " ";
        Debug.Log(path2);





        // Instancier les quads le long des chemins
        foreach (var pos in pathToLeftCloud)
        {
            Instantiate(bugCloudPrefab, new Vector3(pos.x, 0.01f, pos.z), bugCloudPrefab.transform.rotation, transform);
        }
    }
}
