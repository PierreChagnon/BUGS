using UnityEngine;

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
    }

    // Update is called once per frame
    void Update()
    {

    }
}
