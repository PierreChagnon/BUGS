using System.Collections.Generic;
using UnityEngine;

public class TrapSpawner : MonoBehaviour
{
    [Header("Références")]
    public Transform player;          // Pour éviter la case du joueur au départ
    public GameObject trapPrefab;     // Prefab du cube rouge (avec Trap.cs + BoxCollider IsTrigger)

    [Header("Grille")]
    public Vector3 gridOrigin = Vector3.zero; // coin (bas-gauche) de ta grille en monde
    public Vector2Int gridSize = new Vector2Int(10, 10); // nombre de cases (X,Z)
    public float cellSize = 1f;

    [Header("Placement")]
    public int trapCount = 10;        // nombre de pièges à poser

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
            Vector2Int playerCell = WorldToCell(player.position);
            candidates.Remove(playerCell);
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
            Vector3 pos = CellToWorld(candidates[i]);
            Instantiate(trapPrefab, pos, Quaternion.identity, transform);
            Debug.Log($"[TrapSpawner] Piège placé en {pos} (cell {candidates[i]})");
            placed++;
        }

        Debug.Log($"[TrapSpawner] Pièges posés: {placed}/{trapCount}");
    }

    // --- Helpers grille ---

    Vector2Int WorldToCell(Vector3 worldPos)
    {
        int cx = Mathf.RoundToInt((worldPos.x - gridOrigin.x) / cellSize);
        int cz = Mathf.RoundToInt((worldPos.z - gridOrigin.z) / cellSize);
        return new Vector2Int(cx, cz);
    }

    public float groundY = 0f;       // hauteur du sol (tiles)
    public float trapYOffset = 0.5f; // moitié de la hauteur du cube si pivot au centre
    Vector3 CellToWorld(Vector2Int cell)
    {
        // Centre de la case si tes cases sont sur des multiples entiers (comme ton Snap)
        float x = gridOrigin.x + cell.x * cellSize;
        float z = gridOrigin.z + cell.y * cellSize;

        // Hauteur : on colle à la hauteur du player (proto plat)
         float y = groundY + trapYOffset;

        return new Vector3(x, y, z);
    }
}
