using SaiGame.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public class CardHoverDetector : SaiBehaviour
    {
        [SerializeField] private Camera mainCamera;

        private Card3DCtrl currentHovered;

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
            Debug.LogWarning(transform.name + "LoadMainCamera", gameObject);
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Update() => this.DetectHover();

        // ─── Detection ────────────────────────────────────────────────────────────

        private void DetectHover()
        {
            Card3DCtrl hit = this.RaycastCard();
            if (hit == this.currentHovered) return;
            this.ChangeHover(hit);
        }

        private void ChangeHover(Card3DCtrl next)
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

        private void EnterNewHover(Card3DCtrl next)
        {
            if (next == null) return;
            this.currentHovered = next;
            this.currentHovered.NotifyHoverEntered();
        }

        private Card3DCtrl RaycastCard()
        {
            if (Mouse.current == null) return null;
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = this.mainCamera.ScreenPointToRay(mousePos);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                Card3DCtrl card = hit.collider.GetComponent<Card3DCtrl>();
                if (card != null) return card;
            }
            return null;
        }
    }
}
