using System;
using System.Collections;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    /// <summary>
    /// Owns the Soul Energy indicator, currency tooltips, and automatic Soul collection.
    /// </summary>
    public sealed class SoulEnergyUI
    {
        private const string SoulGeneratorItemCode = "soul_generaror";
        private const string SoulItemCode = "soul";

        private readonly MonoBehaviour host;
        private readonly CurrencyWallet currencyWallet;
        private readonly VisualElement panelRoot;
        private readonly VisualElement soulEnergy;
        private readonly Label soulEnergyValue;
        private readonly VisualElement soulEnergyPopup;
        private readonly VisualElement soulEnergyClaimRow;
        private readonly Label soulEnergyClaimValue;
        private readonly VisualElement soulEnergyNextClaimRow;
        private readonly Label soulEnergyNextClaimValue;
        private readonly Label soulEnergyFullLabel;
        private readonly Label soulEnergyFullValue;
        private readonly VisualElement lesserVesselCurrency;
        private readonly VisualElement commonVesselCurrency;
        private readonly VisualElement greaterVesselCurrency;
        private readonly Label lesserVesselValue;
        private readonly Label commonVesselValue;
        private readonly Label greaterVesselValue;
        private readonly VisualElement topCurrencyPopup;
        private readonly Label topCurrencyPopupLabel;

        private IVisualElementScheduledItem popupRefreshSchedule;
        private ItemGenerator subscribedItemGenerator;
        private CurrencyWallet subscribedCurrencyWallet;
        private Coroutine autoClaimCoroutine;
        private bool isClaimInProgress;

        public SoulEnergyUI(MonoBehaviour host, VisualElement root, CurrencyWallet currencyWallet, VisualElement mountPoint = null)
        {
            this.host = host;
            this.currencyWallet = currencyWallet;
            this.panelRoot = root.Q("LobbyRoot") ?? root.Q("GameRoot") ?? root;
            this.CreateVisualTreeIfMissing(root, mountPoint);
            this.soulEnergy = root.Q("SoulEnergy");
            this.soulEnergyValue = root.Q<Label>("SoulEnergyValue");
            this.soulEnergyPopup = root.Q("SoulEnergyPopup");
            this.soulEnergyClaimRow = root.Q("SoulEnergyClaimRow");
            this.soulEnergyClaimValue = root.Q<Label>("SoulEnergyClaimValue");
            this.soulEnergyNextClaimRow = root.Q("SoulEnergyNextClaimRow");
            this.soulEnergyNextClaimValue = root.Q<Label>("SoulEnergyNextClaimValue");
            this.soulEnergyFullLabel = root.Q<Label>("SoulEnergyFullLabel");
            this.soulEnergyFullValue = root.Q<Label>("SoulEnergyFullValue");
            this.lesserVesselCurrency = root.Q("LesserVesselCurrency");
            this.commonVesselCurrency = root.Q("CommonVesselCurrency");
            this.greaterVesselCurrency = root.Q("GreaterVesselCurrency");
            this.lesserVesselValue = root.Q<Label>("LesserVesselValue");
            this.commonVesselValue = root.Q<Label>("CommonVesselValue");
            this.greaterVesselValue = root.Q<Label>("GreaterVesselValue");
            this.topCurrencyPopup = root.Q("TopCurrencyPopup");
            this.topCurrencyPopupLabel = root.Q<Label>("TopCurrencyPopupLabel");
        }

        private void CreateVisualTreeIfMissing(VisualElement root, VisualElement mountPoint)
        {
            if (mountPoint == null) return;

            root.Q("SoulEnergy")?.RemoveFromHierarchy();
            root.Q("SoulEnergyPopup")?.RemoveFromHierarchy();

            Button indicator = new Button { name = "SoulEnergy", tooltip = "Soul Collector" };
            indicator.AddToClassList("soul-energy");
            VisualElement icon = new VisualElement { tooltip = "Soul Energy" };
            icon.AddToClassList("soul-energy__icon");
            Label value = new Label("0 / 0") { name = "SoulEnergyValue" };
            value.AddToClassList("soul-energy__value");
            indicator.Add(icon);
            indicator.Add(value);
            mountPoint.Insert(0, indicator);

            VisualElement popup = new VisualElement { name = "SoulEnergyPopup" };
            popup.AddToClassList("soul-energy-popup");
            popup.style.display = DisplayStyle.None;
            popup.Add(this.CreateLabel("Soul Collector", "soul-energy-popup__title"));
            popup.Add(this.CreatePopupRow("SoulEnergyClaimRow", "Claim now", "SoulEnergyClaimValue", "0"));

            VisualElement nextClaimRow = this.CreatePopupRow("SoulEnergyNextClaimRow", "Next claim in", "SoulEnergyNextClaimValue", "—");
            nextClaimRow.style.display = DisplayStyle.None;
            popup.Add(nextClaimRow);
            popup.Add(this.CreatePopupRow("SoulEnergyFullRow", "Full in", "SoulEnergyFullValue", "—"));
            this.panelRoot.Add(popup);
        }

        private Label CreateLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private VisualElement CreatePopupRow(string rowName, string labelText, string valueName, string valueText)
        {
            VisualElement row = new VisualElement { name = rowName };
            row.AddToClassList("soul-energy-popup__row");
            row.Add(this.CreateLabel(labelText, "soul-energy-popup__label"));
            Label value = this.CreateLabel(valueText, "soul-energy-popup__value");
            value.name = valueName;
            row.Add(value);
            return row;
        }

        public void Initialize()
        {
            this.soulEnergy?.RegisterCallback<PointerEnterEvent>(_ => this.ShowSoulEnergyPopup());
            this.soulEnergy?.RegisterCallback<PointerLeaveEvent>(_ => this.HideSoulEnergyPopup());
            this.RegisterCurrencyTooltip(this.lesserVesselCurrency, "Lesser Vessel");
            this.RegisterCurrencyTooltip(this.commonVesselCurrency, "Common Vessel");
            this.RegisterCurrencyTooltip(this.greaterVesselCurrency, "Greater Vessel");
            this.Subscribe();
            this.RefreshTopCurrencies();
            this.Load();
        }

        public void Load()
        {
            this.RefreshSoulEnergy();
            SaiServer activeServer = SaiServer.Instance;
            if (activeServer == null || !activeServer.IsAuthenticated) return;

            this.EnsureSoulGeneratorLoaded();
            this.RestartAutoClaimTimer();
        }

        public void Dispose()
        {
            this.StopAutoClaimTimer();
            this.popupRefreshSchedule?.Pause();

            if (this.subscribedItemGenerator != null)
                this.subscribedItemGenerator.OnGetGeneratorsSuccess -= this.OnGeneratorsUpdated;
            if (this.subscribedCurrencyWallet != null)
                this.subscribedCurrencyWallet.OnBalancesUpdated -= this.OnCurrenciesUpdated;

            this.subscribedItemGenerator = null;
            this.subscribedCurrencyWallet = null;
        }

        private void RegisterCurrencyTooltip(VisualElement currency, string currencyName)
        {
            currency?.RegisterCallback<PointerEnterEvent>(_ => this.ShowTopCurrencyPopup(currency, currencyName));
            currency?.RegisterCallback<PointerLeaveEvent>(_ => this.HideTopCurrencyPopup());
        }

        private void Subscribe()
        {
            ItemGenerator itemGenerator = SaiServer.Instance?.ItemGenerator;
            if (itemGenerator != null)
            {
                itemGenerator.OnGetGeneratorsSuccess += this.OnGeneratorsUpdated;
                this.subscribedItemGenerator = itemGenerator;
            }

            if (this.currencyWallet != null)
            {
                this.currencyWallet.OnBalancesUpdated += this.OnCurrenciesUpdated;
                this.subscribedCurrencyWallet = this.currencyWallet;
            }
        }

        private void OnGeneratorsUpdated(GeneratorsResponse _)
        {
            this.RefreshSoulEnergy();
            this.RestartAutoClaimTimer();
        }

        private void OnCurrenciesUpdated()
        {
            this.RefreshTopCurrencies();
            this.RefreshSoulEnergy();
            this.EnsureSoulGeneratorLoaded();
            this.RestartAutoClaimTimer();
        }

        private void EnsureSoulGeneratorLoaded()
        {
            ItemGenerator itemGenerator = SaiServer.Instance?.ItemGenerator;
            if (itemGenerator == null) return;
            if (this.FindSoulGenerator(itemGenerator.CurrentGenerators?.generators) != null) return;
            itemGenerator.GetGenerators();
        }

        private void RefreshTopCurrencies()
        {
            this.SetCurrencyValue(this.lesserVesselValue, "lesser_vessel");
            this.SetCurrencyValue(this.commonVesselValue, "common_vessel");
            this.SetCurrencyValue(this.greaterVesselValue, "greater_vessel");
        }

        private void SetCurrencyValue(Label label, string itemCode)
        {
            if (label != null) label.text = (this.currencyWallet?.GetBalanceByItemCode(itemCode) ?? 0).ToString();
        }

        private void ShowTopCurrencyPopup(VisualElement currency, string currencyName)
        {
            if (currency == null || this.topCurrencyPopup == null || this.topCurrencyPopupLabel == null || this.panelRoot == null) return;

            Rect bounds = currency.worldBound;
            Rect rootBounds = this.panelRoot.worldBound;
            this.topCurrencyPopupLabel.text = currencyName;
            this.topCurrencyPopup.style.left = bounds.center.x - rootBounds.xMin - 44f;
            this.topCurrencyPopup.style.top = bounds.yMax - rootBounds.yMin + 6f;
            this.topCurrencyPopup.style.display = DisplayStyle.Flex;
        }

        private void HideTopCurrencyPopup()
        {
            if (this.topCurrencyPopup != null) this.topCurrencyPopup.style.display = DisplayStyle.None;
        }

        private void RefreshSoulEnergy()
        {
            if (this.soulEnergy == null) return;

            GeneratorData generator = this.FindSoulGenerator(SaiServer.Instance?.ItemGenerator?.CurrentGenerators?.generators);
            int current = this.currencyWallet?.GetBalanceByItemCode(SoulItemCode) ?? 0;
            this.soulEnergyValue.text = $"{current} / {this.GetSoulCollectCap(generator)}";
            this.soulEnergy.style.display = DisplayStyle.Flex;
        }

        private void ShowSoulEnergyPopup()
        {
            this.RefreshSoulEnergyPopup();
            if (this.soulEnergyPopup == null) return;
            if (this.popupRefreshSchedule == null)
                this.popupRefreshSchedule = this.soulEnergyPopup.schedule.Execute(this.RefreshSoulEnergyPopup).Every(1000);
            else
                this.popupRefreshSchedule.Resume();
        }

        private void RefreshSoulEnergyPopup()
        {
            ItemGenerator itemGenerator = SaiServer.Instance?.ItemGenerator;
            GeneratorData generator = this.FindSoulGenerator(itemGenerator?.CurrentGenerators?.generators);
            if (this.soulEnergyPopup == null || generator == null) return;

            int current = this.currencyWallet?.GetBalanceByItemCode(SoulItemCode) ?? 0;
            bool isFull = SoulEnergyUtility.IsFull(current, this.GetSoulCollectCap(generator));
            bool hasPending = generator.GetCurrentPendingUnits() > 0;
            if (this.soulEnergyClaimValue != null)
                this.soulEnergyClaimValue.text = FormatExpectedAmount(this.GetExpectedOutput(itemGenerator, generator));
            if (this.soulEnergyClaimRow != null)
                this.soulEnergyClaimRow.style.display = !isFull && hasPending ? DisplayStyle.Flex : DisplayStyle.None;
            if (this.soulEnergyNextClaimRow != null)
                this.soulEnergyNextClaimRow.style.display = !isFull && !hasPending ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasPending && this.soulEnergyNextClaimValue != null)
                this.soulEnergyNextClaimValue.text = FormatCountdown(generator.GetDynamicNextTickSeconds());
            if (this.soulEnergyFullLabel != null) this.soulEnergyFullLabel.text = isFull ? "Already full" : "Full in";
            if (this.soulEnergyFullValue != null)
                this.soulEnergyFullValue.text = isFull ? string.Empty : itemGenerator.GetGeneratorTimeUntilFull(generator.inventory_item_id);

            this.PositionSoulEnergyPopup();
            this.soulEnergyPopup.style.display = DisplayStyle.Flex;
        }

        private void PositionSoulEnergyPopup()
        {
            if (this.soulEnergyPopup == null || this.soulEnergy == null || this.panelRoot == null) return;
            Rect soulBounds = this.soulEnergy.worldBound;
            Rect rootBounds = this.panelRoot.worldBound;
            this.soulEnergyPopup.style.left = soulBounds.xMax - rootBounds.xMin - 164f;
            this.soulEnergyPopup.style.top = soulBounds.yMax - rootBounds.yMin + 6f;
        }

        private void HideSoulEnergyPopup()
        {
            if (this.soulEnergyPopup != null) this.soulEnergyPopup.style.display = DisplayStyle.None;
            this.popupRefreshSchedule?.Pause();
        }

        private void RestartAutoClaimTimer()
        {
            this.StopAutoClaimTimer();
            if (this.isClaimInProgress) return;

            GeneratorData generator = this.FindSoulGenerator(SaiServer.Instance?.ItemGenerator?.CurrentGenerators?.generators);
            if (generator == null || this.IsSoulStorageFull(generator)) return;

            int seconds = generator.GetCurrentPendingUnits() > 0 ? 2 : generator.GetDynamicNextTickSeconds() + 2;
            this.autoClaimCoroutine = this.host.StartCoroutine(this.AutoClaimAfterDelay(seconds));
        }

        private IEnumerator AutoClaimAfterDelay(int seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0, seconds));
            this.autoClaimCoroutine = null;

            ItemGenerator itemGenerator = SaiServer.Instance?.ItemGenerator;
            GeneratorData generator = this.FindSoulGenerator(itemGenerator?.CurrentGenerators?.generators);
            if (generator == null || this.IsSoulStorageFull(generator)) yield break;
            if (generator.GetCurrentPendingUnits() > 0) this.Claim(generator, itemGenerator);
            else this.RestartAutoClaimTimer();
        }

        private void Claim(GeneratorData generator, ItemGenerator itemGenerator)
        {
            if (this.isClaimInProgress || itemGenerator == null) return;
            this.isClaimInProgress = true;
            itemGenerator.CollectGenerator(generator.inventory_item_id,
                _ =>
                {
                    this.isClaimInProgress = false;
                    this.currencyWallet?.Refresh();
                    this.Load();
                },
                _ =>
                {
                    this.isClaimInProgress = false;
                    this.RestartAutoClaimTimer();
                });
        }

        private void StopAutoClaimTimer()
        {
            if (this.autoClaimCoroutine == null) return;
            this.host.StopCoroutine(this.autoClaimCoroutine);
            this.autoClaimCoroutine = null;
        }

        private bool IsSoulStorageFull(GeneratorData generator)
        {
            int current = this.currencyWallet?.GetBalanceByItemCode(SoulItemCode) ?? 0;
            return SoulEnergyUtility.IsFull(current, this.GetSoulCollectCap(generator));
        }

        private GeneratorData FindSoulGenerator(GeneratorData[] generators)
        {
            if (generators == null) return null;
            foreach (GeneratorData generator in generators)
                if (generator != null && string.Equals(generator.definition?.item_code, SoulGeneratorItemCode, StringComparison.OrdinalIgnoreCase))
                    return generator;
            return null;
        }

        private int GetSoulCollectCap(GeneratorData generator)
        {
            if (generator == null) return 0;
            string definitionId = this.currencyWallet?.GetDefinitionIdByItemCode(SoulItemCode);
            foreach (GeneratorOutputPool output in generator.output_pool ?? Array.Empty<GeneratorOutputPool>())
                if (output != null && (string.IsNullOrEmpty(definitionId) || output.item_definition_id == definitionId))
                    return output.collect_cap;
            return 0;
        }

        private GeneratorExpectedOutput GetExpectedOutput(ItemGenerator itemGenerator, GeneratorData generator)
        {
            if (itemGenerator == null || generator == null) return null;
            string definitionId = this.currencyWallet?.GetDefinitionIdByItemCode(SoulItemCode);
            foreach (GeneratorExpectedOutput output in itemGenerator.GetGeneratorExpectedOutput(generator.inventory_item_id) ?? Array.Empty<GeneratorExpectedOutput>())
                if (string.IsNullOrEmpty(definitionId) || output.item_definition_id == definitionId)
                    return output;
            return null;
        }

        private static string FormatExpectedAmount(GeneratorExpectedOutput output)
        {
            if (output == null) return "0";
            return output.expected_min == output.expected_max ? output.expected_min.ToString() : $"{output.expected_min}-{output.expected_max}";
        }

        private static string FormatCountdown(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
            return time.TotalHours >= 1 ? $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}" : $"{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }

    public static class SoulEnergyUtility
    {
        public static bool IsFull(int currentCount, int collectCap)
        {
            return collectCap > 0 && currentCount >= collectCap;
        }
    }
}
