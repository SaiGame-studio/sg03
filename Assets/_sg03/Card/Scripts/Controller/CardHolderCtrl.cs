using System;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/Card/Card Holder Ctrl")]
    public class CardHolderCtrl : SaiBehaviour
    {
        // ─── Static holder events ─────────────────────────────────────────────────

        public static event Action<CardHolderCtrl> HoverEntered;
        public static event Action<CardHolderCtrl> HoverExited;
        public static event Action<CardHolderCtrl> HolderSelected;

        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3DCtrl card;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3DCtrl();
        }

        protected virtual void LoadCard3DCtrl()
        {
            if (this.card != null) return;
            this.card = this.GetComponentInChildren<Card3DCtrl>();
            Debug.LogWarning(this.transform.name + ": LoadCard3DCtrl", this.gameObject);
        }

        // ─── Notify methods ───────────────────────────────────────────────────────

        public void NotifyHoverEntered() => HoverEntered?.Invoke(this);
        public void NotifyHoverExited()  => HoverExited?.Invoke(this);
        public void NotifySelected()     => HolderSelected?.Invoke(this);

        // ─── Public API ───────────────────────────────────────────────────────────

        public Card3DCtrl Card => this.card;
    }
}

