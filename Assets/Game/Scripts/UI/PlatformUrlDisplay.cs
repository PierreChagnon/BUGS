using UnityEngine;

// -----------------------------
// Recupere le lien "platform_url" de la session pour l'ecran de transition vers la partie 2
// (EndSessionScene) et l'ouvre au clic d'un bouton.
//
// Le lien est renseigne par les chercheurs dans les parametres de la session,
// recupere depuis l'API par FlowController et expose via FlowController.PlatformUrl.
//
// Brancher OpenPlatformUrl() sur l'event OnClick du bouton dans l'Inspecteur.
// -----------------------------

public class PlatformUrlDisplay : MonoBehaviour
{
    // Lien resolu depuis la session, memorise pour l'ouverture au clic (OpenPlatformUrl).
    private string _currentUrl;

    void Awake()
    {
        _currentUrl = FlowController.Instance != null ? FlowController.Instance.PlatformUrl : null;

        // Le bouton reste visible meme sans lien : l'ecran demande explicitement au participant
        // de suivre le lien, le masquer le laisserait dans une impasse. Un lien absent est un
        // probleme de configuration de session, signale par ce warning.
        if (string.IsNullOrWhiteSpace(_currentUrl))
            Debug.LogWarning("[PlatformUrlDisplay] Aucun platform_url fourni par la session.");
    }

    // A brancher sur l'event OnClick du bouton dans l'Inspecteur : ouvre le lien dans le navigateur.
    public void OpenPlatformUrl()
    {
        if (string.IsNullOrWhiteSpace(_currentUrl))
        {
            Debug.LogWarning("[PlatformUrlDisplay] OpenPlatformUrl appele sans lien valide.");
            return;
        }

        Application.OpenURL(_currentUrl);
    }
}
