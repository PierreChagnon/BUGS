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

    struct RuntimeValleyScanBinding
    {
        public RuntimeCloudScanBinding firstCloud;
        public RuntimeCloudScanBinding secondCloud;

        public bool IsValid => firstCloud.IsValid && secondCloud.IsValid;

        public void Apply(BugCloudPairData data)
        {
            if (!IsValid)
                return;

            firstCloud.Apply(data.firstCloud);
            secondCloud.Apply(data.secondCloud);
        }
    }

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _advisorChoiceText;
    [SerializeField] private TMP_Text _valleyAText;
    [SerializeField] private TMP_Text _valleyBText;
    [SerializeField] private DistalValleyScanView _valleyAScanView;
    [SerializeField] private DistalValleyScanView _valleyBScanView;

    public bool AdviceVisible { get; private set; }
    public bool AdviceReliable { get; private set; }
    public ValleyChoice AdvisedValley { get; private set; } = ValleyChoice.None;
    public ValleyChoice BestValley { get; private set; } = ValleyChoice.None;
    public BugCloudPairData ValleyAScanData { get; private set; }
    public BugCloudPairData ValleyBScanData { get; private set; }

    bool _autoScanBindingAttempted;
    RuntimeValleyScanBinding _autoValleyAScan;
    RuntimeValleyScanBinding _autoValleyBScan;

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
        ValleyAScanData = BugCloudGenerationUtility.GenerateRepresentativePair(flow.CurrentBlock.valley_a);
        ValleyBScanData = BugCloudGenerationUtility.GenerateRepresentativePair(flow.CurrentBlock.valley_b);

        if (_titleText != null)
            _titleText.text = "Choix distal";

        if (_advisorChoiceText != null)
            _advisorChoiceText.text = $"Advisor choisi: {FlowValueConverters.ToApiValue(flow.State.advisor_choice)}";

        if (_valleyAText != null)
            _valleyAText.text = BuildValleyDescription("Vallee A", flow.CurrentBlock.valley_a);

        if (_valleyBText != null)
            _valleyBText.text = BuildValleyDescription("Vallee B", flow.CurrentBlock.valley_b);

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
        BugCloudPairData scanData,
        DistalValleyScanView explicitScanView,
        ref RuntimeValleyScanBinding automaticScanBinding)
    {
        if (explicitScanView != null)
        {
            explicitScanView.Apply(scanData);
            return;
        }

        EnsureAutomaticScanBindings();
        automaticScanBinding.Apply(scanData);
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

        if (candidateGroups.Count < 4)
        {
            Debug.LogWarning($"[DistalChoiceUI] Auto-bind scans incomplet: {candidateGroups.Count} groupes detectes.");
            return;
        }

        candidateGroups.Sort((a, b) =>
        {
            int sideCompare = Mathf.Sign(a.anchoredPosition.x).CompareTo(Mathf.Sign(b.anchoredPosition.x));
            if (sideCompare != 0)
                return sideCompare;

            return b.anchoredPosition.y.CompareTo(a.anchoredPosition.y);
        });

        var leftGroups = new List<RectTransform>();
        var rightGroups = new List<RectTransform>();
        foreach (var group in candidateGroups)
        {
            if (group.anchoredPosition.x < 0f)
                leftGroups.Add(group);
            else
                rightGroups.Add(group);
        }

        _autoValleyAScan = BuildAutomaticBinding(leftGroups);
        _autoValleyBScan = BuildAutomaticBinding(rightGroups);
    }

    static RuntimeValleyScanBinding BuildAutomaticBinding(List<RectTransform> groups)
    {
        groups.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));

        var binding = new RuntimeValleyScanBinding();
        if (groups.Count > 0)
            binding.firstCloud = BuildCloudBinding(groups[0]);
        if (groups.Count > 1)
            binding.secondCloud = BuildCloudBinding(groups[1]);

        return binding;
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
        FlowController.Instance?.OnValleyChosen(ValleyChoice.A);
    }

    public void OnChooseValleyB()
    {
        FlowController.Instance?.OnValleyChosen(ValleyChoice.B);
    }
}
