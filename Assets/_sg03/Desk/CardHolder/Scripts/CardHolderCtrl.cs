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

        // ─── Held card ────────────────────────────────────────────────────────────

        [Header("State")]
        [SerializeField] private Card3DCtrl heldCard;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void ResetValue()
        {
            this.ParseName();
            this.SetScale();
        }

        private void SetScale()
        {
            this.transform.localScale = new Vector3(7.5f, 0.5f, 10.5f);
        }

        // ─── Name parsing ─────────────────────────────────────────────────────────

        private void ParseName()
        {
            string[] parts = this.name.Split('_');
            if (parts.Length < 3) return;
            this.ParseOwner(parts[0]);
            this.ParseLink(parts[1]);
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

        public int GetIndexFromObjectName()
        {
            string objectName = this.gameObject.name;
            if (string.IsNullOrEmpty(objectName)) return -1;

            int lastSeparatorIndex = objectName.LastIndexOf('_');
            if (lastSeparatorIndex < 0 || lastSeparatorIndex == objectName.Length - 1) return -1;

            string indexPart = objectName.Substring(lastSeparatorIndex + 1);
            return int.TryParse(indexPart, out int result) ? result : -1;
        }

        // ─── Notify methods ───────────────────────────────────────────────────────

        public void NotifyHoverEntered() => HoverEntered?.Invoke(this);
        public void NotifyHoverExited()  => HoverExited?.Invoke(this);
        public void NotifySelected()     => HolderSelected?.Invoke(this);

        // ─── Public API ───────────────────────────────────────────────────────────

        public Owner HolderOwner    => this.owner;
        public Link  HolderLink     => this.link;
        public Location HolderLocation => this.link == Link.front ? Location.in_front : Location.in_back;
        public int   Index          => this.GetIndexFromObjectName();
        public Card3DCtrl HeldCard => this.heldCard;

        /// <summary>Links a card to this holder. Pass null to clear the slot.</summary>
        public void SetCard(Card3DCtrl card) => this.heldCard = card;
    }
}

