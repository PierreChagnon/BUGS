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

// Qualite de la communication advisor -> joueur pour un bloc, affichee dans le
// "Communication Report" de la DistalChoiceScene.
public enum CommunicationQuality
{
    Perfect,
    Partial,
    None
}

public static class ExplanationResolver
{
    // Perfect si advisors et explanations sont garantis a chaque trial, None si
    // aucune explanation ne peut apparaitre (advisors jamais visibles, ou toutes
    // les explanations coupees), Partial sinon. La visibilite des advisors est
    // verifiee en premier : elle conditionne les explanations en amont.
    // La distal explanation ne depend que de son display_mode : son
    // display_probability n'est jamais tire par le FlowController.
    public static CommunicationQuality GetCommunicationQuality(BlockConfig block)
    {
        if (block == null)
            return CommunicationQuality.None;

        float distalAdviceProbability = Mathf.Clamp01(block.distal_advice_visible_probability);
        float proximalAdviceProbabilityA = Mathf.Clamp01(block.valley_a?.path_visible_probability ?? 0f);
        float proximalAdviceProbabilityB = Mathf.Clamp01(block.valley_b?.path_visible_probability ?? 0f);
        float motorAdviceProbabilityA = Mathf.Clamp01(block.valley_a?.motor_advice_visible_probability ?? 0f);
        float motorAdviceProbabilityB = Mathf.Clamp01(block.valley_b?.motor_advice_visible_probability ?? 0f);

        // display_mode est maitre sur display_probability : en "none" (ou invalide)
        // l'explanation est desactivee quelle que soit sa probabilite.
        bool proximalExplanationEnabled = IsExplanationEnabled(block.explanations?.proximal);
        bool motorExplanationEnabled = IsExplanationEnabled(block.explanations?.motor);
        bool distalExplanationEnabled = IsExplanationEnabled(block.explanations?.distal);

        float proximalExplanationProbability = Mathf.Clamp01(block.explanations?.proximal?.display_probability ?? 0f);
        float motorExplanationProbability = Mathf.Clamp01(block.explanations?.motor?.display_probability ?? 0f);

        bool advisorsNeverVisible = distalAdviceProbability <= 0f
            && proximalAdviceProbabilityA <= 0f && proximalAdviceProbabilityB <= 0f
            && motorAdviceProbabilityA <= 0f && motorAdviceProbabilityB <= 0f;

        bool explanationsNeverShown = (!proximalExplanationEnabled || proximalExplanationProbability <= 0f)
            && (!motorExplanationEnabled || motorExplanationProbability <= 0f)
            && !distalExplanationEnabled;

        if (advisorsNeverVisible || explanationsNeverShown)
            return CommunicationQuality.None;

        bool advisorsAlwaysVisible = distalAdviceProbability >= 1f
            && proximalAdviceProbabilityA >= 1f && proximalAdviceProbabilityB >= 1f
            && motorAdviceProbabilityA >= 1f && motorAdviceProbabilityB >= 1f;

        bool explanationsAlwaysShown = proximalExplanationEnabled && proximalExplanationProbability >= 1f
            && motorExplanationEnabled && motorExplanationProbability >= 1f
            && distalExplanationEnabled;

        return advisorsAlwaysVisible && explanationsAlwaysShown
            ? CommunicationQuality.Perfect
            : CommunicationQuality.Partial;
    }

    // rng sert uniquement a resoudre les valeurs mixtes de display_mode et
    // content_variant. Il doit etre deterministe (derive du seed du trial ou du
    // bloc) pour que la session reste rejouable.
    public static ExplanationRuntimeState Resolve(
        BlockConfig block,
        AdviceLevel level,
        AdvisorType advisorType,
        bool adviceVisible,
        System.Random rng)
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

        displayMode = DrawDisplayMode(displayMode, config, rng);
        contentVariant = DrawContentVariant(contentVariant, config, rng);

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
            || displayMode == ExplanationDisplayMode.OptIn
            || displayMode == ExplanationDisplayMode.ForcedOptIn;
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
            case ExplanationDisplayMode.ForcedOptIn:
                return ExplanationDisplayMode.ForcedOptIn;
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
            case ExplanationContentVariant.ShortLong:
                return ExplanationContentVariant.ShortLong;
            default:
                return null;
        }
    }

    // Les valeurs mixtes n'existent qu'en config : elles se resolvent ici en une
    // valeur reelle, qui est ensuite la seule a remonter dans trial_responses.
    static string DrawDisplayMode(
        string normalizedDisplayMode,
        AdviceExplanationConfig config,
        System.Random rng)
    {
        if (normalizedDisplayMode != ExplanationDisplayMode.ForcedOptIn)
            return normalizedDisplayMode;

        float forcedProbability = Mathf.Clamp01(config.display_mode_forced_probability);
        return rng.NextDouble() < forcedProbability
            ? ExplanationDisplayMode.Forced
            : ExplanationDisplayMode.OptIn;
    }

    static string DrawContentVariant(
        string normalizedContentVariant,
        AdviceExplanationConfig config,
        System.Random rng)
    {
        if (normalizedContentVariant != ExplanationContentVariant.ShortLong)
            return normalizedContentVariant;

        float longProbability = Mathf.Clamp01(config.content_variant_long_probability);
        return rng.NextDouble() < longProbability
            ? ExplanationContentVariant.Long
            : ExplanationContentVariant.Short;
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
