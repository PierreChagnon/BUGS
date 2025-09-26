using UnityEngine;
using UnityEngine.InputSystem; // <— nouveau système

public class GridMoverNewInput : MonoBehaviour
{
    [Header("Grille")]
    public float cellSize = 1f;

    [Header("Déplacement")]
    public float moveDuration = 0.15f;
    public bool rotateToDirection = true;

    [Header("Validation de la case cible")]
    public LayerMask tileLayer;
    public float raycastStartHeight = 2f;
    public float raycastDistance = 5f;

    bool isMoving = false;

    void Start()
    {
        SnapToGrid();

        if (FogController.Instance != null)
            FogController.Instance.RevealCell(FogController.Instance.WorldToCell(transform.position));
        if (LevelRegistry.Instance != null)
            LevelRegistry.Instance.MarkVisited(LevelRegistry.Instance.WorldToCell(transform.position));

    }

    void Update()
    {
        if (isMoving) return;

        // Si le round est fini, on n’accepte plus d’input
        if (GameManager.Instance != null && GameManager.Instance.inputLocked) return;

        Vector2Int step = ReadStepNewInput();
        if (step == Vector2Int.zero) return;

        Vector3 dir = new(step.x, 0f, step.y);
        Vector3 targetPos = GetSnappedPosition(transform.position + dir * cellSize);

        // if (requireTileUnderTarget && !HasTileUnder(targetPos)) return;
        var targetCell = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.z));
        if (LevelRegistry.Instance != null && !LevelRegistry.Instance.InBounds(targetCell))
            return; // hors plateau → on ignore l’input


        if (rotateToDirection && dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        StartCoroutine(MoveTo(targetPos, moveDuration));
    }

    // Lit 1 pas (haut/bas/gauche/droite) avec le New Input System
    Vector2Int ReadStepNewInput()
    {
        // Clavier
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) return new Vector2Int(-1, 0);
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) return new Vector2Int(+1, 0);
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame) return new Vector2Int(0, +1);
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) return new Vector2Int(0, -1);
        }

        return Vector2Int.zero;
    }

    System.Collections.IEnumerator MoveTo(Vector3 target, float duration)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.position = target;
        isMoving = false;

        // Révéler la cellule atteinte
        if (FogController.Instance != null)
        {
            var cell = FogController.Instance.WorldToCell(transform.position);
            FogController.Instance.RevealCell(cell);
        }

        if (LevelRegistry.Instance != null)
            LevelRegistry.Instance.MarkVisited(LevelRegistry.Instance.WorldToCell(transform.position));

        // Informer le GameManager
        if (GameManager.Instance != null && LevelRegistry.Instance != null)
        {
            var cell = LevelRegistry.Instance.WorldToCell(transform.position);
            GameManager.Instance.OnPlayerStep(cell);
        }

    }


    Vector3 GetSnappedPosition(Vector3 worldPos)
    {
        float x = Mathf.Round(worldPos.x / cellSize) * cellSize;
        float z = Mathf.Round(worldPos.z / cellSize) * cellSize;
        return new Vector3(x, worldPos.y, z);
    }

    public void SnapToGrid() => transform.position = GetSnappedPosition(transform.position);
}
