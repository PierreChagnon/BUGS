using UnityEngine;

public class DistalValleyScanView : MonoBehaviour
{
    [System.Serializable]
    struct CloudParticlePair
    {
        [SerializeField] private ParticleSystem _greenParticles;
        [SerializeField] private ParticleSystem _redParticles;

        public void Apply(BugCloudSample sample)
        {
            BugCloudParticleUtility.Apply(_greenParticles, _redParticles, sample);
        }
    }

    [SerializeField] private CloudParticlePair _firstCloud;
    [SerializeField] private CloudParticlePair _secondCloud;

    public void Apply(BugCloudPairData data)
    {
        _firstCloud.Apply(data.firstCloud);
        _secondCloud.Apply(data.secondCloud);
    }
}
