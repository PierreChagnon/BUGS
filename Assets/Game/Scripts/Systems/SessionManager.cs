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
    public TrapSpawner trapSpawner; // optionnel: assigner dans l'inspecteur

    // Plus de création de session côté Unity; sessions sont gérées par le dashboard

    [Header("Session meta (optionnel)")]
    public long randomizationSeed = 0;
    public string buildVersion = "1.0.0";

    void Awake()
    {
        ApplySeedForThisRound();
    }

    IEnumerator Start()
    {
        // 1) Lire les arguments passés au build Unity (WebGL/Desktop)
        // Format attendu: "trapCount=<int>" (ex: trapCount=12)
        TryApplyTrapCountFromArgs();

        // 2) Lire sessionId et l'injecter dans TrialManager pour taguer les trials
        TryApplySessionIdFromArgs();

        // Démarrer directement le jeu (la session de recherche existe déjà côté dashboard)
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

            // Priorité: si un TrapSpawner est référencé, on le met à jour
            if (trapSpawner != null)
            {
                trapSpawner.trapCount = parsed;
                Debug.Log($"[SessionManager] trapCount reçu via args: {parsed} (assigné au TrapSpawner référencé)");
                return;
            }

            // Sinon, tenter d'en trouver un dans la scène
            var spawner = Object.FindFirstObjectByType<TrapSpawner>();
            if (spawner != null)
            {
                spawner.trapCount = parsed;
                Debug.Log($"[SessionManager] trapCount reçu via args: {parsed} (assigné au TrapSpawner trouvé)");
            }
            else
            {
                Debug.LogWarning("[SessionManager] Aucun TrapSpawner trouvé pour appliquer trapCount.");
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


