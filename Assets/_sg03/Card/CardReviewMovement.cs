using DG.Tweening;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Temporary review utility that lets a card fly up by a configurable distance
    /// or fly back down to the position it occupied when the scene started.
    /// Uses DOTween for smooth movement.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Review Movement")]
    public class CardReviewMovement : MonoBehaviour
    {
        [Header("Show")]
        [Tooltip("Distance (world units) to move upward along the Y axis.")]
        [SerializeField] private float flyUpDistance = 21f;

        [Header("Hide")]
        [Tooltip("World-space position recorded at Start. Hide returns here.")]
        [SerializeField] private Vector3 originPosition;

        private bool isShown;

        /// <summary>Returns true if the card is currently in the shown (fly-up) state.</summary>
        public bool IsShown => isShown;

        [Header("Animation")]
        [Tooltip("Duration of the fly animation in seconds.")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Ease curve applied to both Fly Up and Fly Down.")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            originPosition = transform.position;
            isShown = false;
        }

        private void OnDestroy() => transform.DOKill();

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Smoothly moves the card upward by <see cref="flyUpDistance"/> on the Y axis
        /// while spinning 360° on the Y axis. Ends face-up (Y rotation = 0°).
        /// Does nothing if the card is already shown.
        /// </summary>
        public void Show()
        {
            if (isShown) return;
            isShown = true;

            Vector3 moveTarget   = transform.position + new Vector3(0f, flyUpDistance, 0f);
            Vector3 rotateTarget = transform.eulerAngles + new Vector3(0f, 360f, 0f);

            transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(moveTarget, duration).SetEase(ease));
            seq.Join(transform.DORotate(rotateTarget, duration, RotateMode.FastBeyond360).SetEase(ease));
        }

        /// <summary>
        /// Smoothly returns the card to its origin position while spinning
        /// 360° on the Y axis. Ends face-up (Y rotation = 0°).
        /// Resets the shown state so <see cref="Show"/> can be called again.
        /// Returns the Sequence so callers can chain an OnComplete callback.
        /// </summary>
        public Sequence Hide()
        {
            isShown = false;

            Vector3 rotateTarget = transform.eulerAngles + new Vector3(0f, 360f, 0f);

            transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(originPosition, duration).SetEase(ease));
            seq.Join(transform.DORotate(rotateTarget, duration, RotateMode.FastBeyond360).SetEase(ease));
            seq.AppendCallback(() => transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z));
            return seq;
        }
    }
}
