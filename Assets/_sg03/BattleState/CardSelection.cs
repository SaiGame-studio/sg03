using System.Collections;
using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    public partial class CardSelection : SaiBehaviour
    {
        // ─── Static events ────────────────────────────────────────────────────────

        /// <summary>Fired when the player confirms a targeting selection (source → target).</summary>
        public static event System.Action<Card3DCtrl, Card3DCtrl> TargetSelected;

        /// <summary>Fired when the player enters full-detail view for a card.</summary>
        public static event System.Action OnFullDetailEntered;

        /// <summary>Fired when the player exits full-detail view.</summary>
        public static event System.Action OnFullDetailExited;

        [SerializeField] private bool debugLog;

        [Header("Input Control")]
        [SerializeField] private bool debugMouseEvents = false;

        [SerializeField] private BattleStateCtrl battleStateCtrl;

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

        [Header("Targeting")]
        [SerializeField] private ArrowIndicatorCtrl arrowIndicator;
        [SerializeField] private Card3DCtrl targeted;
        private Card3DCtrl targetingSource;

        [Header("Front Line Holders")]
        [SerializeField] private CardHolderCtrl[] alphaFrontLineHolders;

        [Header("Deploy Limit")]
        [SerializeField] private int maxCharDeploy   = 1;
        [SerializeField] private int countCharDeploy = 0;

        private readonly Dictionary<string, PlayerDeployRecord> pendingPlayerDeploys = new Dictionary<string, PlayerDeployRecord>();

        private bool IsTargeting => this.targetingSource != null;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleStateCtrl();
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

        protected virtual void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = this.GetComponentInParent<BattleStateCtrl>(true);
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
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
            if (this.fullDetail)
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
            if (this.IsBattleCompleted())
            {
                if (this.IsMouseClickedThisFrame()) { if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Cannot click: Battle is completed."); }
                this.ClearInteractionState();
                return;
            }
            if (CardMovement.IsAnyCardMoving)
            {
                this.HandleCardSelectionOnly();
                return;
            }
            if (this.fullDetail)
            {
                this.HandleFullDetailClick();
                return;
            }
            this.HandleLeftClick();
            this.HandleMiddleClick();
            this.HandleRightClick();
        }

        private void HandleCardSelectionOnly()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            this.HandleCardClick();
        }

        private void HandleFullDetailClick()
        {
            if (this.hovered != this.selected) return;
            if (!this.IsMouseClickedThisFrame() && !this.IsMouseMiddleClickedThisFrame()) return;
            this.ExitFullDetail();
        }

        private void HandleLeftClick()
        {
            if (!this.IsMouseClickedThisFrame()) return;
            if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Left click detected in HandleLeftClick!");
            this.HandleCardClick();
            this.HandleHolderClick();
        }

        private void HandleMiddleClick()
        {
            if (!this.IsMouseMiddleClickedThisFrame()) return;
            if (this.AreClientActionsPending()) return;
            if (this.hovered == null) return;
            if (this.IsLocationNonSelectable(this.hovered.Location)) return;
            if (!this.IsClickOnSelected()) this.SelectHovered();
            this.EnterFullDetail();
        }

        private void HandleRightClick()
        {
            if (!this.IsMouseRightClickedThisFrame()) return;
            if (this.TryConfirmTargeting()) return;
            this.HandleSelectedToggleFace();
        }

        private bool IsMouseClickedThisFrame()
        {
            if (Mouse.current == null) 
            {
                // Commented out to avoid spam, but if we need it we can log here
                return false;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Mouse leftButton.wasPressedThisFrame is TRUE!");
                return true;
            }
            return false;
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
            if (this.AreClientActionsPending()) { if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Cannot click: Actions pending"); return; }
            if (this.hovered == null) { if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Cannot click: Hovered card is null"); return; }
            if (this.IsLocationNonSelectable(this.hovered.Location)) { if (this.debugMouseEvents) Debug.LogWarning($"[CardSelection] Cannot click: Location {this.hovered.Location} non-selectable"); return; }
            
            if (this.IsClickOnSelected()) 
            { 
                if (this.debugMouseEvents) Debug.LogWarning("[CardSelection] Re-selecting already selected card"); 
            }
            else
            {
                if (this.debugMouseEvents) Debug.LogWarning($"[CardSelection] Selecting hovered card: {this.hovered.name}");
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
            this.EvaluateTargetingStart();
        }

        private void EnterFullDetail()
        {
            if (this.deskPositions == null) return;
            this.fullDetail = true;
            this.arrowIndicator?.Hide();
            OnFullDetailEntered?.Invoke();
            this.selected.MoveToFullDetail(this.deskPositions.FullDetailPoint);
        }

        private void ExitFullDetail()
        {
            this.fullDetail = false;
            if (this.IsTargeting)
                this.arrowIndicator?.Show(this.targetingSource.transform.position, this.targetingSource.transform.position);
            OnFullDetailExited?.Invoke();
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



        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited += this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered += this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited += this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected += this.OnHolderSelected;
            BattleState.OnNextMoveChanged += this.OnNextMoveChanged;
            Card3DCtrl.LocationChanged += this.OnLocationChanged;
            Card3DCtrl.TriggerStateChanged += this.OnTriggerStateChanged;
        }

        private void Unsubscribe()
        {
            Card3DCtrl.HoverEntered -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited -= this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered -= this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited -= this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected -= this.OnHolderSelected;
            BattleState.OnNextMoveChanged -= this.OnNextMoveChanged;
            Card3DCtrl.LocationChanged -= this.OnLocationChanged;
            Card3DCtrl.TriggerStateChanged -= this.OnTriggerStateChanged;
        }

        private void OnNextMoveChanged(NextMoveType nextMove)
        {
            this.ClearInteractionState();
        }

        private void OnLocationChanged(Card3DCtrl card, Location newLocation)
        {
            if (this.selected == card && newLocation == Location.in_void)
            {
                this.ClearInteractionState();
            }
        }

        private void OnTriggerStateChanged(Card3DCtrl card, bool isTrigger)
        {
            if (this.selected == card && isTrigger)
            {
                this.ClearInteractionState();
            }
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
            if (this.IsBattleCompleted()) return;
            if (this.AreClientActionsPending()) return;
            if (this.selected == null) { if (this.debugLog) Debug.LogWarning("[CardSelection] OnHolderSelected — no card selected"); return; }
            if (this.debugLog) Debug.Log($"[CardSelection] OnHolderSelected — card='{this.selected.name}' owner={this.selected.CardOwner} isCharacter={this.selected.IsCharacter()} location={this.selected.Location} → holder='{holder.name}' link={holder.HolderLink} owner={holder.HolderOwner} heldCard={holder.HeldCard?.name ?? "null"}");
            if (!this.IsCardDeployPhase()) return;
            if (this.selected.Location != Location.in_hand) return;
            if (holder.HeldCard != null) { if (this.debugLog) Debug.LogWarning($"[CardSelection] OnHolderSelected — holder '{holder.name}' already has card '{holder.HeldCard.name}' — skipped"); return; }
            if (!this.IsPlacementValid(this.selected, holder)) return;
            this.PlaceFromHandIntoHolder(holder);
            this.UpdateLocalStateOnPlacement(holder, this.selected);
            this.RunDeployScript(this.selected);
        }

        private void RunDeployScript(Card3DCtrl card)
        {
            this.battleStateCtrl?.BattleScripts?.RunAlphaCardDeploy(
                response => this.OnDeployScriptSuccess(response, card), null);
        }

        private void OnDeployScriptSuccess(string response, Card3DCtrl card)
        {
            this.TryIncrementCharDeploy(card);
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void UpdateLocalStateOnPlacement(CardHolderCtrl holder, Card3DCtrl card)
        {
            if (this.battleStateCtrl?.BattleState == null) return;
            this.RegisterPlayerDeploy(card, holder);
            this.battleStateCtrl.BattleState.MoveCardFromHandToLine(card.InventoryItemId, holder.HolderLink, holder.Index);
        }

        private void PlaceFromHandIntoHolder(CardHolderCtrl targetHolder)
        {
            if (this.selected.IsFlipping) { if (this.debugLog) Debug.LogWarning($"[CardSelection] PlaceFromHandIntoHolder — card '{this.selected.name}' is still flipping — skipped"); return; }
            Card3DCtrl cardToRotate = this.selected;
            this.ApplyBattleMotionSettings(cardToRotate);
            cardToRotate.MoveToUnknow(targetHolder, () => this.StartCoroutine(this.RotateAfterArrival(cardToRotate)));
            targetHolder.SetCard(cardToRotate);
        }

        private IEnumerator RotateAfterArrival(Card3DCtrl card)
        {
            yield return new UnityEngine.WaitUntil(() => !card.IsFlipping);
            card.RotateZ180();
        }



        private bool AreClientActionsPending()
        {
            return this.battleStateCtrl?.ClientActions?.HasPendingActions == true;
        }

        private bool IsBattleCompleted()
        {
            string battleStatus = this.battleStateCtrl?.BattleState?.BattleStatus;
            return string.Equals(battleStatus, "completed", System.StringComparison.OrdinalIgnoreCase);
        }

        private void ClearInteractionState()
        {
            this.fullDetail = false;
            this.selected = null;
            this.targeted = null;
            this.targetingSource = null;
            this.holderSelected = null;
            this.arrowIndicator?.Hide();
            Card3DCtrl.NotifyDeselected();
        }



        private void ApplyBattleMotionSettings(Card3DCtrl card)
        {
            if (card == null) return;
            if (this.battleStateCtrl == null) return;
            card.SetMoveDuration(this.battleStateCtrl.CardMoveDuration);
            card.SetRotateDuration(this.battleStateCtrl.CardRotateDuration);
        }

        private Card3DCtrl FindFrontLineCharacter(Card3DCtrl excludeCard)
        {
            if (this.alphaFrontLineHolders == null) return null;
            foreach (CardHolderCtrl h in this.alphaFrontLineHolders)
            {
                if (h.HeldCard == null) continue;
                if (h.HeldCard == excludeCard) continue;
                if (string.IsNullOrEmpty(h.HeldCard.InventoryItemId)) continue;
                if (h.HeldCard.IsCharacter()) return h.HeldCard;
            }
            return null;
        }



    }
}
