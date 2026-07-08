using TMPro;
using UnityEngine;

public class ParticipantNoteUI : MonoBehaviour
{
    [Header("Saisie de la note")]
    [Tooltip("Champ de saisie contenant le texte de la note.")]
    [SerializeField] private TMP_Text _noteInputField;

    [Tooltip("GameObject parent regroupant le texte d'invite, la zone de saisie et le bouton Send. Masque tout le bloc apres envoi.")]
    [SerializeField] private GameObject _noteBlockRoot;

    [Header("Confirmation")]
    [Tooltip("GameObject (ex: TextMeshPro) affiche a la place du bloc de saisie une fois la note envoyee.")]
    [SerializeField] private GameObject _confirmationRoot;

    private void Awake()
    {
        // Etat initial : bloc de saisie visible, confirmation masquee.
        if (_confirmationRoot != null)
            _confirmationRoot.SetActive(false);
    }

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

        // On masque tout le bloc de saisie et on affiche la confirmation des le clic.
        ShowConfirmation();

        ApiClient.Instance.SendParticipantNote(
            participantId,
            sessionTemplateId,
            note,
            () => Debug.Log("[ParticipantNoteUI] Note envoyee avec succes."),
            error =>
            {
                Debug.LogWarning($"[ParticipantNoteUI] Echec envoi note: {error}");
                // En cas d'echec, on restaure le bloc de saisie.
                ShowNoteBlock();
            });
    }

    private void ShowConfirmation()
    {
        if (_noteBlockRoot != null)
            _noteBlockRoot.SetActive(false);
        if (_confirmationRoot != null)
            _confirmationRoot.SetActive(true);
    }

    private void ShowNoteBlock()
    {
        if (_confirmationRoot != null)
            _confirmationRoot.SetActive(false);
        if (_noteBlockRoot != null)
            _noteBlockRoot.SetActive(true);
    }
}
