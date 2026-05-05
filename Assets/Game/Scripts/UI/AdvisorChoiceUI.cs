using TMPro;
using UnityEngine;

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

        for (int i = prefabs.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (prefabs[i], prefabs[randomIndex]) = (prefabs[randomIndex], prefabs[i]);
        }

        for (int i = 0; i < prefabs.Length && i < slots.Length; i++)
        {
            GameObject prefab = prefabs[i];
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
            _titleText.text = flow.IsCurrentBlockTutorial ? "Choix d'advisor (tutorial)" : "Choix d'advisor";

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
        }
    }

    public void OnChooseOptionIndex(int index)
    {
        if (_advisorOptions == null || index < 0 || index >= _advisorOptions.Length)
            return;

        AdvisorType type = FlowValueConverters.ToAdvisorType(_advisorOptions[index]);
        FlowController.Instance?.OnAdvisorChosen(type);
    }
}
