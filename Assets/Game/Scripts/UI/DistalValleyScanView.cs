using UnityEngine;

public class DistalValleyScanView : MonoBehaviour
{
    [SerializeField] private ParticleSystem _greenParticles;
    [SerializeField] private ParticleSystem _redParticles;

    public void Apply(BugCloudSample sample)
    {
        BugCloudParticleUtility.Apply(_greenParticles, _redParticles, sample);
    }
}
