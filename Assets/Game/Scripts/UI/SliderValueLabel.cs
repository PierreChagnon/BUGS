using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Affiche la valeur entiere d'un Slider dans un texte.
// Le texte est place en enfant du handle : il suit donc le curseur sans code de positionnement.
[RequireComponent(typeof(Slider))]
public class SliderValueLabel : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Texte affichant la valeur courante, en general enfant du handle du slider")]
    [SerializeField] private TMP_Text _valueText;

    Slider _slider;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener(Refresh);
        Refresh(_slider.value);
    }

    void OnDestroy()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(Refresh);
    }

    void Refresh(float value)
    {
        if (_valueText != null)
            _valueText.text = Mathf.RoundToInt(value).ToString();
    }
}
