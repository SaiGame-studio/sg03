using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Binds InventoryContent.uxml to live data loaded by InventoryList.
    // Re-renders whenever InventoryList fires OnDataUpdated.
    public class InventoryContentUI
    {
        private readonly ScrollView itemGrid;
        private readonly VisualElement emptyState;
        private readonly VisualElement loadingState;
        private readonly VisualElement categoryBar;
        private readonly Button refreshBtn;
        private readonly InventoryList list;

        private string activeCategory = string.Empty;
        private readonly List<Button> categoryTabs = new List<Button>();

        public InventoryContentUI(VisualElement root)
        {
            this.itemGrid     = root.Q<ScrollView>("ItemGrid");
            this.emptyState   = root.Q("EmptyState");
            this.loadingState = root.Q("LoadingState");
            this.categoryBar  = root.Q("CategoryBar");
            this.refreshBtn   = root.Q<Button>("RefreshBtn");

            // Allow the grid content container to wrap cards like a flex grid.
            if (this.itemGrid != null)
            {
                this.itemGrid.contentContainer.style.flexDirection = FlexDirection.Row;
                this.itemGrid.contentContainer.style.flexWrap      = Wrap.Wrap;
            }

            this.list = new InventoryList();
            this.list.OnDataUpdated += this.Render;

            if (this.refreshBtn != null)
                this.refreshBtn.RegisterCallback<ClickEvent>(_ => this.DoRefresh());

            this.ShowLoading();

            // Load category tabs first, then fetch items.
            this.list.LoadCategories(cats =>
            {
                this.BuildCategoryBar(cats);
                this.DoRefresh();
            });
        }

        private void DoRefresh()
        {
            this.ShowLoading();
            this.list.Refresh(this.activeCategory);
        }

        // ── Category bar ──────────────────────────────────────────────────────

        private void BuildCategoryBar(string[] categories)
        {
            if (this.categoryBar == null) return;

            this.categoryBar.Clear();
            this.categoryTabs.Clear();

            this.AddCategoryTab("All", string.Empty);

            if (categories != null)
            {
                foreach (string cat in categories)
                    this.AddCategoryTab(cat, cat);
            }

            this.UpdateActiveTab();
        }

        private void AddCategoryTab(string label, string category)
        {
            Button tab = new Button();
            tab.text     = label;
            tab.userData = category;
            tab.AddToClassList("inv-category-tab");
            tab.RegisterCallback<ClickEvent>(_ =>
            {
                this.activeCategory = category;
                this.UpdateActiveTab();
                this.DoRefresh();
            });
            this.categoryBar?.Add(tab);
            this.categoryTabs.Add(tab);
        }

        private void UpdateActiveTab()
        {
            foreach (Button tab in this.categoryTabs)
            {
                bool isActive = (string)tab.userData == this.activeCategory;
                if (isActive)
                    tab.AddToClassList("inv-category-tab--active");
                else
                    tab.RemoveFromClassList("inv-category-tab--active");
            }
        }

        // ── State views ───────────────────────────────────────────────────────

        private void ShowLoading()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.Flex;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.None;
            if (this.itemGrid     != null) this.itemGrid.style.display      = DisplayStyle.None;
        }

        private void ShowEmpty()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.Flex;
            if (this.itemGrid     != null) this.itemGrid.style.display      = DisplayStyle.None;
        }

        private void ShowGrid()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.None;
            if (this.itemGrid     != null) this.itemGrid.style.display      = DisplayStyle.Flex;
        }

        // ── Render ────────────────────────────────────────────────────────────

        private void Render()
        {
            if (this.itemGrid == null) return;

            InventoryItemData[] items = this.list.Items;
            if (items == null || items.Length == 0)
            {
                this.ShowEmpty();
                return;
            }

            this.itemGrid.Clear();
            foreach (InventoryItemData item in items)
                this.itemGrid.Add(this.BuildItemCard(item));

            this.ShowGrid();
        }

        private VisualElement BuildItemCard(InventoryItemData item)
        {
            string name     = item.definition?.name ?? item.definition?.item_code ?? item.item_definition_id ?? "Unknown";
            string category = item.definition?.category ?? string.Empty;
            string rarity   = (item.definition?.rarity ?? string.Empty).ToLower();
            int    quantity = item.quantity;
            int    level    = item.level;
            string date     = FormatDate(item.acquired_at);

            VisualElement card = new VisualElement();
            card.AddToClassList("inv-item-card");
            if (!string.IsNullOrEmpty(rarity))
                card.AddToClassList($"inv-item-card--{rarity}");

            // Quantity row (top-right badge)
            VisualElement qtyRow = new VisualElement();
            qtyRow.AddToClassList("inv-item-card__qty-row");
            Label qtyLabel = new Label($"×{quantity}");
            qtyLabel.AddToClassList("inv-item-card__qty");
            qtyRow.Add(qtyLabel);
            card.Add(qtyRow);

            // Item name
            Label nameLabel = new Label(name);
            nameLabel.AddToClassList("inv-item-card__name");
            card.Add(nameLabel);

            // Rarity badge
            if (!string.IsNullOrEmpty(rarity))
            {
                Label rarityLabel = new Label(CapitalizeFirst(rarity));
                rarityLabel.AddToClassList("inv-item-card__rarity");
                rarityLabel.AddToClassList($"inv-item-card__rarity--{rarity}");
                card.Add(rarityLabel);
            }

            // Category chip
            if (!string.IsNullOrEmpty(category))
            {
                Label categoryLabel = new Label(category);
                categoryLabel.AddToClassList("inv-item-card__category");
                card.Add(categoryLabel);
            }

            // Level (only if > 0)
            if (level > 0)
            {
                Label levelLabel = new Label($"Lv. {level}");
                levelLabel.AddToClassList("inv-item-card__level");
                card.Add(levelLabel);
            }

            // Acquired date
            if (!string.IsNullOrEmpty(date))
            {
                Label dateLabel = new Label(date);
                dateLabel.AddToClassList("inv-item-card__date");
                card.Add(dateLabel);
            }

            return card;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string FormatDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return string.Empty;
            if (iso.Length < 10) return iso;
            // yyyy-MM-dd → dd/MM/yyyy
            string datePart = iso.Substring(0, 10);
            string[] parts  = datePart.Split('-');
            if (parts.Length != 3) return datePart;
            return $"{parts[2]}/{parts[1]}/{parts[0]}";
        }

        private static string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
