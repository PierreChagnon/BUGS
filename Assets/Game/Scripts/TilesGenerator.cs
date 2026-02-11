using UnityEngine;

public class TilesGenerator : MonoBehaviour
{
    public int width = 11;
    public int height = 11;
    public float cellSize = 1f;
    public GameObject tilePrefab;
    public Transform root;
    [Header("Position de départ du player")]
    public Transform player; // Positione la grille en fonction de cette référence

    [ContextMenu("Generate Grid")]
    void GenerateGridMenu() => Generate();

    [ContextMenu("Clear Grid")]
    void ClearGridMenu() => Clear();

    public void Generate()
    {
        if (!tilePrefab) { Debug.LogError("tilePrefab missing"); return; }
        if (!player) { Debug.LogError("player missing"); return; }

        if (!root)
        {
            var go = new GameObject("TilesRoot");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            root = go.transform;
        }

        Clear();

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var tile = Instantiate(tilePrefab, root);
                tile.transform.localPosition = new Vector3(x * cellSize, 0f, y * cellSize); // XZ top-down
                tile.name = $"Tile_{x}_{y}";
            }
    }

    public void Clear()
    {
        if (!root) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);
    }
}
