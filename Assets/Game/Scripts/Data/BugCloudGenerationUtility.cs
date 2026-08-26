using System;
using UnityEngine;

[Serializable]
public struct BugCloudSample
{
    public int totalBugs;
    public float greenRatio;

    public int GreenBugCount => Mathf.RoundToInt(totalBugs * greenRatio);
    public int RedBugCount => Mathf.Max(0, totalBugs - GreenBugCount);
}

[Serializable]
public struct BugCloudPairData
{
    public BugCloudSample firstCloud;
    public BugCloudSample secondCloud;
    public float ratioGap;
}

public static class BugCloudGenerationUtility
{
    public static BugCloudPairData GenerateGameplayPair(MapGenConfig config, System.Random rng)
    {
        if (config == null)
            return default;

        if (rng == null)
            rng = new System.Random();

        BugCloudPairData orderedPair = GenerateOrderedPair(
            config.min_total_bugs,
            config.max_total_bugs,
            config.min_green_ratio,
            config.max_green_ratio,
            config.gap_min,
            config.gap_max,
            rng);

        if (rng.NextDouble() < 0.5)
            return orderedPair;

        return new BugCloudPairData
        {
            firstCloud = orderedPair.secondCloud,
            secondCloud = orderedPair.firstCloud,
            ratioGap = orderedPair.ratioGap
        };
    }

    public static BugCloudPairData GenerateDistalScanPair(DistalSceneConfig config, string bestScanSide, System.Random rng)
    {
        if (config == null)
            return default;

        if (rng == null)
            rng = new System.Random();

        BugCloudPairData orderedPair = GenerateOrderedPair(
            config.min_total_bugs,
            config.max_total_bugs,
            config.min_green_ratio,
            config.max_green_ratio,
            config.gap_min,
            config.gap_max,
            rng);

        bool bestScanIsLeft = bestScanSide == DistalScanSide.Left;
        return new BugCloudPairData
        {
            firstCloud = bestScanIsLeft ? orderedPair.secondCloud : orderedPair.firstCloud,
            secondCloud = bestScanIsLeft ? orderedPair.firstCloud : orderedPair.secondCloud,
            ratioGap = orderedPair.ratioGap
        };
    }

    static BugCloudPairData GenerateOrderedPair(
        int minTotalBugs,
        int maxTotalBugs,
        float minGreenRatio,
        float maxGreenRatio,
        float gapMin,
        float gapMax,
        System.Random rng)
    {
        int minTotal = Mathf.Min(minTotalBugs, maxTotalBugs);
        int maxTotal = Mathf.Max(minTotalBugs, maxTotalBugs);
        int totalBugs = rng.Next(minTotal, maxTotal + 1);

        SampleGappedRatios(minGreenRatio, maxGreenRatio, gapMin, gapMax, rng, out float lowerRatio, out float higherRatio);

        var lowerCloud = new BugCloudSample { totalBugs = totalBugs, greenRatio = lowerRatio };
        var higherCloud = new BugCloudSample { totalBugs = totalBugs, greenRatio = higherRatio };

        return new BugCloudPairData
        {
            firstCloud = lowerCloud,
            secondCloud = higherCloud,
            ratioGap = Mathf.Abs(higherRatio - lowerRatio)
        };
    }

    static void SampleGappedRatios(
        float minGreenRatio,
        float maxGreenRatio,
        float gapMin,
        float gapMax,
        System.Random rng,
        out float lowerRatio,
        out float higherRatio)
    {
        float minRatio = Mathf.Clamp01(Mathf.Min(minGreenRatio, maxGreenRatio));
        float maxRatio = Mathf.Clamp01(Mathf.Max(minGreenRatio, maxGreenRatio));
        float minGap = Mathf.Max(0f, Mathf.Min(gapMin, gapMax));
        float maxGap = Mathf.Max(minGap, Mathf.Max(gapMin, gapMax));
        float gap = Mathf.Lerp(minGap, maxGap, (float)rng.NextDouble());

        gap = Mathf.Min(gap, Mathf.Max(maxRatio, 1f - minRatio));
        bool addGap = rng.NextDouble() < 0.5;

        if (!TrySampleBaseRatio(minRatio, maxRatio, gap, addGap, rng, out float baseRatio))
        {
            addGap = !addGap;
            TrySampleBaseRatio(minRatio, maxRatio, gap, addGap, rng, out baseRatio);
        }

        float otherRatio = addGap ? baseRatio + gap : baseRatio - gap;
        lowerRatio = Mathf.Min(baseRatio, otherRatio);
        higherRatio = Mathf.Max(baseRatio, otherRatio);
    }

    static bool TrySampleBaseRatio(
        float minRatio,
        float maxRatio,
        float gap,
        bool addGap,
        System.Random rng,
        out float baseRatio)
    {
        float minBase = addGap ? minRatio : Mathf.Max(minRatio, gap);
        float maxBase = addGap ? Mathf.Min(maxRatio, 1f - gap) : maxRatio;

        if (minBase > maxBase)
        {
            baseRatio = 0f;
            return false;
        }

        baseRatio = Mathf.Lerp(minBase, maxBase, (float)rng.NextDouble());
        return true;
    }
}
