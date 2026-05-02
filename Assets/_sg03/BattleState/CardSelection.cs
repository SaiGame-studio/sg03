using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public class CardSelection : MonoBehaviour
    {
        [SerializeField] private Card3DCtrl selected;
        [SerializeField] private Card3DCtrl hovered;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable() => this.Subscribe();
        private void OnDisable() => this.Unsubscribe();
        private void Update() => this.CheckClick();

        // ─── Click detection ──────────────────────────────────────────────────────

        private void CheckClick()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            this.SelectHovered();
        }

        private bool IsMouseClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void SelectHovered()
        {
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
            this.selected = this.hovered;
        }

        private bool IsLocationNonSelectable(Location location)
        {
            return location == Location.in_source || location == Location.in_void;
        }

        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited += this.OnCardHoverExited;
        }

        private void Unsubscribe()
        {
            Card3DCtrl.HoverEntered -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited -= this.OnCardHoverExited;
        }

        // ─── Hover handlers ───────────────────────────────────────────────────────

        private void OnCardHoverEntered(Card3DCtrl card) => this.hovered = card;

        private void OnCardHoverExited(Card3DCtrl card) => this.ClearHoveredIfMatch(card);

        private void ClearHoveredIfMatch(Card3DCtrl card)
        {
            if (this.hovered != card) return;
            this.hovered = null;
        }
    }
}
