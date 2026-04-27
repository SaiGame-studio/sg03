using System.Threading.Tasks;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Manages the Card3DReview GameObject used to display card information.
    ///
    /// Flow:
    ///   1. User selects a card → call ShowCardAsync("Cards/CardData_Warrior").
    ///   2. Manager finds the scene object named "Card3DReview" automatically via LoadComponents.
    ///   3. CardLoader loads the CardData from Addressables and applies textures.
    ///   4. Call HideCard() to deactivate the view and release the Addressables handle.
    ///
    /// Setup:
    ///   - Place a GameObject named "Card3DReview" in the scene (with Card3D + CardLoader).
    ///   - On the CardLoader set "Load On Start" = false so the manager controls loading.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Data Manager")]
    public class CardDataManager : SaiBehaviour
    {
        public static CardDataManager Instance { get; private set; }

        [Header("Card 3D Review")]
        [SerializeField] private Card3D    card3DReview;
        [SerializeField] private CardLoader card3DLoader;

        // ─── SaiBehaviour lifecycle ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.RegisterSingleton();
            this.LoadCard3DReview();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void RegisterSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // Finds the "Card3DReview" object already present in the scene and caches
        // its Card3D and CardLoader references. Called automatically by LoadComponents
        // so the links are always up-to-date after a Reset.
        private void LoadCard3DReview()
        {
            if (this.card3DReview != null) return;

            GameObject go = GameObject.Find("Card3DReview");
            if (go == null) return;

            this.card3DReview = go.GetComponent<Card3D>();
            this.card3DLoader = go.GetComponent<CardLoader>();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the card view for the given Addressable address.
        /// Instantiates the prefab on the first call; subsequent calls reuse the
        /// same instance and just swap the loaded CardData.
        /// </summary>
        public async Task ShowCardAsync(string cardAddress)
        {
            if (string.IsNullOrEmpty(cardAddress))
            {
                Debug.LogWarning("[CardDataManager] cardAddress is empty.", this);
                return;
            }

            EnsureCardInstance();

            if (card3DReview == null) return;

            card3DReview.gameObject.SetActive(true);
            await card3DLoader.LoadAsync(cardAddress);
        }

        /// <summary>
        /// Hides the card view and releases the Addressables handle for the CardData.
        /// </summary>
        public void HideCard()
        {
            if (card3DReview == null) return;

            card3DLoader.ReleaseHandle();
            card3DReview.gameObject.SetActive(false);
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private void EnsureCardInstance()
        {
            if (card3DReview != null) return;
            Debug.LogError("[CardDataManager] Card3DReview not found in scene. " +
                           "Place a GameObject named \"Card3DReview\" with Card3D + CardLoader.", this);
        }
    }
}
