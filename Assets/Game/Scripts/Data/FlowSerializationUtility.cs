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
