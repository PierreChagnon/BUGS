using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// HowToPlayTestRunner : composant de test sandbox-only pour HowToPlayUI.
// Ne fait PAS partie de la livraison composant. Sert uniquement a valider
// l'API publique (SetPages, Open, Close) et les events (Closed, Completed)
// dans la scene de sandbox via des ContextMenu en Play Mode.
// -----------------------------
public class HowToPlayTestRunner : MonoBehaviour
{
    [SerializeField] private HowToPlayUI _target;
    [SerializeField] private List<HowToPlayPage> _alternatePages = new List<HowToPlayPage>();

    void Start()
    {
        if (_target == null)
        {
            Debug.LogWarning("[HowToPlayTestRunner] _target non assigne.");
            return;
        }
        _target.Closed += OnClosed;
        _target.Completed += OnCompleted;
        Debug.Log("[HowToPlayTestRunner] Abonnement aux events Closed/Completed effectue.");
    }

    void OnDestroy()
    {
        if (_target == null) return;
        _target.Closed -= OnClosed;
        _target.Completed -= OnCompleted;
    }

    private void OnClosed()
    {
        Debug.Log("[HowToPlayTestRunner] Event Closed recu.");
    }

    private void OnCompleted()
    {
        Debug.Log("[HowToPlayTestRunner] Event Completed recu.");
    }

    [ContextMenu("Test SetPages alternate list")]
    private void TestSetPagesAlternate()
    {
        if (_target == null) return;
        Debug.Log($"[HowToPlayTestRunner] SetPages avec {_alternatePages.Count} pages alternatives.");
        _target.SetPages(_alternatePages);
        _target.Open();
    }

    [ContextMenu("Test Open")]
    private void TestOpen()
    {
        if (_target == null) return;
        _target.Open();
    }

    [ContextMenu("Test Close")]
    private void TestClose()
    {
        if (_target == null) return;
        _target.Close();
    }

    [ContextMenu("Test SetPages null")]
    private void TestSetPagesNull()
    {
        if (_target == null) return;
        Debug.Log("[HowToPlayTestRunner] SetPages(null).");
        _target.SetPages(null);
    }

    [ContextMenu("Test SetPages empty")]
    private void TestSetPagesEmpty()
    {
        if (_target == null) return;
        Debug.Log("[HowToPlayTestRunner] SetPages(empty list).");
        _target.SetPages(new List<HowToPlayPage>());
    }
}
