using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Controls the DetailPanel: slot grid on the left, inventory picker on the right.
    // Clicking an inventory item animates a card flying to the first empty slot,
    // then calls AddItemToDesk. Items already added are hidden from the inventory list.
    public partial class DeskDetailUI
    {
        private readonly VisualElement detailPanel;
        private readonly Label detailTitle;
        private readonly Label slotCountLabel;
        private readonly Label starCountLabel;
        private readonly Label voidCountLabel;
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
        private readonly DeskList deskList;

        private PresetData currentDesk;
        private InventoryItemData[] allInventoryItems = Array.Empty<InventoryItemData>();
        private string searchText = string.Empty;
        private int pendingApiRequests = 0;

        public event Action OnBackRequested;
        public event Action OnCardViewerShown;
        public event Action OnCardViewerHidden;
        public event Action<InventoryItemData> OnCardViewRequested;

        public DeskDetailUI(VisualElement deskRoot, DeskList deskList)
        {
            this.deskList        = deskList;
            this.detailPanel     = deskRoot.Q("DetailPanel");
            this.detailTitle     = deskRoot.Q<Label>("DetailTitle");
            this.slotCountLabel  = deskRoot.Q<Label>("SlotCountLabel");
            this.starCountLabel  = deskRoot.Q<Label>("StarCountLabel");
            this.voidCountLabel  = deskRoot.Q<Label>("VoidCountLabel");
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

            if (this.flyLayer != null)
                this.flyLayer.pickingMode = PickingMode.Ignore;

            if (this.backBtn != null)
                this.backBtn.RegisterCallback<ClickEvent>(_ => this.OnBackRequested?.Invoke());

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
            this.starredSlots.Clear();

            if (this.inventorySearch != null) this.inventorySearch.SetValueWithoutNotify(string.Empty);

            string name = string.IsNullOrEmpty(desk.name) ? "Unnamed Desk" : desk.name;
            if (this.detailTitle != null) this.detailTitle.text = name;

            this.detailPanel?.RemoveFromClassList("desk-panel--hidden");
            this.ShowLoadingSlots();

            string listMetadataJson = desk.metadataJson;

            this.deskList.GetDesk(
                presetId: desk.id,
                onSuccess: freshDesk =>
                {
                    // GetPreset does not populate metadataJson — inject it from the list data
                    if (string.IsNullOrEmpty(freshDesk.metadataJson))
                        freshDesk.metadataJson = listMetadataJson;

                    this.starredSlots.Clear();
                    this.RestoreStarredSlotsFromMetadata(freshDesk);
                    this.voidedSlots.Clear();
                    this.RestoreVoidedSlotsFromMetadata(freshDesk);

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

            HashSet<int> currentlyFilled = new HashSet<int>();
            if (desk.slots != null)
            {
                foreach (PresetSlotData slot in desk.slots)
                {
                    if (!string.IsNullOrEmpty(slot.inventory_item_id))
                        currentlyFilled.Add(slot.slot_index);
                }
            }

            this.starredSlots.IntersectWith(currentlyFilled);
            this.voidedSlots.IntersectWith(currentlyFilled);

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

            this.UpdateStarCount();
            this.UpdateVoidCount();

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

                bool isStarred = this.starredSlots.Contains(slotIndex);
                bool isVoided  = this.voidedSlots.Contains(slotIndex);
                bool canStar   = (isStarred || this.starredSlots.Count < MaxStarredSlots) && !isVoided;

                Button starBtn = new Button();
                starBtn.name = $"SlotStarBtn_{slotIndex}";
                starBtn.text = "\u2605";
                starBtn.AddToClassList("desk-slot__star-btn");
                if (isStarred)
                    starBtn.AddToClassList("desk-slot__star-btn--active");
                starBtn.SetEnabled(canStar);
                int capturedIndex = slotIndex;
                starBtn.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    this.OnToggleStarSlot(capturedIndex);
                });
                tile.Add(starBtn);

                // Void button (left of card)
                bool canToggleVoid = (isVoided || this.voidedSlots.Count < MaxVoidedSlots) && !isStarred;

                Button voidBtn = new Button();
                voidBtn.name = $"SlotVoidBtn_{slotIndex}";
                voidBtn.text = "\uD83C\uDF00"; // Vortex symbol 🌀
                voidBtn.AddToClassList("desk-slot__void-btn");
                if (isVoided)
                    voidBtn.AddToClassList("desk-slot__void-btn--active");
                voidBtn.SetEnabled(canToggleVoid);
                voidBtn.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    this.OnToggleVoidSlot(capturedIndex);
                });
                tile.Add(voidBtn);

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

            // Art area
            VisualElement artArea = new VisualElement();
            artArea.AddToClassList("desk-card__art-area");
            Label artIcon = new Label("🃏");
            artIcon.AddToClassList("desk-card__art-icon");
            artArea.Add(artIcon);

            // Keep quantity within the art area so it cannot overlap card details.
            artArea.Add(qtyLabel);
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

            VisualElement actions = new VisualElement();
            actions.AddToClassList("desk-card__actions");

            Button viewBtn = new Button { text = "\U0001F441" };
            viewBtn.AddToClassList("desk-card__action-btn");
            viewBtn.AddToClassList("desk-card__view-btn");
            InventoryItemData capturedItem = item;
            string capturedId = item.id;
            viewBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                this.ShowCardViewer(capturedItem, capturedId);
            });
            actions.Add(viewBtn);

            Button addBtn = new Button { text = "+" };
            addBtn.AddToClassList("desk-card__action-btn");
            addBtn.AddToClassList("desk-card__add-btn");
            addBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                this.OnInventoryItemClicked(stack, card);
            });
            actions.Add(addBtn);

            card.Add(actions);

            return card;
        }



        private void UpdateStarCount()
        {
            if (this.starCountLabel == null) return;
            this.starCountLabel.text = $"\u2605 {this.starredSlots.Count} / {MaxStarredSlots}";
        }

        private void UpdateVoidCount()
        {
            if (this.voidCountLabel == null) return;
            this.voidCountLabel.text = $"\uD83C\uDF00 {this.voidedSlots.Count} / {MaxVoidedSlots}";
        }

        private void ShowCardViewer(InventoryItemData item, string itemId)
        {
            this.OnCardViewerShown?.Invoke();
            this.OnCardViewRequested?.Invoke(item);
        }

        private void HideCardViewer()
        {
            this.cardViewerOverlay?.AddToClassList("desk-card-viewer--hidden");
            this.OnCardViewerHidden?.Invoke();
        }

        private void OnRemoveFromDesk(PresetData desk, int slotIndex)
        {
            if (this.currentDesk.slots != null)
            {
                var list = new List<PresetSlotData>(this.currentDesk.slots);
                list.RemoveAll(s => s.slot_index == slotIndex);
                this.currentDesk.slots = list.ToArray();
                this.RenderSlots(this.currentDesk);
                this.RenderInventory();
            }

            this.pendingApiRequests++;
            this.deskList.RemoveItemFromDesk(
                presetId:  desk.id,
                slotIndex: slotIndex,
                onSuccess: updatedDesk =>
                {
                    this.pendingApiRequests--;
                    if (this.pendingApiRequests == 0)
                    {
                        this.currentDesk = updatedDesk;
                    }
                    else
                    {
                        var optSlots = this.currentDesk.slots;
                        this.currentDesk = updatedDesk;
                        this.currentDesk.slots = optSlots;
                    }
                    this.RenderSlots(this.currentDesk);
                    this.RenderInventory();
                },
                onError: _ => 
                {
                    this.pendingApiRequests--;
                    if (this.pendingApiRequests == 0)
                    {
                        string meta = this.currentDesk.metadataJson;
                        this.deskList.GetDesk(desk.id, d => { d.metadataJson = meta; this.currentDesk = d; this.RenderSlots(d); this.RenderInventory(); }, e => {});
                    }
                }
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

            this.AnimateFly(sourceElement, targetSlot, null);

            if (this.currentDesk.slots == null)
            {
                this.currentDesk.slots = new PresetSlotData[] { new PresetSlotData { slot_index = emptySlot, inventory_item_id = itemId } };
            }
            else
            {
                var list = new List<PresetSlotData>(this.currentDesk.slots);
                list.Add(new PresetSlotData { slot_index = emptySlot, inventory_item_id = itemId });
                this.currentDesk.slots = list.ToArray();
            }
            this.RenderSlots(this.currentDesk);
            this.RenderInventory();

            this.pendingApiRequests++;
            this.deskList.AddItemToDesk(
                presetId:        this.currentDesk.id,
                slotIndex:       emptySlot,
                inventoryItemId: itemId,
                onSuccess: updatedDesk =>
                {
                    this.pendingApiRequests--;
                    if (this.pendingApiRequests == 0)
                    {
                        this.currentDesk = updatedDesk;
                    }
                    else
                    {
                        var optSlots = this.currentDesk.slots;
                        this.currentDesk = updatedDesk;
                        this.currentDesk.slots = optSlots;
                    }
                    this.RenderSlots(this.currentDesk);
                    this.RenderInventory();
                },
                onError: _ => 
                {
                    this.pendingApiRequests--;
                    if (this.pendingApiRequests == 0)
                    {
                        string meta = this.currentDesk.metadataJson;
                        this.deskList.GetDesk(this.currentDesk.id, d => { d.metadataJson = meta; this.currentDesk = d; this.RenderSlots(d); this.RenderInventory(); }, e => {});
                    }
                }
            );
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
