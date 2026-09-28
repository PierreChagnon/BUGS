using UnityEngine;

[DisallowMultipleComponent]
public class TrapVisibility : MonoBehaviour
{
    Vector2Int _cell;
    bool _hasCell;
    Renderer[] _renderers;
    FogController _subscribedFog;
    GameManager _subscribedGameManager;

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
        UnbindGameManager();
    }

    void OnDestroy()
    {
        UnbindFog();
        UnbindGameManager();
    }

    // Un piege du chemin advisor se revele quand le joueur marche dessus :
    // sa case est deja sans brouillard, donc le fog ne previent pas.
    void BindGameManager()
    {
        var gameManager = GameManager.Instance;
        if (_subscribedGameManager == gameManager)
            return;

        UnbindGameManager();

        if (gameManager == null)
            return;

        gameManager.OnPlayerCellVisited += HandlePlayerCellVisited;
        _subscribedGameManager = gameManager;
    }

    void UnbindGameManager()
    {
        if (_subscribedGameManager == null)
            return;

        _subscribedGameManager.OnPlayerCellVisited -= HandlePlayerCellVisited;
        _subscribedGameManager = null;
    }

    void HandlePlayerCellVisited(Vector2Int cell)
    {
        if (_hasCell && cell == _cell)
            RefreshVisibility();
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
        BindGameManager();
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

        if (GameManager.Instance != null && GameManager.Instance.IsTrapHiddenOnAdvisorPath(_cell))
            return false;

        var fog = FogController.Instance;
        if (fog == null || !fog.IsInitialized)
            return true;

        return fog.IsCellRevealed(_cell);
    }
}
