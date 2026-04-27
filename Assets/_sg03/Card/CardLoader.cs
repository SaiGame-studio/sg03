using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SG03
{
    /// <summary>
    /// Loads a <see cref="CardData"/> ScriptableObject from Addressables and applies
    /// it to the sibling <see cref="Card3D"/> component.
    ///
    /// Usage:
    ///   1. Mark the CardData asset as Addressable in the Inspector.
    ///   2. Copy the Addressable Address string (e.g. "Cards/CardData_Warrior").
    ///   3. Paste it into the "Card Address" field on this component.
    ///   4. Call LoadAsync() at the desired point in your game flow, or enable
    ///      "Load On Start" for automatic loading.
    ///
    /// The loaded handle is released automatically when this component is destroyed.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Loader")]
    [RequireComponent(typeof(Card3D))]
    public class CardLoader : MonoBehaviour
    {
        [Tooltip("Addressable address of the CardData asset to load (e.g. Cards/CardData_Warrior).")]
        [SerializeField] private string cardAddress;

        [Tooltip("When true, LoadAsync() is called automatically in Start().")]
        [SerializeField] private bool loadOnStart = true;

        private Card3D                         card3D;
        private AsyncOperationHandle<CardData> handle;
        private bool                           handleIsValid;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            card3D = GetComponent<Card3D>();
        }

        private void Start()
        {
            if (!loadOnStart) return;
            _ = LoadAsync();
        }

        private void OnDestroy() => ReleaseHandle();

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the CardData from Addressables asynchronously and applies it to
        /// the Card3D. Releases any previously loaded handle first.
        /// </summary>
        public async Task LoadAsync(string address = null)
        {
            string resolvedAddress = string.IsNullOrEmpty(address) ? cardAddress : address;

            if (string.IsNullOrEmpty(resolvedAddress))
            {
                Debug.LogWarning($"[CardLoader] Card address is empty on '{name}'.", this);
                return;
            }

            ReleaseHandle();

            handle      = Addressables.LoadAssetAsync<CardData>(resolvedAddress);
            handleIsValid = true;

            try
            {
                await handle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardLoader] Failed to load '{resolvedAddress}': {e.Message}", this);
                ReleaseHandle();
                return;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[CardLoader] Addressables load failed for '{resolvedAddress}'.", this);
                ReleaseHandle();
                return;
            }

            card3D.SetCardData(handle.Result);
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
