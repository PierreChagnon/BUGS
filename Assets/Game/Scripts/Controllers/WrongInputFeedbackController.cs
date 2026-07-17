using UnityEngine;

// Couche de présentation du cooldown de mauvais input.
// Ce composant doit rester actif et piloter un GameObject enfant dédié au feedback.
public class WrongInputFeedbackController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("GameObject affichant le message (par exemple 'WRONG INPUT'). Doit être distinct de l'objet portant ce composant.")]
    private GameObject _feedbackRoot;

    void Awake()
    {
        SetFeedbackVisible(false);
    }

    void Start()
    {
        SubscribeToGameManager();
    }

    void OnEnable()
    {
        SubscribeToGameManager();
    }

    void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    void SubscribeToGameManager()
    {
        if (GameManager.Instance == null)
            return;

        // Evite un double abonnement entre OnEnable et Start.
        GameManager.Instance.OnWrongInputCooldownChanged -= SetFeedbackVisible;
        GameManager.Instance.OnWrongInputCooldownChanged += SetFeedbackVisible;
        SetFeedbackVisible(GameManager.Instance.IsWrongInputCooldownActive);
    }

    void UnsubscribeFromGameManager()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnWrongInputCooldownChanged -= SetFeedbackVisible;
    }

    void SetFeedbackVisible(bool visible)
    {
        if (_feedbackRoot == null)
            return;

        if (_feedbackRoot == gameObject)
        {
            Debug.LogError("[WrongInputFeedbackController] Le feedback doit être un GameObject enfant distinct de l'objet portant le contrôleur.", this);
            return;
        }

        _feedbackRoot.SetActive(visible);
    }
}
