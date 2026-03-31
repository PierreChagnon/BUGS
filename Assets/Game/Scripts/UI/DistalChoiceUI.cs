using TMPro;
using UnityEngine;

public class DistalChoiceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _advisorChoiceText;
    [SerializeField] private TMP_Text _valleyAText;
    [SerializeField] private TMP_Text _valleyBText;

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

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
