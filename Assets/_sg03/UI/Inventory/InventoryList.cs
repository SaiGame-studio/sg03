using System;
using SaiGame.Services;

namespace SG03.UI
{
    // Intermediary between InventoryContentUI and the SaiServer PlayerItem component.
    // Fetches the item list on demand and fires OnDataUpdated when the data changes.
    public class InventoryList
    {
        public event Action OnDataUpdated;

        public InventoryItemData[] Items { get; private set; }
        public string[] Categories { get; private set; }
        public bool HasData => this.Items != null;
        public bool IsLoading { get; private set; }

        private readonly PlayerItem playerItem;

        public InventoryList()
        {
            this.playerItem = UnityEngine.Object.FindFirstObjectByType<PlayerItem>(UnityEngine.FindObjectsInactive.Include);
        }

        public void Refresh(string category = "")
        {
            if (this.playerItem == null)
            {
                this.Items = Array.Empty<InventoryItemData>();
                this.OnDataUpdated?.Invoke();
                return;
            }

            if (this.IsLoading) return;

            this.IsLoading = true;
            this.playerItem.GetItems(
                limit: 1000,
                category: category ?? string.Empty,
                onSuccess: response =>
                {
                    this.IsLoading = false;
                    this.Items     = response?.items ?? Array.Empty<InventoryItemData>();
                    this.OnDataUpdated?.Invoke();
                },
                onError: _ =>
                {
                    this.IsLoading = false;
                    this.Items     = Array.Empty<InventoryItemData>();
                    this.OnDataUpdated?.Invoke();
                }
            );
        }

        public void LoadCategories(Action<string[]> onDone)
        {
            if (this.playerItem == null)
            {
                onDone?.Invoke(Array.Empty<string>());
                return;
            }

            this.playerItem.GetItemCategories(
                onSuccess: cats =>
                {
                    this.Categories = cats ?? Array.Empty<string>();
                    onDone?.Invoke(this.Categories);
                },
                onError: _ =>
                {
                    this.Categories = Array.Empty<string>();
                    onDone?.Invoke(this.Categories);
                }
            );
        }
    }
}
