using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SG03
{
    /// <summary>
    /// Loads a <see cref="CardData"/> ScriptableObject from Addressables and returns it.
    /// The caller (e.g. <see cref="CardDataManager"/>) is responsible for applying the
    /// result to a <see cref="Card3D"/> component.
    ///
    /// Usage:
    ///   1. Mark the CardData asset as Addressable in the Inspector.
    ///   2. Copy the Addressable Address string (e.g. "Cards/CardData_Warrior").
    ///   3. Paste it into the "Card Address" field on this component.
    ///   4. Set "Load On Start" = false when managed by CardDataManager.
    ///
    /// The loaded handle is released automatically when this component is destroyed.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Loader")]
    [RequireComponent(typeof(Card3DCtrl))]
    public class CardLoader : SaiBehaviour
    {
        [SerializeField] private Card3DCtrl ctrl;

        [SerializeField] private string cardAddress;

        [SerializeField] private string cardNamePrefix = "";

        [SerializeField] private bool loadOnStart = false;

        private AsyncOperationHandle<CardData> handle;
        private bool                           handleIsValid;

// ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3DCtrl();
        }

        protected virtual void LoadCard3DCtrl()
        {
            if (this.ctrl != null) return;
            this.ctrl = this.GetComponent<Card3DCtrl>();
            Debug.LogWarning(this.transform.name + "LoadCard3DCtrl", this.gameObject);
        }

        protected override void Start()
        {
            base.Start();
            this.LoadOnStartIfEnabled();
        }

        private void LoadOnStartIfEnabled()
        {
            if (!this.loadOnStart) return;
            _ = this.LoadAndApply();
        }

        private void OnDestroy() => this.ReleaseHandle();

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the CardData from Addressables asynchronously and returns it.
        /// Releases any previously loaded handle first. Returns null on failure.
        /// </summary>
        public async Task<CardData> LoadAsync(string address = null)
        {
            string resolvedAddress = string.IsNullOrEmpty(address) ? this.cardAddress : address;

            if (string.IsNullOrEmpty(resolvedAddress))
            {
                Debug.LogWarning($"[CardLoader] Card address is empty on '{this.name}'.", this);
                return null;
            }

            this.ReleaseHandle();

            this.handle        = Addressables.LoadAssetAsync<CardData>(resolvedAddress);
            this.handleIsValid = true;

            try
            {
                await this.handle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardLoader] Failed to load '{resolvedAddress}': {e.Message}", this);
                this.ReleaseHandle();
                return null;
            }

            if (this.handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[CardLoader] Addressables load failed for '{resolvedAddress}'.", this);
                this.ReleaseHandle();
                return null;
            }

            return this.handle.Result;
        }

        /// <summary>
        /// Loads the CardData from <see cref="cardAddress"/> and applies it to the Card3D.
        /// </summary>
        public async System.Threading.Tasks.Task LoadAndApply()
        {
            CardData data = await this.LoadAsync();
            if (data == null) return;
            this.ctrl.SetCardData(data);
        }

        /// <summary>Releases the loaded Addressables handle.</summary>
        public void ReleaseHandle()
        {
            if (!this.handleIsValid) return;
            Addressables.Release(this.handle);
            this.handleIsValid = false;
        }

        /// <summary>
        /// Loads the CardData by short asset name. The full address is built as
        /// <see cref="cardNamePrefix"/> + <paramref name="cardName"/>.
        /// Returns null on failure; does not apply to Card3D.
        /// </summary>
        public async Task<CardData> LoadByNameAsync(string cardName)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                Debug.LogWarning($"[CardLoader] Card name is empty on '{this.name}'.", this);
                return null;
            }

            return await this.LoadAsync(this.cardNamePrefix + cardName);
        }

        /// <summary>
        /// Searches <see cref="CardDataManager.CardAddresses"/> for an address whose asset
        /// file name matches <see cref="cardNamePrefix"/> + ".asset" and writes it into
        /// <see cref="cardAddress"/>.
        /// Returns true when a match is found.
        /// </summary>
        public bool ApplyAddressByPrefix()
        {
            if (CardDataManager.Instance == null)
            {
                Debug.LogWarning($"[CardLoader] CardDataManager not found on '{this.name}'.", this);
                return false;
            }

            if (TryResolveAddressByAssetName(CardDataManager.Instance.CardAddresses, this.cardNamePrefix, out string addr))
            {
                this.cardAddress = addr;
                return true;
            }

            Debug.LogWarning($"[CardLoader] No address found ending with '{this.cardNamePrefix}.asset' on '{this.name}'.", this);
            return false;
        }

        public static bool TryResolveAddressByAssetName(
            IReadOnlyList<string> cardAddresses,
            string codeName,
            out string address)
        {
            address = null;

            if (cardAddresses == null) return false;
            if (string.IsNullOrEmpty(codeName)) return false;

            string expectedFileName = codeName + ".asset";
            foreach (string candidate in cardAddresses)
            {
                if (!IsAddressFileName(candidate, expectedFileName)) continue;
                address = candidate;
                return true;
            }

            return false;
        }

        private static bool IsAddressFileName(string address, string expectedFileName)
        {
            if (string.IsNullOrEmpty(address)) return false;

            string normalizedAddress = address.Replace('\\', '/');
            int fileNameStartIndex = normalizedAddress.LastIndexOf('/') + 1;
            string fileName = fileNameStartIndex > 0 && fileNameStartIndex < normalizedAddress.Length
                ? normalizedAddress.Substring(fileNameStartIndex)
                : normalizedAddress;

            return string.Equals(fileName, expectedFileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets <see cref="cardNamePrefix"/> to <paramref name="codeName"/>, resolves the
        /// full Addressable address via <see cref="ApplyAddressByPrefix"/>, then loads and
        /// applies the CardData — mirroring the "Load Card By Name" → "Load Card By Address"
        /// Inspector button flow.
        /// </summary>
        public async System.Threading.Tasks.Task ShowByCodeName(string codeName)
        {
            if (string.IsNullOrEmpty(codeName)) return;
            this.cardNamePrefix = codeName;
            if (!this.ApplyAddressByPrefix()) return;
            await this.LoadAndApply();
        }
    }
}
