using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FogController : MonoBehaviour
{
    public static FogController Instance { get; private set; }

    [Header("Grille")]
    public Vector2Int gridSize = new(10, 10); // X,Z

    [Header("Masque")]
    [Tooltip("Résolution du masque en pixels par case (32 = doux, 1 = carré net).")]
    public int pixelsPerCell = 32;
    [Tooltip("Rayon (en pixels) du pinceau de révélation.")]
    public int brushRadiusPx = 14;
    [Tooltip("Largeur (en pixels) du dégradé doux du bord.")]
    public int brushFeatherPx = 6;

    Renderer fogRenderer;
    Texture2D mask;
    Color32[] buffer; // on modifie en RAM puis on push

    int texW, texH;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        fogRenderer = GetComponent<Renderer>();

        texW = Mathf.Max(1, gridSize.x * Mathf.Max(1, pixelsPerCell));
        texH = Mathf.Max(1, gridSize.y * Mathf.Max(1, pixelsPerCell));

        // Masque en RGBA32 (simple, universel) : on utilise le canal R dans le shader
        mask = new Texture2D(texW, texH, TextureFormat.RGBA32, false, true);
        mask.wrapMode = TextureWrapMode.Clamp;
        mask.filterMode = pixelsPerCell > 1 ? FilterMode.Bilinear : FilterMode.Point;

        buffer = new Color32[texW * texH];
        for (int i = 0; i < buffer.Length; i++) buffer[i] = new Color32(255, 255, 255, 255); // 1 = opaque (brouillard)
        mask.SetPixels32(buffer);
        mask.Apply(false, false);

        // Assigne au matériau (Shader Graph : property reference _Mask)
        fogRenderer.material.SetTexture("_Mask", mask);
    }

    // --- API ---

    public void RevealCell(Vector2Int cell)
    {
        var center = CellToPixelCenter(cell);
        PaintDisc(center, brushRadiusPx, brushFeatherPx);
        mask.SetPixels32(buffer);
        mask.Apply(false, false);
    }

    public void RevealCells(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells) RevealCell(c);
        // (Apply déjà appelé dans RevealCell)
    }

    public Vector2Int WorldToCell(Vector3 world) =>
        new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));

    // --- Internes ---

    Vector2Int CellToPixelCenter(Vector2Int cell) =>
        new Vector2Int(cell.x * pixelsPerCell + pixelsPerCell / 2,
                       cell.y * pixelsPerCell + pixelsPerCell / 2);

    void PaintDisc(Vector2Int center, int rOut, int feather)
    {
        rOut = Mathf.Max(1, rOut);
        int rIn = Mathf.Max(0, rOut - Mathf.Clamp(feather, 0, rOut));

        int x0 = Mathf.Max(0, center.x - rOut);
        int x1 = Mathf.Min(texW - 1, center.x + rOut);
        int y0 = Mathf.Max(0, center.y - rOut);
        int y1 = Mathf.Min(texH - 1, center.y + rOut);

        for (int y = y0; y <= y1; y++)
        {
            int row = y * texW;
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float a; // 0 = transparent (révélé), 1 = opaque
                if (d <= rIn) a = 0f;
                else if (d >= rOut) a = 1f;
                else
                {
                    float t = (d - rIn) / (rOut - rIn);
                    a = t * t * (3f - 2f * t); // SmoothStep
                }

                int idx = row + x;
                byte newR = (byte)Mathf.RoundToInt(a * 255f);

                // On ne fait que "révéler" : on garde le plus petit (plus transparent)
                if (newR < buffer[idx].r)
                {
                    var px = buffer[idx];
                    px.r = px.g = px.b = newR;
                    buffer[idx] = px;
                }
            }
        }
    }
}
