using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum MotorKeySet
{
    ZQSD,
    TFGH,
    OKLM,
    None
}

// -----------------------------
// Motor Advice: tirage du set actif + advice visible/fiable.
// Fournit un mapping input pour GridMover.
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

        ActiveSet = session != null && session.MotorChoiceIsForced
            ? FlowValueConverters.ToMotorKeySet(session.MotorChoiceForcedSet)
            : (MotorKeySet)rng.Next(0, 3);

        float visibleProb = session != null ? session.motorAdviceVisibleProbability : 1f;
        float reliableProb = session != null ? session.motorAdviceReliableProbability : 1f;
        bool hasAdvisor = session == null || session.HasAdvisor;

        if (!hasAdvisor)
            visibleProb = 0f;

        AdviceVisible = rng.NextDouble() < visibleProb;
        Debug.Log($"[MotorAdviceController]: visible={AdviceVisible} (prob={visibleProb}), reliable={reliableProb}");
        if (!AdviceVisible)
        {
            AdviceReliable = false;
            DisplayedSet = MotorKeySet.None;
            FlowController.Instance?.ResolveMotorExplanationForCurrentTrial(false);
            OnAdviceChanged?.Invoke();
            return;
        }

        if (session != null && session.MotorChoiceIsForced)
        {
            AdviceReliable = true;
            DisplayedSet = ActiveSet;
        }
        else
        {
            AdviceReliable = rng.NextDouble() < reliableProb;
            DisplayedSet = AdviceReliable ? ActiveSet : PickOtherSet(rng, ActiveSet);
        }

        FlowController.Instance?.ResolveMotorExplanationForCurrentTrial(true);
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
            MotorKeySet.OKLM => direction switch
            {
                "up" => kb.oKey,
                "left" => kb.kKey,
                "down" => kb.lKey,
                "right" => kb.semicolonKey,
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
            MotorKeySet.OKLM => direction switch { "up" => "O", "left" => "K", "down" => "L", "right" => "M", _ => string.Empty },
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

    static MotorKeySet PickOtherSet(System.Random rng, MotorKeySet current)
    {
        var options = new List<MotorKeySet> { MotorKeySet.ZQSD, MotorKeySet.TFGH, MotorKeySet.OKLM };
        options.Remove(current);
        return options[rng.Next(0, options.Count)];
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
