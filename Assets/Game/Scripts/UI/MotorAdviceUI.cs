using UnityEngine;
using TMPro;
using UnityEngine.UI;

// -----------------------------
// Affichage du motor advice en bas de l ecran.
// -----------------------------

public class MotorAdviceUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private Text keyUpLabel;
    [SerializeField] private Text keyLeftLabel;
    [SerializeField] private Text keyDownLabel;
    [SerializeField] private Text keyRightLabel;

    void Start()
    {
        if (MotorAdviceController.Instance != null)
            MotorAdviceController.Instance.OnAdviceChanged += Refresh;

        Refresh();
    }

    void OnDestroy()
    {
        if (MotorAdviceController.Instance != null)
            MotorAdviceController.Instance.OnAdviceChanged -= Refresh;
    }

    void Refresh()
    {
        var motor = MotorAdviceController.Instance;
        if (motor == null || _root == null || keyUpLabel == null || keyLeftLabel == null || keyDownLabel == null || keyRightLabel == null) return;

        _root.SetActive(motor.AdviceVisible);
        if (!motor.AdviceVisible) return;

        keyUpLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "up");
        keyLeftLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "left");
        keyDownLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "down");
        keyRightLabel.text = MotorAdviceController.FormatSet(motor.DisplayedSet, "right");
    }
}
