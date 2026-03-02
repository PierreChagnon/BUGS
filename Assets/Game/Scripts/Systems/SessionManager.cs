using System.Collections;
using UnityEngine;

// -----------------------------
// Gère la création et l’envoi de la session de jeu
// -----------------------------

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Références")]
    public TrialManager trialManager;
    public GameManager gameManager;

    [Header("Session")]
    public long randomizationSeed = 0;
    public string buildVersion = "1.0.0";

    [Header("Recherche : Map")]
    [Tooltip("Nombre de pièges à placer. CLI: trapCount=N.")]
    public int trapCount = 10;

    [Tooltip("Distance Manhattan minimale (en cases) entre le joueur et les nuages. CLI: minDistance=N.")]
    public int minDistance = 3;

    [Tooltip("Distance Manhattan maximale (en cases) entre le joueur et les nuages. Bornée par la taille de la map. CLI: maxDistance=N. 0 = pas de limite (fallback map).")]
    public int maxDistance = 0;

    [Header("Recherche : Discrimination")]
    [Tooltip("Nombre minimal de bugs par nuage. CLI: minTotalBugs=N.")]
    public int minTotalBugs = 20;

    [Tooltip("Nombre maximal de bugs par nuage. CLI: maxTotalBugs=N.")]
    public int maxTotalBugs = 80;

    [Tooltip("Borne minimale du ratio de bugs verts (0-1). CLI: minGreenRatio=F.")]
    public float minGreenBugsRatio = 0.4f;

    [Tooltip("Borne maximale du ratio de bugs verts (0-1). CLI: maxGreenRatio=F.")]
    public float maxGreenBugsRatio = 0.8f;

    [Tooltip("Écart MINIMUM entre les ratios verts des deux nuages. CLI: gapMin=F.")]
    public float gapMin = 0.1f;

    [Tooltip("Écart MAXIMUM entre les ratios verts des deux nuages. CLI: gapMax=F.")]
    public float gapMax = 0.3f;

    [Header("Recherche : Advisor")]
    [Tooltip("Si true, le chemin optimal est affiché au joueur (condition advisor). CLI: pathVisible=0|1.")]
    public bool pathVisible = true;

    [Header("Recherche : Protocole")]
    [Tooltip("Identifiant du bloc expérimental pour le pipeline de données. CLI: blockId=N.")]
    public int blockId = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ApplySeedForThisRound();

        // Injecter la config CLI dès Awake pour que les spawners
        // (qui tournent en Start) aient accès aux valeurs correctes.
        TryApplyTrapCountFromArgs();
        TryApplyBugCloudParamsFromArgs();
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

            trapCount = parsed;
            Debug.Log($"[SessionManager] trapCount remplacé par args CLI: {parsed}");
            return;
        }
    }

    void TryApplyBugCloudParamsFromArgs()
    {
        var args = System.Environment.GetCommandLineArgs();
        foreach (var a in args)
        {
            TryParseInt(a, "minDistance", ref minDistance);
            TryParseInt(a, "maxDistance", ref maxDistance);
            TryParseInt(a, "minTotalBugs", ref minTotalBugs);
            TryParseInt(a, "maxTotalBugs", ref maxTotalBugs);
            TryParseFloat(a, "minGreenRatio", ref minGreenBugsRatio);
            TryParseFloat(a, "maxGreenRatio", ref maxGreenBugsRatio);
            TryParseFloat(a, "gapMin", ref gapMin);
            TryParseFloat(a, "gapMax", ref gapMax);
            TryParseBool(a, "pathVisible", ref pathVisible);
            TryParseInt(a, "blockId", ref blockId);
        }
    }



    // ── Helpers de parsing CLI ──
    static void TryParseBool(string arg, string key, ref bool target)
    {
        if (!arg.StartsWith(key + "=", System.StringComparison.OrdinalIgnoreCase)) return;
        var val = arg.Substring(key.Length + 1).Trim();
        if (val == "1" || val.Equals("true", System.StringComparison.OrdinalIgnoreCase))
        {
            target = true;
            Debug.Log($"[SessionManager] {key} remplacé par args CLI: true");
        }
        else if (val == "0" || val.Equals("false", System.StringComparison.OrdinalIgnoreCase))
        {
            target = false;
            Debug.Log($"[SessionManager] {key} remplacé par args CLI: false");
        }
        else
            Debug.LogWarning($"[SessionManager] Argument {key} invalide: '{val}' (attendu: 0|1|true|false)");
    }

    static void TryParseInt(string arg, string key, ref int target)
    {
        if (!arg.StartsWith(key + "=", System.StringComparison.OrdinalIgnoreCase)) return;
        var val = arg.Substring(key.Length + 1);
        if (int.TryParse(val, out var parsed))
        {
            target = parsed;
            Debug.Log($"[SessionManager] {key} remplacé par args CLI: {parsed}");
        }
        else
            Debug.LogWarning($"[SessionManager] Argument {key} invalide: '{val}'");
    }

    static void TryParseFloat(string arg, string key, ref float target)
    {
        if (!arg.StartsWith(key + "=", System.StringComparison.OrdinalIgnoreCase)) return;
        var val = arg.Substring(key.Length + 1);
        if (float.TryParse(val, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            target = parsed;
            Debug.Log($"[SessionManager] {key} remplacé par args CLI: {parsed}");
        }
        else
            Debug.LogWarning($"[SessionManager] Argument {key} invalide: '{val}'");
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


