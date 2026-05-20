using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionnaireUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _questionText;

    [Header("Scale")]
    [SerializeField] private GameObject _scaleRoot;
    [SerializeField] private Slider _scaleSlider;
    [SerializeField] private TMP_Text _scaleMinLabel;
    [SerializeField] private TMP_Text _scaleMaxLabel;
    [SerializeField] private TMP_Text _scaleValueLabel;

    [Header("Multiple Choice")]
    [SerializeField] private GameObject _dropdownRoot;
    [SerializeField] private TMP_Dropdown _dropdown;

    [Header("Free Text")]
    [SerializeField] private GameObject _inputRoot;
    [SerializeField] private TMP_InputField _inputField;

    readonly List<QuestionResponse> _responses = new();
    List<QuestionConfig> _questions = new();
    int _currentQuestionIndex;

    void Start()
    {
        var flow = FlowController.Instance;
        if (flow == null || flow.CurrentBlock == null)
            return;

        _questions = new List<QuestionConfig>();

        if (_titleText != null)
            _titleText.text = "Questionnaire";

        if (_questions.Count == 0)
        {
            FlowController.Instance?.OnQuestionnaireComplete(_responses);
            return;
        }

        ShowCurrentQuestion();
    }

    public void OnNextClicked()
    {
        if (_currentQuestionIndex < 0 || _currentQuestionIndex >= _questions.Count)
            return;

        QuestionConfig question = _questions[_currentQuestionIndex];
        _responses.Add(new QuestionResponse
        {
            order = question.order,
            question_text = question.text,
            response = ReadResponse(question)
        });

        _currentQuestionIndex++;
        if (_currentQuestionIndex >= _questions.Count)
        {
            FlowController.Instance?.OnQuestionnaireComplete(_responses);
            return;
        }

        ShowCurrentQuestion();
    }

    public void OnScaleValueChanged(float value)
    {
        if (_scaleValueLabel != null)
            _scaleValueLabel.text = Mathf.RoundToInt(value).ToString();
    }

    void ShowCurrentQuestion()
    {
        QuestionConfig question = _questions[_currentQuestionIndex];

        if (_progressText != null)
            _progressText.text = $"Question {_currentQuestionIndex + 1}/{_questions.Count}";

        if (_questionText != null)
            _questionText.text = question.text;

        bool isScale = question.type == "scale";
        bool isMultipleChoice = question.type == "multiple_choice";
        bool isFreeText = !isScale && !isMultipleChoice;

        if (_scaleRoot != null)
            _scaleRoot.SetActive(isScale);
        if (_dropdownRoot != null)
            _dropdownRoot.SetActive(isMultipleChoice);
        if (_inputRoot != null)
            _inputRoot.SetActive(isFreeText);

        if (isScale)
            ConfigureScale(question);
        else if (isMultipleChoice)
            ConfigureDropdown(question);
        else
            ConfigureInput();
    }

    void ConfigureScale(QuestionConfig question)
    {
        if (_scaleSlider != null)
        {
            _scaleSlider.wholeNumbers = true;
            _scaleSlider.minValue = question.min_value;
            _scaleSlider.maxValue = question.max_value;
            _scaleSlider.value = question.min_value;
        }

        if (_scaleMinLabel != null)
            _scaleMinLabel.text = string.IsNullOrWhiteSpace(question.min_label) ? question.min_value.ToString() : question.min_label;

        if (_scaleMaxLabel != null)
            _scaleMaxLabel.text = string.IsNullOrWhiteSpace(question.max_label) ? question.max_value.ToString() : question.max_label;

        OnScaleValueChanged(question.min_value);
    }

    void ConfigureDropdown(QuestionConfig question)
    {
        if (_dropdown == null)
            return;

        _dropdown.ClearOptions();
        var options = question.options != null && question.options.Length > 0
            ? new List<string>(question.options)
            : new List<string> { "Option 1" };
        _dropdown.AddOptions(options);
        _dropdown.value = 0;
    }

    void ConfigureInput()
    {
        if (_inputField != null)
            _inputField.text = string.Empty;
    }

    string ReadResponse(QuestionConfig question)
    {
        if (question.type == "scale")
            return _scaleSlider != null ? Mathf.RoundToInt(_scaleSlider.value).ToString() : question.min_value.ToString();

        if (question.type == "multiple_choice")
        {
            if (_dropdown == null || _dropdown.options.Count == 0)
                return string.Empty;

            return _dropdown.options[_dropdown.value].text;
        }

        return _inputField != null ? _inputField.text : string.Empty;
    }
}
