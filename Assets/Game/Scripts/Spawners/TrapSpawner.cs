using System.Collections.Generic;
using UnityEngine;
using System;

[DefaultExecutionOrder(-10)]
public class TrapSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject trapPrefab;     // Prefab du piège (avec Trap.cs + BoxCollider IsTrigger)

    [Header("Placement")]
    public float trapYOffset = 0.5f; // moitié de la hauteur du cube si pivot au centre

    int _trapCount;

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

        // Lire le trapCount depuis SessionManager (propriétaire de la config expérimentale)
        var session = SessionManager.Instance;
        if (session == null)
        {
            Debug.LogError("[TrapSpawner] SessionManager manquant dans la scène.");
            return;
        }
        _trapCount = session.trapCount;

        var rng = registry.CreateRng(nameof(TrapSpawner));

        // ── Pièges sur le chemin suboptimal (comptent dans le budget trapCount) ──
        int suboptimalPlaced = PlaceSuboptimalTraps(registry, session, rng);
        int remainingTraps = _trapCount - suboptimalPlaced;

        // ── Pièges normaux (logique inchangée, hors chemin suboptimal) ──

        // Construire la liste de toutes les cases possibles (source de vérité: LevelRegistry)
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < registry.gridSize.x; x++)
            for (int z = 0; z < registry.gridSize.y; z++)
                candidates.Add(new Vector2Int(x, z));

        // Éviter la case du joueur
        if (registry.TryGetPlayerStartCell(out var playerCell))
            candidates.Remove(playerCell);

        // Exclure les cases du chemin suboptimal (pièges déjà gérés ci-dessus)
        candidates.RemoveAll(c => registry.IsOnSuboptimalPath(c));

        // Filtrer les cases interdites depuis le LevelRegistry 
        candidates.RemoveAll(c => !registry.IsFreeForTrap(c));


        // Mélanger les cases
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = rng.Next(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        // Poser jusqu'à remainingTraps pièges (enfants de ce spawner)
        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < remainingTraps; i++)
        {

            var cell = candidates[i];
            if (!registry.RegisterTrap(cell)) continue; // s'assure registre à jour + évite doublon
            SpawnTrap(registry, cell);

            placed++;
        }

        Debug.Log($"[TrapSpawner] Pièges posés: {placed + suboptimalPlaced}/{_trapCount} ({suboptimalPlaced} sur chemin suboptimal, {placed} ailleurs)");
    }

    /// <summary>
    /// Place des pièges spécifiquement sur les cellules du chemin suboptimal.
    /// Retourne le nombre de pièges effectivement posés.
    /// </summary>
    int PlaceSuboptimalTraps(LevelRegistry registry, SessionManager session, System.Random rng)
    {
        if (session.suboptimalTrapProbability <= 0f) return 0;

        // Collecter les cellules du chemin suboptimal
        var subCells = new List<Vector2Int>();
        for (int x = 0; x < registry.gridSize.x; x++)
            for (int z = 0; z < registry.gridSize.y; z++)
            {
                var c = new Vector2Int(x, z);
                if (registry.IsOnSuboptimalPath(c) && registry.IsFreeForTrap(c))
                    subCells.Add(c);
            }

        if (subCells.Count == 0) return 0;

        // Tirage de la probabilité
        if (rng.NextDouble() >= session.suboptimalTrapProbability) return 0;

        // Tirage du nombre de pièges entre les bornes min/max
        int min = Mathf.Max(0, session.minSuboptimalTraps);
        int max = Mathf.Max(min, session.maxSuboptimalTraps);
        int targetCount = rng.Next(min, max + 1);

        // Mélanger les cellules candidates
        for (int i = 0; i < subCells.Count; i++)
        {
            int j = rng.Next(i, subCells.Count);
            (subCells[i], subCells[j]) = (subCells[j], subCells[i]);
        }

        // Placer les pièges
        int placed = 0;
        for (int i = 0; i < subCells.Count && placed < targetCount; i++)
        {
            var cell = subCells[i];
            if (!registry.RegisterTrap(cell)) continue;
            SpawnTrap(registry, cell);
            placed++;
        }

        Debug.Log($"[TrapSpawner] Pièges suboptimaux: {placed}/{targetCount} (prob={session.suboptimalTrapProbability}, bornes=[{min},{max}])");
        return placed;
    }

    void SpawnTrap(LevelRegistry registry, Vector2Int cell)
    {
        Vector3 pos = registry.CellToWorld(cell, trapYOffset);
        var trap = Instantiate(trapPrefab, pos, Quaternion.identity, transform);

        var visibility = trap.GetComponent<TrapVisibility>();
        if (visibility == null)
            visibility = trap.AddComponent<TrapVisibility>();

        visibility.Initialize(cell);
    }
}
