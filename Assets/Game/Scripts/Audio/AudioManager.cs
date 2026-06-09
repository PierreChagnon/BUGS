using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// -----------------------------
// Singleton persistant qui pilote toute la lecture audio.
// Instancié dans BootScene via le prefab AudioManager.prefab.
// API : PlayMusic / StopMusic / PlayAmbience / StopAmbience / PlaySfx / SetGroupVolume.
//
// Crossfade musique : deux AudioSource A/B se relayent. Si la même track est demandée,
// la requête est un no-op (continuité parfaite entre scènes partageant la musique).
// Idem pour l'ambient sur deux sources dédiées.
// SFX : pool round-robin de N sources, routées sur le groupe SFX_Gameplay ou SFX_UI.
// -----------------------------

[DefaultExecutionOrder(-350)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Paramètres exposés du mixer (doivent correspondre exactement aux noms dans MainMixer.mixer)
    public const string MASTER_VOLUME = "MasterVolume";
    public const string MUSIC_VOLUME = "MusicVolume";
    public const string AMBIENCE_VOLUME = "AmbienceVolume";
    public const string SFX_GAMEPLAY_VOLUME = "SfxGameplayVolume";
    public const string SFX_UI_VOLUME = "SfxUIVolume";

    // Clés PlayerPrefs pour la persistance des volumes
    const string PREF_MASTER = "audio.master";
    const string PREF_MUSIC = "audio.music";
    const string PREF_AMBIENCE = "audio.ambience";
    const string PREF_SFX_GAMEPLAY = "audio.sfx_gameplay";
    const string PREF_SFX_UI = "audio.sfx_ui";

    // Volume linéaire minimum avant de passer en -80 dB (silence total)
    const float MIN_LINEAR = 0.0001f;

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private AudioMixerGroup _ambienceGroup;
    [SerializeField] private AudioMixerGroup _sfxGameplayGroup;
    [SerializeField] private AudioMixerGroup _sfxUiGroup;

    [Header("Pool SFX")]
    [Tooltip("Nombre d'AudioSource one-shot disponibles simultanément. Round-robin si saturé.")]
    [Min(1)] [SerializeField] private int _sfxPoolSize = 8;

    [Header("Volumes par défaut (linéaire 0-1)")]
    [Range(0f, 1f)] [SerializeField] private float _defaultMaster = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float _defaultMusic = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float _defaultAmbience = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float _defaultSfxGameplay = 1f;
    [Range(0f, 1f)] [SerializeField] private float _defaultSfxUi = 1f;

    // Sources internes
    AudioSource _musicA, _musicB;
    AudioSource _ambienceA, _ambienceB;
    AudioSource[] _sfxPool;
    int _sfxIndex;

    // État courant
    MusicTrack _currentMusic;
    MusicTrack _currentAmbience;
    bool _musicUsingA = true;     // true → A est la source active, B fait fade-out
    bool _ambienceUsingA = true;

    // Coroutines en cours (pour cancel en cas de nouvel appel pendant un fade)
    Coroutine _musicFadeCoroutine;
    Coroutine _ambienceFadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAudioSources();
        LoadVolumesFromPrefs();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -------------------------
    // Setup
    // -------------------------

    void BuildAudioSources()
    {
        _musicA = CreateSource("Music_A", _musicGroup, loop: true);
        _musicB = CreateSource("Music_B", _musicGroup, loop: true);
        _ambienceA = CreateSource("Ambience_A", _ambienceGroup, loop: true);
        _ambienceB = CreateSource("Ambience_B", _ambienceGroup, loop: true);

        _sfxPool = new AudioSource[_sfxPoolSize];
        for (int i = 0; i < _sfxPoolSize; i++)
            _sfxPool[i] = CreateSource($"Sfx_{i:00}", null, loop: false);
    }

    AudioSource CreateSource(string name, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = 0f;
        src.outputAudioMixerGroup = group;
        return src;
    }

    // -------------------------
    // API publique
    // -------------------------

    public void PlayMusic(MusicTrack track)
    {
        if (track == null)
        {
            StopMusic();
            return;
        }

        if (track == _currentMusic)
            return; // no-op : continuité parfaite entre scènes partageant la même musique

        if (track.clip == null)
        {
            Debug.LogWarning($"[AudioManager] MusicTrack '{track.name}' n'a pas de clip assigné.");
            return;
        }

        _currentMusic = track;
        StartMusicFade(track);
    }

    public void StopMusic(float fadeDuration = -1f)
    {
        _currentMusic = null;
        if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
        var active = _musicUsingA ? _musicA : _musicB;
        var other = _musicUsingA ? _musicB : _musicA;
        float d = fadeDuration >= 0f ? fadeDuration : 1f;
        _musicFadeCoroutine = StartCoroutine(FadeOutAndStop(active, d, alsoStop: other));
    }

    public void PlayAmbience(MusicTrack track)
    {
        if (track == null)
        {
            StopAmbience();
            return;
        }

        if (track == _currentAmbience)
            return;

        if (track.clip == null)
        {
            Debug.LogWarning($"[AudioManager] Ambience '{track.name}' n'a pas de clip assigné.");
            return;
        }

        _currentAmbience = track;
        StartAmbienceFade(track);
    }

    public void StopAmbience(float fadeDuration = -1f)
    {
        _currentAmbience = null;
        if (_ambienceFadeCoroutine != null) StopCoroutine(_ambienceFadeCoroutine);
        var active = _ambienceUsingA ? _ambienceA : _ambienceB;
        var other = _ambienceUsingA ? _ambienceB : _ambienceA;
        float d = fadeDuration >= 0f ? fadeDuration : 1f;
        _ambienceFadeCoroutine = StartCoroutine(FadeOutAndStop(active, d, alsoStop: other));
    }

    public void PlaySfx(SoundEffect sfx)
    {
        if (sfx == null) return;

        var clip = sfx.PickClip();
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] SoundEffect '{sfx.name}' n'a aucun clip.");
            return;
        }

        var src = _sfxPool[_sfxIndex];
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;

        src.outputAudioMixerGroup = sfx.channel == SfxChannel.UI ? _sfxUiGroup : _sfxGameplayGroup;
        src.clip = clip;
        src.volume = sfx.PickVolume();
        src.pitch = sfx.PickPitch();
        src.Play();
    }

    // Volume linéaire 0-1 → dB sur le paramètre exposé du mixer.
    // Conversion log10*20 = échelle perceptive correcte pour l'oreille humaine.
    public void SetGroupVolume(string mixerParam, float linear01)
    {
        if (_mixer == null) return;
        float clamped = Mathf.Clamp01(linear01);
        float db = clamped <= MIN_LINEAR ? -80f : Mathf.Log10(clamped) * 20f;
        _mixer.SetFloat(mixerParam, db);
        SaveVolumeToPrefs(mixerParam, clamped);
    }

    public float GetGroupVolume(string mixerParam)
    {
        if (_mixer == null) return 1f;
        if (!_mixer.GetFloat(mixerParam, out float db)) return 1f;
        return db <= -80f ? 0f : Mathf.Pow(10f, db / 20f);
    }

    // Règle d'un coup les deux groupes SFX (gameplay + UI) depuis une seule valeur de slider.
    // Garde SFX_Gameplay et SFX_UI synchronisés et persiste les deux.
    public void SetSfxVolume(float linear01)
    {
        SetGroupVolume(SFX_GAMEPLAY_VOLUME, linear01);
        SetGroupVolume(SFX_UI_VOLUME, linear01);
    }

    // -------------------------
    // Crossfade interne
    // -------------------------

    void StartMusicFade(MusicTrack track)
    {
        if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);

        // La source active devient celle qui était "other" et vice-versa
        _musicUsingA = !_musicUsingA;
        var newSrc = _musicUsingA ? _musicA : _musicB;
        var oldSrc = _musicUsingA ? _musicB : _musicA;

        _musicFadeCoroutine = StartCoroutine(CrossfadeRoutine(newSrc, oldSrc, track));
    }

    void StartAmbienceFade(MusicTrack track)
    {
        if (_ambienceFadeCoroutine != null) StopCoroutine(_ambienceFadeCoroutine);

        _ambienceUsingA = !_ambienceUsingA;
        var newSrc = _ambienceUsingA ? _ambienceA : _ambienceB;
        var oldSrc = _ambienceUsingA ? _ambienceB : _ambienceA;

        _ambienceFadeCoroutine = StartCoroutine(CrossfadeRoutine(newSrc, oldSrc, track));
    }

    IEnumerator CrossfadeRoutine(AudioSource newSrc, AudioSource oldSrc, MusicTrack track)
    {
        newSrc.clip = track.clip;
        newSrc.loop = track.loop;
        newSrc.volume = 0f;
        newSrc.Play();

        float fadeIn = Mathf.Max(0.0001f, track.fadeInDuration);
        float fadeOut = Mathf.Max(0.0001f, track.fadeOutDuration);
        float duration = Mathf.Max(fadeIn, fadeOut);
        float oldStartVolume = oldSrc.volume;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float tIn = Mathf.Clamp01(elapsed / fadeIn);
            float tOut = Mathf.Clamp01(elapsed / fadeOut);
            newSrc.volume = Mathf.Lerp(0f, track.targetVolume, tIn);
            oldSrc.volume = Mathf.Lerp(oldStartVolume, 0f, tOut);
            yield return null;
        }

        newSrc.volume = track.targetVolume;
        oldSrc.volume = 0f;
        oldSrc.Stop();
        oldSrc.clip = null;
    }

    IEnumerator FadeOutAndStop(AudioSource src, float duration, AudioSource alsoStop)
    {
        float start = src.volume;
        float elapsed = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (elapsed < d)
        {
            elapsed += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(elapsed / d));
            yield return null;
        }

        src.volume = 0f;
        src.Stop();
        src.clip = null;

        if (alsoStop != null && alsoStop.isPlaying)
        {
            alsoStop.Stop();
            alsoStop.volume = 0f;
            alsoStop.clip = null;
        }
    }

    // -------------------------
    // Persistance PlayerPrefs
    // -------------------------

    void LoadVolumesFromPrefs()
    {
        ApplyVolume(MASTER_VOLUME, PlayerPrefs.GetFloat(PREF_MASTER, _defaultMaster));
        ApplyVolume(MUSIC_VOLUME, PlayerPrefs.GetFloat(PREF_MUSIC, _defaultMusic));
        ApplyVolume(AMBIENCE_VOLUME, PlayerPrefs.GetFloat(PREF_AMBIENCE, _defaultAmbience));
        ApplyVolume(SFX_GAMEPLAY_VOLUME, PlayerPrefs.GetFloat(PREF_SFX_GAMEPLAY, _defaultSfxGameplay));
        ApplyVolume(SFX_UI_VOLUME, PlayerPrefs.GetFloat(PREF_SFX_UI, _defaultSfxUi));
    }

    void ApplyVolume(string mixerParam, float linear01)
    {
        if (_mixer == null) return;
        float clamped = Mathf.Clamp01(linear01);
        float db = clamped <= MIN_LINEAR ? -80f : Mathf.Log10(clamped) * 20f;
        _mixer.SetFloat(mixerParam, db);
    }

    void SaveVolumeToPrefs(string mixerParam, float linear01)
    {
        string key = mixerParam switch
        {
            MASTER_VOLUME => PREF_MASTER,
            MUSIC_VOLUME => PREF_MUSIC,
            AMBIENCE_VOLUME => PREF_AMBIENCE,
            SFX_GAMEPLAY_VOLUME => PREF_SFX_GAMEPLAY,
            SFX_UI_VOLUME => PREF_SFX_UI,
            _ => null
        };
        if (key == null) return;
        PlayerPrefs.SetFloat(key, linear01);
    }
}
