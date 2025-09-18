using UnityEngine;

public class BugCloud : MonoBehaviour
{
    [Header("Butin")]
    [Tooltip("Nombre total d'insectes remis à la collecte.")]
    public int totalBugs = 20;

    [Range(0f, 1f)]
    [Tooltip("Proportion de verts (le reste sera rouge).")]
    public float greenRatio = 0.7f;

    [Header("Effets")]
    public float rotationSpeed = 0.3f; // Vitesse de rotation du nuage

    // Update is called once per frame
    void Update()
    {
        // Faire tourner le nuage autour de son axe Y
        transform.Rotate(0, rotationSpeed, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        print("Collision détectée avec " + other.name);
        if (other.CompareTag("Player"))
        {
            print("Collecte d'insectes !");
            // Destroy the bug cloud when the player collides with it
            Destroy(gameObject);
        }
    }
}
