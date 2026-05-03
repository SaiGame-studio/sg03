using SaiGame.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public class CardSelection : SaiBehaviour
    {
        [SerializeField] private Card3DCtrl selected;
        [SerializeField] private Card3DCtrl hovered;

        [Header("Holders")]
        [SerializeField] private CardHolderCtrl holderSelected;
        [SerializeField] private CardHolderCtrl holderHover;

        [Header("Full Detail")]
        [SerializeField] private DeskPositionCtrl deskPositions;
        [SerializeField] private bool fullDetail;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadDeskPositions();
        }

        protected virtual void LoadDeskPositions()
        {
            if (this.deskPositions != null) return;
            this.deskPositions = Object.FindFirstObjectByType<DeskPositionCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadDeskPositions", this.gameObject);
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable() => this.Subscribe();
        private void OnDisable() => this.Unsubscribe();
        private void Update() => this.CheckClick();

        // ─── Click detection ──────────────────────────────────────────────────────

        private void CheckClick()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            if (CardMovement.IsAnyCardMoving) return;
            this.HandleCardClick();
            this.HandleHolderClick();
        }

        private bool IsMouseClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void HandleCardClick()
        {
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
            if (this.fullDetail)
            {
                this.ExitFullDetail();
                return;
            }
            if (this.IsClickOnSelected())
            {
                this.EnterFullDetail();
                return;
            }
            this.SelectHovered();
        }

        private void HandleHolderClick()
        {
            if (this.holderHover == null) return;
            this.holderHover.NotifySelected();
        }

        private bool IsClickOnSelected()
        {
            return this.hovered == this.selected && this.selected != null;
        }

        private void SelectHovered()
        {
            this.fullDetail = false;
            this.selected = this.hovered;
            this.selected.NotifySelected();
        }

        private void EnterFullDetail()
        {
            if (this.deskPositions == null) return;
            this.fullDetail = true;
            this.selected.MoveToFullDetail(this.deskPositions.FullDetailPoint);
        }

        private void ExitFullDetail()
        {
            this.fullDetail = false;
            this.selected.ReturnFromFullDetail();
        }

        private bool IsLocationNonSelectable(Location location)
        {
            return location == Location.in_source || location == Location.in_void;
        }

        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered    += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited     += this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered  += this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited   += this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected += this.OnHolderSelected;
        }

        private void Unsubscribe()
        {
            Card3DCtrl.HoverEntered    -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited     -= this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered  -= this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited   -= this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected -= this.OnHolderSelected;
        }

        // ─── Card hover handlers ──────────────────────────────────────────────────

        private void OnCardHoverEntered(Card3DCtrl card) => this.hovered = card;

        private void OnCardHoverExited(Card3DCtrl card) => this.ClearHoveredIfMatch(card);

        private void ClearHoveredIfMatch(Card3DCtrl card)
        {
            if (this.hovered != card) return;
            this.hovered = null;
        }

        // ─── Holder hover/select handlers ────────────────────────────────────────

        private void OnHolderHoverEntered(CardHolderCtrl holder) => this.holderHover = holder;

        private void OnHolderHoverExited(CardHolderCtrl holder) => this.ClearHolderHoverIfMatch(holder);

        private void ClearHolderHoverIfMatch(CardHolderCtrl holder)
        {
            if (this.holderHover != holder) return;
            this.holderHover = null;
        }

        private void OnHolderSelected(CardHolderCtrl holder) => this.holderSelected = holder;
    }
}
