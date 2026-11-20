using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Affiche les questions de fin de trial dans la ProximalScene,
// apres le panneau de resultats (RoundUI).
// 3 questions apres chaque trial (2 d'acceptabilite, 1 de sens d'agentivite), + 1 en fin de dernier trial du bloc.
// L'ordre des ecrans est melange a partir du seed du trial.
// Toutes les questions se repondent sur le meme slider entier 1-7 (DEC-053), avec
// les ancres de leur echelle aux extremites. La reponse vaut "1"..."7".
public class TrialQuestionsUI : MonoBehaviour
{
    struct TrialQuestionItem
    {
        public string key;
        public string text;
        public string minLabel;
        public string maxLabel;

        public TrialQuestionItem(string key, string text, string minLabel, string maxLabel)
        {
            this.key = key;
            this.text = text;
            this.minLabel = minLabel;
            this.maxLabel = maxLabel;
        }
    }

    [Header("Textes des questions")]
    [SerializeField] private string _acceptabilityQuestion1 = "Question d'acceptabilite 1 (a definir)";
    [SerializeField] private string _acceptabilityQuestion2 = "Question d'acceptabilite 2 (a definir)";
    [SerializeField] private string _senseOfAgencyQuestion = "Question de sens d'agentivite (a definir)";
    [SerializeField] private string _humanLikenessQuestion = "Question de ressemblance humaine (a definir)";

    [Header("Ancres du slider (sous le 1 et sous le 7)")]
    [SerializeField] private string _acceptabilityMinLabel = "not at all";
    [SerializeField] private string _acceptabilityMaxLabel = "completely";
    [SerializeField] private string _senseOfAgencyMinLabel = "No Control";
    [SerializeField] private string _senseOfAgencyMaxLabel = "Full Control";
    [SerializeField] private string _humanLikenessMinLabel = "Not at all human";
    [SerializeField] private string _humanLikenessMaxLabel = "Completely human";

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _progressText;
    [Tooltip("Rangee du slider (SliderRow), contient le Slider ScaleSlider")]
    [SerializeField] private GameObject _sliderRow;
    [Tooltip("Etiquette sous l'extremite 1 du slider (LabelMin, sous SliderRow)")]
    [SerializeField] private TMP_Text _labelMin;
    [Tooltip("Etiquette sous l'extremite 7 du slider (LabelMax, sous SliderRow)")]
    [SerializeField] private TMP_Text _labelMax;
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
        {
            _sliderRow.SetActive(true);
            _scaleSlider = _sliderRow.GetComponentInChildren<Slider>(true);
        }
        if (_scaleSlider == null)
            Debug.LogError("[TrialQuestionsUI] Slider introuvable sous _sliderRow: les questions ne pourront pas etre repondues.");
        if (_labelMin == null || _labelMax == null)
            Debug.LogError("[TrialQuestionsUI] _labelMin / _labelMax non assignes: les ancres du slider ne changeront pas selon la question.");

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

        // Les questions d'acceptabilite et de human-likeness portent sur l'advisor : elles n'ont pas de sens si aucun advisor n'a ete choisi (none).
        bool hasAdvisor = flow.State.advisor_choice != AdvisorType.None;
        if (hasAdvisor)
        {
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.AcceptabilityQuestion1Key, _acceptabilityQuestion1, _acceptabilityMinLabel, _acceptabilityMaxLabel));
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.AcceptabilityQuestion2Key, _acceptabilityQuestion2, _acceptabilityMinLabel, _acceptabilityMaxLabel));
        }

        _questions.Add(new TrialQuestionItem(FlowSerializationUtility.SensOfAgencyQuestionKey, _senseOfAgencyQuestion, _senseOfAgencyMinLabel, _senseOfAgencyMaxLabel));

        bool isLastTrial = flow.State.current_trial_index >= flow.CurrentBlock.trial_count - 1;
        if (hasAdvisor && isLastTrial)
            _questions.Add(new TrialQuestionItem(FlowSerializationUtility.HumanLikenessQuestionKey, _humanLikenessQuestion, _humanLikenessMinLabel, _humanLikenessMaxLabel));

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
        if (_scaleSlider == null) return;

        var question = _questions[_currentIndex];
        _responses.Add(new QuestionResponse
        {
            order = _currentIndex + 1,
            question_key = question.key,
            question_text = question.text,
            response = Mathf.RoundToInt(_scaleSlider.value).ToString() // slider entier 1…7
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
        if (_labelMin != null)
            _labelMin.text = $"1\n{question.minLabel}";
        if (_labelMax != null)
            _labelMax.text = $"7\n{question.maxLabel}";

        // Le slider a toujours une valeur : il repart du milieu de l'echelle et la validation est immediate.
        if (_scaleSlider != null)
            _scaleSlider.value = Mathf.Round((_scaleSlider.minValue + _scaleSlider.maxValue) / 2f);
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
