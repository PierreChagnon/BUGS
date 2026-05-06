using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// -----------------------------
// Gère la texture masque du brouillard de guerre.
//
// Responsabilités :
//   - Créer une texture RGBA32 où R=255 = brouillard, R=0 = révélé
//   - Exposer RevealCell / RevealCells qui peignent les cellules révélées avec un dégradé
//     interne uniquement sur les bords adjacents à des cellules encore cachées
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
    public int pixelsPerCell = 8;

    [Header("Brush de révélation")]
    [Tooltip("Largeur du dégradé interne, en cellules. Le dégradé s'applique du bord vers " +
             "l'intérieur des cellules révélées, uniquement sur les bords adjacents à une cellule " +
             "encore cachée. Les bords entre deux cellules révélées restent nets (R=0).")]
    [Range(0f, 0.5f)]
    [FormerlySerializedAs("revealRadiusCells")]
    public float falloffCells = 0.3f;

    Renderer _renderer;
    Texture2D _mask;
    Color32[] _buffer;
    int _texW, _texH, _ppc;
    HashSet<Vector2Int> _revealedCells;
    bool _allRevealed;

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
        _mask.filterMode = FilterMode.Bilinear;

        _buffer = new Color32[_texW * _texH];
        var opaque = new Color32(255, 255, 255, 255);
        for (int i = 0; i < _buffer.Length; i++)
            _buffer[i] = opaque;

        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);

        _renderer.material.SetTexture("_Mask", _mask);

        _revealedCells = new HashSet<Vector2Int>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // --- API publique ---

    /// <summary>Révèle une cellule. Repeint la cellule + ses voisines déjà révélées.</summary>
    public void RevealCell(Vector2Int cell)
    {
        if (_allRevealed || _revealedCells == null) return;
        if (!_revealedCells.Add(cell)) return;

        RepaintCell(cell);
        foreach (var n in Get4Neighbors(cell))
            if (_revealedCells.Contains(n))
                RepaintCell(n);

        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);
    }

    /// <summary>Révèle plusieurs cellules en un seul Apply (batch optimisé).</summary>
    public void RevealCells(IEnumerable<Vector2Int> cells)
    {
        if (_allRevealed || _revealedCells == null) return;

        var toRepaint = new HashSet<Vector2Int>();
        foreach (var c in cells)
        {
            if (_revealedCells.Add(c))
            {
                toRepaint.Add(c);
                foreach (var n in Get4Neighbors(c))
                    if (_revealedCells.Contains(n))
                        toRepaint.Add(n);
            }
        }

        foreach (var c in toRepaint)
            RepaintCell(c);

        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);
    }

    /// <summary>Révèle toute la carte d'un coup (fin d'essai).</summary>
    public void RevealAll()
    {
        _allRevealed = true;
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < _buffer.Length; i++)
            _buffer[i] = clear;

        _mask.SetPixels32(_buffer);
        _mask.Apply(false, false);
    }

    // --- Interne ---

    static IEnumerable<Vector2Int> Get4Neighbors(Vector2Int c)
    {
        yield return new Vector2Int(c.x - 1, c.y);
        yield return new Vector2Int(c.x + 1, c.y);
        yield return new Vector2Int(c.x, c.y - 1);
        yield return new Vector2Int(c.x, c.y + 1);
    }

    // Repeint les pixels d'une cellule révélée avec un dégradé interne sur chaque bord
    // adjacent à une cellule cachée. Les bords entre deux cellules révélées sont nets.
    void RepaintCell(Vector2Int cell)
    {
        bool nLeft  = _revealedCells.Contains(new Vector2Int(cell.x - 1, cell.y));
        bool nRight = _revealedCells.Contains(new Vector2Int(cell.x + 1, cell.y));
        bool nDown  = _revealedCells.Contains(new Vector2Int(cell.x, cell.y - 1));
        bool nUp    = _revealedCells.Contains(new Vector2Int(cell.x, cell.y + 1));

        int x0 = Mathf.Max(0, cell.x * _ppc);
        int y0 = Mathf.Max(0, cell.y * _ppc);
        int x1 = Mathf.Min(_texW - 1, (cell.x + 1) * _ppc - 1);
        int y1 = Mathf.Min(_texH - 1, (cell.y + 1) * _ppc - 1);

        float falloffPx = falloffCells * _ppc;

        for (int y = y0; y <= y1; y++)
        {
            int row = y * _texW;
            for (int x = x0; x <= x1; x++)
            {
                // Distance au bord le plus proche parmi ceux qui sont "extérieurs"
                // (côté caché). On ignore les bords intérieurs (côté révélé).
                float dEdge = float.MaxValue;
                if (!nLeft)  dEdge = Mathf.Min(dEdge, x  - x0 + 0.5f);
                if (!nRight) dEdge = Mathf.Min(dEdge, x1 - x  + 0.5f);
                if (!nDown)  dEdge = Mathf.Min(dEdge, y  - y0 + 0.5f);
                if (!nUp)    dEdge = Mathf.Min(dEdge, y1 - y  + 0.5f);

                float t;
                if (falloffPx <= 0f || dEdge >= falloffPx) t = 0f;
                else
                {
                    float u = 1f - dEdge / falloffPx;   // 1 au bord, 0 à la limite du falloff
                    t = u * u * (3f - 2f * u);          // smoothstep
                }

                byte r = (byte)(t * 255f);
                _buffer[row + x] = new Color32(r, r, r, r);
            }
        }
    }
}
