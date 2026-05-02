using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Base controller that owns pre-wired references to <see cref="Card3D"/> and
    /// <see cref="CardLoader"/> on the same GameObject.
    /// Extend this class when a controller only needs card data and loading,
    /// without the review-movement functionality of <see cref="Card3DReviewCtrl"/>.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card 3D Ctrl")]
    [RequireComponent(typeof(Card3D))]
    [RequireComponent(typeof(CardLoader))]
    public class Card3DCtrl : PoolObj
    {
        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3D card;
        [SerializeField] private CardLoader loader;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        public override string GetName() => this.name;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3D();
            this.LoadCardLoader();
        }

        protected virtual void LoadCard3D()
        {
            if (this.card != null) return;
            this.card = this.GetComponent<Card3D>();
            Debug.LogWarning(transform.name + "LoadCard3D", gameObject);
        }

        protected virtual void LoadCardLoader()
        {
            if (this.loader != null) return;
            this.loader = this.GetComponent<CardLoader>();
            Debug.LogWarning(transform.name + "LoadCardLoader", gameObject);
        }


        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Loads CardData via Addressables and applies it to the Card3D.</summary>
        public async void LoadCard() => await this.loader.LoadAndApply();

        /// <summary>Loads CardData by code name and applies it to the Card3D.</summary>
        public async void LoadCardByCodeName(string codeName) => await this.loader.ShowByCodeName(codeName);

        /// <summary>Applies the textures currently set on Card3D.</summary>
        public void ApplyTextures() => this.card.ApplyTextures();

        /// <summary>Shows the card front face immediately.</summary>
        public void ShowFront() => this.card.ShowFront();

        /// <summary>Shows the card back face immediately.</summary>
        public void ShowBack() => this.card.ShowBack();

        /// <summary>Flips the card with animation.</summary>
        public void Flip() => this.card.Flip();

        /// <summary>Assigns new CardData and immediately applies its textures.</summary>
        public void SetCardData(CardData data) => this.card.SetCardData(data);

        /// <summary>
        /// Sets the fallback display name shown in CardNameText when the
        /// assigned CardData has no CardName filled in.
        /// Call before <see cref="LoadCardByCodeName"/> so the name is ready
        /// when ApplyCardText runs.
        /// </summary>
        public void SetFallbackName(string name) => this.card.SetFallbackName(name);

        /// <summary>
        /// Sets fallback ATK / DEF / Stars shown when the assigned CardData has zeros.
        /// Pass stats parsed from <c>ItemDefinitionData.base_stats</c>.
        /// </summary>
        public void SetFallbackStats(CardBaseStats stats) => this.card.SetFallbackStats(stats);

        /// <summary>
        /// Sets fallback description shown in DescriptionText when CardData.Description is empty.
        /// Pass <c>ItemDefinitionMetadata.description</c>.
        /// </summary>
        public void SetFallbackDescription(string description) => this.card.SetFallbackDescription(description);
    }
}
