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

        [Header("Full Detail")]
        [SerializeField] private Transform fullDetailPoint;

        [Header("Alpha — Single Points")]
        [SerializeField] private Transform alphaTheSource;
        [SerializeField] private Transform alphaTheVoid;
        [SerializeField] private Transform alphaSpawnPoint;

        [Header("Omega — Single Points")]
        [SerializeField] private Transform omegaTheSource;
        [SerializeField] private Transform omegaTheVoid;
        [SerializeField] private Transform omegaSpawnPoint;

        [Header("Lamp Positions")]
        [SerializeField] private Transform alphaLampPosition;
        [SerializeField] private Transform omegaLampPosition;
        [SerializeField] private Transform cardDeployPosition;

        [SerializeField] private Transform[] alphaHand = new Transform[LineSize];

        [SerializeField] private Transform[] omegaHand = new Transform[LineSize];

        [SerializeField] private CardHolderCtrl[] alphaFrontLine = new CardHolderCtrl[LineSize];

        [SerializeField] private CardHolderCtrl[] alphaBackLine = new CardHolderCtrl[LineSize];

        [SerializeField] private CardHolderCtrl[] omegaFrontLine = new CardHolderCtrl[LineSize];

        [SerializeField] private CardHolderCtrl[] omegaBackLine = new CardHolderCtrl[LineSize];

        [SerializeField] private List<Card3DCtrl> testCards = new();

        // ─── Public API ───────────────────────────────────────────────────────────

        public Transform FullDetailPoint   => this.fullDetailPoint;
        public Transform AlphaTheSource   => this.alphaTheSource;
        public Transform AlphaTheVoid     => this.alphaTheVoid;
        public Transform OmegaTheSource   => this.omegaTheSource;
        public Transform OmegaTheVoid     => this.omegaTheVoid;
        public Transform AlphaSpawnPoint    => this.alphaSpawnPoint;
        public Transform OmegaSpawnPoint    => this.omegaSpawnPoint;
        public Transform CardDeployPosition  => this.cardDeployPosition;
        public Transform AlphaLampPosition  => this.alphaLampPosition;
        public Transform OmegaLampPosition  => this.omegaLampPosition;
        public Transform[] AlphaHand      => this.alphaHand;
        public Transform[] OmegaHand      => this.omegaHand;
        public CardHolderCtrl[] AlphaFrontLine => this.alphaFrontLine;
        public CardHolderCtrl[] AlphaBackLine  => this.alphaBackLine;
        public CardHolderCtrl[] OmegaFrontLine => this.omegaFrontLine;
        public CardHolderCtrl[] OmegaBackLine  => this.omegaBackLine;
        public List<Card3DCtrl> TestCards  => this.testCards;

        public void ToggleTestCards()
        {
            foreach (Card3DCtrl card in this.testCards)
                card.gameObject.SetActive(!card.gameObject.activeSelf);
        }

        public void ShowTestCards()
        {
            foreach (Card3DCtrl card in this.testCards)
                card.gameObject.SetActive(true);
        }

        public void HideTestCards()
        {
            foreach (Card3DCtrl card in this.testCards)
                card.gameObject.SetActive(false);
        }

        public Transform GetAlphaHand(int index)      => this.GetSlot(this.alphaHand, index);
        public Transform GetOmegaHand(int index)      => this.GetSlot(this.omegaHand, index);
        public CardHolderCtrl GetAlphaFrontLine(int index) => this.GetHolder(this.alphaFrontLine, index);
        public CardHolderCtrl GetAlphaBackLine(int index)  => this.GetHolder(this.alphaBackLine, index);
        public CardHolderCtrl GetOmegaFrontLine(int index) => this.GetHolder(this.omegaFrontLine, index);
        public CardHolderCtrl GetOmegaBackLine(int index)  => this.GetHolder(this.omegaBackLine, index);

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadFullDetailPoint();
            this.LoadAlphaTheSource();
            this.LoadAlphaTheVoid();
            this.LoadOmegaTheSource();
            this.LoadOmegaTheVoid();
            this.LoadAlphaSpawnPoint();
            this.LoadOmegaSpawnPoint();
            this.LoadCardDeployPosition();
            this.LoadAlphaLampPosition();
            this.LoadOmegaLampPosition();
            this.LoadAlphaHand();
            this.LoadOmegaHand();
            this.LoadAlphaFrontLine();
            this.LoadAlphaBackLine();
            this.LoadOmegaFrontLine();
            this.LoadOmegaBackLine();
            this.LoadTestCards();
        }

        protected virtual void LoadFullDetailPoint()
        {
            if (this.fullDetailPoint != null) return;
            this.fullDetailPoint = this.FindOrCreateChild("FullDetailPoint");
            Debug.LogWarning(this.transform.name + ": LoadFullDetailPoint", this.gameObject);
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

        protected virtual void LoadAlphaSpawnPoint()
        {
            if (this.alphaSpawnPoint != null) return;
            this.alphaSpawnPoint = this.FindOrCreateChild("AlphaSpawnPoint");
            Debug.LogWarning(this.transform.name + ": LoadAlphaSpawnPoint", this.gameObject);
        }

        protected virtual void LoadOmegaSpawnPoint()
        {
            if (this.omegaSpawnPoint != null) return;
            this.omegaSpawnPoint = this.FindOrCreateChild("OmegaSpawnPoint");
            Debug.LogWarning(this.transform.name + ": LoadOmegaSpawnPoint", this.gameObject);
        }

        protected virtual void LoadCardDeployPosition()
        {
            if (this.cardDeployPosition != null) return;
            this.cardDeployPosition = this.FindOrCreateChild("CardDeployPosition");
            Debug.LogWarning(this.transform.name + ": LoadCardDeployPosition", this.gameObject);
        }

        protected virtual void LoadAlphaLampPosition()
        {
            if (this.alphaLampPosition != null) return;
            this.alphaLampPosition = this.FindOrCreateChild("AlphaLampPosition");
            Debug.LogWarning(this.transform.name + ": LoadAlphaLampPosition", this.gameObject);
        }

        protected virtual void LoadOmegaLampPosition()
        {
            if (this.omegaLampPosition != null) return;
            this.omegaLampPosition = this.FindOrCreateChild("OmegaLampPosition");
            Debug.LogWarning(this.transform.name + ": LoadOmegaLampPosition", this.gameObject);
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
            if (this.IsHoldersFilled(this.alphaFrontLine)) return;
            this.LoadHoldersFromHierarchy(this.alphaFrontLine, "AlphaFrontLine");
            Debug.LogWarning(this.transform.name + ": LoadAlphaFrontLine", this.gameObject);
        }

        protected virtual void LoadAlphaBackLine()
        {
            this.LoadHoldersFromHierarchy(this.alphaBackLine, "AlphaBackLine");
            Debug.LogWarning(this.transform.name + ": LoadAlphaBackLine", this.gameObject);
        }

        protected virtual void LoadOmegaFrontLine()
        {
            if (this.IsHoldersFilled(this.omegaFrontLine)) return;
            this.LoadHoldersFromHierarchy(this.omegaFrontLine, "OmegaFrontLine");
            Debug.LogWarning(this.transform.name + ": LoadOmegaFrontLine", this.gameObject);
        }

        protected virtual void LoadOmegaBackLine()
        {
            if (this.IsHoldersFilled(this.omegaBackLine)) return;
            this.LoadHoldersFromHierarchy(this.omegaBackLine, "OmegaBackLine");
            Debug.LogWarning(this.transform.name + ": LoadOmegaBackLine", this.gameObject);
        }

        private void LoadHoldersFromHierarchy(CardHolderCtrl[] holders, string lineName)
        {
            Transform line = this.transform.Find(lineName);
            if (line == null)
            {
                Debug.LogWarning(this.transform.name + ": " + lineName + " not found", this.gameObject);
                return;
            }

            for (int i = 0; i < holders.Length; i++) holders[i] = null;
            int slotCount = Mathf.Min(holders.Length, line.childCount);
            for (int i = 0; i < slotCount; i++)
                holders[i] = line.GetChild(i).GetComponent<CardHolderCtrl>();
        }

        private bool IsHoldersFilled(CardHolderCtrl[] holders)
        {
            foreach (CardHolderCtrl h in holders)
            {
                if (h == null) return false;
            }
            return true;
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

        private CardHolderCtrl GetHolder(CardHolderCtrl[] holders, int index)
        {
            if (index < 0 || index >= holders.Length) return null;
            return holders[index];
        }
    }
}
