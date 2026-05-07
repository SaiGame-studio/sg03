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

        private Coroutine spawnRoutine;
        private Coroutine alphaSourceRoutine;
        private Coroutine omegaSourceRoutine;
        private readonly Dictionary<string, Card3DCtrl>    sourceCardRegistry  = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<string, Card3DCtrl>    handCardRegistry    = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<Transform, Card3DCtrl> slotOccupancy       = new Dictionary<Transform, Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  omegaSourceCardQueue = new Queue<Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  omegaHandCardQueue   = new Queue<Card3DCtrl>();
        private readonly Queue<Card3DCtrl>                  alphaSourceCardQueue = new Queue<Card3DCtrl>();
        private BattleCardSlot[] previousOmegaHand;
        private int omegaSourceSpawnedCount = 0;
        private int omegaVoidSpawnedCount   = 0;
        private int alphaSourceSpawnedCount = 0;

        // ─── Public API ───────────────────────────────────────────────────────────

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

        public void MoveOmegaSourceToHand(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) return;
            Transform target = this.ResolveHandTarget(slotIndex, this.deskPosition.OmegaHand);
            if (this.IsSlotOccupied(target)) return;
            Card3DCtrl card = this.DequeueOmegaSourceCard(prefab);
            if (card == null) return;
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            this.omegaHandCardQueue.Enqueue(card);
        }

        public void MoveAlphaHandToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveAlphaHandToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaFrontLine);

        public void MoveAlphaHandToBackLine(string inventoryItemId, int slotIndex)
            => this.MoveAlphaHandToLine(inventoryItemId, slotIndex, this.deskPosition.AlphaBackLine);

        private void MoveAlphaHandToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (!this.handCardRegistry.TryGetValue(inventoryItemId, out Card3DCtrl card)) return;
            if (slotIndex < 0 || slotIndex >= holders.Length) return;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return;
            this.handCardRegistry.Remove(inventoryItemId);
            this.RemoveFromSlotOccupancy(card);
            BattleCardSlot slot = this.FindAlphaSlotById(inventoryItemId);
            if (slot != null) card.SetExpose(slot.expose);
            System.Action faceCallback = slot != null ? () => this.ApplyFaceState(card, slot) : null;
            card.SetCardHolder(holder, faceCallback);
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
        }

        public void MoveOmegaHandToFrontLine(string inventoryItemId, int slotIndex)
            => this.MoveOmegaHandToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaFrontLine);

        public void MoveOmegaHandToBackLine(string inventoryItemId, int slotIndex)
            => this.MoveOmegaHandToLine(inventoryItemId, slotIndex, this.deskPosition.OmegaBackLine);

        private void MoveOmegaHandToLine(string inventoryItemId, int slotIndex, CardHolderCtrl[] holders)
        {
            if (this.omegaHandCardQueue.Count == 0) return;
            if (slotIndex < 0 || slotIndex >= holders.Length) return;
            CardHolderCtrl holder = holders[slotIndex];
            if (holder == null) return;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return;
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
            card.MoveToUnknow(holder, faceCallback);
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
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

        public void SpawnStatusDelta(BattleCardSlot[] newHand, BattleCardSlot[] newFrontLine, BattleCardSlot[] newBackLine,
            BattleCardSlot[] newOmegaFrontLine, BattleCardSlot[] newOmegaBackLine, BattleCardSlot[] previousOmegaHand)
        {
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(
                this.SpawnStatusDeltaRoutine(newHand, newFrontLine, newBackLine, newOmegaFrontLine, newOmegaBackLine, previousOmegaHand));
        }

        public void ClearSourceRegistry()
        {
            this.sourceCardRegistry.Clear();
            this.handCardRegistry.Clear();
            this.omegaHandCardQueue.Clear();
            this.slotOccupancy.Clear();
            this.omegaSourceCardQueue.Clear();
            this.alphaSourceCardQueue.Clear();
            this.previousOmegaHand = null;
            this.omegaSourceSpawnedCount = 0;
            this.omegaVoidSpawnedCount   = 0;
            this.alphaSourceSpawnedCount = 0;
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

        private IEnumerator SpawnStatusDeltaRoutine(BattleCardSlot[] newHand, BattleCardSlot[] newFrontLine, BattleCardSlot[] newBackLine,
            BattleCardSlot[] newOmegaFrontLine, BattleCardSlot[] newOmegaBackLine, BattleCardSlot[] previousOmegaHand)
        {
            this.previousOmegaHand = previousOmegaHand;
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            yield return this.SpawnOmegaSequentialRoutine(prefab, newOmegaFrontLine, newOmegaBackLine);
            BattleCardSlot[] allSlots = this.CombineSlots(newHand, newFrontLine, newBackLine);
            yield return this.DispatchSlotsByAction(prefab, allSlots);
            this.previousOmegaHand = null;
            this.spawnRoutine = null;
        }

        private BattleCardSlot[] CombineSlots(BattleCardSlot[] hand, BattleCardSlot[] frontLine, BattleCardSlot[] backLine)
        {
            int total = (hand?.Length ?? 0) + (frontLine?.Length ?? 0) + (backLine?.Length ?? 0);
            BattleCardSlot[] combined = new BattleCardSlot[total];
            int offset = 0;
            if (hand      != null) { System.Array.Copy(hand,      0, combined, offset, hand.Length);      offset += hand.Length; }
            if (frontLine != null) { System.Array.Copy(frontLine, 0, combined, offset, frontLine.Length); offset += frontLine.Length; }
            if (backLine  != null) { System.Array.Copy(backLine,  0, combined, offset, backLine.Length); }
            return combined;
        }

        private IEnumerator DispatchSlotsByAction(Card3DCtrl prefab, BattleCardSlot[] slots)
        {
            int spawnedThisFrame = 0;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                CardActionType action = slot.CardAction;
                if (action == CardActionType.unknown) continue;
                bool spawned = false;
                if (action == CardActionType.in_front_line || action == CardActionType.in_back_line)
                    spawned = this.DeploySlotToLine(prefab, slot);
                else if (action == CardActionType.draw_from_source_to_hand)
                    spawned = this.DrawSlotToHand(prefab, slot);
                if (!spawned) continue;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private bool DeploySlotToLine(Card3DCtrl prefab, BattleCardSlot slot)
        {
            Location location          = slot.CardAction == CardActionType.in_front_line ? Location.in_front : Location.in_back;
            CardHolderCtrl[] holders   = slot.CardAction == CardActionType.in_front_line ? this.deskPosition.AlphaFrontLine : this.deskPosition.AlphaBackLine;
            int idx = slot.slot_index;
            if (idx < 0 || idx >= holders.Length) return false;
            CardHolderCtrl holder = holders[idx];
            if (holder == null) return false;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return false;
            Card3DCtrl card = this.ResolveOrSpawnFromHand(prefab, slot);
            if (card == null) return false;
            card.SetOwner(Owner.alpha);
            string code = slot.item_definition_code_name;
            card.SetCodeName(code);
            card.SetInventoryItemId(slot.inventory_item_id);
            card.SetFallbackName(slot.item_definition_name);
            card.LoadCardByCodeName(code);
            card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
            card.SetExpose(slot.expose);
            card.SetCardHolder(holder, () => this.ApplyFaceState(card, slot));
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
            return true;
        }

        private bool DrawSlotToHand(Card3DCtrl prefab, BattleCardSlot slot)
        {
            int idx = slot.slot_index;
            Transform target = this.ResolveHandTarget(idx, this.deskPosition.AlphaHand);
            if (this.IsSlotOccupied(target)) return false;
            Card3DCtrl card = this.ResolveOrSpawnCard(prefab, slot);
            if (card == null) return false;
            card.SetOwner(Owner.alpha);
            string code = slot.item_definition_code_name;
            card.SetCodeName(code);
            card.SetInventoryItemId(slot.inventory_item_id);
            card.SetFallbackName(slot.item_definition_name);
            card.LoadCardByCodeName(code);
            card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            if (!string.IsNullOrEmpty(slot.inventory_item_id))
                this.handCardRegistry[slot.inventory_item_id] = card;
            return true;
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

        private IEnumerator SpawnOmegaSequentialRoutine(Card3DCtrl prefab, BattleCardSlot[] omegaFrontLine, BattleCardSlot[] omegaBackLine)
        {
            yield return this.SpawnOmegaVoidCardsRoutine(prefab);
            yield return this.SpawnOmegaHandSlotsRoutine(prefab);
            yield return this.SpawnOmegaLineCardsRoutine(prefab, omegaFrontLine, omegaBackLine);
        }

        private IEnumerator RunConcurrent(IEnumerator routineA, IEnumerator routineB)
        {
            int remaining = 2;
            this.StartCoroutine(this.WrapDone(routineA, () => remaining--));
            this.StartCoroutine(this.WrapDone(routineB, () => remaining--));
            yield return new UnityEngine.WaitUntil(() => remaining <= 0);
        }

        private IEnumerator WrapDone(IEnumerator routine, System.Action onComplete)
        {
            yield return this.StartCoroutine(routine);
            onComplete();
        }

        private IEnumerator SpawnOmegaVoidCardsRoutine(Card3DCtrl prefab)
        {
            int count = this.battleState?.OmegaTheVoidCount ?? 0;
            int spawnedThisFrame = 0;
            while (this.omegaVoidSpawnedCount < count)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card == null) break;
                card.SetOwner(Owner.omega);
                card.MoveAndRotate(this.deskPosition.OmegaTheVoid, Location.in_void);
                this.omegaVoidSpawnedCount++;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnOmegaHandSlotsRoutine(Card3DCtrl prefab)
        {
            BattleCardSlot[] slots = this.battleState?.OmegaHand;
            if (slots == null) yield break;
            int spawnedThisFrame = 0;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.CardAction != CardActionType.draw_from_source_to_hand) continue;
                bool spawned = this.DrawOmegaSlotToHand(prefab, slot);
                if (!spawned) continue;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnOmegaLineCardsRoutine(Card3DCtrl prefab, BattleCardSlot[] frontLine, BattleCardSlot[] backLine)
        {
            BattleCardSlot[] allSlots = this.CombineSlots(null, frontLine, backLine);
            int spawnedThisFrame = 0;
            foreach (BattleCardSlot slot in allSlots)
            {
                if (slot == null) continue;
                CardActionType action = slot.CardAction;
                if (action != CardActionType.in_front_line && action != CardActionType.in_back_line) continue;
                bool spawned = this.DeployOmegaSlotToLine(prefab, slot);
                if (!spawned) continue;
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private bool DeployOmegaSlotToLine(Card3DCtrl prefab, BattleCardSlot slot)
        {
            Location location        = slot.CardAction == CardActionType.in_front_line ? Location.in_front : Location.in_back;
            CardHolderCtrl[] holders = slot.CardAction == CardActionType.in_front_line ? this.deskPosition.OmegaFrontLine : this.deskPosition.OmegaBackLine;
            int idx = slot.slot_index;
            if (idx < 0 || idx >= holders.Length) return false;
            CardHolderCtrl holder = holders[idx];
            if (holder == null) return false;
            Transform target = holder.transform;
            if (this.IsSlotOccupied(target)) return false;
            Card3DCtrl card = this.ResolveOmegaLineCard(prefab, out bool fromHand);
            if (card == null) return false;
            card.SetOwner(Owner.omega);
            string code = slot.item_definition_code_name;
            card.SetCodeName(code);
            card.SetInventoryItemId(slot.inventory_item_id);
            card.SetFallbackName(slot.item_definition_name);
            card.LoadCardByCodeName(code);
            card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
            card.SetExpose(slot.expose);
            if (fromHand)
                card.MoveToUnknow(holder, () => this.ApplyFaceState(card, slot));
            else
                card.SetCardHolder(holder, () => this.ApplyFaceState(card, slot));
            this.slotOccupancy[target] = card;
            holder.SetCard(card);
            return true;
        }

        private bool DrawOmegaSlotToHand(Card3DCtrl prefab, BattleCardSlot slot)
        {
            int idx = slot.slot_index;
            Transform target = this.ResolveHandTarget(idx, this.deskPosition.OmegaHand);
            if (this.IsSlotOccupied(target)) return false;
            Card3DCtrl card = this.DequeueOmegaSourceCard(prefab);
            if (card == null) return false;
            card.SetOwner(Owner.omega);
            string code = slot.item_definition_code_name;
            card.SetCodeName(code);
            card.SetInventoryItemId(slot.inventory_item_id);
            card.SetFallbackName(slot.item_definition_name);
            card.LoadCardByCodeName(code);
            card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
            card.MoveAndRotate(target, Location.in_hand);
            this.slotOccupancy[target] = card;
            this.omegaHandCardQueue.Enqueue(card);
            return true;
        }

        private Card3DCtrl ResolveOrSpawnCard(Card3DCtrl prefab, BattleCardSlot slot)
        {
            string id = slot.inventory_item_id;
            if (!string.IsNullOrEmpty(id) && this.sourceCardRegistry.TryGetValue(id, out Card3DCtrl existing))
            {
                this.sourceCardRegistry.Remove(id);
                return existing;
            }
            return this.SpawnCardAt(prefab, this.deskPosition.AlphaTheSource);
        }

        private Card3DCtrl DequeueOmegaSourceCard(Card3DCtrl prefab)
        {
            if (this.omegaSourceCardQueue.Count > 0) return this.omegaSourceCardQueue.Dequeue();
            return this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
        }

        private Card3DCtrl ResolveOmegaLineCard(Card3DCtrl prefab, out bool fromHand)
        {
            if (this.previousOmegaHand != null && this.previousOmegaHand.Length > 0 && this.omegaHandCardQueue.Count > 0)
            {
                fromHand = true;
                Card3DCtrl handCard = this.omegaHandCardQueue.Dequeue();
                this.RemoveFromSlotOccupancy(handCard);
                return handCard;
            }
            fromHand = false;
            return this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
        }

        private Card3DCtrl ResolveOrSpawnFromHand(Card3DCtrl prefab, BattleCardSlot slot)
        {
            string id = slot.inventory_item_id;
            if (string.IsNullOrEmpty(id)) return this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
            if (!this.handCardRegistry.TryGetValue(id, out Card3DCtrl existing)) return this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
            this.handCardRegistry.Remove(id);
            this.RemoveFromSlotOccupancy(existing);
            return existing;
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
