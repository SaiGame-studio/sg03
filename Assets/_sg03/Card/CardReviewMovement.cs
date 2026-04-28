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
        [Header("Fly Up")]
        [Tooltip("Distance (world units) to move upward along the Y axis.")]
        [SerializeField] private float flyUpDistance = 2f;

        [Header("Fly Down")]
        [Tooltip("World-space position recorded at Start. Fly Down returns here.")]
        [SerializeField] private Vector3 originPosition;

        [Header("Animation")]
        [Tooltip("Duration of the fly animation in seconds.")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Ease curve applied to both Fly Up and Fly Down.")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start() => originPosition = transform.position;

        private void OnDestroy() => transform.DOKill();

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Smoothly moves the card upward by <see cref="flyUpDistance"/> on the Y axis.</summary>
        public void FlyUp()
        {
            Vector3 target = transform.position + new Vector3(0f, flyUpDistance, 0f);
            transform.DOKill();
            transform.DOMove(target, duration).SetEase(ease);
        }

        /// <summary>Smoothly returns the card to its position at scene start.</summary>
        public void FlyDown()
        {
            transform.DOKill();
            transform.DOMove(originPosition, duration).SetEase(ease);
        }
    }
}
