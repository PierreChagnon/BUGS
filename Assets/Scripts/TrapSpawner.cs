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


        var registry = LevelRegistry.Instance;
        // Éviter la case du joueur
        if (player != null)
        {
            Vector2Int playerCell = registry.WorldToCell(player.position);
            candidates.Remove(playerCell);
        }


        // Filtrer les cases interdites depuis le LevelRegistry 
        if (registry == null)
        {
            Debug.LogError("[TrapSpawner] LevelRegistry manquant dans la scène.");
            return;
        }
        candidates.RemoveAll(c => !registry.IsFreeForTrap(c));


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

            var cell = candidates[i];
            if (!registry.RegisterTrap(cell)) continue; // s'assure registre à jour + évite doublon
            Vector3 pos = registry.CellToWorld(cell, trapYOffset);
            Instantiate(trapPrefab, pos, Quaternion.identity, transform);

            Debug.Log($"[TrapSpawner] Piège placé en {pos} (cell {candidates[i]})");
            placed++;
        }

        Debug.Log($"[TrapSpawner] Pièges posés: {placed}/{trapCount}");
    }
}
