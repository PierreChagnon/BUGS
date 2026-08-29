using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------
// InContextTutorialUI : overlay modal d'instructions contextuelles, autonome.
// Affiche un titre + un texte par-dessus la scene de jeu ; se ferme sur clic OK.
// Version simplifiee de HowToPlayUI (panneau unique, non pagine, sans image).
// Socle modal (racine, etat ouvert, dummy de dev, event Closed) : ModalPanelUIBase.
//
// API publique : Show(title, body) / Show() / SetContent(title, body) / Close()
//                + propriete HasContent + events Shown / Closed (base).
//
// Composant agnostique : aucune reference a FlowController, GameManager, etc.
// La decision "afficher ou non" (bloc tutorial ? scene ? 1er trial ?) et le verrou
// des inputs clavier gameplay sont a la charge de l'integrateur
// (cf. Docs/specs/in-context-tutorial/integration-guide.md).
// -----------------------------
public class InContextTutorialUI : ModalPanelUIBase
{
    [Header("Dummy data (mode dev — utilise si rien n'est passe par l'API)")]
    [SerializeField] private string _dummyTitle;
    [SerializeField, TextArea(3, 6)] private string _dummyBody;

    [Header("Refs vue")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _okButton;

    // Notification sortante : l'overlay vient de s'afficher.
    public event Action Shown;

    private string _title;
    private string _body;

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(_title) || !string.IsNullOrWhiteSpace(_body);

    protected override void Awake()
    {
        base.Awake();

        if (_okButton != null) _okButton.onClick.AddListener(Close);
        else Debug.LogWarning("[InContextTutorialUI] _okButton non assigne — fermeture par clic impossible.");

        if (!HasRoot) Debug.LogError("[InContextTutorialUI] _root non assigne.");
    }

    void OnDestroy()
    {
        if (_okButton != null) _okButton.onClick.RemoveListener(Close);
    }

    protected override bool HasDummyContent =>
        !string.IsNullOrWhiteSpace(_dummyTitle) || !string.IsNullOrWhiteSpace(_dummyBody);

    protected override void OpenFromDummy()
    {
        _title = _dummyTitle;
        _body = _dummyBody;
        ShowInternal();
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
        MarkExternallyControlled();
        return ShowInternal();
    }

    /// <summary>
    /// Definit le contenu sans afficher. Desactive le fallback dummy.
    /// Si l'overlay est ouvert : rafraichit les textes, ou ferme silencieusement
    /// si le contenu devient vide.
    /// </summary>
    public void SetContent(string title, string body)
    {
        MarkExternallyControlled();
        _title = title;
        _body = body;

        if (IsOpen)
        {
            if (HasContent) ApplyTexts();
            else CloseSilent();
        }
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
        MarkOpened();
        Shown?.Invoke();
        return true;
    }

    private void ApplyTexts()
    {
        if (_titleText != null) _titleText.text = _title ?? string.Empty;
        if (_bodyText != null) _bodyText.text = _body ?? string.Empty;
    }
}
