using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
        private readonly TextField deskNameInput;
        private readonly Button renameDeskBtn;
        private readonly Button saveDeskNameBtn;
        private readonly Label slotCountLabel;
        private readonly Label starCountLabel;
        private readonly Label highStarCardCountLabel;
        private readonly Label voidCountLabel;
        private readonly Label duplicateCardWarning;
        private readonly Button backBtn;
        private readonly Button softCardBtn;
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
        private bool isSoftCardSortInProgress;
        private bool isSoftCardSortStopRequested;
        private readonly Dictionary<string, Texture2D> cardArtCache = new();
        private readonly Dictionary<string, List<Image>> pendingCardArtImages = new();
        private readonly Dictionary<string, AsyncOperationHandle<CardData>> cardArtHandles = new();

        public event Action OnBackRequested;
        public event Action OnDeskRenamed;
        public event Action OnCardViewerShown;
        public event Action OnCardViewerHidden;
        public event Action<InventoryItemData> OnCardViewRequested;

        public DeskDetailUI(VisualElement deskRoot, DeskList deskList)
        {
            this.deskList        = deskList;
            this.detailPanel     = deskRoot.Q("DetailPanel");
            this.detailTitle     = deskRoot.Q<Label>("DetailTitle");
            this.deskNameInput   = deskRoot.Q<TextField>("DeskNameInput");
            this.renameDeskBtn   = deskRoot.Q<Button>("RenameDeskBtn");
            this.saveDeskNameBtn = deskRoot.Q<Button>("SaveDeskNameBtn");
            this.slotCountLabel  = deskRoot.Q<Label>("SlotCountLabel");
            this.starCountLabel  = deskRoot.Q<Label>("StarCountLabel");
            this.highStarCardCountLabel = deskRoot.Q<Label>("HighStarCardCountLabel");
            this.voidCountLabel  = deskRoot.Q<Label>("VoidCountLabel");
            this.duplicateCardWarning = deskRoot.Q<Label>("DuplicateCardWarning");
            this.slotGrid        = deskRoot.Q<ScrollView>("SlotGrid");
            this.backBtn         = deskRoot.Q<Button>("BackBtn");
            this.softCardBtn     = deskRoot.Q<Button>("SoftCardBtn");
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

            if (this.renameDeskBtn != null)
                this.renameDeskBtn.RegisterCallback<ClickEvent>(_ => this.BeginDeskNameEdit());

            if (this.saveDeskNameBtn != null)
                this.saveDeskNameBtn.RegisterCallback<ClickEvent>(_ => this.SaveDeskName());

            if (this.softCardBtn != null)
                this.softCardBtn.RegisterCallback<ClickEvent>(_ => this.OnSoftCardClicked());

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
            if (this.deskNameInput != null) this.deskNameInput.SetValueWithoutNotify(desk.name ?? string.Empty);
            this.SetDeskNameSaveEnabled(true);
            this.SetDeskNameEditing(false);

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
            this.SetDeskNameSaveEnabled(true);
            this.SetDeskNameEditing(false);
            this.detailPanel?.AddToClassList("desk-panel--hidden");
        }

        private void BeginDeskNameEdit()
        {
            if (this.currentDesk == null) return;

            if (this.deskNameInput != null)
            {
                this.deskNameInput.SetValueWithoutNotify(this.currentDesk.name ?? string.Empty);
                this.deskNameInput.Focus();
            }

            this.SetDeskNameEditing(true);
        }

        private void SaveDeskName()
        {
            if (this.currentDesk == null || this.deskNameInput == null) return;

            string name = this.deskNameInput.value?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                this.deskNameInput.Focus();
                return;
            }

            if (string.Equals(name, this.currentDesk.name, StringComparison.Ordinal))
            {
                this.SetDeskNameEditing(false);
                return;
            }

            string deskId = this.currentDesk.id;
            this.SetDeskNameSaveEnabled(false);
            this.deskList.UpdateDeskName(
                deskId,
                name,
                updatedDesk =>
                {
                    this.SetDeskNameSaveEnabled(true);
                    if (this.currentDesk == null || this.currentDesk.id != deskId) return;

                    this.currentDesk.name = updatedDesk?.name ?? name;
                    if (this.detailTitle != null) this.detailTitle.text = this.currentDesk.name;
                    if (this.deskNameInput != null) this.deskNameInput.SetValueWithoutNotify(this.currentDesk.name);
                    this.SetDeskNameEditing(false);
                    this.OnDeskRenamed?.Invoke();
                },
                error =>
                {
                    this.SetDeskNameSaveEnabled(true);
                });
        }

        private void SetDeskNameEditing(bool isEditing)
        {
            if (this.detailTitle != null) this.detailTitle.EnableInClassList("desk-header__title--hidden", isEditing);
            if (this.deskNameInput != null) this.deskNameInput.EnableInClassList("desk-header__rename-input--hidden", !isEditing);
            if (this.renameDeskBtn != null) this.renameDeskBtn.EnableInClassList("desk-header__rename-btn--hidden", isEditing);
            if (this.saveDeskNameBtn != null) this.saveDeskNameBtn.EnableInClassList("desk-header__save-name-btn--hidden", !isEditing);
        }

        private void SetDeskNameSaveEnabled(bool enabled)
        {
            if (this.deskNameInput != null) this.deskNameInput.SetEnabled(enabled);
            if (this.saveDeskNameBtn != null) this.saveDeskNameBtn.SetEnabled(enabled);
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
            this.UpdateHighStarCardCount(desk);
            this.UpdateVoidCount();
            this.UpdateDuplicateCardWarning(desk);

            for (int i = 0; i < maxSlots; i++)
                this.slotGrid.Add(this.BuildSlotTile(desk, i));
        }

        private void UpdateDuplicateCardWarning(PresetData desk)
        {
            if (this.duplicateCardWarning == null) return;

            const int maxCopiesPerCard = 3;
            Dictionary<string, int> copiesByDefinitionId = new Dictionary<string, int>();
            Dictionary<string, string> namesByDefinitionId = new Dictionary<string, string>();
            Dictionary<string, InventoryItemData> itemsById = new Dictionary<string, InventoryItemData>();

            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (!string.IsNullOrEmpty(item?.id)) itemsById[item.id] = item;
            }

            if (desk?.slots != null)
            {
                foreach (PresetSlotData slot in desk.slots)
                {
                    if (string.IsNullOrEmpty(slot.inventory_item_id)
                        || !itemsById.TryGetValue(slot.inventory_item_id, out InventoryItemData item))
                        continue;

                    string definitionId = item.item_definition_id ?? item.definition?.id;
                    if (string.IsNullOrEmpty(definitionId)) continue;

                    copiesByDefinitionId.TryGetValue(definitionId, out int copies);
                    copiesByDefinitionId[definitionId] = copies + 1;
                    namesByDefinitionId[definitionId] = item.definition?.name ?? definitionId;
                }
            }

            List<string> violations = new List<string>();
            foreach (KeyValuePair<string, int> entry in copiesByDefinitionId)
            {
                if (entry.Value > maxCopiesPerCard)
                    violations.Add($"{namesByDefinitionId[entry.Key]} x{entry.Value}");
            }

            if (violations.Count == 0)
            {
                this.duplicateCardWarning.text = string.Empty;
                this.duplicateCardWarning.AddToClassList("desk-header__duplicate-warning--hidden");
                return;
            }

            this.duplicateCardWarning.text =
                $"Warning: maximum {maxCopiesPerCard} copies per card. Over limit: {string.Join(", ", violations)}.";
            this.duplicateCardWarning.RemoveFromClassList("desk-header__duplicate-warning--hidden");
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
                starBtn.text = "S";
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
                voidBtn.text = "V";
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

            Image itemIcon = new Image { scaleMode = ScaleMode.ScaleToFit };
            itemIcon.AddToClassList("desk-slot__item-icon");
            this.LoadCardArt(itemIcon, foundItem);
            tile.Add(itemIcon);

            Label itemName = new Label(name);
            itemName.AddToClassList("desk-slot__item-name");
            tile.Add(itemName);

            VisualElement actions = new VisualElement();
            actions.AddToClassList("desk-slot__actions");

            Button viewBtn = new Button();
            viewBtn.text = "View";
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
            removeBtn.text = "X";
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

        private void LoadCardArt(Image target, InventoryItemData item)
        {
            string itemCode = item?.definition?.item_code;
            if (string.IsNullOrEmpty(itemCode)) return;

            if (this.cardArtCache.TryGetValue(itemCode, out Texture2D cachedArt))
            {
                target.image = cachedArt;
                return;
            }

            if (this.pendingCardArtImages.TryGetValue(itemCode, out List<Image> pendingImages))
            {
                pendingImages.Add(target);
                return;
            }

            if (CardDataManager.Instance == null
                || !CardLoader.TryResolveAddressByAssetName(
                    CardDataManager.Instance.CardAddresses,
                    itemCode,
                    out string cardAddress))
                return;

            this.pendingCardArtImages[itemCode] = new List<Image> { target };
            AsyncOperationHandle<CardData> handle = Addressables.LoadAssetAsync<CardData>(cardAddress);
            this.cardArtHandles[itemCode] = handle;
            handle.Completed += operation =>
            {
                this.pendingCardArtImages.TryGetValue(itemCode, out List<Image> images);
                this.pendingCardArtImages.Remove(itemCode);

                if (operation.Status != AsyncOperationStatus.Succeeded
                    || operation.Result?.CharacterTexture == null)
                    return;

                Texture2D artwork = operation.Result.CharacterTexture;
                this.cardArtCache[itemCode] = artwork;
                if (images == null) return;

                foreach (Image image in images)
                    image.image = artwork;
            };
        }

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
            Image artIcon = new Image { scaleMode = ScaleMode.ScaleToFit };
            artIcon.AddToClassList("desk-card__art-icon");
            this.LoadCardArt(artIcon, item);
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

            Button viewBtn = new Button { text = "View" };
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
            this.starCountLabel.text = $"Stars {this.starredSlots.Count} / {MaxStarredSlots}";
        }

        private void UpdateHighStarCardCount(PresetData desk)
        {
            if (this.highStarCardCountLabel == null) return;

            Dictionary<string, InventoryItemData> itemsById = new Dictionary<string, InventoryItemData>();
            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (!string.IsNullOrEmpty(item?.id)) itemsById[item.id] = item;
            }

            int highStarCardCount = 0;
            if (desk?.slots != null)
            {
                foreach (PresetSlotData slot in desk.slots)
                {
                    if (!string.IsNullOrEmpty(slot.inventory_item_id)
                        && itemsById.TryGetValue(slot.inventory_item_id, out InventoryItemData item)
                        && this.IsCharacterCard(item)
                        && this.GetCardStarCount(item) >= 4)
                        highStarCardCount++;
                }
            }

            this.highStarCardCountLabel.text = $"4+ Characters: {highStarCardCount}";
        }

        private void UpdateVoidCount()
        {
            if (this.voidCountLabel == null) return;
            this.voidCountLabel.text = $"Void {this.voidedSlots.Count} / {MaxVoidedSlots}";
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
            if (this.isSoftCardSortInProgress) return;

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
            if (this.currentDesk == null || this.isSoftCardSortInProgress) return;
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

        private void OnSoftCardClicked()
        {
            if (this.isSoftCardSortInProgress)
            {
                this.isSoftCardSortStopRequested = true;
                this.softCardBtn?.SetEnabled(false);
                return;
            }

            if (this.currentDesk == null || this.pendingApiRequests > 0) return;

            List<DeskCardSortEntry> cards = this.BuildSortedDeckCards();
            if (cards.Count < 2 || this.IsAlreadySorted(cards))
            {
                return;
            }

            HashSet<string> starredItemIds = this.GetItemIdsInSlots(this.starredSlots);
            HashSet<string> voidedItemIds = this.GetItemIdsInSlots(this.voidedSlots);
            this.isSoftCardSortInProgress = true;
            this.isSoftCardSortStopRequested = false;
            if (this.softCardBtn != null) this.softCardBtn.text = "Stop";
            this.SortNextCard(cards, 0, starredItemIds, voidedItemIds);
        }

        private List<DeskCardSortEntry> BuildSortedDeckCards()
        {
            Dictionary<string, InventoryItemData> itemsById = new Dictionary<string, InventoryItemData>();
            foreach (InventoryItemData item in this.allInventoryItems)
            {
                if (!string.IsNullOrEmpty(item?.id)) itemsById[item.id] = item;
            }

            List<DeskCardSortEntry> cards = new List<DeskCardSortEntry>();
            if (this.currentDesk?.slots == null) return cards;

            foreach (PresetSlotData slot in this.currentDesk.slots)
            {
                if (string.IsNullOrEmpty(slot.inventory_item_id)) continue;
                itemsById.TryGetValue(slot.inventory_item_id, out InventoryItemData item);
                cards.Add(new DeskCardSortEntry(
                    slot.inventory_item_id,
                    slot.slot_index,
                    this.GetCardStarCount(item),
                    item?.item_definition_id ?? item?.definition?.id ?? string.Empty));
            }

            cards.Sort((left, right) =>
            {
                int starComparison = right.Stars.CompareTo(left.Stars);
                if (starComparison != 0) return starComparison;

                int typeComparison = string.Compare(left.CardType, right.CardType, StringComparison.Ordinal);
                if (typeComparison != 0) return typeComparison;

                return left.OriginalSlot.CompareTo(right.OriginalSlot);
            });
            return cards;
        }

        private bool IsAlreadySorted(List<DeskCardSortEntry> cards)
        {
            for (int index = 0; index < cards.Count; index++)
            {
                DeskCardSortEntry cardInSlot = cards.Find(card => card.OriginalSlot == index);
                if (cardInSlot == null || !HasSameSortKey(cardInSlot, cards[index])) return false;
            }
            return true;
        }

        private void SortNextCard(List<DeskCardSortEntry> cards, int index, HashSet<string> starredItemIds, HashSet<string> voidedItemIds)
        {
            if (this.isSoftCardSortStopRequested)
            {
                this.ReloadDeskAfterSortStop();
                return;
            }

            while (index < cards.Count)
            {
                string itemIdInTargetSlot = GetItemIdInSlot(this.currentDesk, index);
                DeskCardSortEntry cardInTargetSlot = cards.Find(card => card.ItemId == itemIdInTargetSlot);
                if (cardInTargetSlot == null || !HasSameSortKey(cardInTargetSlot, cards[index])) break;

                // Cards with the same sort key are interchangeable, so swapping
                // their item instances would only result in unnecessary API calls.
                index++;
            }

            if (index >= cards.Count)
            {
                this.deskList.GetDesk(
                    this.currentDesk.id,
                    desk => this.FinishSoftCardSort(desk, starredItemIds, voidedItemIds),
                    _ => this.ReloadDeskAfterSortFailure());
                return;
            }

            int sourceSlot = this.FindSlotForItem(cards[index].ItemId);
            if (sourceSlot < 0)
            {
                this.ReloadDeskAfterSortFailure();
                return;
            }

            string displacedItemId = GetItemIdInSlot(this.currentDesk, index);
            if (string.IsNullOrEmpty(displacedItemId))
            {
                this.MoveCardForSort(cards, index, sourceSlot, starredItemIds, voidedItemIds);
                return;
            }

            this.SwapCardsForSort(cards, index, sourceSlot, displacedItemId, starredItemIds, voidedItemIds);
        }

        private void MoveCardForSort(List<DeskCardSortEntry> cards, int index, int sourceSlot, HashSet<string> starredItemIds, HashSet<string> voidedItemIds)
        {
            string itemId = cards[index].ItemId;
            this.deskList.RemoveItemFromDesk(
                this.currentDesk.id,
                sourceSlot,
                _ =>
                {
                    this.RemoveSlotFromSortUi(sourceSlot);
                    this.deskList.AddItemToDesk(
                        this.currentDesk.id,
                        index,
                        itemId,
                        _ =>
                        {
                            this.AddCardToSortUi(index, itemId);
                            this.SortNextCard(cards, index + 1, starredItemIds, voidedItemIds);
                        },
                        _ => this.ReloadDeskAfterSortFailure());
                },
                _ => this.ReloadDeskAfterSortFailure());
        }

        private void SwapCardsForSort(List<DeskCardSortEntry> cards, int index, int sourceSlot, string displacedItemId, HashSet<string> starredItemIds, HashSet<string> voidedItemIds)
        {
            string itemId = cards[index].ItemId;
            this.deskList.RemoveItemFromDesk(
                this.currentDesk.id,
                index,
                _ =>
                {
                    this.RemoveSlotFromSortUi(index);
                    this.deskList.RemoveItemFromDesk(
                        this.currentDesk.id,
                        sourceSlot,
                        _ =>
                        {
                            this.RemoveSlotFromSortUi(sourceSlot);
                            this.deskList.AddItemToDesk(
                                this.currentDesk.id,
                                index,
                                itemId,
                                _ =>
                                {
                                    this.AddCardToSortUi(index, itemId);
                                    this.deskList.AddItemToDesk(
                                        this.currentDesk.id,
                                        sourceSlot,
                                        displacedItemId,
                                        _ =>
                                        {
                                            this.AddCardToSortUi(sourceSlot, displacedItemId);
                                            this.SortNextCard(cards, index + 1, starredItemIds, voidedItemIds);
                                        },
                                        _ => this.ReloadDeskAfterSortFailure());
                                },
                                _ => this.ReloadDeskAfterSortFailure());
                        },
                        _ => this.ReloadDeskAfterSortFailure());
                },
                _ => this.ReloadDeskAfterSortFailure());
        }

        private int FindSlotForItem(string itemId)
        {
            if (this.currentDesk?.slots == null) return -1;
            foreach (PresetSlotData slot in this.currentDesk.slots)
            {
                if (slot.inventory_item_id == itemId) return slot.slot_index;
            }
            return -1;
        }

        private static bool HasSameSortKey(DeskCardSortEntry left, DeskCardSortEntry right)
        {
            return left.Stars == right.Stars
                && string.Equals(left.CardType, right.CardType, StringComparison.Ordinal);
        }

        private void FinishSoftCardSort(PresetData desk, HashSet<string> starredItemIds, HashSet<string> voidedItemIds)
        {
            desk.metadataJson = this.currentDesk.metadataJson;
            this.currentDesk = desk;
            this.SetSlotsForItemIds(this.starredSlots, starredItemIds);
            this.SetSlotsForItemIds(this.voidedSlots, voidedItemIds);
            this.RenderSlots(this.currentDesk);
            this.RenderInventory();
            this.SaveMetadata();
            this.isSoftCardSortInProgress = false;
            this.isSoftCardSortStopRequested = false;
            if (this.softCardBtn != null)
            {
                this.softCardBtn.text = "Soft Card";
                this.softCardBtn.SetEnabled(true);
            }
        }

        private void ReloadDeskAfterSortStop()
        {
            this.ReloadDeskAfterSort("Sorting stopped; deck reloaded");
        }

        private void ReloadDeskAfterSortFailure()
        {
            this.ReloadDeskAfterSort("Sort failed; deck reloaded");
        }

        private void ReloadDeskAfterSort(string statusMessage)
        {
            string metadataJson = this.currentDesk?.metadataJson;
            this.deskList.GetDesk(
                this.currentDesk.id,
                desk =>
                {
                    desk.metadataJson = metadataJson;
                    this.currentDesk = desk;
                    this.starredSlots.Clear();
                    this.voidedSlots.Clear();
                    this.RestoreStarredSlotsFromMetadata(desk);
                    this.RestoreVoidedSlotsFromMetadata(desk);
                    this.RenderSlots(desk);
                    this.RenderInventory();
                    this.isSoftCardSortInProgress = false;
                    this.isSoftCardSortStopRequested = false;
                    if (this.softCardBtn != null)
                    {
                        this.softCardBtn.text = "Soft Card";
                        this.softCardBtn.SetEnabled(true);
                    }
                },
                _ =>
                {
                    this.isSoftCardSortInProgress = false;
                    this.isSoftCardSortStopRequested = false;
                    if (this.softCardBtn != null)
                    {
                        this.softCardBtn.text = "Soft Card";
                        this.softCardBtn.SetEnabled(true);
                    }
                });
        }

        private void RemoveSlotFromSortUi(int slotIndex)
        {
            List<PresetSlotData> slots = new List<PresetSlotData>(this.currentDesk.slots);
            slots.RemoveAll(slot => slot.slot_index == slotIndex);
            this.currentDesk.slots = slots.ToArray();
            this.RenderSlots(this.currentDesk);
            this.RenderInventory();
        }

        private void AddCardToSortUi(int slotIndex, string itemId)
        {
            List<PresetSlotData> slots = new List<PresetSlotData>(this.currentDesk.slots)
            {
                new PresetSlotData { slot_index = slotIndex, inventory_item_id = itemId }
            };
            this.currentDesk.slots = slots.ToArray();
            this.RenderSlots(this.currentDesk);
            this.RenderInventory();
        }

        private HashSet<string> GetItemIdsInSlots(HashSet<int> slotIndices)
        {
            HashSet<string> itemIds = new HashSet<string>();
            foreach (int slotIndex in slotIndices)
            {
                string itemId = GetItemIdInSlot(this.currentDesk, slotIndex);
                if (!string.IsNullOrEmpty(itemId)) itemIds.Add(itemId);
            }
            return itemIds;
        }

        private void SetSlotsForItemIds(HashSet<int> slotIndices, HashSet<string> itemIds)
        {
            slotIndices.Clear();
            if (this.currentDesk?.slots == null) return;
            foreach (PresetSlotData slot in this.currentDesk.slots)
            {
                if (itemIds.Contains(slot.inventory_item_id)) slotIndices.Add(slot.slot_index);
            }
        }

        private int GetCardStarCount(InventoryItemData item)
        {
            string baseStatsJson = item?.definition?.base_stats;
            if (string.IsNullOrEmpty(baseStatsJson)) return 0;
            return JsonUtility.FromJson<DeskCardBaseStats>(baseStatsJson)?.star ?? 0;
        }

        private bool IsCharacterCard(InventoryItemData item)
        {
            string metadataJson = item?.definition?.metadata;
            if (string.IsNullOrEmpty(metadataJson)) return false;

            CardDefinitionMetadata metadata = JsonUtility.FromJson<CardDefinitionMetadata>(metadataJson);
            return string.Equals(metadata?.type, "character", StringComparison.OrdinalIgnoreCase);
        }

        [Serializable]
        private class DeskCardBaseStats
        {
            public int star;
        }

        private class DeskCardSortEntry
        {
            public readonly string ItemId;
            public readonly int OriginalSlot;
            public readonly int Stars;
            public readonly string CardType;

            public DeskCardSortEntry(string itemId, int originalSlot, int stars, string cardType)
            {
                this.ItemId = itemId;
                this.OriginalSlot = originalSlot;
                this.Stars = stars;
                this.CardType = cardType;
            }
        }

        private void AnimateFly(VisualElement from, VisualElement to, Action onComplete)
        {
            if (this.flyLayer == null || from == null || to == null)
            {
                onComplete?.Invoke();
                return;
            }

            Label ghost = new Label("C");
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
