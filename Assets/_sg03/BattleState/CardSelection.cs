using System.Collections;
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

        /// <summary>Fired when the player enters full-detail view for a card.</summary>
        public static event System.Action OnFullDetailEntered;

        /// <summary>Fired when the player exits full-detail view.</summary>
        public static event System.Action OnFullDetailExited;

        [SerializeField] private bool debugLog;


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
            if (this.AreClientActionsPending()) return;
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
            if (this.hovered != null)
            {
                if (this.hovered == this.targetingSource) return false;
                this.ConfirmTargeting();
                return true;
            }
            if (this.holderHover == null) return false;
            if (this.holderHover.HolderOwner != Owner.omega) return false;
            this.ConfirmHolderTargeting();
            return true;
        }

        private void BeginTargeting()
        {
            this.targetingSource = this.selected;
            this.targeted = null;
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
            this.DispatchAttackingScripts(source, target);
        }

        private void ConfirmHolderTargeting()
        {
            Card3DCtrl source = this.targetingSource;
            CardHolderCtrl holder = this.holderHover;
            if (source == null || holder == null) return;
            string defenderId = this.ResolveDefenderId(holder);
            Debug.Log($"<color=#00FFAA>[Targeting] <b>{source.name}</b> â†’ <b>{holder.name}</b> ({defenderId})</color>");
            this.battleStateCtrl?.BattleScripts?.RunAlphaAttacking(
                source.InventoryItemId,
                defenderId,
                this.OnAlphaAttackingSuccess,
                null);
        }

        private void DispatchAttackingScripts(Card3DCtrl source, Card3DCtrl target)
        {
            if (this.IsAlphaDrawPhase())
                this.RunAlphaCardDeployThenAttack(source, target);
            else
                this.battleStateCtrl?.BattleScripts?.RunAlphaAttacking(source.InventoryItemId, this.ResolveDefenderId(target), this.OnAlphaAttackingSuccess, null);
        }

        private bool IsAlphaDrawPhase()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            return this.battleStateCtrl.BattleState.NextMove == NextMoveType.alpha_draw;
        }

        private void RunAlphaCardDeployThenAttack(Card3DCtrl source, Card3DCtrl target)
        {
            this.battleStateCtrl?.BattleScripts?.RunAlphaCardDeploy(
                response => this.OnAlphaCardDeploySuccess(response, source, target), null);
        }

        private void OnAlphaCardDeploySuccess(string response, Card3DCtrl source, Card3DCtrl target)
        {
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
            this.StartCoroutine(this.WaitActionsAndAttack(source, target));
        }

        private IEnumerator WaitActionsAndAttack(Card3DCtrl source, Card3DCtrl target)
        {
            yield return null;
            yield return this.WaitUntilActionsComplete();
            this.RunAlphaAttackingAfterDeploy(source, target);
        }

        private IEnumerator WaitUntilActionsComplete()
        {
            ClientActions clientActions = this.battleStateCtrl?.ClientActions;
            if (clientActions == null) yield break;
            yield return new WaitUntil(() => !clientActions.HasPendingActions);
        }

        private void RunAlphaAttackingAfterDeploy(Card3DCtrl source, Card3DCtrl target)
        {
            this.battleStateCtrl?.BattleScripts?.RunAlphaAttacking(
                source.InventoryItemId, this.ResolveDefenderId(target), this.OnAlphaAttackingSuccess, null);
        }

        private string ResolveDefenderId(Card3DCtrl target)
        {
            if (target.CardOwner == Owner.omega && target.Location == Location.in_void)   return "omega_hp";
            if (target.CardOwner == Owner.omega && target.Location == Location.in_source) return "omega_hp";
            return target.InventoryItemId;
        }

        private string ResolveDefenderId(CardHolderCtrl holder)
        {
            if (holder == null) return "omega_hp";
            if (holder.HolderOwner != Owner.omega) return "omega_hp";
            return this.HasAnyOmegaFrontlineCard() ? "omega" : "omega_hp";
        }

        private bool HasAnyOmegaFrontlineCard()
        {
            BattleCardSlot[] slots = this.battleStateCtrl?.BattleState?.OmegaFrontLine;
            if (slots == null) return false;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (!string.IsNullOrEmpty(slot.inventory_item_id)) return true;
            }
            return false;
        }

        private void OnAlphaAttackingSuccess(string response)
        {
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void LogTargetConfirmed(Card3DCtrl source, Card3DCtrl target)
        {
            Debug.Log($"<color=#00FFAA>[Targeting] <b>{source.name}</b> → <b>{target.name}</b></color>");
        }

        private void UpdateArrow()
        {
            if (this.IsBattleCompleted())
            {
                this.ClearInteractionState();
                this.arrowIndicator?.Hide();
                return;
            }
            this.SyncTargetingState();
            if (!this.IsTargeting) return;
            if (this.arrowIndicator == null) return;
            if (this.fullDetail)
            {
                this.arrowIndicator.Hide();
                return;
            }
            if (!this.HasArrowTarget())
            {
                this.arrowIndicator.Hide();
                return;
            }
            Vector3 from = this.targetingSource.transform.position;
            Vector3 to = this.GetArrowTarget();
            this.arrowIndicator.Show(from, to);
        }

        private bool HasArrowTarget()
        {
            if (this.hovered != null && this.hovered != this.targetingSource) return true;
            if (this.holderHover != null) return true;
            return false;
        }

        private void SyncTargetingState()
        {
            if (this.selected == null)
            {
                this.TryCancelTargeting();
                return;
            }
            if (!this.IsAlphaTurn() && !this.IsAlphaDefendingBackLineSelected() && !this.IsAlphaDrawCharacterSelected() && !this.IsAlphaDrawBackLineSelected())
            {
                this.TryCancelTargeting();
                return;
            }
            if (this.selected.CardOwner == Owner.omega)
            {
                this.TryCancelTargeting();
                return;
            }
            if (this.IsSelectedCardTriggered())
            {
                this.TryCancelTargeting();
                return;
            }
            if (this.selected.Location == Location.in_hand)
            {
                this.TryCancelTargeting();
                return;
            }
            if (this.targetingSource != this.selected)
                this.BeginTargeting();
        }

        private bool IsAlphaDefendingBackLineSelected()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            if (!this.battleStateCtrl.BattleState.AlphaDefending) return false;
            return this.selected.CardOwner == Owner.alpha && this.selected.Location == Location.in_back;
        }

        private bool IsAlphaDrawCharacterSelected()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            if (this.battleStateCtrl.BattleState.NextMove != NextMoveType.alpha_draw) return false;
            return this.selected.CardOwner == Owner.alpha && this.selected.IsCharacter();
        }

        private bool IsAlphaDrawBackLineSelected()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            if (this.battleStateCtrl.BattleState.NextMove != NextMoveType.alpha_draw) return false;
            return this.selected.CardOwner == Owner.alpha && this.selected.Location == Location.in_back;
        }

        private bool IsSelectedCardTriggered()
        {
            BattleCardSlot slot = this.FindSlotForCard(this.selected);
            if (slot == null) return false;
            return slot.trigger;
        }

        private BattleCardSlot FindSlotForCard(Card3DCtrl card)
        {
            BattleState state = this.battleStateCtrl?.BattleState;
            if (state == null) return null;
            return this.FindSlotInState(state, card.InventoryItemId);
        }

        private BattleCardSlot FindSlotInState(BattleState state, string inventoryItemId)
        {
            return this.FindInArray(state.AlphaHand,      inventoryItemId)
                ?? this.FindInArray(state.AlphaFrontLine, inventoryItemId)
                ?? this.FindInArray(state.AlphaBackLine,  inventoryItemId)
                ?? this.FindInArray(state.AlphaTheVoid,   inventoryItemId)
                ?? this.FindInArray(state.AlphaTheSource, inventoryItemId);
        }

        private BattleCardSlot FindInArray(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id == inventoryItemId) return slot;
            }
            return null;
        }

        private bool IsAlphaTurn()
        {
            if (this.battleStateCtrl?.BattleState == null) return false;
            return this.battleStateCtrl.BattleState.NextMove == NextMoveType.alpha_turn;
        }

        private Vector3 GetArrowTarget()
        {
            if (this.hovered != null && this.hovered != this.targetingSource)
                return this.hovered.transform.position;
            if (this.holderHover != null)
                return this.holderHover.transform.position;
            return this.GetMouseWorldPosition();
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null) return this.targetingSource.transform.position;
            if (Mouse.current == null) return this.targetingSource.transform.position;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane plane = new Plane(Vector3.up, this.targetingSource.transform.position);
            if (!plane.Raycast(ray, out float distance)) return this.targetingSource.transform.position;
            return ray.GetPoint(distance);
        }

        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited += this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered += this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited += this.OnHolderHoverExited;
            CardHolderCtrl.HolderSelected += this.OnHolderSelected;
        }

        private void Unsubscribe()
        {
            Card3DCtrl.HoverEntered -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited -= this.OnCardHoverExited;
            CardHolderCtrl.HoverEntered -= this.OnHolderHoverEntered;
            CardHolderCtrl.HoverExited -= this.OnHolderHoverExited;
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

        // ─── Placement validation ─────────────────────────────────────────────────

        private bool IsCardDeployPhase()
        {
            if (this.battleStateCtrl?.BattleState == null) { if (this.debugLog) Debug.LogWarning("[CardSelection] IsCardDeployPhase — battleState is NULL"); return false; }
            NextMoveType nextMove = this.battleStateCtrl.BattleState.NextMove;
            bool valid = nextMove == NextMoveType.card_deploy || nextMove == NextMoveType.alpha_draw || nextMove == NextMoveType.alpha_turn;
            if (!valid && this.debugLog) Debug.LogWarning($"[CardSelection] IsCardDeployPhase — NextMove={nextMove} — not a deploy phase, skipped");
            return valid;
        }

        private bool IsPlacementValid(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card.CardOwner == Owner.omega) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — card '{card.name}' is omega-owned — skipped"); return false; }
            if (card.CardOwner != holder.HolderOwner) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — card owner={card.CardOwner} != holder owner={holder.HolderOwner} — skipped"); return false; }
            if (card.IsCharacter() && card.Location == Location.in_hand && this.countCharDeploy >= this.maxCharDeploy) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — char deploy limit reached ({this.countCharDeploy}/{this.maxCharDeploy}) — skipped"); return false; }
            if (card.IsCharacter() && holder.HolderLink != Link.front) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — character card must go to front, but holder link={holder.HolderLink} — skipped"); return false; }
            if (!card.IsCharacter() && holder.HolderLink != Link.back) { if (this.debugLog) Debug.LogWarning($"[CardSelection] IsPlacementValid — non-character card must go to back, but holder link={holder.HolderLink} — skipped"); return false; }
            return true;
        }

        private void TryIncrementCharDeploy(Card3DCtrl card)
        {
            if (!card.IsCharacter()) return;
            this.countCharDeploy++;
            if (this.debugLog) Debug.Log($"[CardSelection] TryIncrementCharDeploy — countCharDeploy={this.countCharDeploy}/{this.maxCharDeploy}");
        }

        public void ResetCharDeployCount()
        {
            this.countCharDeploy = 0;
            if (this.debugLog) Debug.Log($"[CardSelection] ResetCharDeployCount — reset to 0 (max={this.maxCharDeploy})");
        }

        public bool TryConsumePlayerDeploy(string inventoryItemId, Link link, int slotIndex)
        {
            if (string.IsNullOrEmpty(inventoryItemId)) return false;
            if (!this.pendingPlayerDeploys.TryGetValue(inventoryItemId, out PlayerDeployRecord record)) return false;
            if (record.Link != link || record.SlotIndex != slotIndex) return false;
            this.pendingPlayerDeploys.Remove(inventoryItemId);
            if (this.debugLog) Debug.Log($"[CardSelection] Consumed local player deploy — id={inventoryItemId}, link={link}, slot={slotIndex}");
            return true;
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
        }

        private void RegisterPlayerDeploy(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card == null || holder == null) return;
            if (string.IsNullOrEmpty(card.InventoryItemId)) return;
            this.pendingPlayerDeploys[card.InventoryItemId] = new PlayerDeployRecord(holder.HolderLink, holder.Index);
            if (this.debugLog) Debug.Log($"[CardSelection] Registered local player deploy — id={card.InventoryItemId}, link={holder.HolderLink}, slot={holder.Index}");
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

        private readonly struct PlayerDeployRecord
        {
            public PlayerDeployRecord(Link link, int slotIndex)
            {
                this.Link = link;
                this.SlotIndex = slotIndex;
            }

            public Link Link { get; }
            public int SlotIndex { get; }
        }

    }
}
