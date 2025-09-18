using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    [Tooltip("Tag utilisé par les nuages d'insectes.")]
    public string bugCloudTag = "BugCloud";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(bugCloudTag))
        {
            print("Collecte d'insectes !");
            var cloud = other.GetComponent<BugCloud>();
            // if (cloud != null) cloud.Collect();
        }
    }
}
