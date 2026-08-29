using UnityEngine;
using TMPro;
using UnityEngine.UI;

// -----------------------------
// Affichage du motor advice en bas de l ecran.
// -----------------------------

public class MotorAdviceUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _advisorPicture;
    [SerializeField] private GameObject _advisorHumanMale;
    [SerializeField] private GameObject _advisorHumanFemale;
    [SerializeField] private GameObject _advisorRobot;
    [SerializeField] private Text keyUpLabel;
    [SerializeField] private Text keyLeftLabel;
    [SerializeField] private Text keyDownLabel;
    [SerializeField] private Text keyRightLabel;

    void Start()
    {
        CacheAdvisorDisplays();

        if (MotorAdviceController.Instance != null)
            MotorAdviceController.Instance.OnAdviceChanged += Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded += HandleRoundEnded;

        Refresh();
    }

    void OnDestroy()
    {
        if (MotorAdviceController.Instance != null)
            MotorAdviceController.Instance.OnAdviceChanged -= Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.OnRoundEnded -= HandleRoundEnded;
    }

    void HandleRoundEnded(GameManager.RoundEndInfo info)
    {
        if (_root != null) _root.SetActive(false);
        if (_advisorPicture != null) _advisorPicture.SetActive(false);
        SetAdvisorDisplays(false, AdvisorType.None);
    }

    void Refresh()
    {
        var motor = MotorAdviceController.Instance;
        if (motor == null) { Debug.LogWarning("[MotorAdviceUI] Refresh skipped: MotorAdviceController.Instance est null"); return; }
        if (_root == null || keyUpLabel == null || keyLeftLabel == null || keyDownLabel == null || keyRightLabel == null)
        {
            Debug.LogWarning($"[MotorAdviceUI] Refresh skipped: ref null — root={_root != null}, up={keyUpLabel != null}, left={keyLeftLabel != null}, down={keyDownLabel != null}, right={keyRightLabel != null}");
            return;
        }

        CacheAdvisorDisplays();

        _root.SetActive(motor.AdviceVisible);
        if (_advisorPicture != null) _advisorPicture.SetActive(motor.AdviceVisible);
        SetAdvisorDisplays(motor.AdviceVisible, GetAdvisorType());
        if (!motor.AdviceVisible) return;

        keyUpLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "up");
        keyLeftLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "left");
        keyDownLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "down");
        keyRightLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "right");
    }

    void CacheAdvisorDisplays()
    {
        if (_advisorPicture == null) return;

        if (_advisorHumanMale == null)
            _advisorHumanMale = AdvisorBadgeUtility.FindDescendant(_advisorPicture.transform, AdvisorBadgeUtility.HumanMaleName);
        if (_advisorHumanFemale == null)
            _advisorHumanFemale = AdvisorBadgeUtility.FindDescendant(_advisorPicture.transform, AdvisorBadgeUtility.HumanFemaleName);
        if (_advisorRobot == null)
            _advisorRobot = AdvisorBadgeUtility.FindDescendant(_advisorPicture.transform, AdvisorBadgeUtility.RobotName);
    }

    void SetAdvisorDisplays(bool adviceVisible, AdvisorType advisorType)
    {
        AdvisorBadgeUtility.ApplyBadges(_advisorHumanMale, _advisorHumanFemale, _advisorRobot, adviceVisible, advisorType);
    }

    AdvisorType GetAdvisorType()
    {
        var flow = FlowController.Instance;
        if (flow != null && flow.State != null)
            return flow.State.advisor_choice;

        var session = SessionManager.Instance;
        return session == null || session.HasAdvisor ? AdvisorType.Human : AdvisorType.None;
    }
}
