using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Drawer shown by InventoryContentUI for inventory items in the gacha_pack category.
    public class GachaPackDetailUI
    {
        private const string SoulGeneratorItemCode = "soul_generaror";

        private readonly VisualElement panel;
        private readonly VisualElement content;
        private readonly DropdownField packDropdown;
        private readonly Button openPackButton;
        private readonly Label openStatus;
        private readonly List<string> packIds = new List<string>();
        private InventoryItemData selectedItem;

        public event Action OnPackOpened;

        public GachaPackDetailUI(VisualElement root)
        {
            this.panel = root.Q("GachaPackDetailPanel");
            this.content = root.Q("GachaPackDetailContent");
            this.packDropdown = root.Q<DropdownField>("GachaPackSelector");
            this.openPackButton = root.Q<Button>("OpenGachaPackButton");
            this.openStatus = root.Q<Label>("GachaPackOpenStatus");
            root.Q<Button>("CloseGachaPackDetailButton")?.RegisterCallback<ClickEvent>(_ => this.Hide());
            this.openPackButton?.RegisterCallback<ClickEvent>(_ => this.OpenSelectedPack());
        }

        public void Show(InventoryItemData item)
        {
            if (item == null || this.panel == null || this.content == null) return;

            this.content.Clear();
            this.selectedItem = item;
            if (this.openStatus != null) this.openStatus.text = string.Empty;
            if (this.packDropdown != null) this.packDropdown.style.display = DisplayStyle.None;
            if (this.openPackButton != null) this.openPackButton.style.display = DisplayStyle.None;
            this.AddLabel(item.definition?.name ?? item.definition?.item_code ?? "Gacha Pack", "gacha-pack-detail__name");
            this.AddLabel($"Quantity: {item.quantity}", "gacha-pack-detail__quantity");

            string rarity = item.definition?.rarity;
            if (!string.IsNullOrEmpty(rarity))
                this.AddLabel($"Rarity: {rarity}", "gacha-pack-detail__rarity");

            ItemDefinitionMetadata metadata = item.definition?.ParsedMetadata;
            string description = metadata?.description;
            if (string.IsNullOrEmpty(description)) description = metadata?.flavor_text;
            if (!string.IsNullOrEmpty(description))
            {
                this.AddSectionTitle("Description");
                this.AddLabel(description, "gacha-pack-detail__description");
            }

            this.AddSectionTitle("Available Packs");
            string[] packIds = metadata?.gacha_pack_ids;
            this.packIds.Clear();
            if (packIds != null)
            {
                foreach (string packId in packIds)
                {
                    if (!string.IsNullOrEmpty(packId) && !this.packIds.Contains(packId))
                        this.packIds.Add(packId);
                }
            }

            if (this.packIds.Count == 0)
            {
                this.AddLabel("No linked gacha pack was provided.", "gacha-pack-detail__empty");
            }
            else
            {
                this.ConfigurePackSelector();
            }

            this.panel.RemoveFromClassList("gacha-pack-detail-panel--hidden");
            this.panel.AddToClassList("gacha-pack-detail-panel--open");
        }

        public void Hide()
        {
            if (this.panel == null) return;

            this.panel.RemoveFromClassList("gacha-pack-detail-panel--open");
            this.panel.AddToClassList("gacha-pack-detail-panel--hidden");
        }

        private void ConfigurePackSelector()
        {
            if (this.packDropdown != null)
            {
                List<string> choices = new List<string>();
                for (int index = 0; index < this.packIds.Count; index++)
                {
                    string packId = this.packIds[index];
                    string shortId = packId.Length > 8 ? packId.Substring(0, 8) + "…" : packId;
                    choices.Add($"Pack {index + 1} ({shortId})");
                }

                this.packDropdown.choices = choices;
                this.packDropdown.value = choices[0];
                this.packDropdown.SetEnabled(true);
                this.packDropdown.style.display = DisplayStyle.Flex;
            }

            if (this.openPackButton != null)
            {
                this.openPackButton.SetEnabled(true);
                this.openPackButton.style.display = DisplayStyle.Flex;
            }
        }

        private void OpenSelectedPack()
        {
            if (this.selectedItem == null || this.packIds.Count == 0) return;

            GachaPack gachaPack = UnityEngine.Object.FindFirstObjectByType<GachaPack>(FindObjectsInactive.Include);
            if (gachaPack == null)
            {
                this.ShowOpenError("Gacha Pack service is unavailable.");
                return;
            }

            int selectedIndex = this.packDropdown?.index ?? 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, this.packIds.Count - 1);
            bool hadSoulGenerator = this.HasSoulGenerator(SaiServer.Instance?.ItemGenerator?.CurrentGenerators?.generators);

            this.openPackButton?.SetEnabled(false);
            if (this.openStatus != null) this.openStatus.text = "Opening pack…";
            gachaPack.OpenGachaPack(
                gachaPackDefId: this.packIds[selectedIndex],
                targetContainerId: this.selectedItem.item_container_id,
                onSuccess: response =>
                {
                    int rewardCount = response?.items_granted?.Length ?? 0;
                    if (this.openStatus != null) this.openStatus.text = $"Opened! Received {rewardCount} item(s).";
                    if (!hadSoulGenerator)
                        SaiServer.Instance?.ItemGenerator?.GetGenerators();
                    this.OnPackOpened?.Invoke();
                },
                onError: this.ShowOpenError);
        }

        private bool HasSoulGenerator(GeneratorData[] generators)
        {
            if (generators == null) return false;

            foreach (GeneratorData generator in generators)
            {
                if (string.Equals(generator?.definition?.item_code, SoulGeneratorItemCode, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void ShowOpenError(string error)
        {
            if (this.openStatus != null)
                this.openStatus.text = string.IsNullOrEmpty(error) ? "Could not open this pack." : error;

            this.openPackButton?.SetEnabled(true);
        }

        private void AddSectionTitle(string text)
        {
            this.AddLabel(text, "gacha-pack-detail__section-title");
        }

        private void AddLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            this.content.Add(label);
        }
    }
}
