using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Composant léger à attacher sur chaque prefab d'advisor.
/// Transmet le type d'advisor correspondant au FlowController lors du clic.
/// </summary>
public class AdvisorOptionButton : MonoBehaviour
{
    [SerializeField] private AdvisorType _advisorType;

    void Start()
    {
        bool allowed = FlowController.Instance == null || FlowController.Instance.IsAdvisorChoiceAllowed(_advisorType);

        var button = GetComponentInChildren<Button>(true);
        if (button != null)
            button.interactable = allowed;

        var group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        group.alpha = allowed ? 1f : 0.35f;
        group.interactable = allowed;
    }

    public void OnClick()
    {
        if (FlowController.Instance != null && !FlowController.Instance.IsAdvisorChoiceAllowed(_advisorType))
            return;

        FlowController.Instance?.OnAdvisorChosen(_advisorType);
    }
}
