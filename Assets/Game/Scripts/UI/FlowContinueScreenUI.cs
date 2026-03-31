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
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
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

        switch (_screenKind)
        {
            case ScreenKind.Welcome:
                SetTexts(
                    "Bienvenue",
                    $"Session: {flow.State.session_template_id}\nParticipant: {flow.State.participant_id}\n\nClique pour commencer.");
                break;

            case ScreenKind.Intro:
                string nextStep = flow.Config != null && flow.Config.tutorial_enabled
                    ? "Un bloc tutorial demarrera en premier pour te familiariser avec le flow."
                    : "Tu vas demarrer directement les blocs experimentaux.";
                SetTexts("Introduction", nextStep);
                break;

            case ScreenKind.EndSession:
                SetTexts("Session terminee", "Merci pour ta participation.");
                break;
        }
    }

    void SetTexts(string title, string body)
    {
        if (_titleText != null)
            _titleText.text = title;

        if (_bodyText != null)
            _bodyText.text = body;
    }

    public void OnContinueClicked()
    {
        FlowController.Instance?.OnPhaseComplete();
    }
}
