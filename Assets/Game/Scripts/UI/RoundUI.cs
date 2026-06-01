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
    [SerializeField] private TMP_Text _gameOverStats;
    [SerializeField] private TMP_Text _greenBugsCollected;
    [SerializeField] private TMP_Text _trapsHit;
    [SerializeField] private TMP_Text _overtimeSteps;
    [SerializeField] private TMP_Text _steps;

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

        if (_gameOverStats != null)
        {
            _gameOverStats.text =
                $"Pieges touches : {info.trapsHit}\n" +
                $"Pas en trop : {info.overtimeSteps}\n" +
                $"Pas : {info.steps}\n" +
                $"Chemin conseille suivi : {(info.followedAdvisorPath ? "Oui" : "Non")}\n" +
                $"Chemin visible : {(info.optimalPathVisible ? "Oui" : "Non")}\n" +
                $"Bugs nuage G : {info.leftCloudGreenBugs}  |  D : {info.rightCloudGreenBugs}";
        }

        if (_greenBugsCollected != null)
            _greenBugsCollected.text = $"Bugs collectes : {info.bugsCollected}";
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
