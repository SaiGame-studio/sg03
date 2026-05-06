using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public class CardSelection : SaiBehaviour
    {
        // ─── Static events ────────────────────────────────────────────────────────

        /// <summary>Fired when the player confirms a targeting selection (source → target).</summary>
        public static event System.Action<Card3DCtrl, Card3DCtrl> TargetSelected;

        [SerializeField] private Card3DCtrl selected;
        [SerializeField] private Card3DCtrl hovered;

        [Header("Holders")]
        [SerializeField] private CardHolderCtrl holderSelected;
        [SerializeField] private CardHolderCtrl holderHover;

        [Header("Battle State")]
        [SerializeField] private BattleState battleState;

        [Header("Full Detail")]
        [SerializeField] private DeskPositionCtrl deskPositions;
        [SerializeField] private bool fullDetail;

        [Header("Marks")]
        [SerializeField] private GameObject markSelected;
        [SerializeField] private float markFollowSpeed = 10f;
        [SerializeField] private Transform markIdlePosition;

        [Header("Targeting")]
        [SerializeField] private ArrowIndicatorCtrl arrowIndicator;
        [SerializeField] private Card3DCtrl targeted;
        private Card3DCtrl targetingSource;

        [Header("Front Line Holders")]
        [SerializeField] private CardHolderCtrl[] alphaFrontLineHolders;

        private bool IsTargeting => this.targetingSource != null;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleState();
            this.LoadDeskPositions();
            this.LoadMarkSelected();
            this.LoadMarkIdlePosition();
            this.LoadArrowIndicator();
            this.LoadAlphaFrontLineHolders();
        }

        protected virtual void LoadAlphaFrontLineHolders()
        {
            if (this.alphaFrontLineHolders != null && this.alphaFrontLineHolders.Length > 0) return;
            CardHolderCtrl[] all = Object.FindObjectsByType<CardHolderCtrl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<CardHolderCtrl> result = new List<CardHolderCtrl>();
            foreach (CardHolderCtrl h in all)
            {
                if (h.HolderOwner != Owner.alpha) continue;
                if (h.HolderLink != Link.front) continue;
                result.Add(h);
            }
            this.alphaFrontLineHolders = result.ToArray();
            Debug.LogWarning(this.transform.name + ": LoadAlphaFrontLineHolders", this.gameObject);
        }

        protected virtual void LoadArrowIndicator()
        {
            if (this.arrowIndicator != null) return;
            GameObject go = GameObject.Find("ArrowIndicatorCtrl");
            if (go == null) return;
            this.arrowIndicator = go.GetComponent<ArrowIndicatorCtrl>();
            Debug.LogWarning(this.transform.name + ": FindArrowIndicator", this.gameObject);
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            BattleStateCtrl ctrl = this.GetComponentInParent<BattleStateCtrl>(true);
            if (ctrl == null) return;
            this.battleState = ctrl.BattleState;
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
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
            this.UpdateArrow();
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
            this.HandleMiddleClick();
            this.HandleRightClick();
        }

        private void HandleFullDetailClick()
        {
            if (this.hovered != this.selected) return;
            if (!this.IsMouseClickedThisFrame()) return;
            this.ExitFullDetail();
        }

        private void HandleLeftClick()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            this.HandleCardClick();
            this.HandleHolderClick();
        }

        private void HandleMiddleClick()
        {
            if (!this.IsMouseMiddleClickedThisFrame()) return;
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
            if (!this.IsClickOnSelected()) this.SelectHovered();
            this.EnterFullDetail();
        }

        private void HandleRightClick()
        {
            if (!this.IsMouseRightClickedThisFrame()) return;
            if (this.TryConfirmTargeting()) return;
            if (this.TrySwapWithHovered()) return;
            this.HandleSelectedToggleFace();
        }

        private bool TrySwapWithHovered()
        {
            if (this.selected == null) return false;
            if (this.hovered == null) return false;
            if (this.hovered == this.selected) return false;
            if (!this.IsCardDeployPhase()) return false;
            if (!this.IsLocationFlippable(this.hovered.Location)) return false;
            if (!this.IsSwapValid()) return false;
            CardHolderCtrl selectedHolder = this.selected.CardHolder;
            CardHolderCtrl hoveredHolder  = this.hovered.CardHolder;
            this.SwapSelectedWithHovered();
            this.NotifyBattleStateOnSwap(selectedHolder, hoveredHolder);
            return true;
        }

        private void SwapSelectedWithHovered()
        {
            Card3DCtrl     otherCard    = this.hovered;
            if (this.selected.IsFlipping) return;
            if (otherCard.IsFlipping) return;
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

        private bool IsMouseMiddleClickedThisFrame()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.middleButton.wasPressedThisFrame;
        }

        private void HandleCardClick()
        {
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
            if (this.IsClickOnSelected()) return;
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

        private void HandleSelectedToggleFace()
        {
            if (this.selected == null) return;
            if (this.hovered != this.selected) return;
            if (!this.IsLocationFlippable(this.selected.Location)) return;
            if (this.selected.Expose && this.selected.FaceState == FaceState.FaceUp) return;
            this.selected.ToggleFace();
        }

        private bool IsLocationFlippable(Location location)
        {
            return location == Location.in_front || location == Location.in_back;
        }

        // ─── Targeting ────────────────────────────────────────────────────────────

        private bool TryBeginTargeting()
        {
            if (this.selected == null) return false;
            this.BeginTargeting();
            return true;
        }

        private bool TryCancelTargeting()
        {
            if (!this.IsTargeting) return false;
            this.CancelTargeting();
            return true;
        }

        private bool TryConfirmTargeting()
        {
            if (!this.IsTargeting) return false;
            if (this.hovered == null) return false;
            if (this.hovered == this.targetingSource) return false;
            this.ConfirmTargeting();
            return true;
        }

        private void BeginTargeting()
        {
            this.targetingSource = this.selected;
            this.targeted = null;
            if (this.arrowIndicator == null) return;
            this.arrowIndicator.Show(this.targetingSource.transform.position, this.targetingSource.transform.position);
        }

        private void CancelTargeting()
        {
            this.targetingSource = null;
            this.targeted = null;
            this.arrowIndicator?.Hide();
        }

        private void ConfirmTargeting()
        {
            Card3DCtrl source = this.targetingSource;
            Card3DCtrl target = this.hovered;
            this.targeted = target;
            this.LogTargetConfirmed(source, target);
            TargetSelected?.Invoke(source, target);
        }

        private void LogTargetConfirmed(Card3DCtrl source, Card3DCtrl target)
        {
            Debug.Log($"<color=#00FFAA>[Targeting] <b>{source.name}</b> → <b>{target.name}</b></color>");
        }

        private void UpdateArrow()
        {
            this.SyncTargetingState();
            if (!this.IsTargeting) return;
            if (this.arrowIndicator == null) return;
            Vector3 from = this.targetingSource.transform.position;
            Vector3 to   = this.GetArrowTarget();
            this.arrowIndicator.UpdateTarget(from, to);
        }

        private void SyncTargetingState()
        {
            if (!this.IsAlphaTurn() || this.selected == null)
            {
                this.TryCancelTargeting();
                return;
            }
            if (this.targetingSource != this.selected)
                this.BeginTargeting();
        }

        private bool IsAlphaTurn()
        {
            if (this.battleState == null) return false;
            return this.battleState.NextMove == NextMoveType.alpha_turn;
        }

        private Vector3 GetArrowTarget()
        {
            if (this.hovered != null && this.hovered != this.targetingSource)
                return this.hovered.transform.position;
            return this.GetMouseWorldPosition();
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return this.targetingSource.transform.position;
            if (Mouse.current == null) return this.targetingSource.transform.position;
            Ray ray     = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane plane = new Plane(Vector3.up, this.targetingSource.transform.position);
            if (!plane.Raycast(ray, out float distance)) return this.targetingSource.transform.position;
            return ray.GetPoint(distance);
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
            if (!this.IsCardDeployPhase()) return;
            if (holder.HeldCard != null) return;
            if (!this.IsPlacementValid(this.selected, holder)) return;
            Location fromLocation = this.selected.Location;
            CardHolderCtrl fromHolder = this.selected.CardHolder;
            this.PlaceSelectedIntoEmptyHolder(holder);
            this.NotifyBattleStateOnPlacement(fromLocation, fromHolder, holder);
        }

        private void NotifyBattleStateOnPlacement(Location fromLocation, CardHolderCtrl fromHolder, CardHolderCtrl targetHolder)
        {
            if (this.battleState == null) return;
            if (fromLocation == Location.in_hand)
            {
                this.battleState.MoveCardFromHandToLine(this.selected.CodeName, targetHolder.HolderLink, targetHolder.Index);
                return;
            }
            if (fromLocation != Location.in_front && fromLocation != Location.in_back) return;
            if (fromHolder == null) return;
            this.battleState.MoveCardOnLine(this.selected.CodeName, fromHolder.HolderLink, fromHolder.Index, targetHolder.HolderLink, targetHolder.Index);
        }

        private void NotifyBattleStateOnSwap(CardHolderCtrl holderA, CardHolderCtrl holderB)
        {
            if (this.battleState == null) return;
            if (holderA == null || holderB == null) return;
            this.battleState.SwapCardsOnLine(holderA.HolderLink, holderA.Index, holderB.HolderLink, holderB.Index);
        }

        private void PlaceSelectedIntoEmptyHolder(CardHolderCtrl targetHolder)
        {
            if (this.selected.IsFlipping) return;
            this.selected.CardHolder?.SetCard(null);
            this.selected.SetCardHolder(targetHolder);
            targetHolder.SetCard(this.selected);
        }

        // ─── Placement validation ─────────────────────────────────────────────────

        private bool IsCardDeployPhase()
        {
            if (this.battleState == null) return false;
            return this.battleState.NextMove == NextMoveType.card_deploy;
        }

        private bool IsPlacementValid(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card.CardOwner == Owner.omega) return false;
            if (card.CardOwner != holder.HolderOwner) return false;
            if (card.IsCharacter() && holder.HolderLink != Link.front) return false;
            if (!card.IsCharacter() && holder.HolderLink != Link.back) return false;
            if (card.IsCharacter() && this.IsCharacterAlreadyOnFrontLine(card)) return false;
            return true;
        }

        private bool IsCharacterAlreadyOnFrontLine(Card3DCtrl excludeCard)
        {
            if (this.alphaFrontLineHolders == null) return false;
            foreach (CardHolderCtrl h in this.alphaFrontLineHolders)
            {
                if (h.HeldCard == null) continue;
                if (h.HeldCard == excludeCard) continue;
                if (h.HeldCard.IsCharacter()) return true;
            }
            return false;
        }

        private bool IsSwapValid()
        {
            if (!this.IsPlacementValid(this.selected, this.hovered.CardHolder)) return false;
            if (!this.IsPlacementValid(this.hovered, this.selected.CardHolder)) return false;
            return true;
        }
    }
}
