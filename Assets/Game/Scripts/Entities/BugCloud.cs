using UnityEngine;

public class BugCloud : MonoBehaviour
{
    [Header("Références Particules")]
    [SerializeField]
    private ParticleSystem greenBugsParticles;
    [SerializeField]
    private ParticleSystem redBugsParticles;

    [Header("Butin")]
    [Tooltip("Nombre total d'insectes remis à la collecte.")]
    public int totalBugs = 20;

    [Range(0f, 1f)]
    [Tooltip("Proportion de verts (le reste sera rouge).")]
    public float greenRatio = 0.7f;

    [Header("Effets")]
    [Tooltip("Vitesse de rotation en degrés par seconde.")]
    public float rotationSpeed = 30f;

    void Update()
    {
        // Faire tourner le nuage autour de son axe Y
        // Règle de base en Unity: tout mouvement dans Update doit etre multiplié par Time.deltaTime pour être indépendant du framerate
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[BugCloud] Collecte déclenchée !");

        if (GameManager.Instance != null && !GameManager.Instance.OnCloudCollected(this))
            return;

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Assure la désinscription même si l'objet est détruit autrement
        if (LevelRegistry.Instance != null)
        {
            var cell = LevelRegistry.Instance.WorldToCell(transform.position);
            LevelRegistry.Instance.UnregisterBugCloud(cell);
        }
    }

    public void AddBugs(int delta)
    {
        totalBugs = Mathf.Max(0, totalBugs + delta);
    }

    public void SetVisible(bool visible)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }

        var particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles)
        {
            if (particle == null)
                continue;

            if (visible)
                particle.Play(true);
            else
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void SetCollectable(bool collectable)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            if (collider != null)
                collider.enabled = collectable;
        }
    }

    //Méthode appelée par le spawner pour configuer les particules
    public void InitializeParticlesQty()
    {
        if (greenBugsParticles == null || redBugsParticles == null)
        {
            Debug.LogWarning("[BugCloud] Systèmes de particules manquants !");
            return;
        }

        var sample = new BugCloudSample
        {
            totalBugs = totalBugs,
            greenRatio = greenRatio
        };

        BugCloudParticleUtility.Apply(greenBugsParticles, redBugsParticles, sample);

        Debug.Log($"[BugCloud] Initialisé: {sample.GreenBugCount} verts, {sample.RedBugCount} rouges (total={totalBugs};ratio={greenRatio})");
    }

}
