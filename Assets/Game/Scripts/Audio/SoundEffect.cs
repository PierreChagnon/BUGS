using UnityEngine;

// -----------------------------
// Données d'un SFX one-shot.
// Asset créé via Project window → Create → Audio → Sound Effect.
// Plusieurs clips possibles → un tiré aléatoirement à chaque PlaySfx pour variation.
// -----------------------------

public enum SfxChannel
{
    Gameplay,   // Routé sur AudioMixerGroup "SFX_Gameplay"
    UI          // Routé sur AudioMixerGroup "SFX_UI"
}

[CreateAssetMenu(menuName = "Audio/Sound Effect", fileName = "Sfx_")]
public class SoundEffect : ScriptableObject
{
    [Tooltip("Un ou plusieurs clips. Si plusieurs, un est choisi au hasard à chaque PlaySfx.")]
    public AudioClip[] clips;

    [Tooltip("Canal de routage. Gameplay et UI sont mixés indépendamment.")]
    public SfxChannel channel = SfxChannel.Gameplay;

    [Header("Variation volume")]
    [Range(0f, 1f)] public float volumeMin = 0.9f;
    [Range(0f, 1f)] public float volumeMax = 1f;

    [Header("Variation pitch")]
    [Range(0.5f, 1.5f)] public float pitchMin = 0.95f;
    [Range(0.5f, 1.5f)] public float pitchMax = 1.05f;

    // Retourne un clip aléatoire parmi clips[]. Null si la liste est vide.
    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0)
            return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public float PickVolume() => Random.Range(volumeMin, volumeMax);

    public float PickPitch() => Random.Range(pitchMin, pitchMax);
}
