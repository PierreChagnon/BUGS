using UnityEngine;

// Couche de présentation du feedback de pénalité : s'abonne à BugCloud.OnBugsLost et fait
// jaillir un petit nombre rouge ("-N") au-dessus du nuage concerné, à chaque perte réelle.
// BugCloud reste la donnée ; ce contrôleur centralise tout le visuel (prefab, anim, couleur).
public class PenaltyFeedbackController : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField]
    [Tooltip("Prefab du nombre flottant (doit porter un composant FloatingPenaltyNumber).")]
    private FloatingPenaltyNumber _penaltyNumberPrefab;

    [Header("Placement")]
    [SerializeField]
    [Tooltip("Hauteur d'apparition au-dessus du centre du nuage (unités monde).")]
    private float _verticalOffset = 0.8f;
    [SerializeField]
    [Tooltip("Décalage horizontal aléatoire pour éviter que des '-1' en cascade se superposent.")]
    private float _horizontalJitter = 0.15f;

    [Header("Animation")]
    [SerializeField]
    [Tooltip("Distance de montée du nombre avant disparition (unités monde).")]
    private float _riseDistance = 1f;
    [SerializeField]
    [Tooltip("Durée de l'animation montée + fondu (secondes).")]
    private float _duration = 0.9f;
    [SerializeField]
    [Tooltip("Couleur du nombre de pénalité.")]
    private Color _color = Color.red;

    void OnEnable()
    {
        BugCloud.OnBugsLost += HandleBugsLost;
    }

    void OnDisable()
    {
        // Désabonnement obligatoire : OnBugsLost est statique. Sans cela, un contrôleur détruit
        // resterait abonné au rechargement de scène (fuite + double feedback).
        BugCloud.OnBugsLost -= HandleBugsLost;
    }

    void HandleBugsLost(BugCloud cloud, int amount)
    {
        if (cloud == null || _penaltyNumberPrefab == null)
            return;

        // Ne pas trahir la position d'un nuage masqué.
        if (!cloud.IsVisible)
            return;

        Vector3 pos = cloud.transform.position + Vector3.up * _verticalOffset;
        if (_horizontalJitter > 0f)
        {
            pos.x += Random.Range(-_horizontalJitter, _horizontalJitter);
            pos.z += Random.Range(-_horizontalJitter, _horizontalJitter);
        }

        var number = Instantiate(_penaltyNumberPrefab, pos, Quaternion.identity);
        number.Play(amount, _color, _duration, _riseDistance);
    }
}
