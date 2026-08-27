using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DistalChoiceUI : MonoBehaviour
{
    const string AdvisorHumanMaleName = "AdvisorDisplayHumanMale";
    const string AdvisorHumanFemaleName = "AdvisorDisplayHumanFemale";
    const string AdvisorRobotName = "AdvisorDisplayRobot";

    struct RuntimeCloudScanBinding
    {
        public ParticleSystem greenParticles;
        public ParticleSystem redParticles;

        public bool IsValid => greenParticles != null && redParticles != null;

        public void Apply(BugCloudSample sample)
        {
            if (!IsValid)
                return;

            BugCloudParticleUtility.Apply(greenParticles, redParticles, sample);
        }
    }

    [SerializeField] private GameObject _valleyAAdviceIndicator;
    [SerializeField] private GameObject _valleyBAdviceIndicator;
    [SerializeField] private GameObject _enteringExplanationBlockPanel;
    [SerializeField] private TMP_Text _enteringExplanationBlockText;

    public bool AdviceVisible { get; private set; }
    public string AdvisedScanSide { get; private set; }
    public BugCloudSample LeftScanData { get; private set; }
    public BugCloudSample RightScanData { get; private set; }

    bool _autoScanBindingAttempted;
    RuntimeCloudScanBinding _autoValleyAScan;
    RuntimeCloudScanBinding _autoValleyBScan;
    bool _showHumanMale;

    void Start()
    {
        _showHumanMale = Random.value < 0.5f;
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
        ApplyValleyScan(LeftScanData, ref _autoValleyAScan);
        ApplyValleyScan(RightScanData, ref _autoValleyBScan);
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
                ? "Your advisor can give you advice, and communicate freely with you and provide explanations."
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

    void ApplyValleyScan(BugCloudSample scanData, ref RuntimeCloudScanBinding automaticScanBinding)
    {
        EnsureAutomaticScanBindings();
        automaticScanBinding.Apply(scanData);
    }

    void UpdateAdviceIndicators(AdvisorType advisorType)
    {
        bool showValleyAIndicator = AdviceVisible && AdvisedScanSide == DistalScanSide.Left;
        bool showValleyBIndicator = AdviceVisible && AdvisedScanSide == DistalScanSide.Right;

        if (_valleyAAdviceIndicator != null)
            _valleyAAdviceIndicator.SetActive(showValleyAIndicator);

        if (_valleyBAdviceIndicator != null)
            _valleyBAdviceIndicator.SetActive(showValleyBIndicator);

        SetAdvisorBadge(_valleyAAdviceIndicator, showValleyAIndicator, advisorType);
        SetAdvisorBadge(_valleyBAdviceIndicator, showValleyBIndicator, advisorType);
    }

    void SetAdvisorBadge(GameObject indicatorRoot, bool adviceVisible, AdvisorType advisorType)
    {
        if (indicatorRoot == null)
            return;

        bool showHuman = adviceVisible && advisorType == AdvisorType.Human;
        bool showRobot = adviceVisible && advisorType == AdvisorType.Robot;

        SetDescendantActive(indicatorRoot.transform, AdvisorHumanMaleName, showHuman && _showHumanMale);
        SetDescendantActive(indicatorRoot.transform, AdvisorHumanFemaleName, showHuman && !_showHumanMale);
        SetDescendantActive(indicatorRoot.transform, AdvisorRobotName, showRobot);
    }

    void SetDescendantActive(Transform root, string childName, bool isActive)
    {
        GameObject child = FindDescendant(root, childName);
        if (child != null)
            child.SetActive(isActive);
    }

    GameObject FindDescendant(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child.gameObject;

            GameObject match = FindDescendant(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }

    void EnsureAutomaticScanBindings()
    {
        if (_autoScanBindingAttempted)
            return;

        _autoScanBindingAttempted = true;

        var candidateGroups = new List<RectTransform>();
        foreach (Transform child in transform)
        {
            var rectTransform = child as RectTransform;
            if (rectTransform == null)
                continue;

            if (CountParticleSystems(rectTransform) != 2)
                continue;

            candidateGroups.Add(rectTransform);
        }

        if (candidateGroups.Count < 2)
        {
            Debug.LogWarning($"[DistalChoiceUI] Auto-bind scans incomplet: {candidateGroups.Count} groupes detectes.");
            return;
        }

        var leftGroups = new List<RectTransform>();
        var rightGroups = new List<RectTransform>();
        foreach (var group in candidateGroups)
        {
            if (group.anchoredPosition.x < 0f)
                leftGroups.Add(group);
            else
                rightGroups.Add(group);
        }

        _autoValleyAScan = BuildAutomaticBinding(leftGroups, "A");
        _autoValleyBScan = BuildAutomaticBinding(rightGroups, "B");
    }

    static RuntimeCloudScanBinding BuildAutomaticBinding(List<RectTransform> groups, string valleyLabel)
    {
        if (groups.Count == 0)
        {
            Debug.LogWarning($"[DistalChoiceUI] Aucun groupe de particules detecte pour la vallee {valleyLabel}.");
            return default;
        }

        groups.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        return BuildCloudBinding(groups[0]);
    }

    static RuntimeCloudScanBinding BuildCloudBinding(RectTransform group)
    {
        var particles = group.GetComponentsInChildren<ParticleSystem>(true);
        var binding = new RuntimeCloudScanBinding();

        foreach (var particle in particles)
        {
            if (particle == null)
                continue;

            if (IsGreenParticleSystem(particle))
                binding.greenParticles = particle;
            else
                binding.redParticles = particle;
        }

        return binding;
    }


    static int CountParticleSystems(RectTransform group)
    {
        return group != null ? group.GetComponentsInChildren<ParticleSystem>(true).Length : 0;
    }

    static bool IsGreenParticleSystem(ParticleSystem particle)
    {
        var color = particle.main.startColor.colorMax;
        return color.g >= color.r;
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

        Button valleyAButton = FindButtonForMethod(nameof(OnChooseValleyA));
        Button valleyBButton = FindButtonForMethod(nameof(OnChooseValleyB));

        SetButtonState(valleyAButton, !forced || forcedSide == DistalScanSide.Left);
        SetButtonState(valleyBButton, !forced || forcedSide == DistalScanSide.Right);
    }

    Button FindButtonForMethod(string methodName)
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null)
                continue;

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == methodName)
                    return button;
            }
        }

        return null;
    }

    static void SetButtonState(Button button, bool enabled)
    {
        if (button == null)
            return;

        button.interactable = enabled;

        var group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();

        group.alpha = enabled ? 1f : 0.35f;
        group.interactable = enabled;
    }
}
