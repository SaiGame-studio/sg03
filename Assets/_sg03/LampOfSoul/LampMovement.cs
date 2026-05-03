using DG.Tweening;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/LampOfSoul/Lamp Movement")]
    [RequireComponent(typeof(LampOfSoulCtrl))]
    public class LampMovement : SaiBehaviour
    {
        // ─── Linked Components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private LampOfSoulCtrl ctrl;

        // ─── Move Settings ────────────────────────────────────────────────────────

        [Header("Move Settings")]
        [Tooltip("Duration of the move animation in seconds.")]
        [SerializeField] private float duration = 1f;

        [Tooltip("Ease curve applied to the move animation.")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        // ─── Runtime state ────────────────────────────────────────────────────────

        private Tween moveTween;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadLampOfSoulCtrl();
        }

        protected virtual void LoadLampOfSoulCtrl()
        {
            if (this.ctrl != null) return;
            this.ctrl = this.GetComponent<LampOfSoulCtrl>();
            Debug.LogWarning(this.transform.name + ": LoadLampOfSoulCtrl", this.gameObject);
        }

        private void OnDestroy()
        {
            this.KillMoveTween();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Smoothly moves the lamp to the target transform position.</summary>
        public void MoveTo(Transform target)
        {
            this.KillMoveTween();
            this.StartMoveTween(target.position);
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private void StartMoveTween(Vector3 destination)
        {
            this.moveTween = this.transform.DOMove(destination, this.duration).SetEase(this.ease);
        }

        private void KillMoveTween()
        {
            this.moveTween?.Kill();
        }
    }
}
