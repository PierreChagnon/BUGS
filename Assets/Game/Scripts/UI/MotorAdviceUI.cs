using UnityEngine;
using TMPro;

// -----------------------------
// Affichage du motor advice en bas de l ecran.
// -----------------------------

public class MotorAdviceUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TMP_Text _label;

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
        if (motor == null || _root == null || _label == null) return;

        _root.SetActive(motor.AdviceVisible);
        if (!motor.AdviceVisible) return;

        _label.text = MotorAdviceController.FormatSet(motor.DisplayedSet);
    }
}
