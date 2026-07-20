using System;
using UnityEngine;

[Serializable]
public class ExplanationRuntimeState
{
    public string display_mode = ExplanationDisplayMode.None;
    public string content_variant;
    public string text_id;
    public string text;
    public bool? clicked;
    public int? display_duration_ms;

    bool _isCountingDisplayDuration;
    float _displayStartedAtRealtimeSeconds;

    public bool IsNone => string.Equals(display_mode, ExplanationDisplayMode.None, StringComparison.OrdinalIgnoreCase);
    public bool IsOptIn => string.Equals(display_mode, ExplanationDisplayMode.OptIn, StringComparison.OrdinalIgnoreCase);

    public static ExplanationRuntimeState None()
    {
        return new ExplanationRuntimeState
        {
            display_mode = ExplanationDisplayMode.None,
            content_variant = null,
            text_id = null,
            text = null,
            clicked = null,
            display_duration_ms = null
        };
    }

    public static ExplanationRuntimeState Create(
        string displayMode,
        string contentVariant,
        ExplanationText explanationText)
    {
        var state = new ExplanationRuntimeState
        {
            display_mode = displayMode,
            content_variant = contentVariant,
            text_id = explanationText != null ? explanationText.id ?? "" : "",
            text = explanationText != null ? explanationText.text : null,
            clicked = null,
            display_duration_ms = null
        };

        if (state.IsOptIn)
        {
            // En opt-in, le clic est attendu : on initialise clicked=false et le chrono a 0.
            state.clicked = false;
            state.display_duration_ms = 0;
        }
        else if (!state.IsNone)
        {
            // En forced, l'explanation s'affiche automatiquement : pas de clic a enregistrer (clicked=null),
            // mais on prepare le chrono a 0 pour mesurer le temps de lecture.
            state.display_duration_ms = 0;
        }

        return state;
    }

    public void MarkDisplayed(float realtimeSinceStartup)
    {
        // Le chrono de lecture concerne opt-in ET forced (toute explanation reellement affichee).
        if (IsNone)
            return;

        // Le clic n'a de sens qu'en opt-in ; en forced clicked reste null.
        if (IsOptIn)
            clicked = true;

        if (!display_duration_ms.HasValue)
            display_duration_ms = 0;

        if (_isCountingDisplayDuration)
            return;

        _displayStartedAtRealtimeSeconds = realtimeSinceStartup;
        _isCountingDisplayDuration = true;
    }

    public void MarkHidden(float realtimeSinceStartup)
    {
        if (IsNone || !_isCountingDisplayDuration)
            return;

        float elapsedSeconds = Mathf.Max(0f, realtimeSinceStartup - _displayStartedAtRealtimeSeconds);
        int elapsedMs = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds * 1000f));
        display_duration_ms = Mathf.Max(0, display_duration_ms ?? 0) + elapsedMs;
        _isCountingDisplayDuration = false;
    }
}

public static class ExplanationResolver
{
    public static bool HasEnabledExplanation(BlockConfig block, AdviceLevel level)
    {
        if (block == null || block.is_tutorial || block.explanations == null)
            return false;

        return IsExplanationEnabled(block.explanations.GetConfig(level));
    }

    public static bool HasEnabledForestExplanation(BlockConfig block)
    {
        return HasEnabledExplanation(block, AdviceLevel.Proximal)
            || HasEnabledExplanation(block, AdviceLevel.Motor);
    }

    public static ExplanationRuntimeState Resolve(
        BlockConfig block,
        AdviceLevel level,
        AdvisorType advisorType,
        bool adviceVisible)
    {
        if (block == null || block.is_tutorial || advisorType == AdvisorType.None || !adviceVisible)
            return ExplanationRuntimeState.None();

        AdviceExplanationConfig config = block.explanations != null
            ? block.explanations.GetConfig(level)
            : null;
        if (config == null)
            return ExplanationRuntimeState.None();

        string displayMode = NormalizeDisplayMode(config.display_mode);
        if (string.IsNullOrWhiteSpace(displayMode))
        {
            Debug.LogWarning(
                $"[ExplanationResolver] display_mode invalide pour level={FormatLevel(level)}: '{config.display_mode}'. Corriger la config en amont.");
            return ExplanationRuntimeState.None();
        }

        if (displayMode == ExplanationDisplayMode.None)
            return ExplanationRuntimeState.None();

        string contentVariant = NormalizeContentVariant(config.content_variant);
        if (string.IsNullOrWhiteSpace(contentVariant))
        {
            Debug.LogWarning(
                $"[ExplanationResolver] content_variant invalide pour level={FormatLevel(level)}: '{config.content_variant}'. Corriger la config en amont.");
            return ExplanationRuntimeState.None();
        }

        ExplanationText explanationText = config.GetText(advisorType, contentVariant);
        if (explanationText == null || string.IsNullOrWhiteSpace(explanationText.text))
        {
            Debug.LogWarning(
                $"[ExplanationResolver] Texte explanation manquant pour level={FormatLevel(level)}, advisor={FlowValueConverters.ToApiValue(advisorType)}, variant={contentVariant}. Corriger la config en amont.");
            return ExplanationRuntimeState.None();
        }

        return ExplanationRuntimeState.Create(displayMode, contentVariant, explanationText);
    }

    static bool IsExplanationEnabled(AdviceExplanationConfig config)
    {
        string displayMode = config != null
            ? NormalizeDisplayMode(config.display_mode)
            : null;

        return displayMode == ExplanationDisplayMode.Forced
            || displayMode == ExplanationDisplayMode.OptIn;
    }

    static string NormalizeDisplayMode(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        switch (rawValue.Trim().ToLowerInvariant())
        {
            case ExplanationDisplayMode.Forced:
                return ExplanationDisplayMode.Forced;
            case ExplanationDisplayMode.OptIn:
                return ExplanationDisplayMode.OptIn;
            case ExplanationDisplayMode.None:
                return ExplanationDisplayMode.None;
            default:
                return null;
        }
    }

    static string NormalizeContentVariant(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        switch (rawValue.Trim().ToLowerInvariant())
        {
            case ExplanationContentVariant.Short:
                return ExplanationContentVariant.Short;
            case ExplanationContentVariant.Long:
                return ExplanationContentVariant.Long;
            default:
                return null;
        }
    }

    static string FormatLevel(AdviceLevel level)
    {
        return level switch
        {
            AdviceLevel.Distal => "distal",
            AdviceLevel.Proximal => "proximal",
            AdviceLevel.Motor => "motor",
            _ => level.ToString()
        };
    }
}
