using System;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/CardHolder/Card Holder Ctrl")]
    public class CardHolderCtrl : SaiBehaviour
    {
        // ─── Static holder events ─────────────────────────────────────────────────

        public static event Action<CardHolderCtrl> HoverEntered;
        public static event Action<CardHolderCtrl> HoverExited;
        public static event Action<CardHolderCtrl> HolderSelected;

        // ─── Identity ─────────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private Owner owner;
        [SerializeField] private Link  link;
        [SerializeField] private int   index;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void ResetValue()
        {
            this.ParseName();
        }

        // ─── Name parsing ─────────────────────────────────────────────────────────

        private void ParseName()
        {
            string[] parts = this.name.Split('_');
            if (parts.Length < 3) return;
            this.ParseOwner(parts[0]);
            this.ParseLink(parts[1]);
            this.ParseIndex(parts[2]);
        }

        private void ParseOwner(string value)
        {
            if (Enum.TryParse(value, out Owner result))
                this.owner = result;
        }

        private void ParseLink(string value)
        {
            if (Enum.TryParse(value, out Link result))
                this.link = result;
        }

        private void ParseIndex(string value)
        {
            if (int.TryParse(value, out int result))
                this.index = result;
        }

        // ─── Notify methods ───────────────────────────────────────────────────────

        public void NotifyHoverEntered() => HoverEntered?.Invoke(this);
        public void NotifyHoverExited()  => HoverExited?.Invoke(this);
        public void NotifySelected()     => HolderSelected?.Invoke(this);

        // ─── Public API ───────────────────────────────────────────────────────────

        public Owner HolderOwner => this.owner;
        public Link  HolderLink  => this.link;
        public int   Index       => this.index;
    }
}

