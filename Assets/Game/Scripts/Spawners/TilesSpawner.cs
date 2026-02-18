using UnityEngine;

// Runtime tiles spawner.
// Spawns the grid at play time (scene can be empty), positioning it from the player's transform.
// Constraint: player must end up on the first row (z=0) and centered on X.
[DefaultExecutionOrder(-240)]
public class TilesSpawner : MonoBehaviour
{
    [Header("Tiles")]
    public GameObject tilePrefab;

    [Header("Placement")]
    [Tooltip("Transform de référence obligatoire pour positionner la grille.")]
    public Transform root;

    [Tooltip("Hauteur Y à laquelle la grille est générée.")]
    public float tilesY = 0f;

    private Transform tilesRoot;

    void Awake()
    {
        Spawn();
    }

    public void Spawn()
    {
        if (!tilePrefab)
        {
            Debug.LogError("[TilesSpawner] tilePrefab missing");
            return;
        }

        var registry = LevelRegistry.Instance;
        if (!registry)
            registry = FindFirstObjectByType<LevelRegistry>();

        if (!registry)
        {
            Debug.LogError("[TilesSpawner] LevelRegistry missing");
            return;
        }

        // Source de vérité (runtime): LevelRegistry.
        // TilesSpawner ne définit PAS gridSize/cellSize : il ne fait qu'initialiser originWorld
        // à partir de la position du player, afin d'aligner la grille sur le monde.
        if (registry.gridSize.x <= 0 || registry.gridSize.y <= 0)
        {
            Debug.LogError("[TilesSpawner] registry.gridSize invalide (doit être > 0). Configure-le dans LevelRegistry.");
            return;
        }

        if (root == null)
        {
            Debug.LogError("[TilesSpawner] root manquant (obligatoire pour positionner la grille).");
            return;
        }

        Vector3 playerWorld = root.position;

        // On positionne la grille de manière à ce que la cellule du milieu de la première ligne (z=0) soit sous le player.
        registry.originWorld = ComputeOriginFromPlayer(registry, playerWorld);

        EnsureRoot();
        ClearRuntime();

        // Pour garder une hiérarchie claire: root représente la case (0,0).
        tilesRoot.position = new Vector3(registry.originWorld.x, tilesY, registry.originWorld.z);

        for (int y = 0; y < registry.gridSize.y; y++)
        for (int x = 0; x < registry.gridSize.x; x++)
        {
            var tile = Instantiate(tilePrefab, tilesRoot);
            float size = Mathf.Max(0.0001f, registry.cellSize);
            tile.transform.localPosition = new Vector3(x * size, 0f, y * size);
            tile.name = $"Tile_{x}_{y}";
        }
    }

    void EnsureRoot()
    {
        // En runtime, on crée un root temporaire pour organiser les tiles.
        var go = new GameObject("TilesRootRuntime");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        tilesRoot = go.transform;
    }

    void ClearRuntime()
    {
        if (!tilesRoot) return;

        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
            Destroy(tilesRoot.GetChild(i).gameObject);
    }

    static Vector3 ComputeOriginFromPlayer(LevelRegistry registry, Vector3 playerWorld)
    {
        int width = registry != null ? registry.gridSize.x : 0;
        int midX = Mathf.Clamp(width / 2, 0, Mathf.Max(0, width - 1));
        float size = Mathf.Max(0.0001f, registry != null ? registry.cellSize : 1f);

        // On veut que la cellule (midX, 0) soit sous le joueur.
        // Donc origine (0,0) = player - (midX * cellSize, 0).
        return new Vector3(playerWorld.x - (midX * size), 0f, playerWorld.z);
    }
}
