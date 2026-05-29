using UnityEngine;

// -----------------------------
// InContextTutorialSceneArrivalDemo : controleur *mock* sandbox-only.
// Reproduit la decision d'affichage que l'integrateur cablera dans les vraies scenes
// (cf. integration-guide.md §4), SANS reference au vrai FlowController.
// Permet de valider en editeur le gating "bloc tutorial + 1er trial + contenu present"
// via des toggles Inspector, et sert de doc vivante de la logique d'integration.
// Ne fait PAS partie de la livraison composant.
// -----------------------------
public class InContextTutorialSceneArrivalDemo : MonoBehaviour
{
    [Header("Cible")]
    [SerializeField] private InContextTutorialUI _overlay;

    [Header("Etat de session simule (remplace FlowController.State)")]
    [Tooltip("Le bloc courant est-il flagge tutorial ?")]
    [SerializeField] private bool _mockBlockIsTutorial = true;
    [Tooltip("Est-on au 1er trial du bloc ? (gate specifique a la scene Proximal/Forest)")]
    [SerializeField] private bool _mockIsFirstTrialOfBlock = true;
    [Tooltip("Appliquer le gate '1er trial' : ON = comme Proximal ; OFF = comme Advisor/Distal.")]
    [SerializeField] private bool _applyFirstTrialGate = true;

    [Header("Contenu simule (fourni par le backend en prod)")]
    [SerializeField] private string _mockTitle = "Bienvenue dans la foret";
    [SerializeField, TextArea(3, 6)] private string _mockBody = "Collecte les bugs verts en evitant les pieges.";

    [Header("Auto")]
    [Tooltip("Simuler l'arrivee sur la scene automatiquement au Start.")]
    [SerializeField] private bool _simulateOnStart = true;

    void Start()
    {
        if (_simulateOnStart) SimulateSceneArrival();
    }

    [ContextMenu("Simuler arrivee sur scene")]
    public void SimulateSceneArrival()
    {
        if (_overlay == null)
        {
            Debug.LogWarning("[InContextTutorialSceneArrivalDemo] _overlay non assigne.");
            return;
        }

        // Reproduction de la logique integrateur (integration-guide.md §4)
        if (!_mockBlockIsTutorial)
        {
            Debug.Log("[InContextTutorialSceneArrivalDemo] Bloc non tutorial -> pas d'overlay.");
            return;
        }

        if (_applyFirstTrialGate && !_mockIsFirstTrialOfBlock)
        {
            Debug.Log("[InContextTutorialSceneArrivalDemo] Pas le 1er trial du bloc -> pas d'overlay.");
            return;
        }

        bool shown = _overlay.Show(_mockTitle, _mockBody);
        Debug.Log($"[InContextTutorialSceneArrivalDemo] Show -> {shown} (contenu vide -> False).");
    }
}
