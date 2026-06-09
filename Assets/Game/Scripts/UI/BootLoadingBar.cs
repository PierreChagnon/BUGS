using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pilote la barre de chargement de la BootScene sur l'etat reel du flow.
// Monte vers 90% pendant le chargement, file a 100% quand le flow quitte la phase Boot (succes).
// En cas d'erreur, le flow reste en phase Boot : la barre reste bloquee a 90%.
[RequireComponent(typeof(Slider))]
public class BootLoadingBar : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _pendingCap = 0.9f;
    [SerializeField] private float _fillSpeed = 0.5f;
    [Tooltip("Optionnel : texte affichant le pourcentage de chargement.")]
    [SerializeField] private TMP_Text _percentLabel;

    Slider _slider;
    bool _loaded;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.value = 0f;
    }

    void Start()
    {
        if (FlowController.Instance != null)
            FlowController.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDestroy()
    {
        if (FlowController.Instance != null)
            FlowController.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Boot)
            _loaded = true;
    }

    void Update()
    {
        float target = _loaded ? 1f : _pendingCap;
        _slider.value = Mathf.MoveTowards(_slider.value, target, _fillSpeed * Time.deltaTime);

        if (_percentLabel != null)
            _percentLabel.text = $"{Mathf.RoundToInt(_slider.value * 100f)}%";
    }
}
