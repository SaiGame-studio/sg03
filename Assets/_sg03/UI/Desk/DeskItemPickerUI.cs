using System;
using SaiGame.Services;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Manages the ItemPicker overlay inside the DetailPanel.
    // Shows the player's inventory and fires OnItemSelected when an item is chosen.
    public class DeskItemPickerUI
    {
        public event Action<InventoryItemData> OnItemSelected;

        private readonly VisualElement pickerRoot;
        private readonly VisualElement loadingState;
        private readonly VisualElement emptyState;
        private readonly ScrollView pickerList;
        private readonly Button closeBtn;
        private readonly DeskList deskList;

        public DeskItemPickerUI(VisualElement detailPanelRoot, DeskList deskList)
        {
            this.deskList     = deskList;
            this.pickerRoot   = detailPanelRoot.Q("ItemPicker");
            this.loadingState = detailPanelRoot.Q("PickerLoadingState");
            this.emptyState   = detailPanelRoot.Q("PickerEmptyState");
            this.pickerList   = detailPanelRoot.Q<ScrollView>("PickerList");
            this.closeBtn     = detailPanelRoot.Q<Button>("PickerCloseBtn");

            if (this.closeBtn != null)
                this.closeBtn.RegisterCallback<ClickEvent>(_ => this.Hide());
        }

        public void Show()
        {
            if (this.pickerRoot == null) return;
            this.pickerRoot.RemoveFromClassList("desk-picker--hidden");
            this.ShowLoading();
            this.LoadItems();
        }

        public void Hide()
        {
            if (this.pickerRoot == null) return;
            this.pickerRoot.AddToClassList("desk-picker--hidden");
        }

        private void LoadItems()
        {
            this.deskList.GetInventoryItems(
                onSuccess: items =>
                {
                    if (items == null || items.Length == 0)
                    {
                        this.ShowEmpty();
                        return;
                    }

                    this.RenderItems(items);
                },
                onError: _ => this.ShowEmpty()
            );
        }

        private void RenderItems(InventoryItemData[] items)
        {
            if (this.pickerList == null) return;

            this.pickerList.Clear();
            foreach (InventoryItemData item in items)
                this.pickerList.Add(this.BuildPickerRow(item));

            this.ShowList();
        }

        private VisualElement BuildPickerRow(InventoryItemData item)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("desk-picker-item");

            Label icon = new Label("\U0001F0CF");
            icon.AddToClassList("desk-picker-item__icon");
            row.Add(icon);

            VisualElement info = new VisualElement();
            info.AddToClassList("desk-picker-item__info");

            string displayName = item.definition?.name ?? item.item_definition_id ?? "Unknown Item";
            Label nameLabel = new Label(displayName);
            nameLabel.AddToClassList("desk-picker-item__name");
            info.Add(nameLabel);

            string category = item.definition?.category ?? "";
            string rarity   = item.definition?.rarity ?? "";
            string meta     = string.IsNullOrEmpty(rarity) ? category : $"{category}  ·  {rarity}";
            if (!string.IsNullOrEmpty(meta))
            {
                Label metaLabel = new Label(meta);
                metaLabel.AddToClassList("desk-picker-item__meta");
                info.Add(metaLabel);
            }

            row.Add(info);

            Label qtyLabel = new Label($"x{item.quantity}");
            qtyLabel.AddToClassList("desk-picker-item__qty");
            row.Add(qtyLabel);

            row.RegisterCallback<ClickEvent>(_ =>
            {
                this.Hide();
                this.OnItemSelected?.Invoke(item);
            });

            return row;
        }

        // ── State helpers ─────────────────────────────────────────────────────

        private void ShowLoading()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.Flex;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.None;
            if (this.pickerList   != null) this.pickerList.style.display   = DisplayStyle.None;
        }

        private void ShowEmpty()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.Flex;
            if (this.pickerList   != null) this.pickerList.style.display   = DisplayStyle.None;
        }

        private void ShowList()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.None;
            if (this.pickerList   != null) this.pickerList.style.display   = DisplayStyle.Flex;
        }
    }
}
