using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public enum GamePhase
{
    Boot,
    Welcome,
    Consent,
    Intro,
    AdvisorChoice,
    DistalChoice,
    Proximal,
    Questionnaire,
    Break,
    EndSession
}

public enum AdvisorType
{
    None,
    Human,
    Robot
}

public enum AdviceLevel
{
    Distal,
    Proximal,
    Motor
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
    public string label;
    // Lien renseigne par les chercheurs, affiche sur l'ecran de fin de session (EndSessionScene).
    public string platform_url;
    public bool randomize_blocks;
    public int? break_every_trials;
    public int? break_duration_seconds;
    public List<RuleScreen> rules = new();
    public List<BlockConfig> blocks = new();

    public SessionConfig DeepClone()
    {
        return new SessionConfig
        {
            session_template_id = session_template_id,
            label = label,
            platform_url = platform_url,
            randomize_blocks = randomize_blocks,
            break_every_trials = break_every_trials,
            break_duration_seconds = break_duration_seconds,
            rules = FlowCloneUtility.CloneRules(rules),
            blocks = FlowCloneUtility.CloneBlocks(blocks)
        };
    }
}

// Un ecran de regles affiche au demarrage (apres le consentement) : une image + un texte.
// L'ordre dans la liste rules = l'ordre d'affichage dans la modal.
[Serializable]
public class RuleScreen
{
    public string image_url;
    public string title;
    public string text;

    public RuleScreen DeepClone()
    {
        return new RuleScreen
        {
            image_url = image_url,
            title = title,
            text = text
        };
    }
}

// Les DTO de config (BlockConfig, DistalSceneConfig, MapGenConfig,
// AdviceExplanationConfig) ne portent aucun defaut de valeur : le payload de
// GET /api/sessions/[id] est valide par sessionConfigSchema (Zod) cote backend,
// qui garantit la presence de chaque champ. Les defauts de saisie vivent dans
// le dashboard, seule source de verite.
[Serializable]
public class BlockConfig
{
    public string block_template_id;
    public int block_order;
    public bool is_order_locked;
    public string config_fingerprint;
    public int trial_count;
    public bool is_tutorial;
    public bool advisor_forced;
    public string advisor_forced_value;
    public bool distal_forced;
    public float distal_forced_optimal_probability;
    public float proximal_forced_probability;
    public float proximal_forced_optimal_probability;
    public float motor_forced_probability;
    public string motor_forced_set;
    public DistalSceneConfig distal_scene = new();
    public MapGenConfig valley_a = new();
    public MapGenConfig valley_b = new();
    public ExplanationsConfig explanations;
    public float distal_advice_visible_probability;
    public float distal_advice_reliable_probability;
    public bool show_numerical_feedback;
    public InContextTutorialConfig in_context_tutorial = new();

    public BlockConfig DeepClone()
    {
        return new BlockConfig
        {
            block_template_id = block_template_id,
            block_order = block_order,
            is_order_locked = is_order_locked,
            config_fingerprint = config_fingerprint,
            trial_count = trial_count,
            is_tutorial = is_tutorial,
            advisor_forced = advisor_forced,
            advisor_forced_value = advisor_forced_value,
            distal_forced = distal_forced,
            distal_forced_optimal_probability = distal_forced_optimal_probability,
            proximal_forced_probability = proximal_forced_probability,
            proximal_forced_optimal_probability = proximal_forced_optimal_probability,
            motor_forced_probability = motor_forced_probability,
            motor_forced_set = motor_forced_set,
            distal_scene = distal_scene != null ? distal_scene.DeepClone() : new DistalSceneConfig(),
            valley_a = valley_a != null ? valley_a.DeepClone() : new MapGenConfig(),
            valley_b = valley_b != null ? valley_b.DeepClone() : new MapGenConfig(),
            explanations = explanations != null ? explanations.DeepClone() : null,
            distal_advice_visible_probability = distal_advice_visible_probability,
            distal_advice_reliable_probability = distal_advice_reliable_probability,
            show_numerical_feedback = show_numerical_feedback,
            in_context_tutorial = in_context_tutorial != null ? in_context_tutorial.DeepClone() : new()
        };
    }
}

[Serializable]
public class InContextTutorialConfig
{
    public string advisor_title;
    public string advisor_text;
    public string distal_title;
    public string distal_text;
    public string proximal_title;
    public string proximal_text;

    public InContextTutorialConfig DeepClone() => new InContextTutorialConfig
    {
        advisor_title  = advisor_title,  advisor_text  = advisor_text,
        distal_title   = distal_title,   distal_text   = distal_text,
        proximal_title = proximal_title, proximal_text = proximal_text
    };
}

[Serializable]
public class DistalSceneConfig
{
    public int min_total_bugs;
    public int max_total_bugs;
    public float min_green_ratio;
    public float max_green_ratio;
    public float gap_min;
    public float gap_max;

    public DistalSceneConfig DeepClone()
    {
        return new DistalSceneConfig
        {
            min_total_bugs = min_total_bugs,
            max_total_bugs = max_total_bugs,
            min_green_ratio = min_green_ratio,
            max_green_ratio = max_green_ratio,
            gap_min = gap_min,
            gap_max = gap_max
        };
    }
}

[Serializable]
public class MapGenConfig
{
    public int trap_count;
    public int min_distance;
    public int max_distance;
    public int min_total_bugs;
    public int max_total_bugs;
    public float min_green_ratio;
    public float max_green_ratio;
    public float gap_min;
    public float gap_max;
    public float path_visible_probability;
    public float suboptimal_path_probability;
    public float detour_probability;
    public float proximal_advice_reliable_probability;
    public float motor_advice_visible_probability;
    public float motor_advice_reliable_probability;
    public float suboptimal_trap_probability;
    public int min_suboptimal_traps;
    public int max_suboptimal_traps;
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
            path_visible_probability = path_visible_probability,
            suboptimal_path_probability = suboptimal_path_probability,
            detour_probability = detour_probability,
            proximal_advice_reliable_probability = proximal_advice_reliable_probability,
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

public static class ExplanationDisplayMode
{
    public const string Forced = "forced";
    public const string OptIn = "opt-in";
    public const string None = "none";

    // Valeur de config uniquement : le mode reel est tire a l'affichage.
    // Ne remonte jamais dans trial_responses.
    public const string ForcedOptIn = "forced/opt-in";
}

public static class ExplanationContentVariant
{
    public const string Short = "short";
    public const string Long = "long";

    // Valeur de config uniquement : la variante reelle est tiree a l'affichage.
    // Ne remonte jamais dans trial_responses.
    public const string ShortLong = "short/long";
}

[Serializable]
public class ExplanationsConfig
{
    public AdviceExplanationConfig distal = new();
    public AdviceExplanationConfig proximal = new();
    public AdviceExplanationConfig motor = new();

    public AdviceExplanationConfig GetConfig(AdviceLevel level)
    {
        return level switch
        {
            AdviceLevel.Distal => distal,
            AdviceLevel.Proximal => proximal,
            AdviceLevel.Motor => motor,
            _ => null
        };
    }

    public ExplanationsConfig DeepClone()
    {
        return new ExplanationsConfig
        {
            distal = distal != null ? distal.DeepClone() : new AdviceExplanationConfig(),
            proximal = proximal != null ? proximal.DeepClone() : new AdviceExplanationConfig(),
            motor = motor != null ? motor.DeepClone() : new AdviceExplanationConfig()
        };
    }
}

[Serializable]
public class AdviceExplanationConfig
{
    public string display_mode;
    public string content_variant;
    public ExplanationCorpus corpus = new();

    // Absent du payload pour le niveau distal (jamais lu dans ce cas : la
    // distal explanation ne depend que de display_mode).
    public float display_probability;

    // Lu uniquement quand display_mode vaut "forced/opt-in" : probabilite de tirer
    // "forced", sinon "opt-in".
    public float display_mode_forced_probability;

    // Lu uniquement quand content_variant vaut "short/long" : probabilite de tirer
    // "long", sinon "short".
    public float content_variant_long_probability;

    public ExplanationText GetText(AdvisorType advisorType, string variant)
    {
        ExplanationVariantSet variants = corpus != null ? corpus.GetVariantSet(advisorType) : null;
        return variants != null ? variants.GetText(variant) : null;
    }

    public AdviceExplanationConfig DeepClone()
    {
        return new AdviceExplanationConfig
        {
            display_mode = display_mode,
            content_variant = content_variant,
            corpus = corpus != null ? corpus.DeepClone() : new ExplanationCorpus(),
            display_probability = display_probability,
            display_mode_forced_probability = display_mode_forced_probability,
            content_variant_long_probability = content_variant_long_probability
        };
    }
}

[Serializable]
public class ExplanationCorpus
{
    [JsonProperty("human-bot")]
    public ExplanationVariantSet humanBot = new();

    [JsonProperty("bot-bot")]
    public ExplanationVariantSet botBot = new();

    public ExplanationVariantSet GetVariantSet(AdvisorType advisorType)
    {
        return advisorType switch
        {
            AdvisorType.Human => humanBot,
            AdvisorType.Robot => botBot,
            _ => null
        };
    }

    public ExplanationCorpus DeepClone()
    {
        return new ExplanationCorpus
        {
            humanBot = humanBot != null ? humanBot.DeepClone() : new ExplanationVariantSet(),
            botBot = botBot != null ? botBot.DeepClone() : new ExplanationVariantSet()
        };
    }
}

[Serializable]
public class ExplanationVariantSet
{
    [JsonProperty("short")]
    public ExplanationText shortText = new();

    [JsonProperty("long")]
    public ExplanationText longText = new();

    public ExplanationText GetText(string variant)
    {
        return variant switch
        {
            ExplanationContentVariant.Short => shortText,
            ExplanationContentVariant.Long => longText,
            _ => null
        };
    }

    public ExplanationVariantSet DeepClone()
    {
        return new ExplanationVariantSet
        {
            shortText = shortText != null ? shortText.DeepClone() : new ExplanationText(),
            longText = longText != null ? longText.DeepClone() : new ExplanationText()
        };
    }
}

[Serializable]
public class ExplanationText
{
    public string id = "";
    public string text = "";

    public ExplanationText DeepClone()
    {
        return new ExplanationText
        {
            id = id,
            text = text
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
    public string question_key;
    public string question_text;
    public string response;
}

[Serializable]
public class PlayerSessionState
{
    public string participant_id;
    public string session_template_id;
    public GamePhase current_phase = GamePhase.Boot;
    public int current_block_index;
    public int current_trial_index;
    public int completed_non_tutorial_trials;
    public bool break_pending;
    public AdvisorType advisor_choice = AdvisorType.None;
    public ValleyChoice valley_choice = ValleyChoice.None;
    public bool meta_choice_is_forced;
    public string meta_choice_forced_value;
    public bool distal_choice_is_forced;
    public string distal_choice_forced_scan_side;
    public string distal_choice_forced_value;
    public bool? distal_choice_forced_was_optimal;
    public float? distal_choice_forced_optimal_probability;
    public bool proximal_choice_is_forced;
    public string proximal_choice_forced_value;
    public bool? proximal_choice_forced_was_optimal;
    public float proximal_choice_forced_probability;
    public float? proximal_choice_forced_optimal_probability;
    public bool motor_choice_is_forced;
    public string motor_choice_forced_set;
    public float motor_choice_forced_probability;
    public bool distal_advice_visible;
    public bool distal_advice_reliable;
    public string distal_advice_choice;
    public string distal_best_valley;
    public string distal_scan_choice;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class IncludeNullInJsonAttribute : Attribute
{
}

[Serializable]
public class TrialResponseRow
{
    public string participant_id;
    public string session_template_id;
    public string session_name;
    public string build_version;
    public string block_template_id;
    public int block_index;
    public int trial_index;
    public int trial_count;
    public bool is_tutorial;
    public string advisor_choice;
    public string valley_choice;
    public bool advisor_forced;
    public string advisor_forced_value;
    public bool distal_forced;
    public float distal_forced_optimal_probability;
    public float proximal_forced_probability;
    public float proximal_forced_optimal_probability;
    public float motor_forced_probability;
    public string motor_forced_set;
    public float distal_advice_visible_probability;
    public float distal_advice_reliable_probability;
    public bool show_numerical_feedback;
    public bool distal_advice_visible;
    public bool distal_advice_reliable;
    public string distal_advice_choice;
    public string distal_best_valley;
    public string distal_scan_choice;
    public string distal_advice_explanation_display_mode = ExplanationDisplayMode.None;
    [IncludeNullInJson] public string distal_advice_explanation_content_variant;
    [IncludeNullInJson] public string distal_advice_explanation_text_id;
    [IncludeNullInJson] public bool? distal_advice_explanation_clicked;
    [IncludeNullInJson] public int? distal_advice_explanation_display_duration_ms;
    public DistalSceneConfig distal_scene;
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
    public float proximal_advice_reliable_probability;
    public float motor_advice_visible_probability;
    public float motor_advice_reliable_probability;
    public string motor_advice_explanation_display_mode = ExplanationDisplayMode.None;
    [IncludeNullInJson] public string motor_advice_explanation_content_variant;
    [IncludeNullInJson] public string motor_advice_explanation_text_id;
    [IncludeNullInJson] public bool? motor_advice_explanation_clicked;
    [IncludeNullInJson] public int? motor_advice_explanation_display_duration_ms;
    public float suboptimal_trap_probability;
    public int min_suboptimal_traps;
    public int max_suboptimal_traps;
    public string map_config;
    // Nullables : null = jamais mesure sur ce trial, omis du payload — un vrai 0
    // et une absence de mesure doivent rester distinguables dans l'export.
    public int? optimal_path_length;
    public int? cloud_distance;
    public bool optimal_path_visible;
    public bool path_is_suboptimal;
    public bool proximal_advice_reliable;
    public string proximal_choice;
    public string proximal_advice_explanation_display_mode = ExplanationDisplayMode.None;
    [IncludeNullInJson] public string proximal_advice_explanation_content_variant;
    [IncludeNullInJson] public string proximal_advice_explanation_text_id;
    [IncludeNullInJson] public bool? proximal_advice_explanation_clicked;
    [IncludeNullInJson] public int? proximal_advice_explanation_display_duration_ms;
    public bool choice_correct;
    public string true_cloud;
    public int green_bugs_collected;
    public int green_bugs_accumulated;
    public int green_bugs_session_total;
    public int traps_hit;
    public int steps;
    public int overtime_steps;
    public bool followed_advisor_path;
    public string player_path_log;
    [IncludeNullInJson] public string advisor_path_config;
    public string acceptability_question;
    public string sens_of_agency_question;
    public string human_likeness_question;
    public string started_at;
    public string ended_at;
}

public static class FlowCloneUtility
{
    public static List<RuleScreen> CloneRules(List<RuleScreen> rules)
    {
        var clone = new List<RuleScreen>();
        if (rules == null)
            return clone;

        foreach (var rule in rules)
            clone.Add(rule != null ? rule.DeepClone() : new RuleScreen());

        return clone;
    }

    public static List<BlockConfig> CloneBlocks(List<BlockConfig> blocks)
    {
        var clone = new List<BlockConfig>();
        if (blocks == null)
            return clone;

        foreach (var block in blocks)
            clone.Add(block != null ? block.DeepClone() : new BlockConfig());

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

    public static string ToApiValue(MotorKeySet set)
    {
        return set switch
        {
            MotorKeySet.ZQSD => "QZD",
            MotorKeySet.TFGH => "FTH",
            MotorKeySet.IJKL => "JIL",
            _ => null
        };
    }

    public static MotorKeySet ToMotorKeySet(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return MotorKeySet.ZQSD;

        switch (rawValue.Trim().ToUpperInvariant())
        {
            case "QZD":
            case "ZQSD":
                return MotorKeySet.ZQSD;
            case "FTH":
            case "TFGH":
                return MotorKeySet.TFGH;
            case "JIL":
            case "IJKL":
                return MotorKeySet.IJKL;
            default:
                return MotorKeySet.ZQSD;
        }
    }
}

public static class DistalScanSide
{
    public const string Left = "left";
    public const string Right = "right";

    public static string Opposite(string side)
    {
        return side == Left ? Right : Left;
    }
}
