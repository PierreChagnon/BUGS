using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// Binder de la BreakScene.
//
// Ce composant ne cree aucun element d'interface. Il doit etre ajoute a un objet
// de la scene de pause creee par l'equipe, puis recevoir dans l'Inspector :
//   - le TMP_Text qui affiche le countdown ;
//   - le Button utilise pour reprendre la session.
//
// La duree vient exclusivement de break_duration_seconds dans la config API.
// Le bouton reste non interactif jusqu'a ce que cette duree soit ecoulee.
// -----------------------------------------------------------------------------

public class BreakSceneController : MonoBehaviour
{
    [Header("References UI")]
    [Tooltip("Texte TextMeshPro qui affiche le temps restant au format MM:SS.")]
    [SerializeField] private TMP_Text _countdownText;

    [Tooltip("Bouton de reprise, desactive jusqu'a la fin du countdown.")]
    [SerializeField] private Button _resumeButton;

    int _lastDisplayedSeconds = -1;

    void Awake()
    {
        if (_resumeButton == null)
            return;

        _resumeButton.interactable = false;
        _resumeButton.onClick.AddListener(OnResumeClicked);
    }

    void Start()
    {
        if (_countdownText == null || _resumeButton == null)
        {
            Debug.LogError(
                "[BreakSceneController] Assignez le texte du countdown et le bouton de reprise " +
                "dans l'Inspector.");
            enabled = false;
            return;
        }

        var flow = FlowController.Instance;
        if (flow == null || flow.State == null || flow.State.current_phase != GamePhase.Break)
        {
            _countdownText.text = string.Empty;
            _resumeButton.interactable = false;
            Debug.LogError("[BreakSceneController] FlowController absent ou hors phase Break.");
            enabled = false;
            return;
        }

        flow.StartBreakCountdown();
        RefreshCountdown(force: true);
    }

    void Update()
    {
        RefreshCountdown(force: false);
    }

    void OnDestroy()
    {
        if (_resumeButton != null)
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
    }

    void RefreshCountdown(bool force)
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.State == null || flow.State.current_phase != GamePhase.Break)
            return;

        int remainingSeconds = flow.BreakRemainingSeconds;
        if (!force && remainingSeconds == _lastDisplayedSeconds)
            return;

        _lastDisplayedSeconds = remainingSeconds;
        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;
        _countdownText.text = $"{minutes:00}:{seconds:00}";
        _resumeButton.interactable = remainingSeconds <= 0;
    }

    void OnResumeClicked()
    {
        FlowController.Instance?.OnBreakComplete();
    }
}
