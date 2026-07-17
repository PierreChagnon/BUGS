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
    [SerializeField] private GameObject _consentDeclinedRoot;

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

        if (_consentDeclinedRoot != null)
            _consentDeclinedRoot.SetActive(
                _screenKind == ScreenKind.Welcome && flow.WasConsentDeclined);
    }


    public void OnContinueClicked()
    {
        FlowController.Instance?.OnPhaseComplete();
    }
}
