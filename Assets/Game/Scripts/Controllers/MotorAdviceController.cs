using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum MotorKeySet
{
    ZQSD,
    TFGH,
    IJKL,
    None
}

// -----------------------------
// Motor Advice: applique le tirage du set actif + advice visible/fiable
// (résolu par TrialDrawResolver). Fournit un mapping input pour GridMover.
// -----------------------------

[DefaultExecutionOrder(-5)]
public class MotorAdviceController : MonoBehaviour
{
    public static MotorAdviceController Instance { get; private set; }

    public MotorKeySet ActiveSet { get; private set; } = MotorKeySet.ZQSD;
    public MotorKeySet DisplayedSet { get; private set; } = MotorKeySet.None;
    public bool AdviceVisible { get; private set; }
    public bool AdviceReliable { get; private set; }

    public event Action OnAdviceChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        var rng = CreateRng();
        var session = SessionManager.Instance;

        float visibleProb = session != null ? session.Map.motor_advice_visible_probability : 1f;
        float reliableProb = session != null ? session.Map.motor_advice_reliable_probability : 1f;
        bool hasAdvisor = session == null || session.HasAdvisor;
        bool choiceIsForced = session != null && session.MotorChoiceIsForced;

        // Tirages résolus dans Data/ (set actif, visibilité, fiabilité, set affiché).
        var draw = TrialDrawResolver.DrawMotorAdvice(
            hasAdvisor,
            choiceIsForced,
            FlowValueConverters.ToMotorKeySet(session != null ? session.MotorChoiceForcedSet : null),
            visibleProb,
            reliableProb,
            rng);

        ActiveSet = draw.active_set;
        AdviceVisible = draw.visible;
        AdviceReliable = draw.reliable;
        DisplayedSet = draw.displayed_set;

        Debug.Log($"[MotorAdviceController]: visible={AdviceVisible} (prob={(hasAdvisor ? visibleProb : 0f)}), reliable={reliableProb}");
        FlowController.Instance?.ResolveMotorExplanationForCurrentTrial(AdviceVisible);
        OnAdviceChanged?.Invoke();
    }

    public bool TryGetStep(out Vector2Int step)
    {
        step = Vector2Int.zero;
        if (Keyboard.current == null) return false;

        if (IsDirectionPressed(ActiveSet, "up")) { step = Vector2Int.up; return true; }
        if (IsDirectionPressed(ActiveSet, "left")) { step = Vector2Int.left; return true; }
        if (IsDirectionPressed(ActiveSet, "down")) { step = Vector2Int.down; return true; }
        if (IsDirectionPressed(ActiveSet, "right")) { step = Vector2Int.right; return true; }
        return false;
    }

    static bool IsDirectionPressed(MotorKeySet set, string direction)
    {
        var key = GetKeyControl(set, direction);
        return key != null && key.wasPressedThisFrame;
    }

    // Retourne le KeyControl (position PHYSIQUE de la touche, referencee sur le layout US)
    // pour un set + direction donnes. C'est la position physique qui est lue en input :
    // le comportement moteur est donc identique quel que soit le layout du clavier.
    static KeyControl GetKeyControl(MotorKeySet set, string direction)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;

        return set switch
        {
            MotorKeySet.ZQSD => direction switch
            {
                "up" => kb.wKey,
                "left" => kb.aKey,
                "down" => kb.sKey,
                "right" => kb.dKey,
                _ => null
            },
            MotorKeySet.TFGH => direction switch
            {
                "up" => kb.tKey,
                "left" => kb.fKey,
                "down" => kb.gKey,
                "right" => kb.hKey,
                _ => null
            },
            MotorKeySet.IJKL => direction switch
            {
                "up" => kb.iKey,
                "left" => kb.jKey,
                "down" => kb.kKey,
                "right" => kb.lKey,
                _ => null
            },
            _ => null
        };
    }

    // Renvoie le LABEL a afficher pour la touche a presser dans la direction donnee.
    // Utilise KeyControl.displayName : Unity interroge l'OS et renvoie l'etiquette reelle
    // de la touche selon le layout clavier courant (ex: la touche a la position "wKey"
    // affiche "Z" en AZERTY, "W" en QWERTY). Fallback sur les labels AZERTY si aucun
    // clavier n'est disponible (hors Play Mode, headless...).
    public static string FormatSet(MotorKeySet set, string direction = null)
    {
        if (direction == null) return string.Empty;

        var key = GetKeyControl(set, direction);
        if (key != null && !string.IsNullOrEmpty(key.displayName))
            return key.displayName;

        return FallbackLabel(set, direction);
    }

    // Labels AZERTY codes en dur, utilises uniquement en fallback quand displayName
    // n'est pas disponible (pas de Keyboard.current).
    static string FallbackLabel(MotorKeySet set, string direction)
    {
        return set switch
        {
            MotorKeySet.ZQSD => direction switch { "up" => "Z", "left" => "Q", "down" => "S", "right" => "D", _ => string.Empty },
            MotorKeySet.TFGH => direction switch { "up" => "T", "left" => "F", "down" => "G", "right" => "H", _ => string.Empty },
            MotorKeySet.IJKL => direction switch { "up" => "I", "left" => "J", "down" => "K", "right" => "L", _ => string.Empty },
            _ => string.Empty
        };
    }

    public bool IsActiveMoveKey(KeyControl key)
    {
        if (Keyboard.current == null || key == null) return false;

        return key == GetKeyControl(ActiveSet, "up")
            || key == GetKeyControl(ActiveSet, "left")
            || key == GetKeyControl(ActiveSet, "down")
            || key == GetKeyControl(ActiveSet, "right");
    }

    static System.Random CreateRng()
    {
        if (LevelRegistry.Instance == null)
        {
            Debug.LogWarning("[MotorAdvice] LevelRegistry introuvable, RNG non seedee.");
            return new System.Random();
        }

        return LevelRegistry.Instance.CreateRng(nameof(MotorAdviceController));
    }
}
