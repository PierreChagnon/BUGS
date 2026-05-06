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

    public int BestCloudIndex
    {
        get
        {
            int firstGreenBugs = firstCloud.GreenBugCount;
            int secondGreenBugs = secondCloud.GreenBugCount;
            return secondGreenBugs > firstGreenBugs ? 1 : 0;
        }
    }
}

public static class BugCloudGenerationUtility
{
    public static BugCloudPairData GenerateGameplayPair(MapGenConfig config, System.Random rng)
    {
        if (config == null)
            return default;

        if (rng == null)
            rng = new System.Random();

        int minTotalBugs = Mathf.Min(config.min_total_bugs, config.max_total_bugs);
        int maxTotalBugs = Mathf.Max(config.min_total_bugs, config.max_total_bugs);
        int totalBugs = rng.Next(minTotalBugs, maxTotalBugs + 1);

        float minGreenRatio = Mathf.Min(config.min_green_ratio, config.max_green_ratio);
        float maxGreenRatio = Mathf.Max(config.min_green_ratio, config.max_green_ratio);
        float gapMin = Mathf.Max(0f, Mathf.Min(config.gap_min, config.gap_max));
        float gapMax = Mathf.Max(gapMin, Mathf.Max(config.gap_min, config.gap_max));

        float ratio1 = Mathf.Lerp(minGreenRatio, maxGreenRatio, (float)rng.NextDouble());
        float sampledGap = Mathf.Lerp(gapMin, gapMax, (float)rng.NextDouble());
        float ratio2 = rng.NextDouble() < 0.5
            ? ratio1 + sampledGap
            : ratio1 - sampledGap;

        ratio1 = Mathf.Clamp01(ratio1);
        ratio2 = Mathf.Clamp01(ratio2);

        var firstCloud = new BugCloudSample { totalBugs = totalBugs, greenRatio = ratio1 };
        var secondCloud = new BugCloudSample { totalBugs = totalBugs, greenRatio = ratio2 };

        if (rng.NextDouble() < 0.5)
            return new BugCloudPairData { firstCloud = firstCloud, secondCloud = secondCloud, ratioGap = Mathf.Abs(ratio1 - ratio2) };

        return new BugCloudPairData { firstCloud = secondCloud, secondCloud = firstCloud, ratioGap = Mathf.Abs(ratio1 - ratio2) };
    }

    public static BugCloudPairData GenerateRepresentativePair(MapGenConfig config)
    {
        if (config == null)
            return default;

        int minTotalBugs = Mathf.Min(config.min_total_bugs, config.max_total_bugs);
        int maxTotalBugs = Mathf.Max(config.min_total_bugs, config.max_total_bugs);
        int totalBugs = Mathf.RoundToInt((minTotalBugs + maxTotalBugs) * 0.5f);

        float minGreenRatio = Mathf.Min(config.min_green_ratio, config.max_green_ratio);
        float maxGreenRatio = Mathf.Max(config.min_green_ratio, config.max_green_ratio);
        float averageGreenRatio = (minGreenRatio + maxGreenRatio) * 0.5f;
        float gapMin = Mathf.Max(0f, Mathf.Min(config.gap_min, config.gap_max));
        float gapMax = Mathf.Max(gapMin, Mathf.Max(config.gap_min, config.gap_max));
        float averageGap = (gapMin + gapMax) * 0.5f;

        float firstRatio = Mathf.Clamp(averageGreenRatio - averageGap * 0.5f, minGreenRatio, maxGreenRatio);
        float secondRatio = Mathf.Clamp(averageGreenRatio + averageGap * 0.5f, minGreenRatio, maxGreenRatio);

        if (secondRatio < firstRatio)
        {
            float tmp = firstRatio;
            firstRatio = secondRatio;
            secondRatio = tmp;
        }

        if (ShouldSwapRepresentativeClouds(config))
        {
            float tmp = firstRatio;
            firstRatio = secondRatio;
            secondRatio = tmp;
        }

        return new BugCloudPairData
        {
            firstCloud = new BugCloudSample { totalBugs = totalBugs, greenRatio = firstRatio },
            secondCloud = new BugCloudSample { totalBugs = totalBugs, greenRatio = secondRatio },
            ratioGap = Mathf.Abs(firstRatio - secondRatio)
        };
    }

    static bool ShouldSwapRepresentativeClouds(MapGenConfig config)
    {
        if (config == null)
            return false;

        unchecked
        {
            long seed = config.seed != 0 ? config.seed : ComputeFallbackSeed(config);
            return (seed & 1L) == 0L;
        }
    }

    static long ComputeFallbackSeed(MapGenConfig config)
    {
        unchecked
        {
            int totalHash = config.min_total_bugs * 397 ^ config.max_total_bugs;
            int ratioHash = config.min_green_ratio.GetHashCode() * 397 ^ config.max_green_ratio.GetHashCode();
            int gapHash = config.gap_min.GetHashCode() * 397 ^ config.gap_max.GetHashCode();
            return (totalHash ^ ratioHash ^ gapHash) & 0x7FFFFFFF;
        }
    }
}
