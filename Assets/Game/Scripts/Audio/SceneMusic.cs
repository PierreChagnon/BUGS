using UnityEngine;

// -----------------------------
// Composant à poser dans chaque scène du flow.
// Au Start(), demande à AudioManager de jouer la musique (et l'ambient optionnel) configurés.
// Si la même musique est déjà en cours, l'appel est un no-op (continuité parfaite).
// Si _ambience est null, l'ambient en cours est stoppé (fade-out propre).
// -----------------------------

public class SceneMusic : MonoBehaviour
{
    [Tooltip("Piste musicale à jouer sur cette scène. Laisser vide pour ne rien démarrer.")]
    [SerializeField] private MusicTrack _music;

    [Tooltip("Piste d'ambient à jouer en parallèle. Laisser vide pour stopper l'ambient en cours.")]
    [SerializeField] private MusicTrack _ambience;

    void Start()
    {
        if (AudioManager.Instance == null)
        {
            // Cas dev : scène lancée directement sans passer par BootScene.
            // On reste silencieux plutôt que de crasher.
            return;
        }

        if (_music != null)
            AudioManager.Instance.PlayMusic(_music);

        if (_ambience != null)
            AudioManager.Instance.PlayAmbience(_ambience);
        else
            AudioManager.Instance.StopAmbience();
    }
}
