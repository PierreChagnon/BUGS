using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------
// InContextTutorialUI : overlay modal d'instructions contextuelles, autonome.
// Affiche un titre + un texte par-dessus la scene de jeu ; se ferme sur clic OK.
// Version simplifiee de HowToPlayUI (panneau unique, non pagine, sans image).
//
// API publique : Show(title, body) / Show() / SetContent(title, body) / Close()
//                + proprietes IsOpen / HasContent + events Shown / Closed.
// Mode dev : auto-show des dummy (_dummyTitle / _dummyBody) au Start si aucun
//            pilotage externe n'a eu lieu.
//
// Composant agnostique : aucune reference a FlowController, GameManager, etc.
// La decision "afficher ou non" (bloc tutorial ? scene ? 1er trial ?) et le verrou
// des inputs clavier gameplay sont a la charge de l'integrateur
// (cf. Docs/specs/in-context-tutorial/integration-guide.md).
// -----------------------------
public class InContextTutorialUI : MonoBehaviour
{
    [Header("Dummy data (mode dev — utilise si rien n'est passe par l'API)")]
    [SerializeField] private string _dummyTitle;
    [SerializeField, TextArea(3, 6)] private string _dummyBody;

    [Header("Refs vue")]
    [SerializeField] private GameObject _root;       // racine modale (inclut le fond bloquant)
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _okButton;

    // Notifications sortantes
    public event Action Shown;
    public event Action Closed;

    private string _title;
    private string _body;
    private bool _isOpen;
    private bool _externalDataSet;

    public bool IsOpen => _isOpen;
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(_title) || !string.IsNullOrWhiteSpace(_body);

    void Awake()
    {
        if (_okButton != null) _okButton.onClick.AddListener(Close);
        else Debug.LogWarning("[InContextTutorialUI] _okButton non assigne — fermeture par clic impossible.");

        if (_root != null) _root.SetActive(false);
        else Debug.LogError("[InContextTutorialUI] _root non assigne.");
    }

    void Start()
    {
        // Auto-show mode dev : uniquement si aucune data externe et dummy non vide
        if (!_externalDataSet &&
            (!string.IsNullOrWhiteSpace(_dummyTitle) || !string.IsNullOrWhiteSpace(_dummyBody)))
        {
            _title = _dummyTitle;
            _body = _dummyBody;
            ShowInternal();
        }
    }

    // -----------------------------
    // API publique
    // -----------------------------

    /// <summary>
    /// Definit le contenu puis affiche. Retourne true si reellement affiche,
    /// false si le contenu est vide (no-op, aucun overlay).
    /// </summary>
    public bool Show(string title, string body)
    {
        SetContent(title, body);
        return Show();
    }

    /// <summary>
    /// Affiche le contenu deja defini (dummy ou SetContent prealable).
    /// No-op + warning si aucun contenu. Retourne true si affiche.
    /// </summary>
    public bool Show()
    {
        _externalDataSet = true;
        return ShowInternal();
    }

    /// <summary>
    /// Definit le contenu sans afficher. Marque _externalDataSet (desactive le fallback dummy).
    /// Si l'overlay est ouvert : rafraichit les textes, ou ferme silencieusement si le contenu
    /// devient vide.
    /// </summary>
    public void SetContent(string title, string body)
    {
        _externalDataSet = true;
        _title = title;
        _body = body;

        if (_isOpen)
        {
            if (HasContent) ApplyTexts();
            else CloseSilent();
        }
    }

    /// <summary>
    /// Ferme l'overlay et emet Closed. No-op silencieux si deja ferme.
    /// </summary>
    public void Close()
    {
        if (!_isOpen) return;
        if (_root != null) _root.SetActive(false);
        _isOpen = false;
        Closed?.Invoke();
    }

    // -----------------------------
    // Interne
    // -----------------------------

    private bool ShowInternal()
    {
        if (!HasContent)
        {
            Debug.LogWarning("[InContextTutorialUI] Show() sans contenu (titre et corps vides). No-op.");
            return false;
        }

        ApplyTexts();
        if (_root != null) _root.SetActive(true);
        _isOpen = true;
        Shown?.Invoke();
        return true;
    }

    private void ApplyTexts()
    {
        if (_titleText != null) _titleText.text = _title ?? string.Empty;
        if (_bodyText != null) _bodyText.text = _body ?? string.Empty;
    }

    private void CloseSilent()
    {
        if (_root != null) _root.SetActive(false);
        _isOpen = false;
    }

}
