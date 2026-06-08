using UnityEngine;

// -----------------------------
// Pilote l'Animator du personnage joueur à partir des signaux de gameplay.
//
// Responsabilités :
//   - Traduire les events de GridMover (pas démarré + lookahead piège,
//     mouvement bloqué) en paramètres d'Animator.
//   - Synchroniser la lecture du saut avec la durée du pas : le clip
//     Forward_Jump (long) est rejoué à vitesse = longueurClip / moveDuration,
//     via le paramètre de vitesse JumpSpeedMult de l'état. moveDuration est
//     donc l'unique source de vérité du tempo (resync à chaque pas).
//   - Etre le SEUL à connaître les noms de paramètres de l'Animator Controller.
//
// Le gameplay (GridMover) ne référence jamais l'Animator : il émet des events.
//
// A placer sur le root du PlayerPrefab (même GameObject que GridMover).
// L'Animator visé est porté par l'enfant (modèle SciFi) → fallback
// GetComponentInChildren si la référence n'est pas câblée.
// -----------------------------

[RequireComponent(typeof(GridMover))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Animator du modèle (enfant). Auto-résolu si laissé vide.")]
    [SerializeField] private Animator _animator;

    [Header("Saut")]
    [Tooltip("Nom du clip de saut, utilisé pour caler sa vitesse sur la durée du pas.")]
    [SerializeField] private string _jumpClipName = "Forward_Jump";

    // Hash des paramètres du CharacterAnimController (case-sensitive).
    static readonly int JumpTrigger = Animator.StringToHash("JumpTrigger");
    static readonly int BlockedTrigger = Animator.StringToHash("BlockedTrigger");
    static readonly int IsOnTrap = Animator.StringToHash("IsOnTrap");
    static readonly int JumpSpeedMult = Animator.StringToHash("JumpSpeedMult");

    GridMover _mover;
    float _jumpClipLength;

    void Start()
    {
        _mover = GetComponent<GridMover>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _jumpClipLength = ResolveJumpClipLength();

        if (_mover != null)
        {
            _mover.OnStepStarted += HandleStepStarted;
            _mover.OnBlocked += HandleBlocked;
        }
    }

    void OnDestroy()
    {
        if (_mover != null)
        {
            _mover.OnStepStarted -= HandleStepStarted;
            _mover.OnBlocked -= HandleBlocked;
        }
    }

    float ResolveJumpClipLength()
    {
        var rac = _animator != null ? _animator.runtimeAnimatorController : null;
        if (rac == null) return 0f;

        foreach (var clip in rac.animationClips)
            if (clip != null && clip.name == _jumpClipName)
                return clip.length;

        return 0f;
    }

    // ── Handlers ───────────────────────────────────────────────────

    void HandleStepStarted(bool targetHasTrap)
    {
        if (_animator == null) return;

        // Caler la vitesse du saut sur la durée du pas pour que l'envol
        // coïncide avec la translation (recalculé à chaque pas → tuning live
        // de moveDuration pris en compte).
        if (_jumpClipLength > 0f && _mover != null && _mover.moveDuration > 0f)
            _animator.SetFloat(JumpSpeedMult, _jumpClipLength / _mover.moveDuration);

        // Piège cué dès le départ (lookahead) : la transition Forward_Jump →
        // Hit_Trap se déclenche alors en plein vol. False sinon → retour idle.
        _animator.SetBool(IsOnTrap, targetHasTrap);
        _animator.SetTrigger(JumpTrigger);
    }

    void HandleBlocked()
    {
        if (_animator == null) return;
        _animator.SetTrigger(BlockedTrigger);
    }
}
