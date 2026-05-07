using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class DistalChoiceUI : MonoBehaviour
{
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
    public ValleyChoice AdvisedValley { get; private set; } = ValleyChoice.None;
    public ValleyChoice BestValley { get; private set; } = ValleyChoice.None;
    public BugCloudSample ValleyAScanData { get; private set; }
    public BugCloudSample ValleyBScanData { get; private set; }

    bool _autoScanBindingAttempted;
    RuntimeCloudScanBinding _autoValleyAScan;
    RuntimeCloudScanBinding _autoValleyBScan;

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

        AdviceVisible = flow.State != null && flow.State.distal_advice_visible;
        AdviceReliable = flow.State != null && flow.State.distal_advice_reliable;
        AdvisedValley = flow != null && flow.State != null ? flow.State.distal_advice_choice : ValleyChoice.None;
        BestValley = flow != null && flow.State != null ? flow.State.distal_best_valley : ValleyChoice.None;
        ValleyAScanData = BugCloudGenerationUtility.GenerateRepresentativeScan(flow.CurrentBlock.valley_a);
        ValleyBScanData = BugCloudGenerationUtility.GenerateRepresentativeScan(flow.CurrentBlock.valley_b);

        Debug.Log(
            "[DistalChoiceUI] Refresh DistalScene | " +
            $"advisorChoice={FlowValueConverters.ToApiValue(flow.State.advisor_choice)} | " +
            $"adviceVisible={AdviceVisible} | adviceReliable={AdviceReliable} | " +
            $"bestValley={FlowValueConverters.ToApiValue(BestValley)} | " +
            $"advisedValley={FlowValueConverters.ToApiValue(AdvisedValley)}",
            this);
        Debug.Log(
            "[DistalChoiceUI] Valley A config | " +
            FormatMapGenConfig(flow.CurrentBlock.valley_a) + " | " +
            $"expectedGreen={ComputeExpectedGreenBugs(flow.CurrentBlock.valley_a):0.00} | " +
            $"scan={FormatSample(ValleyAScanData)}",
            this);
        Debug.Log(
            "[DistalChoiceUI] Valley B config | " +
            FormatMapGenConfig(flow.CurrentBlock.valley_b) + " | " +
            $"expectedGreen={ComputeExpectedGreenBugs(flow.CurrentBlock.valley_b):0.00} | " +
            $"scan={FormatSample(ValleyBScanData)}",
            this);

        if (_titleText != null)
            _titleText.text = "Choix distal";

        if (_advisorChoiceText != null)
            _advisorChoiceText.text = $"Advisor choisi: {FlowValueConverters.ToApiValue(flow.State.advisor_choice)}";

        if (_valleyAText != null)
            _valleyAText.text = BuildValleyDescription("Vallee A", flow.CurrentBlock.valley_a);

        if (_valleyBText != null)
            _valleyBText.text = BuildValleyDescription("Vallee B", flow.CurrentBlock.valley_b);

        UpdateAdviceIndicators();
        ApplyValleyScan(ValleyAScanData, _valleyAScanView, ref _autoValleyAScan);
        ApplyValleyScan(ValleyBScanData, _valleyBScanView, ref _autoValleyBScan);
    }

    static string BuildValleyDescription(string label, MapGenConfig config)
    {
        if (config == null)
            return $"{label}\nConfig indisponible";

        float expectedGreenBugs = ComputeExpectedGreenBugs(config);

        return
            $"{label}\n" +
            $"Bugs verts attendus: {expectedGreenBugs:0.0}\n" +
            $"Bugs totaux: {config.min_total_bugs}-{config.max_total_bugs} | Ratio vert: {config.min_green_ratio:0.00}-{config.max_green_ratio:0.00}\n" +
            $"Pieges: {config.trap_count} | Brouillard: {config.fog_probability:0.00}";
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
            Debug.Log(
                $"[DistalChoiceUI] Applying scan through explicit view '{explicitScanView.name}' | {FormatSample(scanData)}",
                explicitScanView);
            explicitScanView.Apply(scanData);
            return;
        }

        EnsureAutomaticScanBindings();
        Debug.Log(
            $"[DistalChoiceUI] Applying scan through auto binding | {FormatSample(scanData)} | " +
            $"greenPS={(automaticScanBinding.greenParticles != null ? automaticScanBinding.greenParticles.name : "null")} | " +
            $"redPS={(automaticScanBinding.redParticles != null ? automaticScanBinding.redParticles.name : "null")}",
            this);
        automaticScanBinding.Apply(scanData);
    }

    void UpdateAdviceIndicators()
    {
        bool showValleyAIndicator = AdviceVisible && AdvisedValley == ValleyChoice.A;
        bool showValleyBIndicator = AdviceVisible && AdvisedValley == ValleyChoice.B;

        if (_valleyAAdviceIndicator != null)
            _valleyAAdviceIndicator.SetActive(showValleyAIndicator);

        if (_valleyBAdviceIndicator != null)
            _valleyBAdviceIndicator.SetActive(showValleyBIndicator);

        Debug.Log(
            "[DistalChoiceUI] UpdateAdviceIndicators | " +
            $"adviceVisible={AdviceVisible} | advisedValley={FlowValueConverters.ToApiValue(AdvisedValley)} | " +
            $"showA={showValleyAIndicator} | showB={showValleyBIndicator}",
            this);
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

        Debug.Log(
            $"[DistalChoiceUI] Auto-bind candidate groups | total={candidateGroups.Count} | left={leftGroups.Count} | right={rightGroups.Count}",
            this);

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
        Debug.Log(
            $"[DistalChoiceUI] Auto-bind valley {valleyLabel} -> group '{groups[0].name}' at x={groups[0].anchoredPosition.x:0.##}, y={groups[0].anchoredPosition.y:0.##}");
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
        FlowController.Instance?.OnValleyChosen(ValleyChoice.A);
    }

    public void OnChooseValleyB()
    {
        FlowController.Instance?.OnValleyChosen(ValleyChoice.B);
    }
}
