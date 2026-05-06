using UnityEngine;

public static class BugCloudParticleUtility
{
    public static void Apply(ParticleSystem greenParticles, ParticleSystem redParticles, BugCloudSample sample)
    {
        if (greenParticles == null || redParticles == null)
        {
            Debug.LogWarning("[BugCloudParticleUtility] Systèmes de particules manquants.");
            return;
        }

        int greenCount = sample.GreenBugCount;
        int redCount = sample.RedBugCount;

        var greenEmission = greenParticles.emission;
        var greenMain = greenParticles.main;
        greenMain.maxParticles = greenCount;
        greenEmission.rateOverTime = greenCount;

        var redEmission = redParticles.emission;
        var redMain = redParticles.main;
        redMain.maxParticles = redCount;
        redEmission.rateOverTime = redCount;
    }
}
