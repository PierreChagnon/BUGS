using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class FlowSerializationUtility
{
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

        row.q1_text = null;
        row.q1_response = null;
        row.q2_text = null;
        row.q2_response = null;
        row.q3_text = null;
        row.q3_response = null;

        if (responses == null)
            return;

        if (responses.Count > 0)
        {
            row.q1_text = responses[0]?.question_text;
            row.q1_response = responses[0]?.response;
        }

        if (responses.Count > 1)
        {
            row.q2_text = responses[1]?.question_text;
            row.q2_response = responses[1]?.response;
        }

        if (responses.Count > 2)
        {
            row.q3_text = responses[2]?.question_text;
            row.q3_response = responses[2]?.response;
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
