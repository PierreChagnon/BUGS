using UnityEngine;

public class ProximalExplanationBlockNoticeUI : MonoBehaviour
{
    [SerializeField] private GameObject _enteringExplanationBlockPanel;

    void Start()
    {
        var flow = FlowController.Instance;
        bool isFirstTrial = flow != null
            && flow.State != null
            && flow.State.current_trial_index == 0;
        bool shouldShow = isFirstTrial
            && !ExplanationResolver.HasEnabledExplanation(flow.CurrentBlock, AdviceLevel.Distal)
            && ExplanationResolver.HasEnabledForestExplanation(flow.CurrentBlock);

        SetPanelVisible(shouldShow);
    }

    public void CloseEnteringExplanationBlockPanel()
    {
        SetPanelVisible(false);
    }

    void SetPanelVisible(bool visible)
    {
        if (_enteringExplanationBlockPanel != null)
            _enteringExplanationBlockPanel.SetActive(visible);
    }
}
