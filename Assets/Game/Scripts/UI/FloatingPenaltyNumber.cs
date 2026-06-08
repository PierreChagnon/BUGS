using System.Collections;
using TMPro;
using UnityEngine;

// Petit nombre rouge qui jaillit d'un nuage quand une pénalité retire des bugs verts.
// Porté par un prefab world-space. Le nombre monte, s'estompe, puis se détruit.
// Spawné et configuré par PenaltyFeedbackController.
public class FloatingPenaltyNumber : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Texte 3D (TextMeshPro, pas UGUI) affichant le nombre perdu.")]
    private TextMeshPro _text;

    private Camera _camera;

    void Awake()
    {
        if (_text == null)
            _text = GetComponentInChildren<TextMeshPro>();

        _camera = Camera.main;
    }

    // Affiche "-amount" puis lance l'animation montée + fondu.
    public void Play(int amount, Color color, float duration, float riseDistance)
    {
        if (_text != null)
        {
            _text.text = "-" + amount;
            _text.color = color;
        }

        StartCoroutine(RiseAndFade(duration, riseDistance));
    }

    void LateUpdate()
    {
        // Billboard : le nombre fait face à la caméra et ne tourne pas avec le nuage
        // (qui pivote sur Y, cf. BugCloud.Update).
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
                return;
        }

        transform.rotation = _camera.transform.rotation;
    }

    IEnumerator RiseAndFade(float duration, float riseDistance)
    {
        Vector3 startPos = transform.position;
        Color startColor = _text != null ? _text.color : Color.white;
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            transform.position = startPos + Vector3.up * (Mathf.SmoothStep(0f, 1f, t) * riseDistance);

            if (_text != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                _text.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
