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
    [SerializeField] private Text keyUpLabel;
    [SerializeField] private Text keyLeftLabel;
    [SerializeField] private Text keyDownLabel;
    [SerializeField] private Text keyRightLabel;

    void Start()
    {
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
    }

    void Refresh()
    {
        var motor = MotorAdviceController.Instance;
        if (motor == null) { Debug.LogWarning("[MotorAdviceUI] Refresh skipped: MotorAdviceController.Instance est null"); return; }
        if (_root == null || _advisorPicture == null || keyUpLabel == null || keyLeftLabel == null || keyDownLabel == null || keyRightLabel == null)
        {
            Debug.LogWarning($"[MotorAdviceUI] Refresh skipped: ref null — root={_root != null}, advisor={_advisorPicture != null}, up={keyUpLabel != null}, left={keyLeftLabel != null}, down={keyDownLabel != null}, right={keyRightLabel != null}");
            return;
        }

        _root.SetActive(motor.AdviceVisible);
        _advisorPicture.SetActive(motor.AdviceVisible);
        if (!motor.AdviceVisible) return;

        keyUpLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "up");
        keyLeftLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "left");
        keyDownLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "down");
        keyRightLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "right");
    }
}
