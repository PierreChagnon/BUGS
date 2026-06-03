using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class AdviceExplanationUIBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _explanationRoot;
    [SerializeField] private TMP_Text _explanationText;
    [SerializeField] private Button _showButton;
    [SerializeField] private GameObject _showButtonRoot;
    [SerializeField] private Button _closeButton;

    [Header("Advisor badges")]
    [SerializeField] private GameObject _advisorHumanMaleBadge;
    [SerializeField] private GameObject _advisorHumanFemaleBadge;
    [SerializeField] private GameObject _advisorRobotBadge;

    protected abstract AdviceLevel Level { get; }

    ExplanationRuntimeState _state;
    bool _hasLoggedDebugState;
    bool _showHumanMaleBadge;

    void Awake()
    {
        _showHumanMaleBadge = Random.value < 0.5f;

        if (_explanationRoot == null)
            _explanationRoot = gameObject;

        if (_showButtonRoot == null && _showButton != null)
            _showButtonRoot = _showButton.gameObject;

        if (_showButton != null)
            _showButton.onClick.AddListener(HandleShowClicked);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(HandleCloseClicked);
    }

    void Start()
    {
        Refresh();
    }

    void OnDisable()
    {
        FlowController.Instance?.RecordExplanationHidden(Level);
    }

    void OnDestroy()
    {
        FlowController.Instance?.RecordExplanationHidden(Level);

        if (_showButton != null)
            _showButton.onClick.RemoveListener(HandleShowClicked);

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(HandleCloseClicked);
    }

    public void Refresh()
    {
        _state = FlowController.Instance != null
            ? FlowController.Instance.GetExplanationState(Level)
            : ExplanationRuntimeState.None();
        _state ??= ExplanationRuntimeState.None();

        if (_explanationText != null)
            _explanationText.text = _state.text ?? string.Empty;

        LogDebugStateOnce();

        if (_state.IsNone)
        {
            SetExplanationVisible(false);
            SetShowButtonVisible(false);
            return;
        }

        if (_state.IsOptIn)
        {
            SetExplanationVisible(false);
            SetShowButtonVisible(_state.clicked != true);
            return;
        }

        SetExplanationVisible(true);
        SetShowButtonVisible(false);
    }

    void LogDebugStateOnce()
    {
        if (_hasLoggedDebugState)
            return;

        _hasLoggedDebugState = true;
        var flow = FlowController.Instance;
        string phase = flow != null && flow.State != null ? flow.State.current_phase.ToString() : "none";
        string advisor = flow != null && flow.State != null ? FlowValueConverters.ToApiValue(flow.State.advisor_choice) : "none";
        var block = flow != null ? flow.CurrentBlock : null;
        var config = block != null && block.explanations != null ? block.explanations.GetConfig(Level) : null;
        bool distalAdviceVisible = flow != null && flow.State != null && flow.State.distal_advice_visible;
        int textLength = string.IsNullOrEmpty(_state.text) ? 0 : _state.text.Length;

        Debug.Log(
            $"[AdviceExplanationUI] level={Level}, phase={phase}, tutorial={block != null && block.is_tutorial}, advisor={advisor}, distalAdviceVisible={distalAdviceVisible}, config={config?.display_mode ?? "null"}/{config?.content_variant ?? "null"}, resolved={_state.display_mode}/{_state.content_variant ?? "null"}, textLen={textLength}, root={_explanationRoot != null}, showButton={_showButton != null}, closeButton={_closeButton != null}");
    }

    void HandleShowClicked()
    {
        if (_state == null || !_state.IsOptIn)
            Refresh();

        if (_state == null || !_state.IsOptIn)
            return;

        if (_explanationText != null)
            _explanationText.text = _state.text ?? string.Empty;

        FlowController.Instance?.RecordExplanationDisplayed(Level);
        SetExplanationVisible(true);
        SetShowButtonVisible(false);
    }

    void HandleCloseClicked()
    {
        FlowController.Instance?.RecordExplanationHidden(Level);
        SetExplanationVisible(false);
        SetShowButtonVisible(false);
    }

    void SetExplanationVisible(bool visible)
    {
        SetAdvisorBadgesVisible(visible);

        if (_explanationRoot != null)
            _explanationRoot.SetActive(visible);
    }

    void SetShowButtonVisible(bool visible)
    {
        if (_showButtonRoot != null)
        {
            _showButtonRoot.SetActive(visible);
            return;
        }

        if (_showButton != null)
            _showButton.gameObject.SetActive(visible);
    }

    void SetAdvisorBadgesVisible(bool explanationVisible)
    {
        AdvisorType advisorType = GetAdvisorType();
        bool showHuman = explanationVisible && advisorType == AdvisorType.Human;
        bool showRobot = explanationVisible && advisorType == AdvisorType.Robot;

        if (_advisorHumanMaleBadge != null)
            _advisorHumanMaleBadge.SetActive(showHuman && _showHumanMaleBadge);
        if (_advisorHumanFemaleBadge != null)
            _advisorHumanFemaleBadge.SetActive(showHuman && !_showHumanMaleBadge);
        if (_advisorRobotBadge != null)
            _advisorRobotBadge.SetActive(showRobot);
    }

    AdvisorType GetAdvisorType()
    {
        var flow = FlowController.Instance;
        if (flow != null && flow.State != null)
            return flow.State.advisor_choice;

        return AdvisorType.None;
    }
}
