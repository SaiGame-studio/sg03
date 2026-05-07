using System;
using SG03;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Listens to Card3DCtrl hover events and displays a floating UI Toolkit tooltip
    // above the hovered card showing final_def and total_damage_received.
    public class GameCardHoverTooltipUI
    {
        private readonly Func<BattleStateCtrl> getBattleStateCtrl;

        private VisualElement tooltip;
        private Label finalDefLabel;
        private Label damageReceivedLabel;

        private Card3DCtrl hoveredCard;

        public GameCardHoverTooltipUI(Func<BattleStateCtrl> getBattleStateCtrl)
        {
            this.getBattleStateCtrl = getBattleStateCtrl;
        }

        // ─── Binding ──────────────────────────────────────────────────────────────

        public void Bind(VisualElement root)
        {
            this.BindElements(root);
            this.Subscribe();
        }

        public void Unsubscribe()
        {
            Card3DCtrl.HoverEntered -= this.OnHoverEntered;
            Card3DCtrl.HoverExited  -= this.OnHoverExited;
        }

        private void BindElements(VisualElement root)
        {
            this.tooltip             = root.Q<VisualElement>("CardHoverTooltip");
            this.finalDefLabel       = root.Q<Label>("TooltipFinalDefLabel");
            this.damageReceivedLabel = root.Q<Label>("TooltipDamageReceivedLabel");
        }

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnHoverEntered;
            Card3DCtrl.HoverExited  += this.OnHoverExited;
        }

        // ─── Hover callbacks ──────────────────────────────────────────────────────

        private void OnHoverEntered(Card3DCtrl card)
        {
            this.hoveredCard = card;
            this.RefreshTooltipData();
            this.ShowTooltip();
        }

        private void OnHoverExited(Card3DCtrl card)
        {
            if (this.hoveredCard != card) return;
            this.hoveredCard = null;
            this.HideTooltip();
        }

        // ─── Data refresh ─────────────────────────────────────────────────────────

        private void RefreshTooltipData()
        {
            BattleCardSlot slot = this.FindSlot();
            if (slot == null) return;
            this.SetFinalDef(slot.final_def);
            this.SetDamageReceived(slot.total_damage_received);
        }

        private void SetFinalDef(int value)
        {
            if (this.finalDefLabel == null) return;
            this.finalDefLabel.text = $"DEF: {value}";
        }

        private void SetDamageReceived(int value)
        {
            if (this.damageReceivedLabel == null) return;
            this.damageReceivedLabel.text = $"DMG: {value}";
        }

        // ─── Slot lookup ──────────────────────────────────────────────────────────

        private BattleCardSlot FindSlot()
        {
            if (this.hoveredCard == null) return null;
            BattleState state = this.getBattleStateCtrl()?.BattleState;
            if (state == null) return null;
            return this.FindSlotInState(state, this.hoveredCard.InventoryItemId);
        }

        private BattleCardSlot FindSlotInState(BattleState state, string inventoryItemId)
        {
            return this.FindInArray(state.AlphaHand,      inventoryItemId)
                ?? this.FindInArray(state.AlphaFrontLine, inventoryItemId)
                ?? this.FindInArray(state.AlphaBackLine,  inventoryItemId)
                ?? this.FindInArray(state.AlphaTheVoid,   inventoryItemId)
                ?? this.FindInArray(state.AlphaTheSource, inventoryItemId)
                ?? this.FindInArray(state.OmegaHand,      inventoryItemId)
                ?? this.FindInArray(state.OmegaFrontLine, inventoryItemId)
                ?? this.FindInArray(state.OmegaBackLine,  inventoryItemId)
                ?? this.FindInArray(state.OmegaTheVoid,   inventoryItemId);
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

        // ─── Visibility ───────────────────────────────────────────────────────────

        private void ShowTooltip()
        {
            if (this.tooltip == null) return;
            this.tooltip.RemoveFromClassList("card-hover-tooltip--hidden");
        }

        private void HideTooltip()
        {
            if (this.tooltip == null) return;
            this.tooltip.AddToClassList("card-hover-tooltip--hidden");
        }

        // ─── Position update (call every frame from MonoBehaviour.Update) ─────────

        public void Tick(Camera camera)
        {
            if (this.hoveredCard == null) return;
            if (this.tooltip == null) return;
            if (camera == null) return;
            this.UpdateTooltipPosition(camera);
        }

        private void UpdateTooltipPosition(Camera camera)
        {
            Vector3 worldPos  = this.hoveredCard.transform.position + Vector3.up * 0.8f;
            Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return;
            this.ApplyScreenPosition(screenPos);
        }

        private void ApplyScreenPosition(Vector3 screenPos)
        {
            if (this.tooltip.panel == null) return;
            if (this.tooltip.parent == null) return;
            Vector2 panelPos   = RuntimePanelUtils.ScreenToPanel(this.tooltip.panel, new Vector2(screenPos.x, screenPos.y));
            Rect    parentRect = this.tooltip.parent.worldBound;
            float   halfWidth  = this.tooltip.layout.width * 0.5f;
            float   height     = this.tooltip.layout.height;
            this.tooltip.style.left = panelPos.x - parentRect.x - halfWidth;
            this.tooltip.style.top  = panelPos.y - parentRect.y - height;
        }
    }
}
