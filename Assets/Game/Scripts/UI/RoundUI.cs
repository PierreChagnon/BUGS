using System;
using TMPro;
using UnityEngine;

// -----------------------------
// Panneau de fin de trial.
// S'affiche en premier a la fin du trial. Son bouton laisse ensuite
// TrialQuestionsUI afficher les questions de fin de trial.
// -----------------------------

public class RoundUI : MonoBehaviour
{
    [Header("Game Over")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _greenBugsCollected;
    [SerializeField] private TMP_Text _greenBugsEscaped;
    [SerializeField] private GameObject _successPanel;
    [SerializeField] private GameObject _failurePanel;

    [Header("Feedback numerique")]
    [Tooltip("GameObject regroupant les valeurs numeriques du rapport de mission. Affiche selon show_numerical_feedback du bloc courant.")]
    [SerializeField] private GameObject _numericalFeedbackRoot;

    [Header("Badge")]
    [Tooltip("GameObject regroupant tout ce qui concerne le badge. Recentre quand les valeurs numeriques sont masquees.")]
    [SerializeField] private RectTransform _badgeSectionRoot;
    [Tooltip("Position X du badge quand les valeurs numeriques sont affichees (a droite).")]
    [SerializeField] private float _badgeXWithNumerical = -340f;
    [Tooltip("Position X du badge quand les valeurs numeriques sont masquees (centre).")]
    [SerializeField] private float _badgeXCentered = 0f;

    public event Action OnContinueFromReport;

    void Start()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded += Show;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded -= Show;
    }

    public void Show(GameManager.RoundEndInfo info)
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);

        if (_greenBugsCollected != null)
            _greenBugsCollected.text = $"{info.bugsCollected}";
        if (_greenBugsEscaped != null)
            _greenBugsEscaped.text = $"{info.bugsEscaped}";

        if (_successPanel != null)
            _successPanel.SetActive(info.choiceCorrect);

        if (_failurePanel != null)
            _failurePanel.SetActive(!info.choiceCorrect);

        if (_numericalFeedbackRoot != null)
        {
            // Les chercheurs pilotent l'affichage des valeurs numeriques par bloc.
            // En l'absence de bloc (debug/sandbox), on conserve le comportement historique (affiche).
            var block = FlowController.Instance != null ? FlowController.Instance.CurrentBlock : null;
            bool showNumericalFeedback = block == null || block.show_numerical_feedback;
            _numericalFeedbackRoot.SetActive(showNumericalFeedback);

            // Sans les valeurs numeriques, le badge est recentre car il y a de la place.
            if (_badgeSectionRoot != null)
            {
                Vector2 pos = _badgeSectionRoot.anchoredPosition;
                pos.x = showNumericalFeedback ? _badgeXWithNumerical : _badgeXCentered;
                _badgeSectionRoot.anchoredPosition = pos;
            }
        }
    }

    public void Hide()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    public void OnContinueClicked()
    {
        if (OnContinueFromReport != null)
        {
            OnContinueFromReport.Invoke();
            return;
        }

        GameManager.Instance?.ContinueAfterRound();
    }

}
