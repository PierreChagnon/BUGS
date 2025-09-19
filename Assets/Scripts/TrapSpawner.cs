using System.Collections.Generic;
using UnityEngine;

public class TrapSpawner : MonoBehaviour
{
    [Header("Références")]
    public Transform player;          // Pour éviter la case du joueur au départ
    public GameObject trapPrefab;     // Prefab du piège (avec Trap.cs + BoxCollider IsTrigger)

    [Header("Grille")]
    public Vector2Int gridSize = new Vector2Int(10, 10); // nombre de cases (X,Z)

    [Header("Placement")]
    public int trapCount = 10;        // nombre de pièges à poser
    public float trapYOffset = 0.5f; // moitié de la hauteur du cube si pivot au centre

    void Start()
    {
        if (trapPrefab == null)
        {
            Debug.LogError("[TrapSpawner] Aucun trapPrefab assigné.");
            return;
        }

        // Construire la liste de toutes les cases possibles
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < gridSize.x; x++)
            for (int z = 0; z < gridSize.y; z++)
                candidates.Add(new Vector2Int(x, z));

        // Éviter la case du joueur
        if (player != null)
        {
            int cx = Mathf.RoundToInt(player.position.x);
            int cz = Mathf.RoundToInt(player.position.z);
            Vector2Int playerCell = new(cx, cz);
            candidates.Remove(playerCell);
        }

        // Eviter les cases des nuages
        var clouds = GameObject.FindGameObjectsWithTag("BugCloud");
        foreach (var cloud in clouds)
        {
            Vector2Int cloudCell = new(Mathf.RoundToInt(cloud.transform.position.x), Mathf.RoundToInt(cloud.transform.position.z));
            Debug.Log($"[TrapSpawner] Évite la case du nuage {cloudCell}");
            candidates.Remove(cloudCell);
        }

        // Eviter les cases du chemin optimal
        var bestPath = GameObject.FindGameObjectsWithTag("BestPath");
        foreach (var quad in bestPath)
        {
            Vector2Int quadCell = new(Mathf.RoundToInt(quad.transform.position.x), Mathf.RoundToInt(quad.transform.position.z));
            Debug.Log($"[TrapSpawner] Évite la case du chemin optimal {quadCell}");
            candidates.Remove(quadCell);
        }

        // Mélanger les cases
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        // Poser jusqu'à trapCount pièges (enfants de ce spawner)
        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < trapCount; i++)
        {
            Vector3 pos = new(candidates[i].x, trapYOffset, candidates[i].y);
            Instantiate(trapPrefab, pos, Quaternion.identity, transform);
            Debug.Log($"[TrapSpawner] Piège placé en {pos} (cell {candidates[i]})");
            placed++;
        }

        Debug.Log($"[TrapSpawner] Pièges posés: {placed}/{trapCount}");
    }
}
