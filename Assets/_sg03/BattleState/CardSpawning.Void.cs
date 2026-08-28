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
            bool isFaceUp = slot == null || slot.face_up || slot.expose;
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
            this.slotOccupancy[holder.transform] = card;
            holder.SetCard(card);

            card.MoveVoidToLine(holder, owner == Owner.alpha, isFaceUp, null);
            return card;
        }

        public void SettleAlphaVoidInFrontLine(Card3DCtrl card, string inventoryItemId, int slotIndex) { }
        public void SettleOmegaVoidInFrontLine(Card3DCtrl card, string inventoryItemId, int slotIndex) { }
    }
}
