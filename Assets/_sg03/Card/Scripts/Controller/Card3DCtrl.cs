using System;
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
    [RequireComponent(typeof(CardMovement))]
    public class Card3DCtrl : PoolObj
    {
        // ─── Static card events ───────────────────────────────────────────────────

        public static event Action<Card3DCtrl> HoverEntered;
        public static event Action<Card3DCtrl> HoverExited;
        public static event Action<Card3DCtrl> CardSelected;

        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3D card;
        [SerializeField] private CardLoader loader;
        [SerializeField] private CardMovement movement;

        // ─── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private CardType cardType;
        [SerializeField] private Owner    cardOwner;

        // ─── Optional external references ─────────────────────────────────────────

        [Header("Optional References")]
        [SerializeField] private CardHolderCtrl cardHolder;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        public override string GetName() => this.name;

        public void NotifyHoverEntered() => HoverEntered?.Invoke(this);
        public void NotifyHoverExited()  => HoverExited?.Invoke(this);
        public void NotifySelected()     => CardSelected?.Invoke(this);

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3D();
            this.LoadCardLoader();
            this.LoadCardMovement();
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

        protected virtual void LoadCardMovement()
        {
            if (this.movement != null) return;
            this.movement = this.GetComponent<CardMovement>();
            Debug.LogWarning(transform.name + "LoadCardMovement", gameObject);
        }

        protected override void LoadDespawn()
        {
            //do nothing
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

        /// <summary>Links a <see cref="CardHolderCtrl"/> to this card and moves the card to the holder's position.</summary>
        public void SetCardHolder(CardHolderCtrl holder)
        {
            bool hadHolder = this.cardHolder != null;
            this.cardHolder = holder;
            if (this.cardHolder == null) return;
            Location destination = this.cardHolder.HolderLink == Link.front ? Location.in_front : Location.in_back;
            this.movement.MoveTo(this.cardHolder.transform, destination);
            if (!hadHolder) this.movement.FaceUpUnknown();
        }

        /// <summary>Smoothly moves the card to the specified transform, syncing both position and rotation.</summary>
        public void MoveAndRotate(Transform target, Location destination) => this.movement.MoveAndRotate(target, destination);

        /// <summary>Smoothly moves the card to the specified transform, position only (no rotation change).</summary>
        public void MoveTo(Transform target, Location destination) => this.movement.MoveTo(target, destination);

        /// <summary>Moves the card to the full-detail point without changing its logical location.</summary>
        public void MoveToFullDetail(Transform point) => this.movement.MoveToFullDetail(point);

        /// <summary>Returns the card from full-detail back to its selected position in hand.</summary>
        public void ReturnFromFullDetail() => this.movement.ReturnFromFullDetail();

        /// <summary>Toggles the card between face-up and face-down.</summary>
        public void ToggleFace() => this.movement.ToggleFace();

        /// <summary>Current logical location of this card.</summary>
        public Location Location => this.movement.Location;

        /// <summary>The holder this card is currently assigned to, or null if none.</summary>
        public CardHolderCtrl CardHolder => this.cardHolder;

        /// <summary>The type of this card (character or support).</summary>
        public CardType CardType => this.cardType;

        /// <summary>The owner (alpha or omega) of this card.</summary>
        public Owner CardOwner => this.cardOwner;

        /// <summary>Returns true if this card's type is character.</summary>
        public bool IsCharacter() => this.cardType == CardType.character;
    }
}
