using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// -----------------------------
// Entité joueur : déplacement discret sur grille.
//
// Responsabilités :
//   - Lire l'input clavier (New Input System, wasPressedThisFrame)
//   - Valider la case cible via LevelRegistry.IsWalkable
//   - Interpoler le déplacement par coroutine (SmoothStep)
//   - Signaler chaque pas au GameManager (qui orchestre fog, visited, score)
//
// Ce script ne connaît que LevelRegistry (pour la validation)
// et GameManager (pour signaler). Il ne touche ni au fog, ni au score.
// -----------------------------

public class GridMover : MonoBehaviour
{
    [Header("Grille")]
    [Tooltip("Fallback si LevelRegistry absent (non utilisé en prod).")]
    public float cellSize = 1f;

    [Header("Déplacement")]
    public float moveDuration = 0.15f;
    public bool rotateToDirection = true;

    bool _isMoving = false;

    void Start()
    {
        SnapToGrid();

        // Signaler la position initiale au GameManager
        // pour qu'il révèle le fog et marque la cellule de départ
        if (GameManager.Instance != null && LevelRegistry.Instance != null)
        {
            var cell = LevelRegistry.Instance.WorldToCell(transform.position);
            GameManager.Instance.OnPlayerStep(cell);
        }
    }

    void Update()
    {
        if (_isMoving) return;
        if (GameManager.Instance != null && GameManager.Instance.inputLocked) return;

        bool invalidKeyPressed = IsAnyNonActiveMoveKeyPressedThisFrame();
        Vector2Int step = ReadStep();
        if (invalidKeyPressed && GameManager.Instance != null)
            GameManager.Instance.OnInvalidMoveKeyPressed();

        if (step == Vector2Int.zero) return;

        Vector3 dir = new(step.x, 0f, step.y);

        var reg = LevelRegistry.Instance;
        Vector3 targetPos;

        if (reg != null)
        {
            var curCell = reg.WorldToCell(transform.position);
            var targetCell = curCell + step;
            if (!reg.InBounds(targetCell)) return;
            if (!reg.IsWalkable(targetCell)) return;
            targetPos = reg.CellToWorld(targetCell, transform.position.y);
        }
        else
        {
            targetPos = GetSnappedPosition(transform.position + dir * cellSize);
        }

        if (rotateToDirection && dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        StartCoroutine(MoveToCoroutine(targetPos, moveDuration));
    }

    // ── Input ──────────────────────────────────────────────────────

    Vector2Int ReadStep()
    {
        if (MotorAdviceController.Instance != null &&
            MotorAdviceController.Instance.TryGetStep(out var motorStep))
            return motorStep;

        if (Keyboard.current == null) return Vector2Int.zero;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) return new Vector2Int(-1, 0);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) return new Vector2Int(+1, 0);
        if (Keyboard.current.upArrowKey.wasPressedThisFrame) return new Vector2Int(0, +1);
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) return new Vector2Int(0, -1);

        return Vector2Int.zero;
    }

    bool IsAnyNonActiveMoveKeyPressedThisFrame()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        foreach (KeyControl key in keyboard.allKeys)
        {
            if (!key.wasPressedThisFrame) continue;
            if (!IsActiveMoveKey(key)) return true;
        }

        return false;
    }

    bool IsActiveMoveKey(KeyControl key)
    {
        if (MotorAdviceController.Instance != null)
            return MotorAdviceController.Instance.IsActiveMoveKey(key);

        return key == Keyboard.current.leftArrowKey
            || key == Keyboard.current.rightArrowKey
            || key == Keyboard.current.upArrowKey
            || key == Keyboard.current.downArrowKey;
    }

    // ── Déplacement ────────────────────────────────────────────────

    System.Collections.IEnumerator MoveToCoroutine(Vector3 target, float duration)
    {
        _isMoving = true;
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = target;
        _isMoving = false;

        // Signaler le pas terminé au GameManager
        // (lui orchestre : fog, visited, score, trial log)
        if (GameManager.Instance != null && LevelRegistry.Instance != null)
        {
            var cell = LevelRegistry.Instance.WorldToCell(transform.position);
            GameManager.Instance.OnPlayerStep(cell);
        }
    }

    // ── Utilitaires ────────────────────────────────────────────────

    Vector3 GetSnappedPosition(Vector3 worldPos)
    {
        float x = Mathf.Round(worldPos.x / cellSize) * cellSize;
        float z = Mathf.Round(worldPos.z / cellSize) * cellSize;
        return new Vector3(x, worldPos.y, z);
    }

    public void SnapToGrid()
    {
        var reg = LevelRegistry.Instance;
        if (reg != null)
        {
            transform.position = reg.SnapWorldToCellCenter(transform.position);
            return;
        }
        transform.position = GetSnappedPosition(transform.position);
    }
}
