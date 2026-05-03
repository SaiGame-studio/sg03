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

        [Header("Marks")]
        [SerializeField] private GameObject markSelected;
        [SerializeField] private float markFollowSpeed = 10f;
        [SerializeField] private Transform markIdlePosition;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadDeskPositions();
            this.LoadMarkSelected();
            this.LoadMarkIdlePosition();
        }

        protected virtual void LoadMarkIdlePosition()
        {
            if (this.markIdlePosition != null) return;
            if (this.deskPositions == null) return;
            this.markIdlePosition = this.deskPositions.AlphaTheSource;
            Debug.LogWarning(this.transform.name + ": LoadMarkIdlePosition", this.gameObject);
        }

        protected virtual void LoadMarkSelected()
        {
            if (this.markSelected != null) return;
            this.markSelected = this.transform.Find("MarkSelected")?.gameObject;
            Debug.LogWarning(this.transform.name + ": LoadMarkSelected", this.gameObject);
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
        private void Update()
        {
            this.CheckClick();
            this.UpdateMarkSelected();
        }

        // ─── Mark selected ────────────────────────────────────────────────────────

        private void UpdateMarkSelected()
        {
            if (this.markSelected == null) return;
            if (this.selected == null || !this.IsLocationFlippable(this.selected.Location))
            {
                this.HideMarkSelected();
                return;
            }
            if (CardMovement.IsAnyCardMoving)
            {
                this.HideMarkSelected();
                return;
            }
            this.SnapToSelectedCard();
            this.ShowMarkSelected();
        }

        private void ShowMarkSelected()
        {
            if (this.markSelected.activeSelf) return;
            this.markSelected.SetActive(true);
        }

        private void HideMarkSelected()
        {
            if (!this.markSelected.activeSelf) return;
            this.markSelected.SetActive(false);
        }

        private void FollowIdlePosition()
        {
            if (this.markIdlePosition == null) return;
            this.markSelected.transform.position = Vector3.Lerp(
                this.markSelected.transform.position,
                this.markIdlePosition.position,
                this.markFollowSpeed * Time.deltaTime);
        }

        private void SnapToSelectedCard()
        {
            this.markSelected.transform.position = this.selected.transform.position;
        }

        // ─── Click detection ──────────────────────────────────────────────────────

        private void CheckClick()
        {
            if (CardMovement.IsAnyCardMoving) return;
            if (this.fullDetail)
            {
                this.HandleFullDetailClick();
                return;
            }
            this.HandleLeftClick();
            this.HandleRightClick();
        }

        private void HandleFullDetailClick()
        {
            if (this.hovered != this.selected) return;
            if (!this.IsMouseClickedThisFrame() && !this.IsMouseRightClickedThisFrame()) return;
            this.ExitFullDetail();
        }

        private void HandleLeftClick()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            this.HandleCardClick();
            this.HandleHolderClick();
        }

        private void HandleRightClick()
        {
            if (!this.IsMouseRightClickedThisFrame()) return;
            if (this.TrySwapWithHovered()) return;
            this.HandleSelectedToggleFace();
        }

        private bool TrySwapWithHovered()
        {
            if (this.selected == null) return false;
            if (this.hovered == null) return false;
            if (this.hovered == this.selected) return false;
            if (!this.IsLocationFlippable(this.hovered.Location)) return false;
            this.SwapSelectedWithHovered();
            return true;
        }

        private void SwapSelectedWithHovered()
        {
            Card3DCtrl     otherCard    = this.hovered;
            CardHolderCtrl targetHolder = otherCard.CardHolder;
            CardHolderCtrl prevHolder   = this.selected.CardHolder;
            this.selected.SetCardHolder(targetHolder);
            targetHolder?.SetCard(this.selected);
            otherCard.SetCardHolder(prevHolder);
            prevHolder?.SetCard(otherCard);
        }

        private bool IsMouseClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        private bool IsMouseRightClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.rightButton.wasPressedThisFrame;
        }

        private void HandleCardClick()
        {
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
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
            if (this.IsLocationFlippable(this.selected.Location)) return;
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

        private void HandleSelectedToggleFace()
        {
            if (this.selected == null) return;
            if (this.hovered != this.selected) return;
            if (!this.IsLocationFlippable(this.selected.Location)) return;
            this.selected.ToggleFace();
            this.selected = null;
        }

        private bool IsLocationFlippable(Location location)
        {
            return location == Location.in_front || location == Location.in_back;
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

        private void OnHolderSelected(CardHolderCtrl holder)
        {
            this.holderSelected = holder;
            if (this.selected == null) return;
            if (holder.HeldCard != null) return;
            this.PlaceSelectedIntoEmptyHolder(holder);
        }

        private void PlaceSelectedIntoEmptyHolder(CardHolderCtrl targetHolder)
        {
            this.selected.CardHolder?.SetCard(null);
            this.selected.SetCardHolder(targetHolder);
            targetHolder.SetCard(this.selected);
        }
    }
}
