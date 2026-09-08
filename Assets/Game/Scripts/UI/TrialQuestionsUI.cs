using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Affiche les questions de fin de trial dans la ProximalScene,
// apres le panneau de resultats (RoundUI).
// 3 questions apres chaque trial (2 d'acceptabilite, 1 de sens d'agentivite), + 1 en fin de dernier trial du bloc.
// L'ordre des ecrans est melange a partir du seed du trial.
// Deux echelles de reponse : un slider entier 1-7 pour l'acceptabilite, 7 boutons radio pour les autres.
// Les deux produisent une reponse "1"..."7".
public class TrialQuestionsUI : MonoBehaviour
{
    struct TrialQuestionItem
    {
        public string key;
        public string text;
        public bool useSlider;

        public TrialQuestionItem(string key, string text, bool useSlider)
        {
            this.key = key;
            this.text = text;
            this.useSlider = useSlider;
        }
    }

    [Header("Textes des questions")]
    [SerializeField] private string _acceptabilityQuestion1 = "Question d'acceptabilite 1 (a definir)";
    [SerializeField] private string _acceptabilityQuestion2 = "Question d'acceptabilite 2 (a definir)";
    [SerializeField] private string _senseOfAgencyQuestion = "Question de sens d'agentivite (a definir)";
    [SerializeField] private string _humanLikenessQuestion = "Question de ressemblance humaine (a definir)";

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _progressText;
    [Tooltip("Rangee des boutons radio (AnswerRow)")]
    [SerializeField] private GameObject _answerRow;
    [SerializeField] private Toggle[] _choiceToggles; // 7 toggles : strongly disagree → strongly agree
    [Tooltip("Rangee du slider (SliderRow), contient le Slider ScaleSlider")]
    [SerializeField] private GameObject _sliderRow;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private RoundUI _roundUI;

    readonly List<TrialQuestionItem> _questions = new();
    readonly List<QuestionResponse> _responses = new();
    Slider _scaleSlider;
    int _currentIndex;
    bool _hasPreparedQuestions;

    void Start()
    {
        if (_panel != null)
            _panel.SetActive(false);

        if (_sliderRow != null)
            _scaleSlider = _sliderRow.GetComponentInChildren<Slider>(true);
        if (_scaleSlider == null)
            Debug.LogError("[TrialQuestionsUI] Slider introuvable sous _sliderRow: les questions d'acceptabilite ne pourront pas etre repondues.");

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

        if (_roundUI == null)
            _roundUI = FindFirstObjectByType<RoundUI>();

        if (_roundUI != null)
            _roundUI.OnContinueFromReport += HandleMissionReportContinue;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded -= HandleRoundEnded;

        if (_roundUI != null)
            _roundUI.OnContinueFromReport -= HandleMissionReportContinue;
    }

    void HandleRoundEnded(GameManager.RoundEndInfo info)
    {
        _questions.Clear();
        _responses.Clear();
        _currentIndex = 0;
        _hasPreparedQuestions = false;

        var flow = FlowController.Instance;

        if (flow == null || flow.CurrentBlock == null)
            return;

        // Les questions d'acceptabilite portent sur l'advisor : elles n'ont pas de sens si aucun advisor n'a ete choisi (none).
        if (flow.State.advisor_choice != AdvisorType.None)
        {
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.AcceptabilityQuestion1Key, _acceptabilityQuestion1, useSlider: true));
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.AcceptabilityQuestion2Key, _acceptabilityQuestion2, useSlider: true));
        }

        _questions.Add(new TrialQuestionItem(FlowSerializationUtility.SensOfAgencyQuestionKey, _senseOfAgencyQuestion, useSlider: false));

        bool isLastTrial = flow.State.current_trial_index >= flow.CurrentBlock.trial_count - 1;
        if (isLastTrial)
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.HumanLikenessQuestionKey, _humanLikenessQuestion, useSlider: false));

        var rng = new System.Random((int)flow.CurrentTrialSeed);
        for (int i = _questions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_questions[i], _questions[j]) = (_questions[j], _questions[i]);
        }

        _hasPreparedQuestions = _questions.Count > 0;
    }

    void HandleMissionReportContinue()
    {
        if (!_hasPreparedQuestions)
        {
            GameManager.Instance?.ContinueAfterRound();
            return;
        }

        _roundUI?.Hide();

        if (_panel != null)
            _panel.SetActive(true);

        ShowCurrentQuestion();
    }

    public void OnConfirmClicked()
    {
        if (_currentIndex < 0 || _currentIndex >= _questions.Count) return;

        var question = _questions[_currentIndex];
        string response;
        if (question.useSlider)
        {
            if (_scaleSlider == null) return;
            response = Mathf.RoundToInt(_scaleSlider.value).ToString(); // slider entier 1…7
        }
        else
        {
            int selected = GetSelectedIndex();
            if (selected < 0) return;
            response = (selected + 1).ToString(); // 1=strongly disagree … 7=strongly agree
        }

        _responses.Add(new QuestionResponse
        {
            order = _currentIndex + 1,
            question_key = question.key,
            question_text = question.text,
            response = response
        });

        _currentIndex++;
        if (_currentIndex >= _questions.Count)
        {
            SubmitAndContinue();
            return;
        }

        ShowCurrentQuestion();
    }

    void ShowCurrentQuestion()
    {
        if (_progressText != null)
            _progressText.text = $"{_currentIndex + 1} / {_questions.Count}";

        var question = _questions[_currentIndex];

        if (_questionText != null)
            _questionText.text = question.text;

        if (_answerRow != null)
            _answerRow.SetActive(!question.useSlider);
        if (_sliderRow != null)
            _sliderRow.SetActive(question.useSlider);

        foreach (var toggle in _choiceToggles)
            toggle.isOn = false;

        // Le slider a toujours une valeur : il repart du milieu de l'echelle et la validation est immediate.
        if (_scaleSlider != null)
            _scaleSlider.value = Mathf.Round((_scaleSlider.minValue + _scaleSlider.maxValue) / 2f);

        if (_confirmButton != null)
            _confirmButton.interactable = question.useSlider;
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

    void SubmitAndContinue()
    {
        var trialManager = GameManager.Instance != null ? GameManager.Instance.trialManager : null;
        if (trialManager != null)
            trialManager.SubmitCurrentTrialResponses(_responses);
        else
            Debug.LogWarning("[TrialQuestionsUI] TrialManager introuvable: impossible d'envoyer les reponses.");

        if (_panel != null)
            _panel.SetActive(false);

        _hasPreparedQuestions = false;
        GameManager.Instance?.ContinueAfterRound();
    }
}
