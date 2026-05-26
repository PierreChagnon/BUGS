using TMPro;
using UnityEngine;
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

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _advisorChoiceText;
    [SerializeField] private TMP_Text _valleyAText;
    [SerializeField] private TMP_Text _valleyBText;
    [SerializeField] private DistalValleyScanView _valleyAScanView;
    [SerializeField] private DistalValleyScanView _valleyBScanView;
    [SerializeField] private GameObject _valleyAAdviceIndicator;
    [SerializeField] private GameObject _valleyBAdviceIndicator;

    public bool AdviceVisible { get; private set; }
    public bool AdviceReliable { get; private set; }
    public string AdvisedScanSide { get; private set; }
    public string BestScanSide { get; private set; }
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
        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

        AdviceVisible = flow.State != null && flow.State.distal_advice_visible;
        AdviceReliable = flow.State != null && flow.State.distal_advice_reliable;
        AdvisedScanSide = flow.State != null ? flow.State.distal_advice_choice : null;
        BestScanSide = flow.State != null ? flow.State.distal_best_valley : null;
        BugCloudPairData distalScans = flow.GenerateCurrentDistalScans();
        LeftScanData = distalScans.firstCloud;
        RightScanData = distalScans.secondCloud;


        if (_titleText != null)
            _titleText.text = "Choix distal";

        if (_advisorChoiceText != null)
            _advisorChoiceText.text = $"Advisor choisi: {FlowValueConverters.ToApiValue(flow.State.advisor_choice)}";

        if (_valleyAText != null)
            _valleyAText.text = "Scan gauche";

        if (_valleyBText != null)
            _valleyBText.text = "Scan droit";

        UpdateAdviceIndicators();
        ApplyValleyScan(LeftScanData, _valleyAScanView, ref _autoValleyAScan);
        ApplyValleyScan(RightScanData, _valleyBScanView, ref _autoValleyBScan);
    }

    static float ComputeExpectedGreenBugs(MapGenConfig config)
    {
        if (config == null)
            return 0f;

        float averageTotalBugs = (config.min_total_bugs + config.max_total_bugs) * 0.5f;
        float averageGreenRatio = (config.min_green_ratio + config.max_green_ratio) * 0.5f;
        return averageTotalBugs * averageGreenRatio;
    }

    void ApplyValleyScan(
        BugCloudSample scanData,
        DistalValleyScanView explicitScanView,
        ref RuntimeCloudScanBinding automaticScanBinding)
    {
        if (explicitScanView != null)
        {
            explicitScanView.Apply(scanData);
            return;
        }

        EnsureAutomaticScanBindings();
        automaticScanBinding.Apply(scanData);
    }

    void UpdateAdviceIndicators()
    {
        bool showValleyAIndicator = AdviceVisible && AdvisedScanSide == DistalScanSide.Left;
        bool showValleyBIndicator = AdviceVisible && AdvisedScanSide == DistalScanSide.Right;
        AdvisorType advisorType = GetAdvisorType();

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

    AdvisorType GetAdvisorType()
    {
        var flow = FlowController.Instance;
        if (flow != null && flow.State != null)
            return flow.State.advisor_choice;

        var session = SessionManager.Instance;
        return session == null || session.HasAdvisor ? AdvisorType.Human : AdvisorType.None;
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

    static string FormatMapGenConfig(MapGenConfig config)
    {
        if (config == null)
            return "config=null";

        return
            $"totalBugs={config.min_total_bugs}-{config.max_total_bugs}, " +
            $"greenRatio={config.min_green_ratio:0.00}-{config.max_green_ratio:0.00}, " +
            $"gap={config.gap_min:0.00}-{config.gap_max:0.00}, " +
            $"traps={config.trap_count}, fog={config.fog_probability:0.00}, seed={config.seed}";
    }

    static string FormatDistalSceneConfig(DistalSceneConfig config)
    {
        if (config == null)
            return "config=null";

        return
            $"totalBugs={config.min_total_bugs}-{config.max_total_bugs}, " +
            $"greenRatio={config.min_green_ratio:0.00}-{config.max_green_ratio:0.00}, " +
            $"gap={config.gap_min:0.00}-{config.gap_max:0.00}";
    }

    static string FormatSample(BugCloudSample sample)
    {
        return
            $"total={sample.totalBugs}, greenRatio={sample.greenRatio:0.000}, " +
            $"greenCount={sample.GreenBugCount}, redCount={sample.RedBugCount}";
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
        FlowController.Instance?.OnValleyChosen(DistalScanSide.Left);
    }

    public void OnChooseValleyB()
    {
        FlowController.Instance?.OnValleyChosen(DistalScanSide.Right);
    }
}
