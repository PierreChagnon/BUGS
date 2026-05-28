using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------
// HowToPlayPage : modele d'une page du tutoriel.
// Volontairement minimal : une image, un titre court, un corps de texte.
// -----------------------------
[Serializable]
public class HowToPlayPage
{
    public Sprite image;
    public string header;
    [TextArea(3, 6)] public string body;
}

// -----------------------------
// HowToPlayUI : composant UI de tutoriel pagine autonome.
// LOT 3 : API publique SetPages/Open/Close + events Closed/Completed.
// Mode dev preserve : auto-show des _dummyPages au Start si aucun SetPages externe.
// Le composant reste agnostique de son integration (pas de ref a FlowController, etc.).
// -----------------------------
public class HowToPlayUI : MonoBehaviour
{
    [Header("Dummy data (mode dev — utilise si rien n'est passe par SetPages)")]
    [SerializeField] private List<HowToPlayPage> _dummyPages = new List<HowToPlayPage>();

    [Header("Refs vue")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _image;
    [SerializeField] private TMP_Text _headerText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private TMP_Text _nextButtonLabel;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _dotsContainer;
    [SerializeField] private GameObject _dotPrefab;

    [Header("Labels bouton Next (bascule derniere page)")]
    [SerializeField] private string _labelNext = "Next";
    [SerializeField] private string _labelClose = "Close";

    [Header("Couleurs dots")]
    [SerializeField] private Color _dotActiveColor = Color.white;
    [SerializeField] private Color _dotInactiveColor = new Color(1f, 1f, 1f, 0.35f);

    // Notifications sortantes
    public event Action Closed;
    public event Action Completed;

    private List<HowToPlayPage> _pages;
    private readonly List<Image> _dotImages = new List<Image>();
    private int _index;
    private bool _lastReached;
    private bool _externalDataSet;
    private bool _isOpen;

    void Awake()
    {
        if (_prevButton != null) _prevButton.onClick.AddListener(OnPrevClicked);
        if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);

        if (_root != null) _root.SetActive(false);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);
    }

    void Start()
    {
        // Auto-show mode dev : uniquement si aucune data n'a ete passee par l'API externe
        if (!_externalDataSet && _dummyPages != null && _dummyPages.Count > 0)
        {
            _pages = _dummyPages;
            OpenInternal();
        }
    }

    // -----------------------------
    // API publique
    // -----------------------------

    /// <summary>
    /// Remplace la liste interne par <paramref name="pages"/>. Reset index a 0 et lastReached a false.
    /// Si null ou vide : log warning + ferme silencieusement le panel s'il etait ouvert.
    /// Si le panel est ouvert, affiche immediatement la page 0 de la nouvelle liste.
    /// </summary>
    public void SetPages(IList<HowToPlayPage> pages)
    {
        _externalDataSet = true;

        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[HowToPlayUI] SetPages appele avec null ou liste vide. Panel ferme.");
            _pages = new List<HowToPlayPage>();
            _index = 0;
            _lastReached = false;
            RebuildDots();
            if (_isOpen)
                CloseSilent();
            return;
        }

        _pages = new List<HowToPlayPage>(pages);
        _index = 0;
        _lastReached = false;

        if (_isOpen)
        {
            RebuildDots();
            Show(0);
            if (_closeButton != null) _closeButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Affiche le panel sur la page 0. No-op silencieux si aucune data n'est disponible.
    /// </summary>
    public void Open()
    {
        if (_pages == null || _pages.Count == 0)
        {
            Debug.LogWarning("[HowToPlayUI] Open() appele sans pages disponibles. No-op.");
            return;
        }
        OpenInternal();
    }

    /// <summary>
    /// Ferme le panel. Emet Closed (et Completed si la derniere page a ete atteinte).
    /// No-op silencieux si deja ferme.
    /// </summary>
    public void Close()
    {
        if (!_isOpen) return;

        bool wasCompleted = _lastReached;
        if (_root != null) _root.SetActive(false);
        _isOpen = false;

        Closed?.Invoke();
        if (wasCompleted)
            Completed?.Invoke();
    }

    // -----------------------------
    // Interne
    // -----------------------------

    private void OpenInternal()
    {
        _isOpen = true;
        _index = 0;
        _lastReached = false;
        if (_root != null) _root.SetActive(true);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);
        RebuildDots();
        Show(0);
    }

    private void CloseSilent()
    {
        if (_root != null) _root.SetActive(false);
        _isOpen = false;
    }

    private void Show(int i)
    {
        if (_pages == null || _pages.Count == 0)
            return;

        i = Mathf.Clamp(i, 0, _pages.Count - 1);
        _index = i;
        var page = _pages[i];

        if (_image != null) _image.sprite = page.image;
        if (_headerText != null) _headerText.text = page.header ?? string.Empty;
        if (_bodyText != null) _bodyText.text = page.body ?? string.Empty;

        if (_prevButton != null) _prevButton.interactable = i > 0;

        bool isLast = (i == _pages.Count - 1);
        if (isLast) _lastReached = true;

        if (_nextButtonLabel != null)
            _nextButtonLabel.text = isLast ? _labelClose : _labelNext;

        if (_closeButton != null)
            _closeButton.gameObject.SetActive(_lastReached);

        UpdateActiveDot();
    }

    private void OnNextClicked()
    {
        if (_pages == null || _pages.Count == 0) return;
        if (_index >= _pages.Count - 1)
            Close();
        else
            Show(_index + 1);
    }

    private void OnPrevClicked()
    {
        if (_index > 0)
            Show(_index - 1);
    }

    private void OnCloseClicked()
    {
        Close();
    }

    private void RebuildDots()
    {
        if (_dotsContainer == null) return;

        for (int i = _dotsContainer.childCount - 1; i >= 0; i--)
            Destroy(_dotsContainer.GetChild(i).gameObject);

        _dotImages.Clear();

        if (_dotPrefab == null || _pages == null) return;

        for (int i = 0; i < _pages.Count; i++)
        {
            var dotGO = Instantiate(_dotPrefab, _dotsContainer);
            var img = dotGO.GetComponent<Image>();
            if (img != null) _dotImages.Add(img);
        }
    }

    private void UpdateActiveDot()
    {
        for (int i = 0; i < _dotImages.Count; i++)
        {
            if (_dotImages[i] == null) continue;
            _dotImages[i].color = (i == _index) ? _dotActiveColor : _dotInactiveColor;
        }
    }
}
