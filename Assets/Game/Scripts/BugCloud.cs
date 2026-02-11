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
    public float rotationSpeed = 0.03f; // Vitesse de rotation du nuage

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
            // On informe le GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.OnCloudCollected(this);

            // Destroy the bug cloud when the player collides with it
            Destroy(gameObject);
        }
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
        // (option: mettre à jour un label au-dessus du nuage si tu en as un)
    }

    //Méthode appelée par le spawner pour configuer les particules
    public void InitializeParticlesQty()
    {
        if (greenBugsParticles == null || redBugsParticles == null)
        {
            Debug.LogWarning("[BugCloud] Systèmes de particules manquants !");
            return;
        }

        int greenCount = Mathf.RoundToInt(totalBugs * greenRatio);
        int redCount = totalBugs - greenCount;

        //Configurer le nombre de particules vertes
        var greenEmission = greenBugsParticles.emission; //TODO : Ajuster les paramètres à modifier au besoin ! ici juste pour test
        var greenMain = greenBugsParticles.main; //TODO : Ajuster les paramètres à modifier au besoin ! ici juste pour test
        greenMain.maxParticles = greenCount;
        greenEmission.rateOverTime = greenCount * 2;

        //Configurer le nombre de particules rouges
        var redEmission = redBugsParticles.emission; //TODO : Ajuster les paramètres à modifier au besoin ! ici juste pour test
        var redMain = redBugsParticles.main; //TODO : Ajuster les paramètres à modifier au besoin ! ici juste pour test
        redMain.maxParticles = redCount;
        redEmission.rateOverTime = redCount * 2;

        Debug.Log($"[BugCloud] Initialisé: {greenCount} verts, {redCount} rouges (total={totalBugs};ratio={greenRatio})");
    }

}
