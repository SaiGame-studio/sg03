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
        public static event Action<Card3DCtrl, bool> FaceStateChanged;

        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3D card;
        [SerializeField] private CardLoader loader;
        [SerializeField] private CardMovement movement;

        // ─── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private Owner              cardOwner;
        [SerializeField] private string             codeName;
        [SerializeField] private string             inventoryItemId;
        [SerializeField] private CardDefinitionData definition;
        [SerializeField] private bool               expose;

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

        /// <summary>Links a <see cref="CardHolderCtrl"/> to this card and moves the card to the holder's position.
        /// <paramref name="onReady"/> is invoked after RotateZ180 completes (new card) or immediately after move starts (existing holder).</summary>
        public void SetCardHolder(CardHolderCtrl holder, System.Action onReady = null)
        {
            if (this.movement.IsFlipping) return;
            bool isNewHolder = this.cardHolder == null;
            this.cardHolder = holder;
            if (this.cardHolder == null) return;
            if (!isNewHolder)
            {
                this.MoveBackToHolder();
                return;
            }
            this.movement.MoveTo(this.cardHolder.transform, this.cardHolder.HolderLocation, () => this.RotateZ180(onReady));
            this.FaceDownUnknown();
        }

        /// <summary>Smoothly moves the card to the specified transform, syncing both position and rotation.</summary>
        public void MoveAndRotate(Transform target, Location destination) => this.movement.MoveAndRotate(target, destination);

        /// <summary>Smoothly moves the card to the specified world position and rotation.</summary>
        public void MoveAndRotate(Vector3 worldPosition, Quaternion rotation, Location destination) => this.movement.MoveAndRotate(worldPosition, rotation, destination);

        /// <summary>Smoothly moves the card to the specified transform, position only (no rotation change).</summary>
        public void MoveTo(Transform target, Location destination) => this.movement.MoveTo(target, destination);

        /// <summary>Smoothly moves the card to the specified world position, no rotation change.</summary>
        public void MoveTo(Vector3 worldPosition, Location destination) => this.movement.MoveTo(worldPosition, destination);

        /// <summary>Smoothly rotates the card in-place to the target world-space rotation.</summary>
        public void RotateTo(Quaternion targetRotation) => this.movement.RotateTo(targetRotation);

        /// <summary>Moves the card to <paramref name="holder"/>'s position and flips face-down via the Unknown axis.
        /// Intended for hand → line transitions.</summary>
        public void MoveToUnknow(CardHolderCtrl holder, System.Action onReady = null) => this.movement.MoveToUnknow(holder, onReady);

        /// <summary>Assigns the card-holder reference without triggering any movement or animation.</summary>
        public void AssignCardHolder(CardHolderCtrl holder) => this.cardHolder = holder;

        /// <summary>Rotates the card 180 degrees around the world Z axis, then invokes <paramref name="onComplete"/>.</summary>
        public void RotateZ180(System.Action onComplete = null) => this.movement.RotateY180(onComplete);

        /// <summary>Smoothly rotates the card to face-down using the Unknown axis, without rising.</summary>
        public void FaceDownUnknown()
        {
            this.movement.FaceDownUnknown();
            FaceStateChanged?.Invoke(this, false);
        }

        /// <summary>Smoothly rotates the card to face-up using the Unknown axis, without rising.</summary>
        public void FaceUpUnknown()
        {
            this.movement.FaceUpUnknown();
            FaceStateChanged?.Invoke(this, true);
        }

        /// <summary>Smoothly rotates the card to face-up.</summary>
        public void FaceUp()
        {
            this.movement.FaceUp();
            FaceStateChanged?.Invoke(this, true);
        }

        /// <summary>Smoothly rotates the card to face-down.</summary>
        public void FaceDown()
        {
            this.movement.FaceDown();
            FaceStateChanged?.Invoke(this, false);
        }

        /// <summary>Moves the card to the full-detail point without changing its logical location.</summary>
        public void MoveToFullDetail(Transform point) => this.movement.MoveToFullDetail(point);

        /// <summary>Returns the card from full-detail back to its selected position in hand.</summary>
        public void ReturnFromFullDetail() => this.movement.ReturnFromFullDetail();

        /// <summary>Plays the damage run-up animation: card rises then returns to its current position.</summary>
        public void RunUp() => this.movement.RunUp();

        /// <summary>Plays the damage shake animation on the Z axis.</summary>
        public void Damaged() => this.movement.Damaged();

        /// <summary>Plays the ability activation animation.</summary>
        public void AbilityActive() => this.movement.AbilityActive();

        /// <summary>Plays the attack lunge animation: card charges toward the defender then returns.</summary>
        public void AttackLunge(Vector3 defenderPosition) => this.movement.AttackLunge(defenderPosition);

        /// <summary>Plays the attack animation with a small backstep before the lunge, then returns.</summary>
        public void AttackBackstepLunge(Vector3 defenderPosition) => this.movement.AttackBackstepLunge(defenderPosition);

        /// <summary>Moves the card back to its currently assigned <see cref="CardHolderCtrl"/> position (no flip).</summary>
        public void MoveBackToHolder()
        {
            if (this.cardHolder == null) return;
            this.movement.MoveBackToLineHolder(this.cardHolder);
        }

        /// <summary>Moves the card forward toward the defender and stops there (no return).</summary>
        public void PlanningLunge(Vector3 defenderPosition) => this.movement.PlanningLunge(defenderPosition);

        /// <summary>Moves the card directly to the given destination (no stop-distance offset, no return).</summary>
        public void PlanningLungeTo(Vector3 destination) => this.movement.PlanningLungeTo(destination);

        /// <summary>Plays the ability activation animation: card rises + scales up, holds, then returns.</summary>
        public void ActivateAbility() => this.RunUp();

        /// <summary>Toggles the card between face-up and face-down.</summary>
        public void ToggleFace()
        {
            if (this.movement.FaceState == FaceState.FaceUp)
            {
                this.FaceDown();
                return;
            }
            this.FaceUp();
        }

        /// <summary>Current logical location of this card.</summary>
        public Location Location   => this.movement.Location;
        public bool    IsFlipping  => this.movement.IsFlipping;
        public bool    IsAnimating => this.movement.IsAnimating;
        public string  InventoryItemId => this.inventoryItemId;

        public void SetMoveDuration(float d)  => this.movement.SetMoveDuration(d);
        public void SetRotateDuration(float d) => this.movement.SetRotateDuration(d);

        public void SetInventoryItemId(string id) { this.inventoryItemId = id; }

        /// <summary>The holder this card is currently assigned to, or null if none.</summary>
        public CardHolderCtrl CardHolder => this.cardHolder;

        /// <summary>The type of this card (character or support), derived from Definition.Metadata.type.</summary>
        public CardType CardType => Enum.TryParse(this.definition?.metadata?.type, out CardType t) ? t : default;

        // ─── Per-owner spawn counters ─────────────────────────────────────────────
        // Counted independently so alpha and omega each start at 1.

        private static int alphaSpawnIndex = 0;
        private static int omegaSpawnIndex = 0;

        /// <summary>Sets the owner of this card (alpha or omega) and prefixes the
        /// GameObject name with [alpha] or [omega] so cards are easy to identify
        /// in the Hierarchy and Debug logs.
        /// The trailing index is counted independently per owner so alpha and omega
        /// each start from 1 (e.g. [alpha]Card3D_1, [omega]Card3D_1).</summary>
        public void SetOwner(Owner owner)
        {
            this.cardOwner = owner;
            string prefix = $"[{owner}]";
            if (!this.name.StartsWith(prefix))
            {
                // Derive the bare prefab name by stripping any existing owner prefix
                // and the trailing _N index added by Spawner.UpdateName.
                string baseName = this.name;
                if (baseName.StartsWith("[alpha]")) baseName = baseName.Substring(7);
                else if (baseName.StartsWith("[omega]")) baseName = baseName.Substring(7);
                int underscoreIdx = baseName.LastIndexOf('_');
                if (underscoreIdx >= 0 && int.TryParse(baseName.Substring(underscoreIdx + 1), out _))
                    baseName = baseName.Substring(0, underscoreIdx);

                int index = owner == Owner.alpha ? ++alphaSpawnIndex : ++omegaSpawnIndex;
                this.name = $"{prefix}{baseName}_{index}";
            }
        }

        /// <summary>The owner (alpha or omega) of this card.</summary>
        public Owner CardOwner => this.cardOwner;

        /// <summary>Returns true if this card's type is character.</summary>
        public bool IsCharacter() => this.definition?.metadata?.type == "character";

        /// <summary>Stores the definition data looked up by code name from BattleCardDefinitions.</summary>
        public void SetDefinition(CardDefinitionData def) => this.definition = def;

        /// <summary>The definition data currently assigned to this card.</summary>
        public CardDefinitionData Definition => this.definition;

        /// <summary>Stores the code name used to look up this card's definition.</summary>
        public void SetCodeName(string code) => this.codeName = code;

        /// <summary>The code name assigned to this card.</summary>
        public string CodeName => this.codeName;

        /// <summary>Marks whether this card is exposed (always face-up).</summary>
        public void SetExpose(bool value) => this.expose = value;

        /// <summary>Returns true if this card is exposed and must not be flipped face-down.</summary>
        public bool Expose => this.expose;

        /// <summary>Returns true when this is an omega card that should not reveal its tooltip
        /// (hidden unless both exposed and face-up).</summary>
        public bool IsOmegaCardHidden()
        {
            if (this.cardOwner != Owner.omega) return false;
            return !this.expose || this.FaceState != FaceState.FaceUp;
        }

        /// <summary>The current face state of this card.</summary>
        public FaceState FaceState => this.movement.FaceState;
    }
}
