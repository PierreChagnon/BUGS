using UnityEngine;

[DefaultExecutionOrder(-250)] // Start(-250) : premier spawner à tourner en Start, après que TilesSpawner.Awake(-240) ait posé originWorld
public class PlayerSpawner : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Prefab du joueur à instancier.")]
    [SerializeField] private GameObject playerPrefab;

    [Tooltip("Transform de spawn (position + rotation).")]
    [SerializeField] private Transform spawnTransform;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: playerPrefab n'est pas assigné.", this);
            return;
        }

        if (spawnTransform == null)
        {
            Debug.LogError("PlayerSpawner: spawnTransform n'est pas assigné.", this);
            return;
        }

        Instantiate(playerPrefab, spawnTransform.position, spawnTransform.rotation);

        // Enregistre l'info dans le LevelRegistry pour éviter de placer des pièges sur la case de départ du joueur
        var registry = LevelRegistry.Instance;
        if (registry != null)
        {
            Vector2Int spawnCell = registry.WorldToCell(spawnTransform.position);
            registry.RegisterPlayerStart(spawnCell, spawnTransform.position);
        }
    }
}
