using System;
using System.Collections.Generic;
using SaiGame.Services;

namespace SG03.UI
{
    public partial class DeskDetailUI
    {
        private readonly HashSet<int> starredSlots = new HashSet<int>();
        private readonly HashSet<int> voidedSlots = new HashSet<int>();

        private const int MaxStarredSlots = 3;
        private const int MaxVoidedSlots = 7;

        private void RestoreStarredSlotsFromMetadata(PresetData desk)
        {
            this.starredSlots.Clear();

            if (string.IsNullOrEmpty(desk.metadataJson)) return;
            if (desk.slots == null) return;

            for (int i = 1; i <= MaxStarredSlots; i++)
            {
                string key = $"choose_card_{i}";
                string itemId = ParseJsonStringValue(desk.metadataJson, key);
                if (string.IsNullOrEmpty(itemId)) continue;

                foreach (PresetSlotData slot in desk.slots)
                {
                    if (slot.inventory_item_id != itemId) continue;
                    this.starredSlots.Add(slot.slot_index);
                    break;
                }
            }
        }

        private void RestoreVoidedSlotsFromMetadata(PresetData desk)
        {
            this.voidedSlots.Clear();

            if (string.IsNullOrEmpty(desk.metadataJson)) return;
            if (desk.slots == null) return;

            for (int i = 1; i <= MaxVoidedSlots; i++)
            {
                string key = $"void_card_{i}";
                string itemId = ParseJsonStringValue(desk.metadataJson, key);
                if (string.IsNullOrEmpty(itemId)) continue;

                foreach (PresetSlotData slot in desk.slots)
                {
                    if (slot.inventory_item_id != itemId) continue;
                    if (!this.starredSlots.Contains(slot.slot_index))
                        this.voidedSlots.Add(slot.slot_index);
                    break;
                }
            }
        }

        private void OnToggleStarSlot(int slotIndex)
        {
            if (this.starredSlots.Contains(slotIndex))
                this.starredSlots.Remove(slotIndex);
            else if (this.starredSlots.Count < MaxStarredSlots && !this.voidedSlots.Contains(slotIndex))
                this.starredSlots.Add(slotIndex);

            if (this.currentDesk != null)
                this.RenderSlots(this.currentDesk);

            this.SaveMetadata();
        }

        private void OnToggleVoidSlot(int slotIndex)
        {
            if (this.voidedSlots.Contains(slotIndex))
            {
                this.voidedSlots.Remove(slotIndex);
            }
            else
            {
                // Enforce "maximum 7 cards in the void" and ensure card is not starred
                if (this.voidedSlots.Count < MaxVoidedSlots && !this.starredSlots.Contains(slotIndex))
                {
                    this.voidedSlots.Add(slotIndex);
                }
            }

            if (this.currentDesk != null)
                this.RenderSlots(this.currentDesk);

            this.SaveMetadata();
        }

        private void SaveMetadata()
        {
            if (this.currentDesk == null) return;

            string metadataJson = this.BuildMetadataJson();
            this.deskList.UpdateDeskMetadata(
                presetId:     this.currentDesk.id,
                metadataJson: metadataJson,
                onSuccess:    () => { },
                onError:      _ => { }
            );
        }

        private string BuildMetadataJson()
        {
            List<string> kvPairs = new List<string>();

            // Build choose_card_1..3
            int[] sortedStars = new int[this.starredSlots.Count];
            this.starredSlots.CopyTo(sortedStars);
            System.Array.Sort(sortedStars);

            for (int i = 0; i < MaxStarredSlots; i++)
            {
                string itemId = string.Empty;
                if (i < sortedStars.Length)
                    itemId = GetItemIdInSlot(this.currentDesk, sortedStars[i]) ?? string.Empty;
                kvPairs.Add($"\"choose_card_{i + 1}\":\"{EscapeJson(itemId)}\"");
            }

            // Build void_card_1..7
            int[] sortedVoided = new int[this.voidedSlots.Count];
            this.voidedSlots.CopyTo(sortedVoided);
            System.Array.Sort(sortedVoided);

            for (int i = 0; i < MaxVoidedSlots; i++)
            {
                string itemId = string.Empty;
                if (i < sortedVoided.Length)
                    itemId = GetItemIdInSlot(this.currentDesk, sortedVoided[i]) ?? string.Empty;
                kvPairs.Add($"\"void_card_{i + 1}\":\"{EscapeJson(itemId)}\"");
            }

            return "{" + string.Join(",", kvPairs) + "}";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ParseJsonStringValue(string json, string key)
        {
            string search = $"\"{key}\"";
            int keyIdx = json.IndexOf(search);
            if (keyIdx < 0) return null;

            int colon = json.IndexOf(':', keyIdx + search.Length);
            if (colon < 0) return null;

            int openQuote = json.IndexOf('"', colon + 1);
            if (openQuote < 0) return null;

            int closeQuote = json.IndexOf('"', openQuote + 1);
            if (closeQuote < 0) return null;

            return json.Substring(openQuote + 1, closeQuote - openQuote - 1);
        }
    }
}
