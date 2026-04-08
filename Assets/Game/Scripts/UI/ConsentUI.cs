using TMPro;
using UnityEngine;

public class ConsentUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private TMP_Text _statusText;

    void Start()
    {
        var flow = FlowController.Instance;
        if (flow == null)
            return;

        if (_titleText != null)
            _titleText.text = "Consentement";

        if (_bodyText != null)
            _bodyText.text = "Veuillez accepter le consentement pour demarrer la session.";

        if (_statusText != null)
            _statusText.text = string.Empty;
    }

    public void OnAcceptClicked()
    {
        FlowController.Instance?.OnConsentGiven();
    }

    public void OnDeclineClicked()
    {
        if (_statusText != null)
            _statusText.text = "Vous avez refuse le consentement. La session s'arrete ici.";
    }
}
