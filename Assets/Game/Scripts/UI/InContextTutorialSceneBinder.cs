using UnityEngine;

// -----------------------------
// InContextTutorialSceneBinder : glue de scene pour l'overlay in-context tutorial.
// A poser dans AdvisorChoiceScene / DistalChoiceScene / ProximalScene avec _scene regle.
// Lit BlockConfig.in_context_tutorial pour alimenter l'overlay si le bloc est is_tutorial.
// Sur Proximal, l'overlay n'est affiche qu'au 1er trial du bloc (current_trial_index == 0).
// -----------------------------
public class InContextTutorialSceneBinder : MonoBehaviour
{
    public enum SceneKind { AdvisorChoice, DistalChoice, Proximal }

    [Header("Refs")]
    [SerializeField] private InContextTutorialUI _overlay;
    [SerializeField] private SceneKind _scene = SceneKind.AdvisorChoice;

    [Header("Proximal : verrou input gameplay pendant l'overlay")]
    [Tooltip("Verrouille les inputs clavier gameplay tant que l'overlay est ouvert (scene Proximal).")]
    [SerializeField] private bool _lockGameplayInputWhileOpen = true;

    private bool _inputLockedByThis;

    void Start()
    {
        if (_overlay == null)
        {
            Debug.LogWarning("[InContextTutorialSceneBinder] _overlay non assigne. Skip.");
            return;
        }

        var flow = FlowController.Instance;
        if (flow == null) return;

        var block = flow.CurrentBlock;
        if (block == null || !block.is_tutorial) return;

        // Proximal recharge a chaque trial : n'afficher qu'au 1er trial du bloc
        if (_scene == SceneKind.Proximal && flow.State != null && flow.State.current_trial_index != 0)
            return;

        var (title, body) = LoadContent(block);
        bool shown = _overlay.Show(title, body); // no-op si contenu vide
        if (!shown) return;

        if (_lockGameplayInputWhileOpen && _scene == SceneKind.Proximal && GameManager.Instance != null)
        {
            GameManager.Instance.HoldInputLock();
            _inputLockedByThis = true;
            _overlay.Closed += OnOverlayClosed;
        }
    }

    void OnDestroy()
    {
        if (_overlay != null)
            _overlay.Closed -= OnOverlayClosed;
        if (_inputLockedByThis && GameManager.Instance != null)
            GameManager.Instance.ReleaseInputLock();
    }

    private void OnOverlayClosed()
    {
        _overlay.Closed -= OnOverlayClosed;
        if (_inputLockedByThis && GameManager.Instance != null)
            GameManager.Instance.ReleaseInputLock();
        _inputLockedByThis = false;
    }

    private (string title, string body) LoadContent(BlockConfig block)
    {
        var c = block.in_context_tutorial;
        if (c == null) return (string.Empty, string.Empty);
        return _scene switch
        {
            SceneKind.AdvisorChoice => (c.advisor_title, c.advisor_text),
            SceneKind.DistalChoice  => (c.distal_title,  c.distal_text),
            _                       => (c.proximal_title, c.proximal_text)
        };
    }
}
