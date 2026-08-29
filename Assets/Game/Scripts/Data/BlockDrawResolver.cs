using UnityEngine;

// -----------------------------
// Tirages probabilistes de niveau bloc (seed distale du bloc, un salt par
// tirage). FlowController crée les RNG et applique les résultats dans
// PlayerSessionState ; la logique de tirage vit ici (règle projet :
// résolution dans Data/).
// -----------------------------

public static class BlockDrawResolver
{
    public struct DistalForcedDraw
    {
        public bool is_forced;
        public string forced_scan_side;
        public bool forced_was_optimal;
        public float optimal_probability;
    }

    public struct DistalAdviceDraw
    {
        public bool visible;
        public bool reliable;
        public string choice;
    }

    // Salt 0 : côté du meilleur scan distal.
    public static string DrawBestValleySide(System.Random rng)
    {
        return rng.NextDouble() < 0.5 ? DistalScanSide.Left : DistalScanSide.Right;
    }

    // Salt 6 : genre du conseiller humain affiché par les badges advisor
    // (un seul tirage par bloc, consommé par toutes les UI via AdvisorBadgeUtility).
    public static bool DrawAdvisorDisplayIsMale(System.Random rng)
    {
        return rng.NextDouble() < 0.5;
    }

    // Salt 3 : forçage du choix distal.
    public static DistalForcedDraw DrawDistalForced(BlockConfig block, string bestValleySide, System.Random rng)
    {
        if (block == null || !block.distal_forced)
            return default;

        float optimalProbability = Mathf.Clamp01(block.distal_forced_optimal_probability);
        bool forcedWasOptimal = rng.NextDouble() < optimalProbability;

        return new DistalForcedDraw
        {
            is_forced = true,
            forced_scan_side = forcedWasOptimal ? bestValleySide : DistalScanSide.Opposite(bestValleySide),
            forced_was_optimal = forcedWasOptimal,
            optimal_probability = optimalProbability
        };
    }

    // Salt 4 : visibilité puis fiabilité du conseil distal. En distal forced,
    // le conseil pointe le scan imposé et est fiable ssi le forçage était
    // optimal (aucun tirage de fiabilité consommé dans ce cas).
    public static DistalAdviceDraw DrawDistalAdvice(
        BlockConfig block,
        bool choiceIsForced,
        string forcedScanSide,
        bool forcedWasOptimal,
        string bestValleySide,
        System.Random rng)
    {
        float visibleProbability = Mathf.Clamp01(block.distal_advice_visible_probability);
        if (rng.NextDouble() >= visibleProbability)
            return default;

        if (choiceIsForced)
        {
            return new DistalAdviceDraw
            {
                visible = true,
                reliable = forcedWasOptimal,
                choice = forcedScanSide
            };
        }

        float reliableProbability = Mathf.Clamp01(block.distal_advice_reliable_probability);
        bool reliable = rng.NextDouble() < reliableProbability;

        return new DistalAdviceDraw
        {
            visible = true,
            reliable = reliable,
            choice = reliable ? bestValleySide : DistalScanSide.Opposite(bestValleySide)
        };
    }

    // Salt 1 : vallée la plus payante du bloc (tie-break aléatoire).
    public static ValleyChoice DrawMostRewardingValley(BlockConfig block, System.Random rng)
    {
        float valleyAExpectedGreenBugs = ComputeExpectedGreenBugs(block?.valley_a);
        float valleyBExpectedGreenBugs = ComputeExpectedGreenBugs(block?.valley_b);

        if (Mathf.Approximately(valleyAExpectedGreenBugs, valleyBExpectedGreenBugs))
            return rng.NextDouble() < 0.5 ? ValleyChoice.A : ValleyChoice.B;

        return valleyAExpectedGreenBugs > valleyBExpectedGreenBugs
            ? ValleyChoice.A
            : ValleyChoice.B;
    }

    static float ComputeExpectedGreenBugs(MapGenConfig map)
    {
        if (map == null)
            return 0f;

        float averageTotalBugs = (map.min_total_bugs + map.max_total_bugs) * 0.5f;
        float averageGreenRatio = (map.min_green_ratio + map.max_green_ratio) * 0.5f;
        return averageTotalBugs * averageGreenRatio;
    }
}
