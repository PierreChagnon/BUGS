using UnityEngine;
using TMPro;

// -----------------------------
// Affichage du panneau de fin de round.
// S'abonne à GameManager.OnRoundEnded pour recevoir les stats.
// -----------------------------
public class RoundUI : MonoBehaviour
{
    [Header("Game Over")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _gameOverStats;

    void Start()
    {
        // S'abonner à l'événement de fin de round
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded += HandleRoundEnded;

        // Masquer le panneau au démarrage
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded -= HandleRoundEnded;
    }

    // ══════════════════════════════════════════════════════════════
    //  ÉVÉNEMENT
    // ══════════════════════════════════════════════════════════════

    private void HandleRoundEnded(GameManager.RoundEndInfo info)
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);

        if (_gameOverStats != null)
        {
            _gameOverStats.text =
                $"Bugs collectés : {info.bugsCollected}\n" +
                $"Pièges touchés : {info.trapsHit}\n" +
                $"Pas en trop : {info.overtimeSteps}\n" +
                $"Pas : {info.steps}\n" +
                $"Chemin conseillé suivi : {(info.followedAdvisorPath ? "Oui" : "Non")}\n" +
                $"Bugs nuage G : {info.leftCloudGreenBugs}  |  D : {info.rightCloudGreenBugs}";
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  BOUTON UI
    // ══════════════════════════════════════════════════════════════

    /// <summary>Appelé par le bouton Restart dans le panneau Game Over.</summary>
    public void OnRestartClicked()
    {
        GameManager.Instance?.RestartRound();
    }
}
