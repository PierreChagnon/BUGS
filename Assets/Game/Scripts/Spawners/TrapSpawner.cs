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

        // Lire la config expérimentale via SessionManager.Map (source de vérité unique)
        var session = SessionManager.Instance;
        if (session == null)
        {
            Debug.LogError("[TrapSpawner] SessionManager manquant dans la scène.");
            return;
        }
        MapGenConfig map = session.Map;
        _trapCount = map.trap_count;

        var rng = registry.CreateRng(nameof(TrapSpawner));

        // ── Pièges sur le chemin suboptimal (comptent dans le budget trapCount) ──
        int suboptimalPlaced = PlaceSuboptimalTraps(registry, map, rng);
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
    int PlaceSuboptimalTraps(LevelRegistry registry, MapGenConfig map, System.Random rng)
    {
        if (map.suboptimal_trap_probability <= 0f) return 0;

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

        // Tirages probabilité puis nombre de pièges (résolus dans Data/)
        int targetCount = TrialDrawResolver.DrawSuboptimalTrapCount(
            map.suboptimal_trap_probability,
            map.min_suboptimal_traps,
            map.max_suboptimal_traps,
            rng);

        if (targetCount <= 0) return 0;

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

        Debug.Log($"[TrapSpawner] Pièges suboptimaux: {placed}/{targetCount} (prob={map.suboptimal_trap_probability}, bornes=[{map.min_suboptimal_traps},{map.max_suboptimal_traps}])");
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
