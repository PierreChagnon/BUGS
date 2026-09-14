using System.Collections.Generic;
using UnityEngine;

public static class FlowSerializationUtility
{
    public const string AcceptabilityQuestion1Key = "acceptability_1";
    public const string AcceptabilityQuestion2Key = "acceptability_2";
    public const string SensOfAgencyQuestionKey = "sens_of_agency";
    public const string HumanLikenessQuestionKey = "human_likeness";

    [System.Serializable]
    private class PlayerStepsWrapper
    {
        public PlayerStep[] items;
    }

    [System.Serializable]
    private class PathCellsWrapper
    {
        public PathCell[] items;
    }

    public static string ToPlayerStepsJson(IReadOnlyList<PlayerStep> steps)
    {
        if (steps == null || steps.Count == 0)
            return "[]";

        var wrapper = new PlayerStepsWrapper
        {
            items = new PlayerStep[steps.Count]
        };

        for (int i = 0; i < steps.Count; i++)
            wrapper.items[i] = steps[i];

        string wrapped = JsonUtility.ToJson(wrapper);
        const string prefix = "{\"items\":";
        if (!wrapped.StartsWith(prefix))
            return "[]";

        return wrapped.Substring(prefix.Length, wrapped.Length - prefix.Length - 1);
    }

    public static string ToPathCellsJson(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
            return "[]";

        var wrapper = new PathCellsWrapper
        {
            items = new PathCell[cells.Count]
        };

        for (int i = 0; i < cells.Count; i++)
            wrapper.items[i] = new PathCell(cells[i]);

        string wrapped = JsonUtility.ToJson(wrapper);
        const string prefix = "{\"items\":";
        if (!wrapped.StartsWith(prefix))
            return "[]";

        return wrapped.Substring(prefix.Length, wrapped.Length - prefix.Length - 1);
    }

    public static void ApplyQuestionnaireResponses(
        TrialResponseRow row,
        IReadOnlyList<QuestionResponse> responses)
    {
        if (row == null)
            return;

        row.acceptability_question_1 = null;
        row.acceptability_question_2 = null;
        row.sens_of_agency_question = null;
        row.human_likeness_question = null;

        if (responses == null)
            return;

        for (int i = 0; i < responses.Count; i++)
        {
            QuestionResponse response = responses[i];
            if (response == null)
                continue;

            switch (response.question_key)
            {
                case AcceptabilityQuestion1Key:
                    row.acceptability_question_1 = response.response;
                    break;
                case AcceptabilityQuestion2Key:
                    row.acceptability_question_2 = response.response;
                    break;
                case SensOfAgencyQuestionKey:
                    row.sens_of_agency_question = response.response;
                    break;
                case HumanLikenessQuestionKey:
                    row.human_likeness_question = response.response;
                    break;
            }
        }
    }

    // Echo de la config explanations du bloc pour le niveau (valeurs mixtes et
    // probabilites recopiees telles quelles, config null => chaines null et
    // probabilites 0 comme les autres echos de config), puis etat realise du
    // trial. visible (proximal/motor) est le resultat du tirage display_probability,
    // null si le tirage n'a pas eu lieu. Le texte est celui propose, qu'il ait ete
    // ouvert ou non ; il reste null quand rien n'a ete propose (display_mode "none").
    public static void ApplyExplanationState(
        TrialResponseRow row,
        AdviceLevel level,
        AdviceExplanationConfig config,
        ExplanationRuntimeState state)
    {
        if (row == null)
            return;

        state ??= ExplanationRuntimeState.None();

        string configuredDisplayMode = config?.display_mode;
        float displayModeForcedProbability = config?.display_mode_forced_probability ?? 0f;
        string configuredContentVariant = config?.content_variant;
        float contentVariantLongProbability = config?.content_variant_long_probability ?? 0f;
        float displayProbability = config?.display_probability ?? 0f;

        switch (level)
        {
            case AdviceLevel.Distal:
                row.distal_advice_explanation_configured_display_mode = configuredDisplayMode;
                row.distal_advice_explanation_display_mode_forced_probability = displayModeForcedProbability;
                row.distal_advice_explanation_configured_content_variant = configuredContentVariant;
                row.distal_advice_explanation_content_variant_long_probability = contentVariantLongProbability;
                row.distal_advice_explanation_display_mode = state.display_mode;
                row.distal_advice_explanation_content_variant = state.content_variant;
                row.distal_advice_explanation_text = state.text;
                row.distal_advice_explanation_clicked = state.clicked;
                row.distal_advice_explanation_display_duration_ms = state.display_duration_ms;
                break;

            case AdviceLevel.Proximal:
                row.proximal_advice_explanation_configured_display_mode = configuredDisplayMode;
                row.proximal_advice_explanation_display_mode_forced_probability = displayModeForcedProbability;
                row.proximal_advice_explanation_configured_content_variant = configuredContentVariant;
                row.proximal_advice_explanation_content_variant_long_probability = contentVariantLongProbability;
                row.proximal_advice_explanation_display_probability = displayProbability;
                row.proximal_advice_explanation_visible = state.visible;
                row.proximal_advice_explanation_display_mode = state.display_mode;
                row.proximal_advice_explanation_content_variant = state.content_variant;
                row.proximal_advice_explanation_text = state.text;
                row.proximal_advice_explanation_clicked = state.clicked;
                row.proximal_advice_explanation_display_duration_ms = state.display_duration_ms;
                break;

            case AdviceLevel.Motor:
                row.motor_advice_explanation_configured_display_mode = configuredDisplayMode;
                row.motor_advice_explanation_display_mode_forced_probability = displayModeForcedProbability;
                row.motor_advice_explanation_configured_content_variant = configuredContentVariant;
                row.motor_advice_explanation_content_variant_long_probability = contentVariantLongProbability;
                row.motor_advice_explanation_display_probability = displayProbability;
                row.motor_advice_explanation_visible = state.visible;
                row.motor_advice_explanation_display_mode = state.display_mode;
                row.motor_advice_explanation_content_variant = state.content_variant;
                row.motor_advice_explanation_text = state.text;
                row.motor_advice_explanation_clicked = state.clicked;
                row.motor_advice_explanation_display_duration_ms = state.display_duration_ms;
                break;
        }
    }
}
