using System.Collections;
using UnityEngine;

// -----------------------------
// Gère la création et l’envoi de la session de jeu
// -----------------------------

public class SessionManager : MonoBehaviour
{
    [Header("Refs")]
    public TrialManager trialManager;
    public GameManager gameManager;

    // Plus de création de session côté Unity; sessions sont gérées par le dashboard

    [Header("Session meta (optionnel)")]
    public long randomizationSeed = 0;
    public string buildVersion = "1.0.0";

    void Awake()
    {
        ApplySeedForThisRound();

        // Injecter la config CLI dès Awake pour que les spawners
        // (qui tournent en Start) aient accès aux valeurs correctes.
        TryApplyTrapCountFromArgs();
        TryApplySessionIdFromArgs();
    }

    IEnumerator Start()
    {
        // Tous les spawners ont tourné (Start, ordres négatifs).
        // On lance la partie.
        yield return null;
        gameManager.BeginFirstRound();
    }

    void ApplySeedForThisRound()
    {
        var reg = LevelRegistry.Instance;
        if (reg == null)
        {
            Debug.LogWarning("[SessionManager] LevelRegistry introuvable en Awake: seed non appliquée.");
            return;
        }

        // 1) seed via args (seed=<long>)
        if (TryGetSeedFromArgs(out var fromArgs))
            randomizationSeed = fromArgs;

        // 2) sinon génération
        if (randomizationSeed == 0)
            randomizationSeed = System.DateTime.UtcNow.Ticks ^ System.Guid.NewGuid().GetHashCode();

        // 3) Sinon on ne fait rien, elle est fourni dans l'inspecteur (utile pour tests reproductibles)

        
        reg.SetRoundSeed(randomizationSeed);
        Debug.Log($"[SessionManager] roundSeed={randomizationSeed}");
    }

    static bool TryGetSeedFromArgs(out long seed)
    {
        var args = System.Environment.GetCommandLineArgs();
        foreach (var a in args)
        {
            if (!a.StartsWith("seed=", System.StringComparison.OrdinalIgnoreCase)) continue;

            var val = a.Substring("seed=".Length);
            if (!long.TryParse(val, out seed))
            {
                Debug.LogWarning($"[SessionManager] Argument seed invalide: '{val}'");
                seed = default;
                return false;
            }

            return true;
        }

        seed = default;
        return false;
    }

    void TryApplyTrapCountFromArgs()
    {
        var args = System.Environment.GetCommandLineArgs();
        foreach (var a in args)
        {
            if (!a.StartsWith("trapCount=", System.StringComparison.OrdinalIgnoreCase)) continue;

            var val = a.Substring("trapCount=".Length);
            if (!int.TryParse(val, out var parsed) || parsed < 0)
            {
                Debug.LogWarning($"[SessionManager] Argument trapCount invalide: '{val}'");
                return;
            }

            // Écrire dans le registre central (TrapSpawner le lira à son Start)
            var reg = LevelRegistry.Instance;
            if (reg != null)
            {
                reg.trapCount = parsed;
                Debug.Log($"[SessionManager] trapCount reçu via args: {parsed} (écrit dans LevelRegistry)");
            }
            else
            {
                Debug.LogWarning("[SessionManager] LevelRegistry introuvable pour appliquer trapCount.");
            }
            return;
        }
    }

    void TryApplySessionIdFromArgs()
    {
        var args = System.Environment.GetCommandLineArgs();
        foreach (var a in args)
        {
            if (!a.StartsWith("sessionId=", System.StringComparison.OrdinalIgnoreCase)) continue;

            var val = a.Substring("sessionId=".Length);
            if (string.IsNullOrWhiteSpace(val))
            {
                Debug.LogWarning("[SessionManager] Argument sessionId vide.");
                return;
            }

            if (trialManager != null)
            {
                trialManager.SetSessionId(val);
                Debug.Log($"[SessionManager] sessionId reçu via args: '{val}'");
            }
            else
            {
                Debug.LogWarning("[SessionManager] TrialManager est null: impossible d'assigner sessionId.");
            }
            return;
        }
    }
}


