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

        ActiveSet = (MotorKeySet)rng.Next(0, 3);

        var session = SessionManager.Instance;
        float visibleProb = session != null ? session.motorAdviceVisibleProbability : 1f;
        float reliableProb = session != null ? session.motorAdviceReliableProbability : 1f;

        AdviceVisible = rng.NextDouble() < visibleProb;
        if (!AdviceVisible)
        {
            AdviceReliable = false;
            DisplayedSet = MotorKeySet.None;
            OnAdviceChanged?.Invoke();
            return;
        }

        AdviceReliable = rng.NextDouble() < reliableProb;
        DisplayedSet = AdviceReliable ? ActiveSet : PickOtherSet(rng, ActiveSet);

        OnAdviceChanged?.Invoke();
    }

    public bool TryGetStep(out Vector2Int step)
    {
        step = Vector2Int.zero;
        if (Keyboard.current == null) return false;

        switch (ActiveSet)
        {
            case MotorKeySet.ZQSD:
                if (Keyboard.current.wKey.wasPressedThisFrame) { step = Vector2Int.up; return true; }
                if (Keyboard.current.aKey.wasPressedThisFrame) { step = Vector2Int.left; return true; }
                if (Keyboard.current.sKey.wasPressedThisFrame) { step = Vector2Int.down; return true; }
                if (Keyboard.current.dKey.wasPressedThisFrame) { step = Vector2Int.right; return true; }
                return false;

            case MotorKeySet.TFGH:
                if (Keyboard.current.tKey.wasPressedThisFrame) { step = Vector2Int.up; return true; }
                if (Keyboard.current.fKey.wasPressedThisFrame) { step = Vector2Int.left; return true; }
                if (Keyboard.current.gKey.wasPressedThisFrame) { step = Vector2Int.down; return true; }
                if (Keyboard.current.hKey.wasPressedThisFrame) { step = Vector2Int.right; return true; }
                return false;

            case MotorKeySet.OKLM:
                if (Keyboard.current.oKey.wasPressedThisFrame) { step = Vector2Int.up; return true; }
                if (Keyboard.current.kKey.wasPressedThisFrame) { step = Vector2Int.left; return true; }
                if (Keyboard.current.lKey.wasPressedThisFrame) { step = Vector2Int.down; return true; }
                if (Keyboard.current.semicolonKey.wasPressedThisFrame) { step = Vector2Int.right; return true; }
                return false;
        }

        return false;
    }

    public static string FormatSet(MotorKeySet set)
    {
        return set switch
        {
            MotorKeySet.ZQSD => "Haut: Z  Gauche: Q  Bas: S  Droite: D",
            MotorKeySet.TFGH => "Haut: T  Gauche: F  Bas: G  Droite: H",
            MotorKeySet.OKLM => "Haut: O  Gauche: K  Bas: L  Droite: M",
            _ => string.Empty
        };
    }

    public bool IsActiveMoveKey(KeyControl key)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        return ActiveSet switch
        {
            MotorKeySet.ZQSD => key == kb.wKey || key == kb.aKey || key == kb.sKey || key == kb.dKey,
            MotorKeySet.TFGH => key == kb.tKey || key == kb.fKey || key == kb.gKey || key == kb.hKey,
            MotorKeySet.OKLM => key == kb.oKey || key == kb.kKey || key == kb.lKey || key == kb.semicolonKey,
            _ => false
        };
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
