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
