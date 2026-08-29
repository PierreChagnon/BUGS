using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistalChoiceUI : MonoBehaviour
{
    [Header("Indicateurs de conseil")]
    [SerializeField] private GameObject _valleyAAdviceIndicator;
    [SerializeField] private GameObject _valleyBAdviceIndicator;

    [Header("Communication Report")]
    [SerializeField] private GameObject _enteringExplanationBlockPanel;
    [SerializeField] private TMP_Text _enteringExplanationBlockText;

    [Header("Scans de vallées (références sérialisées)")]
    [SerializeField] private DistalValleyScanView _valleyAScanView;
    [SerializeField] private DistalValleyScanView _valleyBScanView;

    [Header("Boutons de choix")]
    [SerializeField] private Button _valleyAButton;
    [SerializeField] private Button _valleyBButton;

    public bool AdviceVisible { get; private set; }
    public string AdvisedScanSide { get; private set; }
    public BugCloudSample LeftScanData { get; private set; }
    public BugCloudSample RightScanData { get; private set; }

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        // Le Communication Report est toujours presente au joueur : seul son
        // texte varie selon la config du bloc et le choix d'advisor.
        SetExplanationBlockPanelVisible(true);

        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

        AdviceVisible = flow.State != null && flow.State.distal_advice_visible;
        AdvisedScanSide = flow.State != null ? flow.State.distal_advice_choice : null;
        AdvisorType advisorType = flow.State != null ? flow.State.advisor_choice : AdvisorType.None;

        UpdateExplanationBlockText(flow.CurrentBlock, advisorType);

        BugCloudPairData distalScans = flow.GenerateCurrentDistalScans();
        LeftScanData = distalScans.firstCloud;
        RightScanData = distalScans.secondCloud;

        UpdateAdviceIndicators(advisorType);
        ApplyValleyScan(_valleyAScanView, LeftScanData, "A");
        ApplyValleyScan(_valleyBScanView, RightScanData, "B");
        ApplyForcedChoiceButtons(flow);
    }

    void SetExplanationBlockPanelVisible(bool visible)
    {
        if (_enteringExplanationBlockPanel != null)
            _enteringExplanationBlockPanel.SetActive(visible);
    }

    void UpdateExplanationBlockText(BlockConfig block, AdvisorType advisorType)
    {
        if (_enteringExplanationBlockText == null)
            return;

        CommunicationQuality quality = ExplanationResolver.GetCommunicationQuality(block);
        bool hasAdvisor = advisorType != AdvisorType.None;

        _enteringExplanationBlockText.text = quality switch
        {
            CommunicationQuality.Perfect => hasAdvisor
                ? "Radio reception is excellent in this area.Your advisor can give you advice, and communicate freely with you and provide explanations."
                : "Radio reception is excellent in this area. However, you do not have an advisor to communicate with you.",
            CommunicationQuality.None => hasAdvisor
                ? "Radio reception is terrible in this area. Your advisor can give you advice, but cannot communicate with you and provide explanations."
                : "Radio reception is terrible in this area. If you had an advisor, they would not be able to communicate with you.",
            _ => hasAdvisor
                ? "Radio reception is disrupted but partially functional in this area."
                : "Radio reception is disrupted but partially functional in this area. However, you do not have an advisor to communicate with you."
        };
    }

    public void CloseEnteringExplanationBlockPanel()
    {
        SetExplanationBlockPanelVisible(false);
    }

    static void ApplyValleyScan(DistalValleyScanView view, BugCloudSample scanData, string valleyLabel)
    {
        if (view == null)
        {
            Debug.LogError($"[DistalChoiceUI] DistalValleyScanView de la vallée {valleyLabel} non assignée dans l'inspecteur.");
            return;
        }

        view.Apply(scanData);
    }

    void UpdateAdviceIndicators(AdvisorType advisorType)
    {
        bool showValleyAIndicator = AdviceVisible && AdvisedScanSide == DistalScanSide.Left;
        bool showValleyBIndicator = AdviceVisible && AdvisedScanSide == DistalScanSide.Right;

        if (_valleyAAdviceIndicator != null)
            _valleyAAdviceIndicator.SetActive(showValleyAIndicator);

        if (_valleyBAdviceIndicator != null)
            _valleyBAdviceIndicator.SetActive(showValleyBIndicator);

        AdvisorBadgeUtility.ApplyBadgesByName(
            _valleyAAdviceIndicator != null ? _valleyAAdviceIndicator.transform : null,
            showValleyAIndicator,
            advisorType);
        AdvisorBadgeUtility.ApplyBadgesByName(
            _valleyBAdviceIndicator != null ? _valleyBAdviceIndicator.transform : null,
            showValleyBIndicator,
            advisorType);
    }

    public void OnChooseValleyA()
    {
        if (FlowController.Instance != null && !FlowController.Instance.IsDistalScanChoiceAllowed(DistalScanSide.Left))
            return;

        FlowController.Instance?.OnValleyChosen(DistalScanSide.Left);
    }

    public void OnChooseValleyB()
    {
        if (FlowController.Instance != null && !FlowController.Instance.IsDistalScanChoiceAllowed(DistalScanSide.Right))
            return;

        FlowController.Instance?.OnValleyChosen(DistalScanSide.Right);
    }

    void ApplyForcedChoiceButtons(FlowController flow)
    {
        bool forced = flow != null && flow.State != null && flow.State.distal_choice_is_forced;
        string forcedSide = forced ? flow.State.distal_choice_forced_scan_side : null;

        SetButtonState(_valleyAButton, "A", !forced || forcedSide == DistalScanSide.Left);
        SetButtonState(_valleyBButton, "B", !forced || forcedSide == DistalScanSide.Right);
    }

    static void SetButtonState(Button button, string valleyLabel, bool enabled)
    {
        if (button == null)
        {
            Debug.LogError($"[DistalChoiceUI] Bouton de la vallée {valleyLabel} non assigné dans l'inspecteur.");
            return;
        }

        button.interactable = enabled;

        var group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();

        group.alpha = enabled ? 1f : 0.35f;
        group.interactable = enabled;
    }
}
