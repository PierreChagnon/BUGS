using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;

    void Awake()
    {
        _resumeButton.onClick.AddListener(Close);
        _quitButton.onClick.AddListener(Quit);
        _musicSlider.onValueChanged.AddListener(v =>
            AudioManager.Instance?.SetGroupVolume(AudioManager.MUSIC_VOLUME, v));
        _sfxSlider.onValueChanged.AddListener(v =>
            AudioManager.Instance?.SetGroupVolume(AudioManager.SFX_GAMEPLAY_VOLUME, v));
        _root.SetActive(false);
    }

    public void Open()
    {
        if (AudioManager.Instance != null)
        {
            _musicSlider.SetValueWithoutNotify(
                AudioManager.Instance.GetGroupVolume(AudioManager.MUSIC_VOLUME));
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
