using UnityEngine;

// -----------------------------
// Entité piège.
// Détecte la collision avec le joueur et signale le déclenchement
// au GameManager (qui applique la pénalité en conséquence).
// Le piège ne connaît pas les règles du jeu : il sait juste
// qu'il est un piège et qu'il a été déclenché.
// -----------------------------

public class Trap : MonoBehaviour
{
    bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        Debug.Log("[Trap] Piège déclenché !");

        if (GameManager.Instance != null)
            GameManager.Instance.OnTrapTriggered();
    }
}
