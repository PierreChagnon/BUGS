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
    [Tooltip("Nombre courant d'insectes verts remis à la collecte.")]
    public int greenBugs;
    [Tooltip("Nombre d'insectes verts perdus à cause des pénalités.")]
    public int bugsEscaped;

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
        if (delta >= 0)
        {
            totalBugs += delta;
            return;
        }

        int penalty = -delta;
        int greenBugsLost = Mathf.Min(penalty, totalBugs, greenBugs);
        totalBugs -= greenBugsLost;
        greenBugs -= greenBugsLost;
        bugsEscaped += greenBugsLost;
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
        var sample = new BugCloudSample
        {
            totalBugs = totalBugs,
            greenRatio = greenRatio
        };

        greenBugs = Mathf.Clamp(sample.GreenBugCount, 0, totalBugs);
        bugsEscaped = 0;

        if (greenBugsParticles == null || redBugsParticles == null)
        {
            Debug.LogWarning("[BugCloud] Systèmes de particules manquants !");
            return;
        }

        BugCloudParticleUtility.Apply(greenBugsParticles, redBugsParticles, sample);

        Debug.Log($"[BugCloud] Initialisé: {greenBugs} verts, {sample.RedBugCount} rouges (total={totalBugs};ratio={greenRatio})");
    }

}
