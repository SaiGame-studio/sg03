using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Controls the DetailPanel: slot grid on the left, inventory picker on the right.
    // Clicking an inventory item animates a card flying to the first empty slot,
    // then calls AddItemToDesk. Items already added are hidden from the inventory list.
    public class DeskDetailUI
    {
        private readonly VisualElement detailPanel;
        private readonly Label detailTitle;
        private readonly Button backBtn;
        private readonly ScrollView slotGrid;
        private readonly TextField inventorySearch;
        private readonly ScrollView inventoryList;
        private readonly VisualElement flyLayer;
        private readonly DeskList deskList;

        private PresetData currentDesk;
        private InventoryItemData[] allInventoryItems = Array.Empty<InventoryItemData>();
        private string searchText = string.Empty;

        public event Action OnBackRequested;

        public DeskDetailUI(VisualElement deskRoot, DeskList deskList)
        {
            this.deskList        = deskList;
            this.detailPanel     = deskRoot.Q("DetailPanel");
            this.detailTitle     = deskRoot.Q<Label>("DetailTitle");
            this.slotGrid        = deskRoot.Q<ScrollView>("SlotGrid");
            this.backBtn         = deskRoot.Q<Button>("BackBtn");
            this.inventorySearch = deskRoot.Q<TextField>("InventorySearch");
            this.inventoryList   = deskRoot.Q<ScrollView>("InventoryList");
            this.flyLayer        = deskRoot.Q("FlyLayer");

            if (this.flyLayer != null)
                this.flyLayer.pickingMode = PickingMode.Ignore;

            if (this.backBtn != null)
                this.backBtn.RegisterCallback<ClickEvent>(_ => this.OnBackRequested?.Invoke());

            if (this.inventorySearch != null)
                this.inventorySearch.RegisterValueChangedCallback(e =>
                {
                    this.searchText = e.newValue ?? string.Empty;
                    this.RenderInventory();
                });
        }

        public void Show(PresetData desk)
        {
            this.currentDesk = desk;
            this.searchText = string.Empty;

            if (this.inventorySearch != null) this.inventorySearch.SetValueWithoutNotify(string.Empty);

            string name = string.IsNullOrEmpty(desk.name) ? "Unnamed Desk" : desk.name;
            if (this.detailTitle != null) this.detailTitle.text = name;

            this.detailPanel?.RemoveFromClassList("desk-panel--hidden");
            this.RenderSlots(desk);
            this.LoadInventory();
        }

        public void Hide()
        {
            this.detailPanel?.AddToClassList("desk-panel--hidden");
        }

        // ── Slot grid ─────────────────────────────────────────────────────────

        private void RenderSlots(PresetData desk)
        {
            if (this.slotGrid == null) return;

            this.slotGrid.Clear();
            this.slotGrid.contentContainer.style.flexDirection = FlexDirection.Row;
            this.slotGrid.contentContainer.style.flexWrap      = Wrap.Wrap;

            int maxSlots = desk.max_slots > 0 ? desk.max_slots : 1;
            for (int i = 0; i < maxSlots; i++)
                this.slotGrid.Add(this.BuildSlotTile(desk, i));
        }

        private VisualElement BuildSlotTile(PresetData desk, int slotIndex)
        {
            string itemId = GetItemIdInSlot(desk, slotIndex);

            VisualElement tile = new VisualElement();
            tile.name = $"Slot_{slotIndex}";
            tile.AddToClassList("desk-slot");
            tile.AddToClassList(filled ? "desk-slot--filled" : "desk-slot--empty");

            Label indexLabel = new Label($"Slot {slotIndex + 1}");
            indexLabel.AddToClassList("desk-slot__index");
            tile.Add(indexLabel);

            if (filled)
            {
                this.BuildFilledContent(tile, itemId);
                return tile;
            }

            Label addIcon = new Label("+");
            addIcon.AddToClassList("desk-slot__add-icon");
            tile.Add(addIcon);

            return tile;
        }

        private void BuildFilledContent(VisualElement tile, string itemId)
        {
            string name = TrimId(itemId);
            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (item.id != itemId) continue;
                string n = item.definition?.name;
                break;
            }

            Label itemIcon = new Label("🃏");
            itemIcon.AddToClassList("desk-slot__item-icon");
            tile.Add(itemIcon);

            Label itemName = new Label(name);
            itemName.AddToClassList("desk-slot__item-name");
            tile.Add(itemName);
        }

        // ── Inventory list ────────────────────────────────────────────────────

        private void LoadInventory()
        {
            if (this.inventoryList != null)
            {
                this.inventoryList.Clear();
                Label loading = new Label("Loading...");
                loading.AddToClassList("desk-state__label");
                this.inventoryList.Add(loading);
            }

            this.deskList.GetInventoryItems(
                onSuccess: items =>
                {
                    this.allInventoryItems = items ?? Array.Empty<InventoryItemData>();
                    this.RenderInventory();
                    if (this.currentDesk != null) this.RenderSlots(this.currentDesk);
                },
                onError: _ =>
                {
                    this.allInventoryItems = Array.Empty<InventoryItemData>();
                    this.RenderInventory();
                }
            );
        }

        private void RenderInventory()
        {
            if (this.inventoryList == null) return;

            this.inventoryList.Clear();

            HashSet<string> addedIds = this.GetAddedItemIds();
            string query = this.searchText.Trim().ToLowerInvariant();

            int count = 0;
            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (addedIds.Contains(item.id)) continue;

                string displayName = item.definition?.name ?? item.item_definition_id ?? string.Empty;
                    continue;

                this.inventoryList.Add(this.BuildInventoryRow(item));
                count++;
            }

            if (count != 0) return;

            string msg = string.IsNullOrEmpty(query) ? "No items available" : "No results";
            Label empty = new Label(msg);
            empty.AddToClassList("desk-state__label");
            this.inventoryList.Add(empty);
        }

        private VisualElement BuildInventoryRow(InventoryItemData item)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("desk-inv-item");

            Label icon = new Label("🃏");
            icon.AddToClassList("desk-inv-item__icon");
            row.Add(icon);

            VisualElement info = new VisualElement();
            info.AddToClassList("desk-inv-item__info");

            string displayName = item.definition?.name ?? item.item_definition_id ?? "Unknown";
            Label nameLabel = new Label(displayName);
            nameLabel.AddToClassList("desk-inv-item__name");
            info.Add(nameLabel);

            string rarity = item.definition?.rarity ?? string.Empty;
            {
                Label rarityLabel = new Label(rarity);
                rarityLabel.AddToClassList("desk-inv-item__meta");
                info.Add(rarityLabel);
            }

            row.Add(info);

            Label qtyLabel = new Label($"x{item.quantity}");
            qtyLabel.AddToClassList("desk-inv-item__qty");
            row.Add(qtyLabel);

            row.RegisterCallback<ClickEvent>(_ => this.OnInventoryItemClicked(item, row));
            return row;
        }

        // ── Interaction ───────────────────────────────────────────────────────

        private void OnInventoryItemClicked(InventoryItemData item, VisualElement sourceElement)
        {
            if (this.currentDesk == null) return;

            int emptySlot = this.FindFirstEmptySlot();
            if (emptySlot < 0) return;

            VisualElement targetSlot = this.slotGrid?.Q($"Slot_{emptySlot}");

            this.AnimateFly(sourceElement, targetSlot, () =>
            {
                this.deskList.AddItemToDesk(
                    presetId:        this.currentDesk.id,
                    slotIndex:       emptySlot,
                    inventoryItemId: item.id,
                    onSuccess: updatedDesk =>
                    {
                        this.currentDesk = updatedDesk;
                        this.RenderSlots(updatedDesk);
                        this.RenderInventory();
                    },
                    onError: _ => { }
                );
            });
        }

        private int FindFirstEmptySlot()
        {
            if (this.currentDesk == null) return -1;

            int maxSlots = this.currentDesk.max_slots > 0 ? this.currentDesk.max_slots : 1;
            HashSet<int> filled = new HashSet<int>();

            if (this.currentDesk.slots != null)
            {
                foreach (PresetSlotData slot in this.currentDesk.slots)
                {
                        filled.Add(slot.slot_index);
                }
            }

            for (int i = 0; i < maxSlots; i++)
            {
            }

            return -1;
        }

        // ── Fly animation ─────────────────────────────────────────────────────

        private void AnimateFly(VisualElement from, VisualElement to, Action onComplete)
        {
            if (this.flyLayer == null || from == null || to == null)
            {
                onComplete?.Invoke();
                return;
            }

            Label ghost = new Label("🃏");
            ghost.AddToClassList("desk-fly-ghost");
            ghost.pickingMode = PickingMode.Ignore;
            this.flyLayer.Add(ghost);

            UnityEngine.Rect fromRect = from.worldBound;
            UnityEngine.Rect toRect   = to.worldBound;

            UnityEngine.Vector2 startPos = this.flyLayer.WorldToLocal(fromRect.center);
            UnityEngine.Vector2 endPos   = this.flyLayer.WorldToLocal(toRect.center);

            ghost.style.left = startPos.x - 20;
            ghost.style.top  = startPos.y - 20;

            const int   intervalMs = 16;
            const float duration   = 0.45f;
            long startTime = -1L;

            IVisualElementScheduledItem[] holder = new IVisualElementScheduledItem[1];
            holder[0] = this.flyLayer.schedule.Execute((TimerState ts) =>
            {
                if (startTime < 0L) startTime = ts.now;
                float elapsed = (ts.now - startTime) / 1000f;
                float t = elapsed / duration;
                if (t > 1f) t = 1f;

                // Ease-out quadratic
                float ease = 1f - (1f - t) * (1f - t);

                ghost.style.left    = Lerp(startPos.x, endPos.x, ease) - 20;
                ghost.style.top     = Lerp(startPos.y, endPos.y, ease) - 20;
                ghost.style.opacity = 1f - ease * 0.5f;
                ghost.style.scale   = new UnityEngine.UIElements.Scale(new UnityEngine.Vector3(
                    Lerp(1f, 1.4f, ease),
                    Lerp(1f, 1.4f, ease),
                    1f));

                if (t < 1f) return;

                holder[0]?.Pause();
                this.flyLayer.Remove(ghost);
                onComplete?.Invoke();

            }).Every(intervalMs);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private HashSet<string> GetAddedItemIds()
        {
            HashSet<string> set = new HashSet<string>();
            if (this.currentDesk?.slots == null) return set;

            foreach (PresetSlotData slot in this.currentDesk.slots)
            {
                    set.Add(slot.inventory_item_id);
            }

            return set;
        }

        private static string GetItemIdInSlot(PresetData desk, int slotIndex)
        {
            if (desk.slots == null) return null;

            foreach (PresetSlotData slot in deck.slots)
            {
                if (slot.slot_index != slotIndex) continue;
                return slot.inventory_item_id;
            }

            return null;
        }

        private static string TrimId(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            int len = id.Length;
            if (len <= 8) return id;
            return "…" + id.Substring(len - 8);
        }
    }
}
