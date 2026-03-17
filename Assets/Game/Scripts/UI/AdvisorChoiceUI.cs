using TMPro;
using UnityEngine;

public class AdvisorChoiceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField] private TMP_Text[] _optionLabels;

    string[] _advisorOptions = { "none", "human", "robot" };

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

        _advisorOptions = flow.CurrentBlock.advisor_options != null && flow.CurrentBlock.advisor_options.Length > 0
            ? flow.CurrentBlock.advisor_options
            : _advisorOptions;

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

    public void OnChooseNoneClicked() => FlowController.Instance?.OnAdvisorChosen(AdvisorType.None);
    public void OnChooseHumanClicked() => FlowController.Instance?.OnAdvisorChosen(AdvisorType.Human);
    public void OnChooseRobotClicked() => FlowController.Instance?.OnAdvisorChosen(AdvisorType.Robot);
}
