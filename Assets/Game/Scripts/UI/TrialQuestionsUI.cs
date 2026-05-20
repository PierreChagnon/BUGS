using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Affiche les questions de fin de trial dans la ProximalScene,
// avant d'afficher le panneau de resultats (RoundUI).
// 2 questions apres chaque trial, + 1 en fin de dernier trial du bloc.
public class TrialQuestionsUI : MonoBehaviour
{
    struct TrialQuestionItem
    {
        public string key;
        public string text;

        public TrialQuestionItem(string key, string text)
        {
            this.key = key;
            this.text = text;
        }
    }

    [Header("Textes des questions")]
    [SerializeField] private string _acceptabilityQuestion = "Question d'acceptabilite (a definir)";
    [SerializeField] private string _senseOfAgencyQuestion = "Question de sens d'agentivite (a definir)";
    [SerializeField] private string _humanLikenessQuestion = "Question de ressemblance humaine (a definir)";

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Toggle[] _choiceToggles; // 5 toggles : strongly disagree → strongly agree
    [SerializeField] private Button _confirmButton;
    [SerializeField] private RoundUI _roundUI;

    GameManager.RoundEndInfo _pendingInfo;
    readonly List<TrialQuestionItem> _questions = new();
    readonly List<QuestionResponse> _responses = new();
    int _currentIndex;

    void Start()
    {
        if (_panel != null)
            _panel.SetActive(false);

        for (int i = 0; i < _choiceToggles.Length; i++)
        {
            int index = i;
            _choiceToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn) DeselectAllExcept(index);
                RefreshConfirmButton();
            });
        }

        if (_confirmButton != null)
            _confirmButton.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded += HandleRoundEnded;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded -= HandleRoundEnded;
    }

    void HandleRoundEnded(GameManager.RoundEndInfo info)
    {
        _pendingInfo = info;
        _questions.Clear();
        _responses.Clear();
        _currentIndex = 0;

        var flow = FlowController.Instance;

        // Pas de questions en mode tutorial
        if (flow == null || flow.CurrentBlock == null || flow.CurrentBlock.is_tutorial)
        {
            _roundUI?.Show(info);
            return;
        }

        _questions.Add(new TrialQuestionItem(FlowSerializationUtility.AcceptabilityQuestionKey, _acceptabilityQuestion));
        _questions.Add(new TrialQuestionItem(FlowSerializationUtility.SensOfAgencyQuestionKey, _senseOfAgencyQuestion));

        bool isLastTrial = flow.State.current_trial_index >= flow.CurrentBlock.trial_count - 1;
        if (isLastTrial)
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.HumanLikenessQuestionKey, _humanLikenessQuestion));

        var rng = new System.Random((int)flow.CurrentTrialSeed);
        for (int i = _questions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_questions[i], _questions[j]) = (_questions[j], _questions[i]);
        }

        if (_panel != null)
            _panel.SetActive(true);

        ShowCurrentQuestion();
    }

    public void OnConfirmClicked()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;
        int selected = GetSelectedIndex();
        if (selected < 0) return;

        _responses.Add(new QuestionResponse
        {
            order = _currentIndex + 1,
            question_key = _questions[_currentIndex].key,
            question_text = _questions[_currentIndex].text,
            response = (selected + 1).ToString() // 1=strongly disagree … 5=strongly agree
        });

        _currentIndex++;
        if (_currentIndex >= _questions.Count)
        {
            SubmitAndFinish();
            return;
        }

        ShowCurrentQuestion();
    }

    void ShowCurrentQuestion()
    {
        if (_progressText != null)
            _progressText.text = $"{_currentIndex + 1} / {_questions.Count}";

        if (_questionText != null)
            _questionText.text = _questions[_currentIndex].text;

        foreach (var toggle in _choiceToggles)
            toggle.isOn = false;

        if (_confirmButton != null)
            _confirmButton.interactable = false;
    }

    void RefreshConfirmButton()
    {
        if (_confirmButton != null)
            _confirmButton.interactable = GetSelectedIndex() >= 0;
    }

    void DeselectAllExcept(int keepIndex)
    {
        for (int i = 0; i < _choiceToggles.Length; i++)
            if (i != keepIndex)
                _choiceToggles[i].isOn = false;
    }

    int GetSelectedIndex()
    {
        for (int i = 0; i < _choiceToggles.Length; i++)
            if (_choiceToggles[i].isOn)
                return i;
        return -1;
    }

    void SubmitAndFinish()
    {
        var trialManager = GameManager.Instance != null ? GameManager.Instance.trialManager : null;
        if (trialManager != null)
            trialManager.SubmitCurrentTrialResponses(_responses);
        else
            Debug.LogWarning("[TrialQuestionsUI] TrialManager introuvable: impossible d'envoyer les reponses.");

        if (_panel != null)
            _panel.SetActive(false);

        _roundUI?.Show(_pendingInfo);
    }
}
