using UnityEngine;

// -----------------------------
// Données d'une piste musicale ou ambient.
// Asset créé via Project window → Create → Audio → Music Track.
// Utilisé par AudioManager.PlayMusic et AudioManager.PlayAmbience.
// -----------------------------

[CreateAssetMenu(menuName = "Audio/Music Track", fileName = "Music_")]
public class MusicTrack : ScriptableObject
{
    [Tooltip("Clip audio source. Format recommandé : .ogg Vorbis, Streaming.")]
    public AudioClip clip;

    [Tooltip("Volume cible une fois le fade-in terminé.")]
    [Range(0f, 1f)] public float targetVolume = 1f;

    [Tooltip("Durée du fade-in lors d'un PlayMusic/PlayAmbience.")]
    [Min(0f)] public float fadeInDuration = 2f;

    [Tooltip("Durée du fade-out lors d'un StopMusic/StopAmbience ou d'un crossfade.")]
    [Min(0f)] public float fadeOutDuration = 2f;

    [Tooltip("Lecture en boucle. Désactiver pour un stinger ou une intro non-loopée.")]
    public bool loop = true;
}
