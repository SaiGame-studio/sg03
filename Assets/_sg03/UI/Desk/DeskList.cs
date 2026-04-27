using System;
using SaiGame.Services;

namespace SG03.UI
{
    // Intermediary between DeskContentUI and the SaiServer ItemPreset / PlayerItem components.
    // Fetches the desk list on demand and fires OnDataUpdated when data changes.
    public class DeskList
    {
        private const string DESK_CODE_NAME = "card_desk";

        public event Action OnDataUpdated;

        public PresetData[] Desks { get; private set; }
        public bool HasData => this.Desks != null;
        public bool IsLoading { get; private set; }

        private readonly ItemPreset itemPreset;
        private readonly PlayerItem playerItem;

        public DeskList()
        {
            this.itemPreset = UnityEngine.Object.FindFirstObjectByType<ItemPreset>(UnityEngine.FindObjectsInactive.Include);
            this.playerItem = UnityEngine.Object.FindFirstObjectByType<PlayerItem>(UnityEngine.FindObjectsInactive.Include);
        }

        public void Refresh()
        {
            if (this.itemPreset == null)
            {
                this.Desks = Array.Empty<PresetData>();
                this.OnDataUpdated?.Invoke();
                return;
            }

            if (this.IsLoading) return;

            this.IsLoading = true;
            this.itemPreset.GetPresets(
                onSuccess: response =>
                {
                    this.IsLoading = false;
                    this.Desks     = response?.containers ?? Array.Empty<PresetData>();
                    this.OnDataUpdated?.Invoke();
                },
                onError: _ =>
                {
                    this.IsLoading = false;
                    this.Desks     = Array.Empty<PresetData>();
                    this.OnDataUpdated?.Invoke();
                }
            );
        }

        public void CreateDesk(string name, Action<PresetData> onSuccess, Action<string> onError)
        {
            if (this.itemPreset == null)
            {
                onError?.Invoke("ItemPreset service not available.");
                return;
            }

            this.itemPreset.CreatePresetByCodeName(
                codeName: DESK_CODE_NAME,
                name:     name,
                onSuccess: onSuccess,
                onError:   onError
            );
        }

        public void AddItemToDesk(
            string presetId,
            int slotIndex,
            string inventoryItemId,
            Action<PresetData> onSuccess,
            Action<string> onError)
        {
            if (this.itemPreset == null)
            {
                onError?.Invoke("ItemPreset service not available.");
                return;
            }

            this.itemPreset.AddItemToPreset(presetId, slotIndex, inventoryItemId, onSuccess, onError);
        }

        public void GetInventoryItems(Action<InventoryItemData[]> onSuccess, Action<string> onError)
        {
            if (this.playerItem == null)
            {
                onSuccess?.Invoke(Array.Empty<InventoryItemData>());
                return;
            }

            this.playerItem.GetItems(
                onSuccess: response => onSuccess?.Invoke(response?.items ?? Array.Empty<InventoryItemData>()),
                onError:   onError
            );
        }

        public void GetDesk(string presetId, Action<PresetData> onSuccess, Action<string> onError)
        {
            if (this.itemPreset == null)
            {
                onError?.Invoke("ItemPreset service not available.");
                return;
            }

            this.itemPreset.GetPreset(presetId, onSuccess, onError);
        }

        public void RemoveItemFromDesk(
            string presetId,
            int slotIndex,
            Action<PresetData> onSuccess,
            Action<string> onError)
        {
            if (this.itemPreset == null)
            {
                onError?.Invoke("ItemPreset service not available.");
                return;
            }

            this.itemPreset.RemoveItemFromPreset(presetId, slotIndex, onSuccess, onError);
        }
    }
}
