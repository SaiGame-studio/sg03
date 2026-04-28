using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Controller that wires together <see cref="Card3D"/>, <see cref="CardLoader"/>,
    /// and <see cref="CardReviewMovement"/> via GetComponent on the same GameObject.
    /// Provides a single public API for review scenes / test rigs.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card 3D Review Ctrl")]
    [RequireComponent(typeof(Card3D))]
    [RequireComponent(typeof(CardLoader))]
    [RequireComponent(typeof(CardReviewMovement))]
    public class Card3DReviewCtrl : SaiBehaviour
    {
        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3D             card;
        [SerializeField] private CardLoader         loader;
        [SerializeField] private CardReviewMovement movement;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            card     = GetComponent<Card3D>();
            loader   = GetComponent<CardLoader>();
            movement = GetComponent<CardReviewMovement>();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Loads CardData via Addressables and applies it to the Card3D.</summary>
        public async void LoadCard() => await loader.LoadAndApply();

        /// <summary>Applies the textures currently set on Card3D.</summary>
        public void ApplyTextures() => card.ApplyTextures();

        /// <summary>Shows the card front face immediately.</summary>
        public void ShowFront() => card.ShowFront();

        /// <summary>Shows the card back face immediately.</summary>
        public void ShowBack() => card.ShowBack();

        /// <summary>Flips the card with animation.</summary>
        public void Flip() => card.Flip();

        /// <summary>Flies the card upward with spin animation.</summary>
        public void FlyUp() => movement.FlyUp();

        /// <summary>Returns the card to its origin position with spin animation.</summary>
        public void FlyDown() => movement.FlyDown();
    }
}
