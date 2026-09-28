using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------
// Saisie du username sur l'ecran de fin de partie 1 (EndSessionScene).
//
// Le meme username est redemande au debut de la partie 2 : avec les
// timestamps, c'est la cle qui permet aux chercheurs de joindre les deux
// parties (retour Mark 24/09/2026, DEC-048).
//
// Le bouton vers la partie 2 reste inactif tant que le champ est vide. Son
// OnClick garde PlatformUrlDisplay.OpenPlatformUrl (branche dans l'Inspecteur) :
// ce script ajoute seulement l'envoi du username au meme clic. Le lien s'ouvre
// dans un nouvel onglet, l'onglet du jeu reste ouvert le temps de l'envoi.
//
// Contrat : POST /api/participant-usernames (docs/payload-examples.md).
// -----------------------------

public class ParticipantUsernameUI : MonoBehaviour
{
    // Meme borne que participantUsernameSchema (backend) et la contrainte DB.
    const int MaxLength = 64;

    [Tooltip("Champ de saisie du username.")]
    [SerializeField] private TMP_InputField _usernameInput;

    [Tooltip("Bouton vers la partie 2, inactif tant que le username est vide.")]
    [SerializeField] private Button _partTwoButton;

    TMP_Text _partTwoLabel;
    Color _partTwoLabelColor;

    void Awake()
    {
        if (_usernameInput == null || _partTwoButton == null)
        {
            Debug.LogError("[ParticipantUsernameUI] Assignez le champ username et le bouton partie 2 dans l'Inspector.");
            enabled = false;
            return;
        }

        _usernameInput.characterLimit = MaxLength;

        _partTwoLabel = _partTwoButton.GetComponentInChildren<TMP_Text>(true);
        if (_partTwoLabel != null)
            _partTwoLabelColor = _partTwoLabel.color;

        _usernameInput.onValueChanged.AddListener(HandleUsernameChanged);
        _partTwoButton.onClick.AddListener(SendUsername);
        HandleUsernameChanged(_usernameInput.text);
    }

    void OnDestroy()
    {
        if (_usernameInput != null)
            _usernameInput.onValueChanged.RemoveListener(HandleUsernameChanged);

        if (_partTwoButton != null)
            _partTwoButton.onClick.RemoveListener(SendUsername);
    }

    void HandleUsernameChanged(string value)
    {
        SetPartTwoInteractable(!string.IsNullOrWhiteSpace(value));
    }

    // Le Button ne teinte que son image : le libelle recoit la meme teinte
    // disabledColor, comme le bouton de la salle de pause.
    void SetPartTwoInteractable(bool interactable)
    {
        _partTwoButton.interactable = interactable;

        if (_partTwoLabel != null)
            _partTwoLabel.color = interactable
                ? _partTwoLabelColor
                : _partTwoLabelColor * _partTwoButton.colors.disabledColor;
    }

    void SendUsername()
    {
        var state = FlowController.Instance?.State;
        if (ApiClient.Instance == null || state == null)
        {
            Debug.LogError("[ParticipantUsernameUI] ApiClient ou etat de session absent : username non envoye.");
            return;
        }

        // Un second clic renvoie le username : le backend remplace la valeur (upsert).
        ApiClient.Instance.SendParticipantUsername(
            state.participant_id,
            state.session_template_id,
            _usernameInput.text,
            () => Debug.Log("[ParticipantUsernameUI] Username envoye."),
            error => Debug.LogError($"[ParticipantUsernameUI] Echec envoi username : {error}"));
    }
}
