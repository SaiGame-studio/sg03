using System.Collections;
using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/Desk/Card Spawning")]
    public class CardSpawning : SaiBehaviour
    {
        [SerializeField] private CardPool               cardPool;
        [SerializeField] private DeskPositionCtrl         deskPosition;
        [SerializeField] private BattleState              battleState;
        [SerializeField] private BattleCardDefinitions    battleCardDefinitions;
        [SerializeField] private string prefabName = "Card3D";
        [SerializeField] private int spawnPerFrame = 1;
        [SerializeField] private float spawnInterval = 0.05f;

        private Coroutine alphaSourceRoutine;
        private Coroutine omegaSourceRoutine;
        private readonly Dictionary<string, Card3DCtrl>    sourceCardRegistry  = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<string, Card3DCtrl>    handCardRegistry    = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<Transform, Card3DCtrl> slotOccupancy       = new Dictionary<Transform, Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  omegaSourceCardQueue = new Queue<Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  omegaHandCardQueue   = new Queue<Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  alphaSourceCardQueue = new Queue<Card3DCtrl>();
        private int omegaSourceSpawnedCount = 0;
        private int alphaSourceSpawnedCount = 0;

        // ─── Public API ───────────────────────────────────────────────────────────

        public float ActionMoveDuration   { get; set; } = 1f;
        public float ActionRotateDuration { get; set; } = 0.4f;

        public Coroutine SpawnAlphaSourceCards(int count)
        {
            if (this.alphaSourceRoutine != null) this.StopCoroutine(this.alphaSourceRoutine);
            this.alphaSourceRoutine = this.StartCoroutine(this.SpawnAlphaSourceCardsRoutine(count));
            return this.alphaSourceRoutine;
        }

        public Coroutine SpawnOmegaSourceCards(int count)
        {
            if (this.omegaSourceRoutine != null) this.StopCoroutine(this.omegaSourceRoutine);
            this.omegaSourceRoutine = this.StartCoroutine(this.SpawnOmegaSourceCardsRoutine(count));
            return this.omegaSourceRoutine;
        }

        public Card3DCtrl SetAlphaSourceCardData(string inventoryItemId)
        {
            if (this.alphaSourceCardQueue.Count == 0) return null;
            Card3DCtrl card = this.alphaSourceCardQueue.Dequeue();
            card.SetOwner(Owner.alpha);
            card.SetInventoryItemId(inventoryItemId);
            BattleCardSlot slot = this.FindAlphaSlotById(inventoryItemId);
            if (slot == null) return card;
            string code = slot.item_definition_code_name;
            card.SetCodeName(code);
            card.SetFallbackName(slot.item_definition_name);
            card.LoadCardByCodeName(code);
            card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
            return card;
        }

        public void CommitAlphaSourceToHand(Card3DCtrl card, string inventoryItemId, int slotIndex)
        {
            if (card == null) return;
            Transform target = this.ResolveHandTarget(slotIndex, this.deskPosition.AlphaHand);
            if (this.IsSlotOccupied(target)) return;
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            this.handCardRegistry[inventoryItemId] = card;
        }

        private BattleCardSlot FindAlphaSlotById(string inventoryItemId)
        {
            return this.FindSlotById(this.battleState?.AlphaHand, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaFrontLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaBackLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaTheVoid, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.AlphaTheSource, inventoryItemId);
        }

        private BattleCardSlot FindOmegaSlotById(string inventoryItemId)
        {
            return this.FindSlotById(this.battleState?.OmegaHand, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.OmegaFrontLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.OmegaBackLine, inventoryItemId)
                ?? this.FindSlotById(this.battleState?.OmegaTheVoid, inventoryItemId);
        }

        public Card3DCtrl MoveOmegaSourceToHand(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) return null;
            Transform target = this.ResolveHandTarget(slotIndex, this.deskPosition.OmegaHand);
            if (this.IsSlotOccupied(target)) return null;
            Card3DCtrl card = this.DequeueOmegaSourceCard(prefab);
            if (card == null) return null;
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            this.omegaHandCardQueue.Enqueue(card);
            return card;
        }

        public Card3DCtrl MoveAlphaHandToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveAlphaHandToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaFrontLine);

        public Card3DCtrl MoveAlphaHandToBackLine(string inventoryItemId, int slotIndex)
            => this.MoveAlphaHandToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaBackLine);

        private Card3DCtrl MoveAlphaHandToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (!this.handCardRegistry.TryGetValue(inventoryItemId, out Card3DCtrl card)) return null;
            if (slotIndex < 0 || slotIndex >= holders.Length) return null;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return null;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return null;
            this.handCardRegistry.Remove(inventoryItemId);
            this.RemoveFromSlotOccupancy(card);
            BattleCardSlot slot = this.FindAlphaSlotById(inventoryItemId);
            if (slot != null) card.SetExpose(slot.expose);
            System.Action faceCallback = slot != null ? () => this.ApplyFaceState(card, slot) : null;
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.SetCardHolder(holder, faceCallback);
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
            return card;
        }

        public Card3DCtrl MoveOmegaHandToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveOmegaHandToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaFrontLine);

        public Card3DCtrl MoveOmegaHandToBackLine(string inventoryItemId, int slotIndex)
            => this.MoveOmegaHandToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaBackLine);

        private Card3DCtrl MoveOmegaHandToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (this.omegaHandCardQueue.Count == 0) return null;
            if (slotIndex < 0 || slotIndex >= holders.Length) return null;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return null;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return null;
            Card3DCtrl card = this.omegaHandCardQueue.Dequeue();
            this.RemoveFromSlotOccupancy(card);
            BattleCardSlot slot = this.FindOmegaSlotById(inventoryItemId);
            if (slot != null)
            {
                string code = slot.item_definition_code_name;
                card.SetOwner(Owner.omega);
                card.SetInventoryItemId(inventoryItemId);
                card.SetCodeName(code);
                card.SetFallbackName(slot.item_definition_name);
                card.LoadCardByCodeName(code);
                card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
                card.SetExpose(slot.expose);
            }
            System.Action faceCallback = slot != null ? () => this.ApplyFaceState(card, slot) : null;
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveToUnknow(holder, faceCallback);
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
            return card;
        }

        private BattleCardSlot FindSlotById(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id == inventoryItemId) return slot;
            }
            return null;
        }

        public void ClearSourceRegistry()
        {
            this.sourceCardRegistry.Clear();
            this.handCardRegistry.Clear();
            this.omegaHandCardQueue.Clear();
            this.slotOccupancy.Clear();
            this.omegaSourceCardQueue.Clear();
            this.alphaSourceCardQueue.Clear();
            this.omegaSourceSpawnedCount = 0;
            this.alphaSourceSpawnedCount = 0;
        }

        public Card3DCtrl FindCardById(string inventoryItemId)
        {
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            if (this.handCardRegistry.TryGetValue(inventoryItemId, out Card3DCtrl handCard)) return handCard;
            foreach (Card3DCtrl card in this.slotOccupancy.Values)
            {
                if (card != null && card.InventoryItemId == inventoryItemId) return card;
            }
            return null;
        }

        public Card3DCtrl MoveAlphaCardToVoid(string inventoryItemId)
            => this.MoveCardToVoid(inventoryItemId, this.deskPosition.AlphaTheVoid);

        public Card3DCtrl MoveOmegaCardToVoid(string inventoryItemId)
            => this.MoveCardToVoid(inventoryItemId, this.deskPosition.OmegaTheVoid);

        private Card3DCtrl MoveCardToVoid(string inventoryItemId, Transform voidPoint)
        {
            Card3DCtrl card = this.FindCardById(inventoryItemId);
            if (card == null) return null;
            this.handCardRegistry.Remove(inventoryItemId);
            this.RemoveFromSlotOccupancy(card);
            card.SetMoveDuration(this.ActionMoveDuration);
            card.SetRotateDuration(this.ActionRotateDuration);
            card.MoveAndRotate(voidPoint, Location.in_void);
            return card;
        }

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCardPool();
            this.LoadDeskPosition();
            this.LoadBattleState();
            this.LoadBattleCardDefinitions();
        }

        protected virtual void LoadCardPool()
        {
            if (this.cardPool != null) return;
            this.cardPool = GameObject.FindAnyObjectByType<CardPool>();
            Debug.LogWarning(this.transform.name + ": LoadCardPool", this.gameObject);
        }

        protected virtual void LoadDeskPosition()
        {
            if (this.deskPosition != null) return;
            this.deskPosition = GameObject.FindAnyObjectByType<DeskPositionCtrl>();
            Debug.LogWarning(this.transform.name + ": LoadDeskPosition", this.gameObject);
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleState = ctrl.BattleState;
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        protected virtual void LoadBattleCardDefinitions()
        {
            if (this.battleCardDefinitions != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleCardDefinitions = ctrl.BattleCardDefinitions;
            Debug.LogWarning(this.transform.name + ": LoadBattleCardDefinitions", this.gameObject);
        }

        private IEnumerator WaitForDefinitions()
        {
            if (this.battleCardDefinitions == null) yield break;
            if (this.battleCardDefinitions.IsLoaded) yield break;
            Debug.Log("<color=#FFD700>[CardSpawning] Waiting for BattleCardDefinitions to load...</color>");
            bool loaded = false;
            BattleCardDefinitions.OnDefinitionsLoaded += SetLoaded;
            yield return new UnityEngine.WaitUntil(() => loaded);
            BattleCardDefinitions.OnDefinitionsLoaded -= SetLoaded;
            void SetLoaded() => loaded = true;
        }

        private bool IsSlotOccupied(Transform target)
        {
            if (!this.slotOccupancy.TryGetValue(target, out Card3DCtrl existing)) return false;
            if (!existing.gameObject.activeInHierarchy)
            {
                this.slotOccupancy.Remove(target);
                return false;
            }
            return true;
        }

        private Transform ResolveHandTarget(int slotIndex, Transform[] handTargets)
        {
            return slotIndex < handTargets.Length ? handTargets[slotIndex] : this.deskPosition.AlphaSpawnPoint;
        }

        private IEnumerator SpawnAlphaSourceCardsRoutine(int count)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            int spawnedThisFrame = 0;
            while (this.alphaSourceSpawnedCount < count)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card == null) break;
                card.SetOwner(Owner.alpha);
                card.MoveAndRotate(this.deskPosition.AlphaTheSource, Location.in_source);
                this.alphaSourceCardQueue.Enqueue(card);
                this.alphaSourceSpawnedCount++;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
            this.alphaSourceRoutine = null;
        }

        private IEnumerator SpawnOmegaSourceCardsRoutine(int count)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            int spawnedThisFrame = 0;
            while (this.omegaSourceSpawnedCount < count)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card == null) break;
                card.SetOwner(Owner.omega);
                card.MoveAndRotate(this.deskPosition.OmegaTheSource, Location.in_source);
                this.omegaSourceCardQueue.Enqueue(card);
                this.omegaSourceSpawnedCount++;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
            this.omegaSourceRoutine = null;
        }

        private Card3DCtrl DequeueOmegaSourceCard(Card3DCtrl prefab)
        {
            if (this.omegaSourceCardQueue.Count > 0) return this.omegaSourceCardQueue.Dequeue();
            return this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
        }

        private void RemoveFromSlotOccupancy(Card3DCtrl card)
        {
            Transform keyToRemove = null;
            foreach (KeyValuePair<Transform, Card3DCtrl> kvp in this.slotOccupancy)
            {
                if (kvp.Value != card) continue;
                keyToRemove = kvp.Key;
                break;
            }
            if (keyToRemove != null) this.slotOccupancy.Remove(keyToRemove);
        }

        private void ApplyFaceState(Card3DCtrl card, BattleCardSlot slot)
        {
            if (!slot.face_up && !slot.expose) return;
            this.StartCoroutine(this.WaitForFlipThenFaceUp(card));
        }

        private IEnumerator WaitForFlipThenFaceUp(Card3DCtrl card)
        {
            yield return new UnityEngine.WaitUntil(() => !card.IsFlipping);
            card.FaceUp();
        }

        private Card3DCtrl ResolvePrefab()
        {
            if (this.cardPool == null) return null;
            return this.cardPool.PoolPrefabs.GetByName(this.prefabName);
        }

        private YieldInstruction WaitAfterSpawn()
        {
            if (this.spawnInterval > 0f) return new WaitForSeconds(this.spawnInterval);
            return null;
        }

        private Card3DCtrl SpawnCardAt(Card3DCtrl prefab, Transform spawnPoint)
        {
            if (spawnPoint == null) return null;
            Card3DCtrl card = this.cardPool.Spawn(prefab, spawnPoint.position);
            card.transform.rotation = spawnPoint.rotation;
            card.gameObject.SetActive(true);
            return card;
        }

    }
}
