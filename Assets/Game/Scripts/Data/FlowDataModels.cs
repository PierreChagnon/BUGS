using System;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Boot,
    Welcome,
    Consent,
    Intro,
    Tutorial,
    AdvisorChoice,
    DistalChoice,
    Proximal,
    Questionnaire,
    EndSession
}

public enum AdvisorType
{
    None,
    Human,
    Robot
}

public enum ValleyChoice
{
    None,
    A,
    B
}

[Serializable]
public class SessionConfig
{
    public string session_template_id;
    [TextArea(3, 10)] public string consent_text;
    public bool tutorial_enabled = true;
    public List<BlockConfig> blocks = new();

    public SessionConfig DeepClone()
    {
        return new SessionConfig
        {
            session_template_id = session_template_id,
            consent_text = consent_text,
            tutorial_enabled = tutorial_enabled,
            blocks = FlowCloneUtility.CloneBlocks(blocks)
        };
    }
}

[Serializable]
public class BlockConfig
{
    public string block_template_id;
    public int block_order;
    public int trial_count = 1;
    public bool is_tutorial;
    public MapGenConfig valley_a = new();
    public MapGenConfig valley_b = new();
    public ValleyPreview valley_a_preview = new();
    public ValleyPreview valley_b_preview = new();
    public List<QuestionConfig> questions = new();

    public BlockConfig DeepClone()
    {
        return new BlockConfig
        {
            block_template_id = block_template_id,
            block_order = block_order,
            trial_count = trial_count,
            is_tutorial = is_tutorial,
            valley_a = valley_a != null ? valley_a.DeepClone() : new MapGenConfig(),
            valley_b = valley_b != null ? valley_b.DeepClone() : new MapGenConfig(),
            valley_a_preview = valley_a_preview != null ? valley_a_preview.DeepClone() : new ValleyPreview(),
            valley_b_preview = valley_b_preview != null ? valley_b_preview.DeepClone() : new ValleyPreview(),
            questions = FlowCloneUtility.CloneQuestions(questions)
        };
    }
}

[Serializable]
public class MapGenConfig
{
    public int trap_count = 10;
    public int min_distance = 3;
    public int max_distance = 10;
    public int min_total_bugs = 20;
    public int max_total_bugs = 80;
    public float min_green_ratio = 0.4f;
    public float max_green_ratio = 0.8f;
    public float gap_min = 0.1f;
    public float gap_max = 0.3f;
    public float path_visible = 1f;
    public float suboptimal_path_probability;
    public float detour_probability;
    public float motor_advice_visible_probability = 1f;
    public float motor_advice_reliable_probability = 1f;
    public float suboptimal_trap_probability;
    public int min_suboptimal_traps = 1;
    public int max_suboptimal_traps = 3;
    public float fog_probability;
    public long seed;

    public MapGenConfig DeepClone()
    {
        return new MapGenConfig
        {
            trap_count = trap_count,
            min_distance = min_distance,
            max_distance = max_distance,
            min_total_bugs = min_total_bugs,
            max_total_bugs = max_total_bugs,
            min_green_ratio = min_green_ratio,
            max_green_ratio = max_green_ratio,
            gap_min = gap_min,
            gap_max = gap_max,
            path_visible = path_visible,
            suboptimal_path_probability = suboptimal_path_probability,
            detour_probability = detour_probability,
            motor_advice_visible_probability = motor_advice_visible_probability,
            motor_advice_reliable_probability = motor_advice_reliable_probability,
            suboptimal_trap_probability = suboptimal_trap_probability,
            min_suboptimal_traps = min_suboptimal_traps,
            max_suboptimal_traps = max_suboptimal_traps,
            fog_probability = fog_probability,
            seed = seed
        };
    }
}

[Serializable]
public class ValleyPreview
{
    public float left_cloud_size;
    public float right_cloud_size;
    public float left_green_hint;
    public float right_green_hint;

    public ValleyPreview DeepClone()
    {
        return new ValleyPreview
        {
            left_cloud_size = left_cloud_size,
            right_cloud_size = right_cloud_size,
            left_green_hint = left_green_hint,
            right_green_hint = right_green_hint
        };
    }
}

[Serializable]
public class QuestionConfig
{
    public int order;
    public string text;
    public string type;
    public string[] options = Array.Empty<string>();
    public int min_value = 1;
    public int max_value = 7;
    public string min_label;
    public string max_label;

    public QuestionConfig DeepClone()
    {
        return new QuestionConfig
        {
            order = order,
            text = text,
            type = type,
            options = FlowCloneUtility.CloneArray(options),
            min_value = min_value,
            max_value = max_value,
            min_label = min_label,
            max_label = max_label
        };
    }
}

[Serializable]
public class QuestionResponse
{
    public int order;
    public string question_text;
    public string response;
}

[Serializable]
public class PlayerSessionState
{
    public string participant_id;
    public string session_template_id;
    public string last_trial_response_id;
    public GamePhase current_phase = GamePhase.Boot;
    public int current_block_index;
    public int current_trial_index;
    public AdvisorType advisor_choice = AdvisorType.None;
    public ValleyChoice valley_choice = ValleyChoice.None;
    public int green_bugs_accumulated;
}

[Serializable]
public class TrialResponseRow
{
    public string participant_id;
    public string session_template_id;
    public string build_version;
    public int block_index;
    public int trial_index;
    public int trial_count;
    public string advisor_choice;
    public string valley_choice;
    public int grid_width;
    public int grid_height;
    public int trap_count;
    public int min_distance;
    public int max_distance;
    public int min_total_bugs;
    public int max_total_bugs;
    public float min_green_ratio;
    public float max_green_ratio;
    public float gap_min;
    public float gap_max;
    public float fog_probability;
    public long trial_seed;
    public float path_visible_probability;
    public float suboptimal_path_probability;
    public float detour_probability;
    public float motor_advice_visible_probability;
    public float motor_advice_reliable_probability;
    public float suboptimal_trap_probability;
    public int min_suboptimal_traps;
    public int max_suboptimal_traps;
    public string map_config;
    public int optimal_path_length;
    public int cloud_distance;
    public bool optimal_path_visible;
    public bool path_is_suboptimal;
    public string proximal_choice;
    public bool choice_correct;
    public string true_cloud;
    public int green_bugs_collected;
    public int green_bugs_accumulated;
    public int traps_hit;
    public int steps;
    public int overtime_steps;
    public bool followed_advisor_path;
    public string player_path_log;
    public string q1_text;
    public string q1_response;
    public string q2_text;
    public string q2_response;
    public string q3_text;
    public string q3_response;
    public string started_at;
    public string ended_at;
}

public static class FlowCloneUtility
{
    public static List<BlockConfig> CloneBlocks(List<BlockConfig> blocks)
    {
        var clone = new List<BlockConfig>();
        if (blocks == null)
            return clone;

        foreach (var block in blocks)
            clone.Add(block != null ? block.DeepClone() : new BlockConfig());

        return clone;
    }

    public static List<QuestionConfig> CloneQuestions(List<QuestionConfig> questions)
    {
        var clone = new List<QuestionConfig>();
        if (questions == null)
            return clone;

        foreach (var question in questions)
            clone.Add(question != null ? question.DeepClone() : new QuestionConfig());

        return clone;
    }

    public static string[] CloneArray(string[] source)
    {
        if (source == null)
            return Array.Empty<string>();

        var clone = new string[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }
}

public static class FlowValueConverters
{
    public static string ToApiValue(AdvisorType advisorType)
    {
        return advisorType switch
        {
            AdvisorType.Human => "human",
            AdvisorType.Robot => "robot",
            _ => "none"
        };
    }

    public static string ToApiValue(ValleyChoice valleyChoice)
    {
        return valleyChoice switch
        {
            ValleyChoice.A => "A",
            ValleyChoice.B => "B",
            _ => null
        };
    }

    public static AdvisorType ToAdvisorType(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return AdvisorType.None;

        switch (rawValue.Trim().ToLowerInvariant())
        {
            case "human":
                return AdvisorType.Human;
            case "robot":
                return AdvisorType.Robot;
            default:
                return AdvisorType.None;
        }
    }
}
