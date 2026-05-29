using UnityEngine;

// -----------------------------
// InContextTutorialTestRunner : composant de test sandbox-only pour InContextTutorialUI.
// Ne fait PAS partie de la livraison composant. Valide l'API publique
// (Show / SetContent / Close) et les events (Shown / Closed) via des ContextMenu en Play Mode.
// -----------------------------
public class InContextTutorialTestRunner : MonoBehaviour
{
    [SerializeField] private InContextTutorialUI _target;
    [SerializeField] private string _title = "Titre de test";
    [SerializeField, TextArea(3, 6)] private string _body = "Corps de texte de test pour l'overlay d'instructions.";

    void Start()
    {
        if (_target == null)
        {
            Debug.LogWarning("[InContextTutorialTestRunner] _target non assigne.");
            return;
        }
        _target.Shown += OnShown;
        _target.Closed += OnClosed;
        Debug.Log("[InContextTutorialTestRunner] Abonnement aux events Shown/Closed effectue.");
    }

    void OnDestroy()
    {
        if (_target == null) return;
        _target.Shown -= OnShown;
        _target.Closed -= OnClosed;
    }

    private void OnShown() => Debug.Log("[InContextTutorialTestRunner] Event Shown recu.");
    private void OnClosed() => Debug.Log("[InContextTutorialTestRunner] Event Closed recu.");

    [ContextMenu("Test Show (contenu)")]
    private void TestShowContent()
    {
        if (_target == null) return;
        bool shown = _target.Show(_title, _body);
        Debug.Log($"[InContextTutorialTestRunner] Show(contenu) -> {shown}");
    }

    [ContextMenu("Test Show (vide -> no-op)")]
    private void TestShowEmpty()
    {
        if (_target == null) return;
        bool shown = _target.Show("", "");
        Debug.Log($"[InContextTutorialTestRunner] Show(vide) -> {shown} (attendu: False)");
    }

    [ContextMenu("Test SetContent + Show")]
    private void TestSetContentThenShow()
    {
        if (_target == null) return;
        _target.SetContent(_title, _body);
        bool shown = _target.Show();
        Debug.Log($"[InContextTutorialTestRunner] SetContent + Show() -> {shown}");
    }

    [ContextMenu("Test Close")]
    private void TestClose()
    {
        if (_target == null) return;
        _target.Close();
    }
}
