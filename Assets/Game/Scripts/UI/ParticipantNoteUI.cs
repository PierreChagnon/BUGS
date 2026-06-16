using TMPro;
using UnityEngine;

public class ParticipantNoteUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _noteInputField;
    [SerializeField] private GameObject _sendButtonRoot;

    public void OnSendClicked()
    {
        if (_noteInputField == null)
            return;

        string note = _noteInputField.text?.Trim();
        if (string.IsNullOrEmpty(note))
            return;

        if (ApiClient.Instance == null)
        {
            Debug.LogWarning("[ParticipantNoteUI] ApiClient introuvable.");
            return;
        }

        var flow = FlowController.Instance;
        string participantId = flow?.State?.participant_id;
        string sessionTemplateId = flow?.State?.session_template_id;

        if (_sendButtonRoot != null)
            _sendButtonRoot.SetActive(false);

        ApiClient.Instance.SendParticipantNote(
            participantId,
            sessionTemplateId,
            note,
            () => Debug.Log("[ParticipantNoteUI] Note envoyee avec succes."),
            error =>
            {
                Debug.LogWarning($"[ParticipantNoteUI] Echec envoi note: {error}");
                if (_sendButtonRoot != null)
                    _sendButtonRoot.SetActive(true);
            });
    }
}
