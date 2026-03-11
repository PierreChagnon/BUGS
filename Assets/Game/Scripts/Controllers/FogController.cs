using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// Gère la texture masque du brouillard de guerre.
//
// Responsabilités :
//   - Créer une texture RGBA32 où R=255 = brouillard, R=0 = révélé
//   - Exposer RevealCell / RevealCells pour révéler des cases entières (carrés)
//   - Pousser le buffer vers le shader (property _Mask)
//
// Ne gère PAS : le spawn de la surface (→ FogSpawner),
//               la décision de révéler (→ GameManager, PathSpawner).
//
// Interroge LevelRegistry pour la taille de grille (source de vérité).
// Instancié dynamiquement par FogSpawner — pas de DefaultExecutionOrder.
// -----------------------------

[RequireComponent(typeof(Renderer))]
public class FogController : MonoBehaviour
{
    public static FogController Instance { get; private set; }

    [Header("Masque")]
    [Tooltip("Résolution du masque en pixels par case (1 = carré net, >1 = résolution plus fine).")]
    public int pixelsPerCell = 1;

    Renderer _renderer;
    Texture2D _mask;
    Color32[] _buffer;
    int _texW, _texH, _ppc;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var reg = LevelRegistry.Instance;
        if (reg == null)
        {
            Debug.LogError("[FogController] LevelRegistry introuvable.");
            return;
        }

        _renderer = GetComponent<Renderer>();
        _ppc = Mathf.Max(1, pixelsPerCell);
        _texW = Mathf.Max(1, reg.gridSize.x * _ppc);
        _texH = Mathf.Max(1, reg.gridSize.y * _ppc);

        // Masque RGBA32 : canal R lu par le shader (FogUnlitMask.shadergraph)
        _mask = new Texture2D(_texW, _texH, TextureFormat.RGBA32, false, true);
        _mask.wrapMode = TextureWrapMode.Clamp;
        _mask.filterMode = FilterMode.Point;

        _buffer = new Color32[_texW * _texH];
        var opaque = new Color32(255, 255, 255, 255);
        for (int i = 0; i < _buffer.Length; i++)
            _buffer[i] = opaque;

        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);

        _renderer.material.SetTexture("_Mask", _mask);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // --- API publique ---

    /// <summary>Révèle une cellule entière (carré net).</summary>
    public void RevealCell(Vector2Int cell)
    {
        PaintCellSquare(cell);
        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);
    }

    /// <summary>Révèle plusieurs cellules en un seul Apply (batch optimisé).</summary>
    public void RevealCells(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells)
            PaintCellSquare(c);

        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);
    }

    // --- Interne ---

    void PaintCellSquare(Vector2Int cell)
    {
        int x0 = cell.x * _ppc;
        int y0 = cell.y * _ppc;
        var clear = new Color32(0, 0, 0, 0);

        for (int y = y0; y < y0 + _ppc && y < _texH; y++)
        {
            int row = y * _texW;
            for (int x = x0; x < x0 + _ppc && x < _texW; x++)
                _buffer[row + x] = clear;
        }
    }
}
