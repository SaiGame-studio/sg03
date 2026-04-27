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
        private readonly Label slotCountLabel;
        private readonly Button backBtn;
        private readonly ScrollView slotGrid;
        private readonly TextField inventorySearch;
        private readonly ScrollView inventoryList;
        private readonly VisualElement flyLayer;
        private readonly VisualElement cardViewerOverlay;
        private readonly Label cardViewerName;
        private readonly Label cardViewerRarity;
        private readonly Label cardViewerCategory;
        private readonly Label cardViewerQty;
        private readonly Button toggleBgBtn;
        private readonly DeskList deskList;

        private PresetData currentDesk;
        private InventoryItemData[] allInventoryItems = Array.Empty<InventoryItemData>();
        private string searchText = string.Empty;
        private bool isImmersive;

        public event Action OnBackRequested;
        public event Action OnCardViewerShown;
        public event Action OnCardViewerHidden;

        public DeskDetailUI(VisualElement deskRoot, DeskList deskList)
        {
            this.deskList        = deskList;
            this.detailPanel     = deskRoot.Q("DetailPanel");
            this.detailTitle     = deskRoot.Q<Label>("DetailTitle");
            this.slotCountLabel  = deskRoot.Q<Label>("SlotCountLabel");
            this.slotGrid        = deskRoot.Q<ScrollView>("SlotGrid");
            this.backBtn         = deskRoot.Q<Button>("BackBtn");
            this.inventorySearch = deskRoot.Q<TextField>("InventorySearch");
            this.inventoryList   = deskRoot.Q<ScrollView>("InventoryList");
            this.flyLayer        = deskRoot.Q("FlyLayer");
            this.cardViewerOverlay  = deskRoot.Q("CardViewerOverlay");
            this.cardViewerName     = deskRoot.Q<Label>("CardViewerName");
            this.cardViewerRarity   = deskRoot.Q<Label>("CardViewerRarity");
            this.cardViewerCategory = deskRoot.Q<Label>("CardViewerCategory");
            this.cardViewerQty      = deskRoot.Q<Label>("CardViewerQty");

            this.toggleBgBtn = deskRoot.Q<Button>("ToggleBgBtn");

            if (this.flyLayer != null)
                this.flyLayer.pickingMode = PickingMode.Ignore;

            if (this.backBtn != null)
                this.backBtn.RegisterCallback<ClickEvent>(_ => this.OnBackRequested?.Invoke());

            if (this.toggleBgBtn != null)
                this.toggleBgBtn.RegisterCallback<ClickEvent>(_ => this.ToggleBackground());

            Button closeBtn = deskRoot.Q<Button>("CardViewerCloseBtn");
            if (closeBtn != null)
                closeBtn.RegisterCallback<ClickEvent>(_ => this.HideCardViewer());

            VisualElement backdrop = deskRoot.Q("CardViewerBackdrop");
            if (backdrop != null)
                backdrop.RegisterCallback<ClickEvent>(_ => this.HideCardViewer());

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
            this.ShowLoadingSlots();

            this.deskList.GetDesk(
                presetId: desk.id,
                onSuccess: freshDesk =>
                {
                    this.currentDesk = freshDesk;
                    this.RenderSlots(freshDesk);
                    this.LoadInventory();
                },
                onError: _ =>
                {
                    // Fall back to the data we already have
                    this.RenderSlots(desk);
                    this.LoadInventory();
                }
            );
        }

        public void Hide()
        {
            this.detailPanel?.AddToClassList("desk-panel--hidden");
            if (this.isImmersive)
                this.ExitImmersive();
        }

        // ── Slot grid ─────────────────────────────────────────────────────────

        private void ShowLoadingSlots()
        {
            if (this.slotGrid == null) return;
            this.slotGrid.Clear();
            Label loading = new Label("Loading...");
            loading.AddToClassList("desk-state__label");
            this.slotGrid.Add(loading);
        }

        private void RenderSlots(PresetData desk)
        {
            if (this.slotGrid == null) return;

            this.slotGrid.Clear();
            this.slotGrid.contentContainer.style.flexDirection = FlexDirection.Row;
            this.slotGrid.contentContainer.style.flexWrap      = Wrap.Wrap;

            int maxSlots = desk.max_slots > 0 ? desk.max_slots : 1;

            int filledSlots = 0;
            if (desk.slots != null)
            {
                foreach (PresetSlotData slot in desk.slots)
                    filledSlots++;
            }

            if (this.slotCountLabel != null)
                this.slotCountLabel.text = $"{filledSlots}/{maxSlots}";

            for (int i = 0; i < maxSlots; i++)
                this.slotGrid.Add(this.BuildSlotTile(desk, i));
        }

        private VisualElement BuildSlotTile(PresetData desk, int slotIndex)
        {
            string itemId = GetItemIdInSlot(desk, slotIndex);
            bool filled   = !string.IsNullOrEmpty(itemId);

            VisualElement tile = new VisualElement();
            tile.name = $"Slot_{slotIndex}";
            tile.AddToClassList("desk-slot");
            tile.AddToClassList(filled ? "desk-slot--filled" : "desk-slot--empty");

            Label indexLabel = new Label($"Slot {slotIndex + 1}");
            indexLabel.AddToClassList("desk-slot__index");
            tile.Add(indexLabel);

            if (filled)
            {
                this.BuildFilledContent(tile, desk, slotIndex, itemId);
                return tile;
            }

            Label addIcon = new Label("+");
            addIcon.AddToClassList("desk-slot__add-icon");
            tile.Add(addIcon);

            return tile;
        }

        private void BuildFilledContent(VisualElement tile, PresetData desk, int slotIndex, string itemId)
        {
            InventoryItemData foundItem = null;
            string name = TrimId(itemId);
            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (item.id != itemId) continue;
                string n = item.definition?.name;
                if (!string.IsNullOrEmpty(n)) name = n;
                foundItem = item;
                break;
            }

            Label itemIcon = new Label("🃏");
            itemIcon.AddToClassList("desk-slot__item-icon");
            tile.Add(itemIcon);

            Label itemName = new Label(name);
            itemName.AddToClassList("desk-slot__item-name");
            tile.Add(itemName);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("desk-slot__actions");

            Button viewBtn = new Button();
            viewBtn.text = "👁";
            viewBtn.AddToClassList("desk-slot__btn");
            viewBtn.AddToClassList("desk-slot__btn--view");
            InventoryItemData capturedItem = foundItem;
            string capturedId = itemId;
            viewBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                this.ShowCardViewer(capturedItem, capturedId);
            });
            actions.Add(viewBtn);

            Button removeBtn = new Button();
            removeBtn.text = "✕";
            removeBtn.AddToClassList("desk-slot__btn");
            removeBtn.AddToClassList("desk-slot__btn--remove");
            PresetData capturedDesk = desk;
            int capturedSlot = slotIndex;
            removeBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                this.OnRemoveFromDesk(capturedDesk, capturedSlot);
            });
            actions.Add(removeBtn);

            tile.Add(actions);
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
            this.inventoryList.contentContainer.style.flexDirection = FlexDirection.Row;
            this.inventoryList.contentContainer.style.flexWrap      = Wrap.Wrap;

            HashSet<string> addedIds = this.GetAddedItemIds();
            string query = this.searchText.Trim().ToLowerInvariant();

            // Group available items by item_definition_id into stacks
            Dictionary<string, CardStack> stackMap = new Dictionary<string, CardStack>();
            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (addedIds.Contains(item.id)) continue;

                string key = item.item_definition_id ?? item.id;
                if (!stackMap.TryGetValue(key, out CardStack stack))
                {
                    stackMap[key] = new CardStack(item);
                    continue;
                }

                stack.Add(item.id);
            }

            int count = 0;
            foreach (CardStack stack in stackMap.Values)
            {
                string displayName = stack.Representative.definition?.name
                    ?? stack.Representative.item_definition_id
                    ?? string.Empty;

                if (!string.IsNullOrEmpty(query) && !displayName.ToLowerInvariant().Contains(query))
                    continue;

                this.inventoryList.Add(this.BuildInventoryCard(stack));
                count++;
            }

            if (count != 0) return;

            string msg = string.IsNullOrEmpty(query) ? "No items available" : "No results";
            Label empty = new Label(msg);
            empty.AddToClassList("desk-state__label");
            this.inventoryList.Add(empty);
        }

        private VisualElement BuildInventoryCard(CardStack stack)
        {
            InventoryItemData item = stack.Representative;

            VisualElement card = new VisualElement();
            card.AddToClassList("desk-card");

            // Rarity-based border modifier
            string rarity = (item.definition?.rarity ?? string.Empty).ToLowerInvariant();
            if (!string.IsNullOrEmpty(rarity))
                card.AddToClassList($"desk-card--rarity-{rarity}");

            // Quantity badge — top-right corner
            Label qtyLabel = new Label($"x{stack.Count}");
            qtyLabel.AddToClassList("desk-card__qty");
            card.Add(qtyLabel);

            // Art area
            VisualElement artArea = new VisualElement();
            artArea.AddToClassList("desk-card__art-area");
            Label artIcon = new Label("🃏");
            artIcon.AddToClassList("desk-card__art-icon");
            artArea.Add(artIcon);
            card.Add(artArea);

            // Info area
            VisualElement info = new VisualElement();
            info.AddToClassList("desk-card__info");

            string displayName = item.definition?.name ?? item.item_definition_id ?? "Unknown";
            Label nameLabel = new Label(displayName);
            nameLabel.AddToClassList("desk-card__name");
            info.Add(nameLabel);

            if (!string.IsNullOrEmpty(rarity))
            {
                Label rarityLabel = new Label(rarity);
                rarityLabel.AddToClassList("desk-card__rarity");
                rarityLabel.AddToClassList($"desk-card__rarity--{rarity}");
                info.Add(rarityLabel);
            }

            card.Add(info);

            card.RegisterCallback<ClickEvent>(_ => this.OnInventoryItemClicked(stack, card));
            return card;
        }

        // ── Interaction ───────────────────────────────────────────────────────

        private void ToggleBackground()
        {
            if (this.isImmersive)
                this.ExitImmersive();
            else
                this.EnterImmersive();
        }

        private void EnterImmersive()
        {
            this.isImmersive = true;
            if (this.toggleBgBtn != null)
            {
                this.toggleBgBtn.text = "👁 Show BG";
                this.toggleBgBtn.AddToClassList("desk-header__toggle-bg-btn--active");
            }
            this.OnCardViewerShown?.Invoke();
        }

        private void ExitImmersive()
        {
            this.isImmersive = false;
            if (this.toggleBgBtn != null)
            {
                this.toggleBgBtn.text = "👁 Hide BG";
                this.toggleBgBtn.RemoveFromClassList("desk-header__toggle-bg-btn--active");
            }
            this.OnCardViewerHidden?.Invoke();
        }

        private void ShowCardViewer(InventoryItemData item, string itemId)
        {
            if (this.cardViewerOverlay == null) return;

            string name     = item?.definition?.name ?? TrimId(itemId);
            string rarity   = (item?.definition?.rarity ?? string.Empty).ToUpperInvariant();
            string category = item?.definition?.category ?? string.Empty;
            int    qty      = item?.quantity ?? 1;

            if (this.cardViewerName != null)     this.cardViewerName.text     = name;
            if (this.cardViewerRarity != null)   this.cardViewerRarity.text   = rarity;
            if (this.cardViewerCategory != null) this.cardViewerCategory.text = category;
            if (this.cardViewerQty != null)      this.cardViewerQty.text      = $"Qty: {qty}";

            this.cardViewerOverlay.RemoveFromClassList("desk-card-viewer--hidden");
            this.OnCardViewerShown?.Invoke();
        }

        private void HideCardViewer()
        {
            this.cardViewerOverlay?.AddToClassList("desk-card-viewer--hidden");
            this.OnCardViewerHidden?.Invoke();
        }

        private void OnRemoveFromDesk(PresetData desk, int slotIndex)
        {
            this.deskList.RemoveItemFromDesk(
                presetId:  desk.id,
                slotIndex: slotIndex,
                onSuccess: updatedDesk =>
                {
                    this.currentDesk = updatedDesk;
                    this.RenderSlots(updatedDesk);
                    this.RenderInventory();
                },
                onError: _ => { }
            );
        }

        private void OnInventoryItemClicked(CardStack stack, VisualElement sourceElement)
        {
            if (this.currentDesk == null) return;
            if (stack.ItemIds.Count == 0) return;

            int emptySlot = this.FindFirstEmptySlot();
            if (emptySlot < 0) return;

            VisualElement targetSlot = this.slotGrid?.Q($"Slot_{emptySlot}");
            string itemId = stack.ItemIds[0];

            this.AnimateFly(sourceElement, targetSlot, () =>
            {
                this.deskList.AddItemToDesk(
                    presetId:        this.currentDesk.id,
                    slotIndex:       emptySlot,
                    inventoryItemId: itemId,
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
                if (!filled.Contains(i)) return i;
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
                if (!string.IsNullOrEmpty(slot.inventory_item_id))
                    set.Add(slot.inventory_item_id);
            }

            return set;
        }

        private static string GetItemIdInSlot(PresetData desk, int slotIndex)
        {
            if (desk.slots == null) return null;

            foreach (PresetSlotData slot in desk.slots)
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
