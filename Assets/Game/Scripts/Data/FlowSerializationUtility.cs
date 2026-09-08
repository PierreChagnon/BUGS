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

}
