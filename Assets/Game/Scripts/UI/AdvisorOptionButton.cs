using UnityEngine;

/// <summary>
/// Composant léger à attacher sur chaque prefab d'advisor.
/// Transmet le type d'advisor correspondant au FlowController lors du clic.
/// </summary>
public class AdvisorOptionButton : MonoBehaviour
{
    [SerializeField] private AdvisorType _advisorType;

    public void OnClick()
    {
        Debug.Log($"AdvisorOptionButton: {_advisorType} clicked");
        FlowController.Instance?.OnAdvisorChosen(_advisorType);
    }
}
