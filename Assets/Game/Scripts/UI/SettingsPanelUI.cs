using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;

    void Awake()
    {
        _resumeButton.onClick.AddListener(Close);
        _quitButton.onClick.AddListener(Quit);
        if (_masterSlider != null)
            _masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        // Le slider SFX pilote les deux groupes SFX (gameplay + UI) via SetSfxVolume.
        _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        _root.SetActive(false);
    }

    void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Close);
        if (_quitButton != null) _quitButton.onClick.RemoveListener(Quit);
        if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    static void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetGroupVolume(AudioManager.MASTER_VOLUME, value);
    }

    static void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetGroupVolume(AudioManager.MUSIC_VOLUME, value);
    }

    static void OnSfxVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
    }

    public void Open()
    {
        if (AudioManager.Instance != null)
        {
            if (_masterSlider != null)
                _masterSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetGroupVolume(AudioManager.MASTER_VOLUME));
            _musicSlider.SetValueWithoutNotify(
                AudioManager.Instance.GetGroupVolume(AudioManager.MUSIC_VOLUME));
            // SFX_Gameplay est représentatif : il reste synchronisé avec SFX_UI.
            _sfxSlider.SetValueWithoutNotify(
                AudioManager.Instance.GetGroupVolume(AudioManager.SFX_GAMEPLAY_VOLUME));
        }
        // Pause + verrouillage input uniquement en contexte gameplay.
        // Sur un ecran de menu (Welcome, etc.) il n'y a pas de GameManager :
        // on ouvre le panneau sans figer l'ecran.
        if (GameManager.Instance != null)
        {
            Time.timeScale = 0f;
            GameManager.Instance.SetInputLocked(true);
        }
        _root.SetActive(true);
    }

    public void Close()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetInputLocked(false);
        // Toujours restaurer timeScale (no-op si on ne l'avait pas mis a 0).
        Time.timeScale = 1f;
        _root.SetActive(false);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
