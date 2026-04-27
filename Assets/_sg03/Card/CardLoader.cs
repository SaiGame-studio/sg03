using System;
using System.Threading.Tasks;
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
    [RequireComponent(typeof(Card3D))]
    public class CardLoader : MonoBehaviour
    {
        [SerializeField] private string cardAddress;

        [SerializeField] private string cardNamePrefix = "azure_blade";

        [SerializeField] private bool loadOnStart = false;

        private AsyncOperationHandle<CardData> handle;
        private bool                           handleIsValid;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (!loadOnStart) return;
            _ = LoadAndApplyAsync();
        }

        // Convenience wrapper for standalone use (loadOnStart = true).
        // Loads CardData and applies it directly to the sibling Card3D.
        private async System.Threading.Tasks.Task LoadAndApplyAsync()
        {
            CardData data = await LoadAsync();
            if (data == null) return;
            GetComponent<Card3D>().SetCardData(data);
        }

        /// <summary>
        /// Loads the CardData and applies it to the sibling Card3D immediately.
        /// Callable from Editor buttons or runtime code.
        /// </summary>
        public async System.Threading.Tasks.Task LoadAndApply()
        {
            await LoadAndApplyAsync();
        }

        private void OnDestroy() => ReleaseHandle();

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the CardData from Addressables asynchronously and returns it.
        /// The caller is responsible for calling Card3D.SetCardData with the result.
        /// Releases any previously loaded handle first.
        /// Returns null on failure.
        /// </summary>
        public async Task<CardData> LoadAsync(string address = null)
        {
            string resolvedAddress = string.IsNullOrEmpty(address) ? cardAddress : address;

            if (string.IsNullOrEmpty(resolvedAddress))
            {
                Debug.LogWarning($"[CardLoader] Card address is empty on '{name}'.", this);
                return null;
            }

            ReleaseHandle();

            handle        = Addressables.LoadAssetAsync<CardData>(resolvedAddress);
            handleIsValid = true;

            try
            {
                await handle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardLoader] Failed to load '{resolvedAddress}': {e.Message}", this);
                ReleaseHandle();
                return null;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[CardLoader] Addressables load failed for '{resolvedAddress}'.", this);
                ReleaseHandle();
                return null;
            }

            return handle.Result;
        }

        /// <summary>
        /// Loads the CardData by short asset name (e.g. "azure_blade").
        /// The full address is built as: <see cref="cardNamePrefix"/> + <paramref name="cardName"/>.
        /// </summary>
        public async Task<CardData> LoadByNameAsync(string cardName)
        {
            if (string.IsNullOrEmpty(cardName))
            {
                Debug.LogWarning($"[CardLoader] Card name is empty on '{name}'.", this);
                return null;
            }

            return await LoadAsync(cardNamePrefix + cardName);
        }

        /// <summary>Releases the loaded Addressables handle.</summary>
        public void ReleaseHandle()
        {
            if (!handleIsValid) return;
            Addressables.Release(handle);
            handleIsValid = false;
        }
    }
}
