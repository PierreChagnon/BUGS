using System.Collections.Generic;
using UnityEngine;
using System;

[DefaultExecutionOrder(-10)]
public class TrapSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject trapPrefab;     // Prefab du piège (avec Trap.cs + BoxCollider IsTrigger)

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

        var registry = LevelRegistry.Instance;
        if (registry == null)
        {
            Debug.LogError("[TrapSpawner] LevelRegistry manquant dans la scène.");
            return;
        }

        var rng = registry.CreateRng(nameof(TrapSpawner));

        // Construire la liste de toutes les cases possibles (source de vérité: LevelRegistry)
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < registry.gridSize.x; x++)
            for (int z = 0; z < registry.gridSize.y; z++)
                candidates.Add(new Vector2Int(x, z));

        // Éviter la case du joueur
        if (registry.TryGetPlayerStartCell(out var playerCell))
            candidates.Remove(playerCell);


        // Filtrer les cases interdites depuis le LevelRegistry 
        candidates.RemoveAll(c => !registry.IsFreeForTrap(c));


        // Mélanger les cases
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = rng.Next(i, candidates.Count);
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

            placed++;
        }

        Debug.Log($"[TrapSpawner] Pièges posés: {placed}/{trapCount}");
    }
}
