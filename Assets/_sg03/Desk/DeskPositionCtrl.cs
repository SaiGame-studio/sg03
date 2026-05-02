using SaiGame.Services;
using System.Collections.Generic;
using UnityEngine;
using SG03;

namespace SG03
{
    [AddComponentMenu("SG03/Desk/Desk Position Ctrl")]
    public class DeskPositionCtrl : SaiBehaviour
    {
        private const int LineSize = 5;

        [Header("Alpha — Single Points")]
        [SerializeField] private Transform alphaTheSource;
        [SerializeField] private Transform alphaTheVoid;

        [Header("Omega — Single Points")]
        [SerializeField] private Transform omegaTheSource;
        [SerializeField] private Transform omegaTheVoid;

        [Header("Alpha Hand (5)")]
        [SerializeField] private Transform[] alphaHand = new Transform[LineSize];

        [Header("Omega Hand (5)")]
        [SerializeField] private Transform[] omegaHand = new Transform[LineSize];

        [Header("Alpha Front Line (5)")]
        [SerializeField] private Transform[] alphaFrontLine = new Transform[LineSize];

        [Header("Alpha Back Line (5)")]
        [SerializeField] private Transform[] alphaBackLine = new Transform[LineSize];

        [Header("Omega Front Line (5)")]
        [SerializeField] private Transform[] omegaFrontLine = new Transform[LineSize];

        [Header("Omega Back Line (5)")]
        [SerializeField] private Transform[] omegaBackLine = new Transform[LineSize];

        [Header("Test Cards")]
        [SerializeField] private List<Card3DCtrl> testCards = new();

        // ─── Public API ───────────────────────────────────────────────────────────

        public Transform AlphaTheSource   => this.alphaTheSource;
        public Transform AlphaTheVoid     => this.alphaTheVoid;
        public Transform OmegaTheSource   => this.omegaTheSource;
        public Transform OmegaTheVoid     => this.omegaTheVoid;
        public Transform[] AlphaHand      => this.alphaHand;
        public Transform[] OmegaHand      => this.omegaHand;
        public Transform[] AlphaFrontLine => this.alphaFrontLine;
        public Transform[] AlphaBackLine  => this.alphaBackLine;
        public Transform[] OmegaFrontLine => this.omegaFrontLine;
        public Transform[] OmegaBackLine  => this.omegaBackLine;
        public List<Card3DCtrl> TestCards  => this.testCards;

        public void ToggleTestCards()
        {
            foreach (Card3DCtrl card in this.testCards)
                card.gameObject.SetActive(!card.gameObject.activeSelf);
        }

        public Transform GetAlphaHand(int index)      => this.GetSlot(this.alphaHand, index);
        public Transform GetOmegaHand(int index)      => this.GetSlot(this.omegaHand, index);
        public Transform GetAlphaFrontLine(int index) => this.GetSlot(this.alphaFrontLine, index);
        public Transform GetAlphaBackLine(int index)  => this.GetSlot(this.alphaBackLine, index);
        public Transform GetOmegaFrontLine(int index) => this.GetSlot(this.omegaFrontLine, index);
        public Transform GetOmegaBackLine(int index)  => this.GetSlot(this.omegaBackLine, index);

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadAlphaTheSource();
            this.LoadAlphaTheVoid();
            this.LoadOmegaTheSource();
            this.LoadOmegaTheVoid();
            this.LoadAlphaHand();
            this.LoadOmegaHand();
            this.LoadAlphaFrontLine();
            this.LoadAlphaBackLine();
            this.LoadOmegaFrontLine();
            this.LoadOmegaBackLine();
            this.LoadTestCards();
        }

        protected virtual void LoadAlphaTheSource()
        {
            if (this.alphaTheSource != null) return;
            this.alphaTheSource = this.FindOrCreateChild("AlphaTheSource");
            Debug.LogWarning(this.transform.name + ": LoadAlphaTheSource", this.gameObject);
        }

        protected virtual void LoadAlphaTheVoid()
        {
            if (this.alphaTheVoid != null) return;
            this.alphaTheVoid = this.FindOrCreateChild("AlphaTheVoid");
            Debug.LogWarning(this.transform.name + ": LoadAlphaTheVoid", this.gameObject);
        }

        protected virtual void LoadOmegaTheSource()
        {
            if (this.omegaTheSource != null) return;
            this.omegaTheSource = this.FindOrCreateChild("OmegaTheSource");
            Debug.LogWarning(this.transform.name + ": LoadOmegaTheSource", this.gameObject);
        }

        protected virtual void LoadOmegaTheVoid()
        {
            if (this.omegaTheVoid != null) return;
            this.omegaTheVoid = this.FindOrCreateChild("OmegaTheVoid");
            Debug.LogWarning(this.transform.name + ": LoadOmegaTheVoid", this.gameObject);
        }

        protected virtual void LoadAlphaHand()
        {
            if (this.IsSlotsFilled(this.alphaHand)) return;
            this.LoadOrCreateSlots(this.alphaHand, "AlphaHand");
            Debug.LogWarning(this.transform.name + ": LoadAlphaHand", this.gameObject);
        }

        protected virtual void LoadOmegaHand()
        {
            if (this.IsSlotsFilled(this.omegaHand)) return;
            this.LoadOrCreateSlots(this.omegaHand, "OmegaHand");
            Debug.LogWarning(this.transform.name + ": LoadOmegaHand", this.gameObject);
        }

        protected virtual void LoadAlphaFrontLine()
        {
            if (this.IsSlotsFilled(this.alphaFrontLine)) return;
            this.LoadOrCreateSlots(this.alphaFrontLine, "AlphaFrontLine");
            Debug.LogWarning(this.transform.name + ": LoadAlphaFrontLine", this.gameObject);
        }

        protected virtual void LoadAlphaBackLine()
        {
            if (this.IsSlotsFilled(this.alphaBackLine)) return;
            this.LoadOrCreateSlots(this.alphaBackLine, "AlphaBackLine");
            Debug.LogWarning(this.transform.name + ": LoadAlphaBackLine", this.gameObject);
        }

        protected virtual void LoadOmegaFrontLine()
        {
            if (this.IsSlotsFilled(this.omegaFrontLine)) return;
            this.LoadOrCreateSlots(this.omegaFrontLine, "OmegaFrontLine");
            Debug.LogWarning(this.transform.name + ": LoadOmegaFrontLine", this.gameObject);
        }

        protected virtual void LoadOmegaBackLine()
        {
            if (this.IsSlotsFilled(this.omegaBackLine)) return;
            this.LoadOrCreateSlots(this.omegaBackLine, "OmegaBackLine");
            Debug.LogWarning(this.transform.name + ": LoadOmegaBackLine", this.gameObject);
        }

        protected virtual void LoadTestCards()
        {
            if (this.testCards.Count > 0) return;
            this.testCards.AddRange(this.GetComponentsInChildren<Card3DCtrl>(true));
            Debug.LogWarning(this.transform.name + ": LoadTestCards", this.gameObject);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private Transform FindOrCreateChild(string childName)
        {
            Transform found = this.transform.Find(childName);
            if (found != null) return found;
            GameObject go = new GameObject(childName);
            go.transform.SetParent(this.transform, false);
            return go.transform;
        }

        private bool IsSlotsFilled(Transform[] slots)
        {
            foreach (Transform slot in slots)
            {
                if (slot == null) return false;
            }
            return true;
        }

        private void LoadOrCreateSlots(Transform[] slots, string parentName)
        {
            Transform parent = this.FindOrCreateChild(parentName);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) continue;
                Transform existing = i < parent.childCount ? parent.GetChild(i) : null;
                if (existing != null)
                {
                    slots[i] = existing;
                    continue;
                }
                GameObject go = new GameObject(i.ToString());
                go.transform.SetParent(parent, false);
                slots[i] = go.transform;
            }
        }

        private Transform GetSlot(Transform[] slots, int index)
        {
            if (index < 0 || index >= slots.Length) return null;
            return slots[index];
        }
    }
}
