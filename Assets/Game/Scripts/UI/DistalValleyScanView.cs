using UnityEngine;

public class DistalValleyScanView : MonoBehaviour
{
    [SerializeField] private ParticleSystem _greenParticles;
    [SerializeField] private ParticleSystem _redParticles;

    public void Apply(BugCloudSample sample)
    {
        Debug.Log(
            "[DistalValleyScanView] Apply | " +
            $"view='{name}' | " +
            $"greenPS={(_greenParticles != null ? _greenParticles.name : "null")} | " +
            $"redPS={(_redParticles != null ? _redParticles.name : "null")} | " +
            $"total={sample.totalBugs} | greenRatio={sample.greenRatio:0.000} | " +
            $"greenCount={sample.GreenBugCount} | redCount={sample.RedBugCount}",
            this);
        BugCloudParticleUtility.Apply(_greenParticles, _redParticles, sample);
    }
}
