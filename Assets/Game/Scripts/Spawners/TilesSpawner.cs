using UnityEngine;

// Runtime tiles spawner.
// Awake : calcule originWorld (donnée) pour que les autres spawners puissent convertir monde/grille.
// Start : instancie les tuiles visuelles (construction).
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
        // Awake = initialiser les données.
        // On calcule originWorld ici pour qu'il soit disponible dès le Start des autres spawners.
        InitOriginWorld();
    }

    void Start()
    {
        // Start = construire le niveau.
        SpawnTiles();
    }

    /// <summary>Calcule originWorld à partir de la position du root (données).</summary>
    void InitOriginWorld()
    {
        var registry = LevelRegistry.Instance;
        if (!registry)
            registry = FindFirstObjectByType<LevelRegistry>();

        if (!registry)
        {
            Debug.LogError("[TilesSpawner] LevelRegistry missing");
            return;
        }

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

        registry.originWorld = ComputeOriginFromPlayer(registry, root.position);
    }

    /// <summary>Instancie les tuiles visuelles (construction).</summary>
    void SpawnTiles()
    {
        if (!tilePrefab)
        {
            Debug.LogError("[TilesSpawner] tilePrefab missing");
            return;
        }

        var registry = LevelRegistry.Instance;
        if (!registry)
        {
            Debug.LogError("[TilesSpawner] LevelRegistry missing");
            return;
        }

        EnsureRoot();
        ClearRuntime();

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

        return new Vector3(playerWorld.x - (midX * size), 0f, playerWorld.z);
    }
}
