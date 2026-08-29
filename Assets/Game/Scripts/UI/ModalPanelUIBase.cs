using System;
using UnityEngine;

// -----------------------------
// Socle commun des panneaux modaux autonomes (HowToPlayUI, InContextTutorialUI) :
// racine on/off, état ouvert, pilotage externe vs contenu dummy de dev au
// Start, event Closed. Les classes dérivées gardent leur contenu et leur API
// spécifiques (pages paginées, titre+corps...).
// -----------------------------

public abstract class ModalPanelUIBase : MonoBehaviour
{
    [Header("Racine modale")]
    [SerializeField] private GameObject _root;

    // Notification sortante commune : le panneau vient de se fermer.
    public event Action Closed;

    bool _isOpen;
    bool _externalDataSet;

    protected bool IsOpen => _isOpen;
    protected bool HasRoot => _root != null;

    protected virtual void Awake()
    {
        SetRootActive(false);
    }

    void Start()
    {
        // Auto-show mode dev : uniquement si aucune data n'a été passée par l'API externe.
        if (!_externalDataSet && HasDummyContent)
            OpenFromDummy();
    }

    /// <summary>
    /// Déclare que le composant est piloté de l'extérieur (ex : IntroSceneController).
    /// Désactive l'auto-show du contenu dummy au Start, évitant le flash de
    /// placeholders pendant le chargement asynchrone du contenu de session.
    /// À appeler depuis un Awake() pour garantir l'ordre avant le Start() de ce composant.
    /// </summary>
    public void MarkExternallyControlled()
    {
        _externalDataSet = true;
    }

    /// <summary>
    /// Ferme le panneau et émet Closed. No-op silencieux si déjà fermé.
    /// </summary>
    public virtual void Close()
    {
        if (!_isOpen)
            return;

        SetRootActive(false);
        _isOpen = false;
        Closed?.Invoke();
    }

    // Le contenu dummy à afficher au Start en mode dev existe-t-il ?
    protected abstract bool HasDummyContent { get; }

    // Charge le contenu dummy puis ouvre le panneau (mode dev uniquement).
    protected abstract void OpenFromDummy();

    protected void SetRootActive(bool active)
    {
        if (_root != null)
            _root.SetActive(active);
    }

    protected void MarkOpened()
    {
        SetRootActive(true);
        _isOpen = true;
    }

    protected void CloseSilent()
    {
        SetRootActive(false);
        _isOpen = false;
    }
}
