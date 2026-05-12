using UnityEngine;
using UnityEngine.UI;

// -----------------------------
// Composant à coller sur n'importe quel Button UI.
// Joue le SFX configuré à chaque clic, routé sur le groupe SFX_UI du mixer.
// -----------------------------

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Tooltip("SFX joué au clic. SoundEffect.channel doit être UI pour un routage correct.")]
    [SerializeField] private SoundEffect _clickSound;

    void Awake()
    {
        if (_clickSound == null)
            return;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(_clickSound);
    }
}
