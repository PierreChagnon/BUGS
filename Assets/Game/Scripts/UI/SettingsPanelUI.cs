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
            _masterSlider.onValueChanged.AddListener(v =>
                AudioManager.Instance?.SetGroupVolume(AudioManager.MASTER_VOLUME, v));
        _musicSlider.onValueChanged.AddListener(v =>
            AudioManager.Instance?.SetGroupVolume(AudioManager.MUSIC_VOLUME, v));
        // Le slider SFX pilote les deux groupes SFX (gameplay + UI) via SetSfxVolume.
        _sfxSlider.onValueChanged.AddListener(v =>
            AudioManager.Instance?.SetSfxVolume(v));
        _root.SetActive(false);
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
        Time.timeScale = 0f;
        GameManager.Instance.SetInputLocked(true);
        _root.SetActive(true);
    }

    public void Close()
    {
        Time.timeScale = 1f;
        GameManager.Instance.SetInputLocked(false);
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
