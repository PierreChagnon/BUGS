using UnityEngine;

public class Trap : MonoBehaviour
{
    [Tooltip("Combien de bugs on 'perd' (exemple visuel via log)")]
    public int bugsLost = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[TRAP] Piège déclenché par {other.name} → -{bugsLost} bugs (exemple)");
            // Ici plus tard: appliquer un vrai effet (score, ralentissement, etc.)
        }
    }
}
