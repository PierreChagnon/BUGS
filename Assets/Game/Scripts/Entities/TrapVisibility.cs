using UnityEngine;

[DisallowMultipleComponent]
public class TrapVisibility : MonoBehaviour
{
    Vector2Int _cell;
    bool _hasCell;
    Renderer[] _renderers;
    FogController _subscribedFog;

    public void Initialize(Vector2Int cell)
    {
        _cell = cell;
        _hasCell = true;

        CacheRenderers();
        BindFog();
        RefreshVisibility();
    }

    void OnEnable()
    {
        CacheRenderers();
        BindFog();
        RefreshVisibility();
    }

    void OnDisable()
    {
        UnbindFog();
    }

    void OnDestroy()
    {
        UnbindFog();
    }

    void BindFog()
    {
        var fog = FogController.Instance;
        if (_subscribedFog == fog)
            return;

        UnbindFog();

        if (fog == null)
            return;

        fog.RevealedCellsChanged += RefreshVisibility;
        _subscribedFog = fog;
    }

    void UnbindFog()
    {
        if (_subscribedFog == null)
            return;

        _subscribedFog.RevealedCellsChanged -= RefreshVisibility;
        _subscribedFog = null;
    }

    void CacheRenderers()
    {
        if (_renderers != null && _renderers.Length > 0)
            return;

        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    void EnsureCellFromPosition()
    {
        if (_hasCell)
            return;

        var registry = LevelRegistry.Instance;
        if (registry == null)
            return;

        _cell = registry.WorldToCell(transform.position);
        _hasCell = true;
    }

    public void RefreshVisibility()
    {
        CacheRenderers();
        BindFog();
        EnsureCellFromPosition();

        bool isVisible = ShouldShowTrap();
        foreach (var trapRenderer in _renderers)
        {
            if (trapRenderer != null)
                trapRenderer.enabled = isVisible;
        }
    }

    bool ShouldShowTrap()
    {
        if (!_hasCell)
            return true;

        var fog = FogController.Instance;
        if (fog == null || !fog.IsInitialized)
            return true;

        return fog.IsCellRevealed(_cell);
    }
}
