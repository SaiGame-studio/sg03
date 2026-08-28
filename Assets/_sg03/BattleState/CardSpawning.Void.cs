using System.Collections.Generic;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    public partial class CardSpawning
    {
        public Card3DCtrl MoveAlphaVoidToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveVoidToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaFrontLine, this.alphaVoidCardList, Owner.alpha);

        public Card3DCtrl MoveOmegaVoidToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveVoidToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaFrontLine, this.omegaVoidCardList, Owner.omega);

        private Card3DCtrl MoveVoidToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders, List<Card3DCtrl> voidList, Owner owner)
        {
            Card3DCtrl card = this.FindCardById(inventoryItemId);
            if (card == null)
            {
                Card3DCtrl prefab = this.ResolvePrefab();
                Transform spawnPoint = owner == Owner.alpha ? this.deskPosition.AlphaTheVoid : this.deskPosition.OmegaTheVoid;
                card = this.SpawnCardAt(prefab, spawnPoint);
            }
            if (card == null || slotIndex < 0 || slotIndex >= holders.Length) return null;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null || this.IsSlotOccupied(holder.transform)) return null;

            voidList.Remove(card);
            if (owner == Owner.alpha && this.alphaVoidSpawnedCount > 0) this.alphaVoidSpawnedCount--;
            if (owner == Owner.omega && this.omegaVoidSpawnedCount > 0) this.omegaVoidSpawnedCount--;

            this.RemoveFromSlotOccupancy(card);
            card.SetOwner(owner);
            card.SetInventoryItemId(inventoryItemId);

            BattleCardSlot slot = owner == Owner.alpha ? this.FindAlphaSlotById(inventoryItemId) : this.FindOmegaSlotById(inventoryItemId);
            if (slot != null)
            {
                string code = slot.item_definition_code_name;
                card.SetCodeName(code);
                CardDefinitionData def = this.battleCardDefinitions?.GetDefinitionByCode(code);
                this.ApplyCardFallbacks(card, slot.item_definition_name, def);
                card.LoadCardByCodeName(code);
                card.SetDefinition(def);
                card.SetExpose(slot.expose);
                card.SetIsTrigger(slot.trigger);
            }
            else
            {
                card.SetExpose(true);
                card.SetIsTrigger(true);
            }

            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            Vector3 abovePos = holder.transform.position + Vector3.up * this.aboveLineHeight;
            card.MoveTo(abovePos, holder.HolderLocation);
            this.slotOccupancy[holder.transform] = card;
            holder.SetCard(card);
            return card;
        }

        public void SettleAlphaVoidInFrontLine(Card3DCtrl card, string inventoryItemId, int slotIndex)
            => this.SettleVoidInLine(card, inventoryItemId, slotIndex, this.deskPosition.AlphaFrontLine, Owner.alpha);

        public void SettleOmegaVoidInFrontLine(Card3DCtrl card, string inventoryItemId, int slotIndex)
            => this.SettleVoidInLine(card, inventoryItemId, slotIndex, this.deskPosition.OmegaFrontLine, Owner.omega);

        private void SettleVoidInLine(Card3DCtrl card, string inventoryItemId, int slotIndex, CardHolderCtrl[] holders, Owner owner)
        {
            if (slotIndex < 0 || slotIndex >= holders.Length) return;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return;
            BattleCardSlot slot = owner == Owner.alpha ? this.FindAlphaSlotById(inventoryItemId) : this.FindOmegaSlotById(inventoryItemId);
            bool isFaceUp = slot == null || slot.face_up || slot.expose;
            if (slot != null)
            {
                card.SetExpose(slot.expose);
                card.SetIsTrigger(slot.trigger);
            }
            else
            {
                card.SetExpose(true);
                card.SetIsTrigger(true);
            }
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            if (isFaceUp)
            {
                card.MoveToLineFaceUp(holder, owner == Owner.alpha);
            }
            else
            {
                if (owner == Owner.alpha)
                    card.MoveToUnknow(holder, slot != null ? () => this.ApplyAlphaFaceState(card, slot) : () => card.FaceUp());
                else
                    card.MoveToUnknow(holder, slot != null ? () => this.ApplyFaceState(card, slot) : () => card.FaceUp());
            }
        }
    }
}
