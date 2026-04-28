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
        [SerializeField] private CardReviewMovement movement;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCardReviewMovement();
        }

        protected virtual void LoadCardReviewMovement()
        {
            if (this.movement != null) return;
            this.movement = this.GetComponent<CardReviewMovement>();
            Debug.LogWarning(transform.name + "LoadCardReviewMovement", gameObject);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Flies the card upward with spin animation.</summary>
        public void Show() => this.movement.Show();

        /// <summary>Returns the card to its origin position with spin animation.</summary>
        public void Hide() => this.movement.Hide();

        /// <summary>
        /// Shows the card with the given <paramref name="codeName"/>.
        /// Loads the CardData via <see cref="CardLoader.ShowByCodeName"/> first, then
        /// triggers the movement animation. If the card is already shown, hides it
        /// first and waits for the hide animation to finish before showing again.
        /// </summary>
        public void RequestShow(string codeName)
        {
            if (this.movement == null) return;
            if (!this.movement.IsShown)
            {
                this.LoadCardByCodeName(codeName);
                this.movement.Show();
                return;
            }
            this.movement.Hide().OnComplete(() =>
            {
                this.LoadCardByCodeName(codeName);
                this.movement.Show();
            });
        }
    }
}
