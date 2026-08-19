using DG.Tweening;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Extends <see cref="Card3DCtrl"/> with <see cref="CardReviewMovement"/> wiring.
    /// Adds Show / Hide / RequestShow APIs for review scenes and test rigs.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card 3D Review Ctrl")]
    [RequireComponent(typeof(CardReviewMovement))]
    public class Card3DReviewCtrl : Card3DCtrl
    {
        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Review Components")]
        [SerializeField] private CardReviewMovement reviewMovement;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCardReviewMovement();
        }

        protected virtual void LoadCardReviewMovement()
        {
            if (this.reviewMovement != null) return;
            this.reviewMovement = this.GetComponent<CardReviewMovement>();
            Debug.LogWarning(transform.name + "LoadCardReviewMovement", gameObject);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Flies the card upward with spin animation.</summary>
        public void Show() => this.reviewMovement.Show();

        /// <summary>Returns the card to its origin position with spin animation.</summary>
        public void Hide() => this.reviewMovement.Hide();

        /// <summary>
        /// Shows the card with the given <paramref name="codeName"/>.
        /// <paramref name="displayName"/> is the card name shown on the preview.
        /// <paramref name="stats"/> provides the ATK / DEF / Stars shown on the preview.
        /// <paramref name="description"/> is the description shown on the preview.
        /// </summary>
        public void RequestShow(
            string codeName,
            string displayName = null,
            CardBaseStats stats = null,
            string description = null,
            string cardType = null)
        {
            if (this.reviewMovement == null) return;
            this.SetFallbackName(displayName ?? codeName);
            this.SetFallbackStats(stats);
            this.SetFallbackDescription(description);
            this.SetCardType(cardType);
            // Apply fallbacks to the currently loaded card immediately.
            // LoadCardByCodeName is async — if the address lookup fails the card
            // data never updates, so fallback text would never render without this call.
            this.ApplyTextures();
            if (!this.reviewMovement.IsShown)
            {
                this.LoadCardByCodeName(codeName);
                this.reviewMovement.Show();
                return;
            }
            this.reviewMovement.Hide().OnComplete(() =>
            {
                this.LoadCardByCodeName(codeName);
                this.reviewMovement.Show();
            });
        }
    }
}
