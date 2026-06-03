using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class FlowSerializationUtility
{
    public const string AcceptabilityQuestionKey = "acceptability";
    public const string SensOfAgencyQuestionKey = "sens_of_agency";
    public const string HumanLikenessQuestionKey = "human_likeness";

    [System.Serializable]
    private class PlayerStepsWrapper
    {
        public PlayerStep[] items;
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

    public static void ApplyQuestionnaireResponses(
        TrialResponseRow row,
        IReadOnlyList<QuestionResponse> responses)
    {
        if (row == null)
            return;

        row.acceptability_question = null;
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
                case AcceptabilityQuestionKey:
                    row.acceptability_question = response.response;
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

    public static void ApplyExplanationState(
        TrialResponseRow row,
        AdviceLevel level,
        ExplanationRuntimeState state)
    {
        if (row == null)
            return;

        state ??= ExplanationRuntimeState.None();

        switch (level)
        {
            case AdviceLevel.Distal:
                row.distal_advice_explanation_display_mode = state.display_mode;
                row.distal_advice_explanation_content_variant = state.content_variant;
                row.distal_advice_explanation_text_id = state.text_id;
                row.distal_advice_explanation_clicked = state.clicked;
                row.distal_advice_explanation_display_duration_ms = state.display_duration_ms;
                break;

            case AdviceLevel.Proximal:
                row.proximal_advice_explanation_display_mode = state.display_mode;
                row.proximal_advice_explanation_content_variant = state.content_variant;
                row.proximal_advice_explanation_text_id = state.text_id;
                row.proximal_advice_explanation_clicked = state.clicked;
                row.proximal_advice_explanation_display_duration_ms = state.display_duration_ms;
                break;

            case AdviceLevel.Motor:
                row.motor_advice_explanation_display_mode = state.display_mode;
                row.motor_advice_explanation_content_variant = state.content_variant;
                row.motor_advice_explanation_text_id = state.text_id;
                row.motor_advice_explanation_clicked = state.clicked;
                row.motor_advice_explanation_display_duration_ms = state.display_duration_ms;
                break;
        }
    }

    public static string BuildQuestionnaireDebugSummary(IReadOnlyList<QuestionResponse> responses)
    {
        if (responses == null || responses.Count == 0)
            return "Aucune réponse";

        var builder = new StringBuilder();
        for (int i = 0; i < responses.Count; i++)
        {
            if (i > 0)
                builder.Append(" | ");

            builder.Append(responses[i]?.order ?? i + 1);
            builder.Append(": ");
            builder.Append(responses[i]?.response);
        }

        return builder.ToString();
    }
}
