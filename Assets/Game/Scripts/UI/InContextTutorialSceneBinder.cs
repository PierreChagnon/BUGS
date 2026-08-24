using UnityEngine;

// -----------------------------
// InContextTutorialSceneBinder : glue de scene pour l'overlay in-context tutorial.
// A poser dans AdvisorChoiceScene / DistalChoiceScene / ProximalScene avec _scene regle.
// Lit BlockConfig.in_context_tutorial pour alimenter l'overlay si le bloc est is_tutorial.
//
// Deux visibilites distinctes, portees par le meme prefab :
//  - le bandeau persistant (frere de Root dans le prefab) reste affiche pendant TOUT le
//    bloc tutorial => pilote par l'etat actif du GameObject de l'overlay ;
//  - la modale d'instructions ne s'ouvre qu'a l'arrivee dans la scene, et sur Proximal
//    uniquement au 1er trial du bloc (current_trial_index == 0), la scene rechargeant
//    a chaque trial.
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
        if (flow == null) return; // scene lancee seule (dev) : on laisse l'overlay a son comportement dummy

        // Le binder est l'unique source de verite pour la visibilite de l'overlay.
        // On (re)force l'etat actif du GameObject a chaque runtime, pour ne PAS dependre
        // d'une activation/desactivation oubliee en editeur (ex. panneau desactive pour
        // travailler dans la scene). Le panel reste actif pendant tout le bloc tutorial :
        // c'est lui qui porte le bandeau persistant, qui doit survivre a la fermeture de
        // la modale et aux trials suivants.
        var block = flow.CurrentBlock;
        bool isTutorialBlock = block != null && block.is_tutorial;
        _overlay.gameObject.SetActive(isTutorialBlock);
        if (!isTutorialBlock) return;

        // La modale, elle, ne s'ouvre qu'au 1er trial du bloc sur Proximal.
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
