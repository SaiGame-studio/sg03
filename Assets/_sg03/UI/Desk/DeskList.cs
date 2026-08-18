using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SaiGame.Services;

namespace SG03.UI
{
    // Intermediary between DeskContentUI and the SaiServer ItemPreset / PlayerItem components.
    // Fetches the desk list on demand and fires OnDataUpdated when data changes.
    public class DeskList
    {
        private const string DESK_CODE_NAME = "card_desk";
        private const string DefaultDeskMetadataKey = "is_default";
        private static readonly Regex DefaultDeskMetadataRegex = new Regex(
            "\\\"" + DefaultDeskMetadataKey + "\\\"\\s*:\\s*(true|false)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public event Action OnDataUpdated;

        public PresetData[] Desks { get; private set; }
        public bool HasData => this.Desks != null;
        public bool IsLoading { get; private set; }
        public bool IsUpdatingDefaultDesk { get; private set; }

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
                limit: 1000,
                category: "card",
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

        public void DeleteDesk(string presetId, Action onSuccess, Action<string> onError)
        {
            if (this.itemPreset == null)
            {
                onError?.Invoke("ItemPreset service not available.");
                return;
            }

            this.itemPreset.DeletePreset(presetId, _ => onSuccess?.Invoke(), onError);
        }

        public void UpdateDeskMetadata(string presetId, string metadataJson, Action onSuccess, Action<string> onError)
        {
            if (this.itemPreset == null)
            {
                onError?.Invoke("ItemPreset service not available.");
                return;
            }

            this.itemPreset.UpdatePreset(presetId, null, metadataJson,
                onSuccess: _ => onSuccess?.Invoke(),
                onError:   onError);
        }

        public bool IsDefaultDesk(PresetData desk)
        {
            return HasDefaultDeskMetadata(desk);
        }

        public static bool HasDefaultDeskMetadata(PresetData desk)
        {
            return desk != null && IsDefaultDeskMetadata(desk.metadataJson);
        }

        // A desk default is stored in its own metadata because the preset API has no dedicated default field.
        // Existing defaults are cleared first so the server never retains two default desks.
        public void SetDefaultDesk(PresetData desk, Action onSuccess, Action<string> onError)
        {
            if (desk == null || string.IsNullOrWhiteSpace(desk.id))
            {
                onError?.Invoke("Desk is required.");
                return;
            }

            if (this.IsUpdatingDefaultDesk) return;
            if (this.IsDefaultDesk(desk))
            {
                onSuccess?.Invoke();
                return;
            }

            this.IsUpdatingDefaultDesk = true;
            var currentDefaults = new List<PresetData>();
            if (this.Desks != null)
            {
                foreach (PresetData existingDesk in this.Desks)
                {
                    if (existingDesk == null || existingDesk.id == desk.id || !this.IsDefaultDesk(existingDesk)) continue;
                    currentDefaults.Add(existingDesk);
                }
            }

            this.ClearDefaultDesksThenSet(desk, currentDefaults, 0, onSuccess, onError);
        }

        private void ClearDefaultDesksThenSet(
            PresetData newDefault,
            List<PresetData> currentDefaults,
            int index,
            Action onSuccess,
            Action<string> onError)
        {
            if (index < currentDefaults.Count)
            {
                PresetData currentDefault = currentDefaults[index];
                this.UpdateDeskMetadata(
                    currentDefault.id,
                    SetDefaultDeskMetadata(currentDefault.metadataJson, false),
                    () => this.ClearDefaultDesksThenSet(newDefault, currentDefaults, index + 1, onSuccess, onError),
                    error => this.CompleteDefaultDeskUpdate(error, onSuccess, onError));
                return;
            }

            this.UpdateDeskMetadata(
                newDefault.id,
                SetDefaultDeskMetadata(newDefault.metadataJson, true),
                () => this.CompleteDefaultDeskUpdate(null, onSuccess, onError),
                error => this.CompleteDefaultDeskUpdate(error, onSuccess, onError));
        }

        private void CompleteDefaultDeskUpdate(string error, Action onSuccess, Action<string> onError)
        {
            this.IsUpdatingDefaultDesk = false;
            if (string.IsNullOrEmpty(error)) onSuccess?.Invoke();
            else onError?.Invoke(error);
        }

        private static bool IsDefaultDeskMetadata(string metadataJson)
        {
            Match match = DefaultDeskMetadataRegex.Match(metadataJson ?? string.Empty);
            return match.Success && string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string SetDefaultDeskMetadata(string metadataJson, bool isDefault)
        {
            string value = isDefault ? "true" : "false";
            string metadata = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson.Trim();
            if (!metadata.StartsWith("{") || !metadata.EndsWith("}")) metadata = "{}";

            if (DefaultDeskMetadataRegex.IsMatch(metadata))
                return DefaultDeskMetadataRegex.Replace(metadata, $"\"{DefaultDeskMetadataKey}\":{value}", 1);

            int closingBraceIndex = metadata.LastIndexOf('}');
            string prefix = metadata.Substring(0, closingBraceIndex).TrimEnd();
            bool hasProperties = prefix.Length > 1;
            return prefix + (hasProperties ? "," : string.Empty) + $"\"{DefaultDeskMetadataKey}\":{value}" + "}";
        }
    }
}
