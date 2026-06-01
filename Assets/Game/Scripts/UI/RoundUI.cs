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
