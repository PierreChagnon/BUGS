using TMPro;
using UnityEngine;

public class FlowContinueScreenUI : MonoBehaviour
{
    public enum ScreenKind
    {
        Welcome,
        Intro,
        EndSession
    }

    [SerializeField] private ScreenKind _screenKind;
    [SerializeField] private GameObject _continueButtonRoot;

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        var flow = FlowController.Instance;
        if (flow == null)
            return;

        if (_continueButtonRoot != null)
            _continueButtonRoot.SetActive(_screenKind != ScreenKind.EndSession);

        
    }


    public void OnContinueClicked()
    {
        FlowController.Instance?.OnPhaseComplete();
    }
}
