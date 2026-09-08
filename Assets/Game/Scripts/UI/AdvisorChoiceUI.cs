using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdvisorChoiceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField] private TMP_Text[] _optionLabels;
    [SerializeField]
    private GameObject _slotLeft
        , _slotMiddle
        , _slotRight;

    [SerializeField] private GameObject _nonePrefab;
    [SerializeField] private GameObject _humanPrefab;
    [SerializeField] private GameObject _robotPrefab;

    string[] _advisorOptions = { "none", "human", "robot" };

    void Start()
    {
        Refresh();

        GameObject[] prefabs = { _nonePrefab, _humanPrefab, _robotPrefab };
        GameObject[] slots = { _slotLeft, _slotMiddle, _slotRight };

        // Ordre tire de facon seedee par FlowController (salt 7, une fois par bloc).
        // Sans FlowController (sandbox), ordre par defaut none/human/robot.
        int[] order = FlowController.Instance?.State?.advisor_display_order;

        for (int i = 0; i < slots.Length; i++)
        {
            int prefabIndex = order != null && i < order.Length ? order[i] : i;
            GameObject prefab = prefabIndex < prefabs.Length ? prefabs[prefabIndex] : null;
            GameObject slot = slots[i];

            if (prefab != null && slot != null)
            {
                Instantiate(prefab, slot.transform);
            }
        }
    }

    void Refresh()
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

        if (_titleText != null)
            _titleText.text = "Choix d'advisor";

        if (_subtitleText != null)
            _subtitleText.text = "Ce choix est enregistre mais n'affecte pas encore la generation de la map.";

        if (_optionLabels == null)
            return;

        for (int i = 0; i < _optionLabels.Length; i++)
        {
            if (_optionLabels[i] == null)
                continue;

            string rawValue = i < _advisorOptions.Length ? _advisorOptions[i] : $"option_{i + 1}";
            _optionLabels[i].text = rawValue.ToUpperInvariant();
            SetOptionState(_optionLabels[i], flow, rawValue);
        }
    }

    public void OnChooseOptionIndex(int index)
    {
        if (_advisorOptions == null || index < 0 || index >= _advisorOptions.Length)
            return;

        AdvisorType type = FlowValueConverters.ToAdvisorType(_advisorOptions[index]);
        if (FlowController.Instance != null && !FlowController.Instance.IsAdvisorChoiceAllowed(type))
            return;

        FlowController.Instance?.OnAdvisorChosen(type);
    }

    static void SetOptionState(TMP_Text label, FlowController flow, string rawValue)
    {
        if (label == null)
            return;

        AdvisorType type = FlowValueConverters.ToAdvisorType(rawValue);
        bool allowed = flow == null || flow.IsAdvisorChoiceAllowed(type);
        label.alpha = allowed ? 1f : 0.35f;

        var button = label.GetComponentInParent<Button>();
        if (button != null)
            button.interactable = allowed;
    }
}
