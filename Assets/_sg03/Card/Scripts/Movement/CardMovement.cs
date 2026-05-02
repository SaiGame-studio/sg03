using DG.Tweening;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Handles physical movement of a card in world space.
    /// Attach alongside <see cref="Card3DCtrl"/> on the same GameObject.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Movement")]
    [RequireComponent(typeof(Card3DCtrl))]
    public class CardMovement : SaiBehaviour
    {
        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3DCtrl ctrl;

        // ─── Config ───────────────────────────────────────────────────────────────

        [Header("Animation")]
        [Tooltip("Duration of the move animation in seconds.")]
        [SerializeField] private float duration = 1f;

        [Tooltip("Ease curve applied to the move animation.")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Header("State")]
        [SerializeField] private Location location;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3DCtrl();
        }

        protected virtual void LoadCard3DCtrl()
        {
            if (this.ctrl != null) return;
            this.ctrl = this.GetComponent<Card3DCtrl>();
            Debug.LogWarning(transform.name + ": LoadCard3DCtrl", gameObject);
        }

        private void OnDestroy() => this.transform.DOKill();

        // ─── Public API ───────────────────────────────────────────────────────────

        public Location Location => this.location;

        /// <summary>
        /// Smoothly moves the card to the specified world-space <paramref name="target"/> position.
        /// Any in-progress tween is cancelled before starting the new one.
        /// </summary>
        /// <param name="target">Destination transform in world space.</param>
        /// <param name="destination">Location enum value for the destination.</param>
        public void MoveTo(Transform target, Location destination)
        {
            this.location = destination;
            this.transform.DOKill();
            this.transform.DOMove(target.position, this.duration).SetEase(this.ease);
            this.transform.DORotateQuaternion(target.rotation, this.duration).SetEase(this.ease);
        }

    }
}
