using TMPro;
using UnityEngine;

// -----------------------------
// Panneau de fin de trial.
// S'abonne a GameManager.OnRoundEnded et propose de continuer
// vers le trial / ecran suivant via FlowController.
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



    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded += HandleRoundEnded;

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);

    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded -= HandleRoundEnded;
    }

    void HandleRoundEnded(GameManager.RoundEndInfo info)
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

    public void OnContinueClicked()
    {
        GameManager.Instance?.ContinueAfterRound();
    }

}
