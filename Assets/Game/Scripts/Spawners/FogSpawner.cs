using UnityEngine;

// -----------------------------
// Décide si le brouillard de guerre est actif ce round,
// puis instancie et positionne la surface de fog sur la grille.
//
// Responsabilités :
//   - Lire fogProbability depuis SessionManager
//   - Tirer au sort la présence du fog (via le RNG reproductible)
//   - Instancier fogSurfacePrefab, le positionner/dimensionner sur la grille
//
// Ce qui n'est PAS ici :
//   - Logique de révélation → FogController (sur le prefab)
//   - Décision de quoi révéler → GameManager, PathSpawner
//
// Exécution : Start(-245) — après TilesSpawner.Awake(-240) qui pose originWorld,
//             avant BugCloudSpawner.Start(-200).
// -----------------------------

[DefaultExecutionOrder(-245)]
public class FogSpawner : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Prefab FogSurface (Quad + Renderer + FogController).")]
    public GameObject fogSurfacePrefab;

    [Header("Placement")]
    [Tooltip("Hauteur Y du plan de brouillard au-dessus de la grille.")]
    public float fogY = 0.3f;

    [Header("Bord adouci")]
    [Tooltip("Largeur du halo extérieur, en proportion de la grille (chaque côté). " +
             "0.05 = 5 % de halo, 0 = quad serré sur la grille (bords nets).")]
    [Range(0f, 0.3f)]
    public float borderMarginRatio = 0.05f;

    void Start()
    {
        var reg = LevelRegistry.Instance;
        var session = SessionManager.Instance;

        if (reg == null)
        {
            Debug.LogError("[FogSpawner] LevelRegistry manquant.");
            return;
        }
        if (session == null)
        {
            Debug.LogError("[FogSpawner] SessionManager manquant.");
            return;
        }
        if (fogSurfacePrefab == null)
        {
            Debug.LogError("[FogSpawner] fogSurfacePrefab non assigné.");
            return;
        }

        // Tirage : brouillard actif ce round ?
        var rng = reg.CreateRng(nameof(FogSpawner));
        if (rng.NextDouble() >= session.fogProbability)
        {
            Debug.Log("[FogSpawner] Pas de brouillard ce round (fogProbability=" +
                      session.fogProbability + ").");
            return;
        }

        // Instancier le prefab (FogController.Awake = singleton seulement, pas d'allocation)
        var fog = Instantiate(fogSurfacePrefab, transform);

        // Allouer la texture étendue avec vignette de bord
        var fc = fog.GetComponent<FogController>();
        if (fc == null)
        {
            Debug.LogError("[FogSpawner] FogController manquant sur le prefab.");
            return;
        }
        fc.Initialize(borderMarginRatio);

        // Positionner et dimensionner — le quad couvre la grille + la marge périmétrique
        float gridW = reg.gridSize.x * reg.cellSize;
        float gridH = reg.gridSize.y * reg.cellSize;
        float quadW = gridW + 2f * fc.MarginWorldX;
        float quadH = gridH + 2f * fc.MarginWorldY;

        // Centre de la grille en monde (les cellules sont centrées sur leur coordonnée)
        Vector3 center = new Vector3(
            reg.originWorld.x + (reg.gridSize.x - 1) * reg.cellSize / 2f,
            fogY,
            reg.originWorld.z + (reg.gridSize.y - 1) * reg.cellSize / 2f
        );

        fog.transform.position = center;
        fog.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        fog.transform.localScale = new Vector3(quadW, quadH, 1f);
        fog.SetActive(true);

    }
}
