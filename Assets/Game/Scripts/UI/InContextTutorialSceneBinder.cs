using UnityEngine;

// -----------------------------
// InContextTutorialSceneBinder : glue de scene (etape 1 d'integration) pour l'overlay in-context.
// A poser dans AdvisorChoiceScene / DistalChoiceScene / ProximalScene avec _scene regle.
// Reference l'InContextTutorialUI de la scene et decide de l'affichage a l'arrivee.
//
// Modele : meme pattern que IntroSceneController (contenu de test serialise + seam TODO pour
// le branchement backend) et SettingsPanelUI (verrou input via GameManager.SetInputLocked).
//
// Frontiere : ce composant est la glue NON agnostique (il connait FlowController/GameManager).
// Le composant InContextTutorialUI, lui, reste agnostique.
//
// Etape 2 (collegue) : remplacer LoadContent() par la lecture de BlockConfig.in_context_tutorial,
// et affiner la coordination du verrou input Proximal avec GameManager.GetReadySequence
// (cf. Docs/specs/in-context-tutorial/integration-guide.md).
// -----------------------------
public class InContextTutorialSceneBinder : MonoBehaviour
{
    public enum SceneKind { AdvisorChoice, DistalChoice, Proximal }

    [Header("Refs")]
    [SerializeField] private InContextTutorialUI _overlay;
    [SerializeField] private SceneKind _scene = SceneKind.AdvisorChoice;

    [Header("Contenu de test (etape 1 — sera remplace par BlockConfig.in_context_tutorial)")]
    [SerializeField] private string _testTitle;
    [SerializeField, TextArea(3, 6)] private string _testBody;

    [Header("Proximal : verrou input gameplay pendant l'overlay")]
    [Tooltip("Verrouille les inputs clavier gameplay tant que l'overlay est ouvert (scene Proximal).")]
    [SerializeField] private bool _lockGameplayInputWhileOpen = true;

    [Header("Test editeur (scene lancee seule, sans FlowController)")]
    [Tooltip("Si aucun FlowController (scene ouverte directement), afficher quand meme le contenu de test.")]
    [SerializeField] private bool _showTestContentWhenNoFlow = true;

    private bool _inputLockedByThis;

    void Start()
    {
        if (_overlay == null)
        {
            Debug.LogWarning("[InContextTutorialSceneBinder] _overlay non assigne. Skip.");
            return;
        }

        var flow = FlowController.Instance;
        if (flow != null)
        {
            // ----- Chemin reel (jeu complet) -----
            var block = flow.CurrentBlock;
            if (block == null || !block.is_tutorial)
                return; // pas un bloc tutorial -> aucun overlay

            // Proximal recharge a chaque trial : n'afficher qu'au 1er trial du bloc
            if (_scene == SceneKind.Proximal && flow.State != null && flow.State.current_trial_index != 0)
                return;
        }
        else
        {
            // ----- Chemin editeur (scene lancee seule, sans boot/back-end) -----
            if (!_showTestContentWhenNoFlow)
                return;
        }

        var (title, body) = LoadContent();
        bool shown = _overlay.Show(title, body); // no-op si contenu vide
        if (!shown)
            return;

        // Verrou des inputs clavier gameplay (le fond modal ne bloque que la souris)
        if (_lockGameplayInputWhileOpen && _scene == SceneKind.Proximal && GameManager.Instance != null)
        {
            GameManager.Instance.SetInputLocked(true);
            _inputLockedByThis = true;
            _overlay.Closed += OnOverlayClosed;
        }
    }

    void OnDestroy()
    {
        if (_overlay != null)
            _overlay.Closed -= OnOverlayClosed;
    }

    private void OnOverlayClosed()
    {
        _overlay.Closed -= OnOverlayClosed;
        if (_inputLockedByThis && GameManager.Instance != null)
            GameManager.Instance.SetInputLocked(false);
        _inputLockedByThis = false;
    }

    private (string title, string body) LoadContent()
    {
        // TODO etape 2 (branchement backend) : remplacer par le contenu de
        // BlockConfig.in_context_tutorial pour la scene _scene, par ex :
        //
        //   var c = FlowController.Instance?.CurrentBlock?.in_context_tutorial;
        //   if (c == null) return (string.Empty, string.Empty);
        //   return _scene switch
        //   {
        //       SceneKind.AdvisorChoice => (c.advisor_title,  c.advisor_text),
        //       SceneKind.DistalChoice  => (c.distal_title,   c.distal_text),
        //       _                       => (c.proximal_title, c.proximal_text),
        //   };
        return (_testTitle, _testBody);
    }
}
