using SaiGame.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public class CardHolderHoverDetector : SaiBehaviour
    {
        [SerializeField] private Camera mainCamera;

        private CardHolderCtrl currentHovered;

        // ─── LoadComponents ───────────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadMainCamera();
        }

        protected virtual void LoadMainCamera()
        {
            if (this.mainCamera != null) return;
            this.mainCamera = Camera.main;
            Debug.LogWarning(this.transform.name + ": LoadMainCamera", this.gameObject);
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Update() => this.DetectHover();

        // ─── Detection ────────────────────────────────────────────────────────────

        private void DetectHover()
        {
            CardHolderCtrl hit = this.RaycastHolder();
            if (hit == this.currentHovered) return;
            this.ChangeHover(hit);
        }

        private void ChangeHover(CardHolderCtrl next)
        {
            this.ExitCurrentHover();
            this.EnterNewHover(next);
        }

        private void ExitCurrentHover()
        {
            if (this.currentHovered == null) return;
            this.currentHovered.NotifyHoverExited();
            this.currentHovered = null;
        }

        private void EnterNewHover(CardHolderCtrl next)
        {
            if (next == null) return;
            this.currentHovered = next;
            this.currentHovered.NotifyHoverEntered();
        }

        private CardHolderCtrl RaycastHolder()
        {
            if (Mouse.current == null) return null;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = this.mainCamera.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return null;
            return hit.collider.GetComponent<CardHolderCtrl>();
        }
    }
}
