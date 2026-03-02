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
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[Trap] Piège déclenché !");

        if (GameManager.Instance != null)
            GameManager.Instance.OnTrapTriggered();
    }
}
