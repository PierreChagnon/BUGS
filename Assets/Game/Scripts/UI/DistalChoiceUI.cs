using TMPro;
using UnityEngine;

public class DistalChoiceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _advisorChoiceText;
    [SerializeField] private TMP_Text _valleyAText;
    [SerializeField] private TMP_Text _valleyBText;

    public bool AdviceVisible { get; private set; }
    public bool AdviceReliable { get; private set; }
    public ValleyChoice AdvisedValley { get; private set; } = ValleyChoice.None;
    public ValleyChoice BestValley { get; private set; } = ValleyChoice.None;

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

        if (_titleText != null)
            _titleText.text = "Choix distal";

        if (_advisorChoiceText != null)
            _advisorChoiceText.text = $"Advisor choisi: {FlowValueConverters.ToApiValue(flow.State.advisor_choice)}";

        if (_valleyAText != null)
            _valleyAText.text = BuildValleyDescription("Vallee A", flow.CurrentBlock.valley_a_preview, flow.CurrentBlock.valley_a);

        if (_valleyBText != null)
            _valleyBText.text = BuildValleyDescription("Vallee B", flow.CurrentBlock.valley_b_preview, flow.CurrentBlock.valley_b);
    }

    static string BuildValleyDescription(string label, ValleyPreview preview, MapGenConfig config)
    {
        if (config == null)
            return $"{label}\nConfig indisponible";

        string previewText = preview != null
            ? $"Preview verts: {preview.left_green_hint:0.00}/{preview.right_green_hint:0.00}"
            : "Preview indisponible";

        return $"{label}\n{previewText}\nPieges: {config.trap_count} | Brouillard: {config.fog_probability:0.00}";
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
