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
        private readonly Dictionary<string, Card3DCtrl>    sourceCardRegistry = new Dictionary<string, Card3DCtrl>();
        private readonly Dictionary<Transform, Card3DCtrl> slotOccupancy      = new Dictionary<Transform, Card3DCtrl>();

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SpawnBattleStatus(bool spawnSource)
        {
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.SpawnBattleStatusRoutine(spawnSource));
        }

        public void SpawnAlphaHand(BattleCardSlot[] slots)
        {
            this.SpawnSlots(slots, this.deskPosition.AlphaHand);
        }

        public void SpawnAlphaFrontLine(BattleCardSlot[] slots)
        {
            this.SpawnSlots(slots, this.deskPosition.AlphaFrontLine);
        }

        public void SpawnAlphaBackLine(BattleCardSlot[] slots)
        {
            this.SpawnSlots(slots, this.deskPosition.AlphaBackLine);
        }

        public void SpawnStatusDelta(BattleCardSlot[] newHand, BattleCardSlot[] newFrontLine, BattleCardSlot[] newBackLine)
        {
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.SpawnStatusDeltaRoutine(newHand, newFrontLine, newBackLine));
        }

        public void ClearSourceRegistry()
        {
            this.sourceCardRegistry.Clear();
            this.slotOccupancy.Clear();
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

        private IEnumerator SpawnStatusDeltaRoutine(BattleCardSlot[] newHand, BattleCardSlot[] newFrontLine, BattleCardSlot[] newBackLine)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            yield return this.SpawnNewSourceCardsRoutine(prefab);
            yield return this.SpawnDeltaLineRoutine(prefab, newHand,      this.deskPosition.AlphaHand,      Location.in_hand);
            yield return this.SpawnDeltaLineRoutine(prefab, newFrontLine, this.deskPosition.AlphaFrontLine, Location.in_front);
            yield return this.SpawnDeltaLineRoutine(prefab, newBackLine,  this.deskPosition.AlphaBackLine,  Location.in_back);
            this.spawnRoutine = null;
        }

        private IEnumerator SpawnDeltaLineRoutine(Card3DCtrl prefab, BattleCardSlot[] slots, Transform[] targets, Location location)
        {
            if (slots == null) yield break;
            int spawnedThisFrame = 0;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.CardAction == CardActionType.unknown) continue;
                int idx = slot.slot_index;
                CardHolderCtrl holder = this.FindAlphaHolder(slot.CardAction, idx);
                Transform target = holder != null ? holder.transform : this.ResolveTarget(slot.CardAction, idx, targets);
                if (this.IsSlotOccupied(target)) continue;
                Transform spawnOrigin = this.ResolveSpawnOrigin(slot.CardAction);
                Card3DCtrl card = this.ResolveOrSpawnCard(prefab, slot, spawnOrigin);
                if (card == null) continue;
                card.SetOwner(Owner.alpha);
                string code = slot.item_definition_code_name;
                card.SetCodeName(code);
                card.SetInventoryItemId(slot.inventory_item_id);
                card.SetFallbackName(slot.item_definition_name);
                card.LoadCardByCodeName(code);
                card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
                card.MoveAndRotate(target, location);
                this.slotOccupancy[target] = card;
                if (holder != null) holder.SetCard(card);
                this.ApplyFaceState(card, slot, location);
                Debug.Log($"<color=#00FF88><b>[CardSpawning] card_action=<i>{slot.card_action}</i></b> | code=<b>{code}</b> | slot={idx} | location={location}</color>");
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
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

        private Transform ResolveTarget(CardActionType cardAction, int slotIndex, Transform[] defaultTargets)
        {
            if (cardAction == CardActionType.draw_from_source_to_hand)
            {
                Transform[] hand = this.deskPosition.AlphaHand;
                return slotIndex < hand.Length ? hand[slotIndex] : this.deskPosition.AlphaSpawnPoint;
            }
            if (cardAction == CardActionType.in_front_line)
            {
                Transform[] front = this.deskPosition.AlphaFrontLine;
                return slotIndex < front.Length ? front[slotIndex] : this.deskPosition.AlphaSpawnPoint;
            }
            if (cardAction == CardActionType.in_back_line)
            {
                Transform[] back = this.deskPosition.AlphaBackLine;
                return slotIndex < back.Length ? back[slotIndex] : this.deskPosition.AlphaSpawnPoint;
            }
            return slotIndex < defaultTargets.Length ? defaultTargets[slotIndex] : this.deskPosition.AlphaSpawnPoint;
        }

        private Transform ResolveSpawnOrigin(CardActionType cardAction)
        {
            Debug.Log($"<color=#FFD700>[CardSpawning] ResolveSpawnOrigin: <b>{cardAction}</b></color>");
            if (cardAction == CardActionType.draw_from_source_to_hand) return this.deskPosition.AlphaTheSource;
            return this.deskPosition.AlphaSpawnPoint;
        }

        private CardHolderCtrl FindAlphaHolder(CardActionType cardAction, int slotIndex)
        {
            if (cardAction == CardActionType.in_front_line) return this.deskPosition.GetAlphaFrontHolder(slotIndex);
            if (cardAction == CardActionType.in_back_line)  return this.deskPosition.GetAlphaBackHolder(slotIndex);
            return null;
        }

        private CardHolderCtrl FindAlphaHolderByLocation(Location location, int slotIndex)
        {
            if (location == Location.in_front) return this.deskPosition.GetAlphaFrontHolder(slotIndex);
            if (location == Location.in_back)  return this.deskPosition.GetAlphaBackHolder(slotIndex);
            return null;
        }

        private IEnumerator SpawnNewSourceCardsRoutine(Card3DCtrl prefab)
        {
            BattleCardSlot[] sourceSlots = this.battleState?.AlphaTheSource;
            if (sourceSlots == null) yield break;
            int spawnedThisFrame = 0;
            foreach (BattleCardSlot slot in sourceSlots)
            {
                if (slot == null) continue;
                if (string.IsNullOrEmpty(slot.inventory_item_id)) continue;
                if (this.sourceCardRegistry.ContainsKey(slot.inventory_item_id)) continue;
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card == null) continue;
                card.SetOwner(Owner.alpha);
                string code = slot.item_definition_code_name;
                card.SetCodeName(code);
                card.SetInventoryItemId(slot.inventory_item_id);
                card.SetFallbackName(slot.item_definition_name);
                card.LoadCardByCodeName(code);
                card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
                card.MoveAndRotate(this.deskPosition.AlphaTheSource, Location.in_source);
                this.sourceCardRegistry[slot.inventory_item_id] = card;
                Debug.Log($"<color=#4FC3F7>[CardSpawning] SpawnNewSource: <b>{code}</b> | id={slot.inventory_item_id}</color>");
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private Card3DCtrl ResolveOrSpawnCard(Card3DCtrl prefab, BattleCardSlot slot, Transform spawnOrigin)
        {
            if (slot.CardAction != CardActionType.draw_from_source_to_hand) return this.SpawnCardAt(prefab, spawnOrigin);
            string id = slot.inventory_item_id;
            if (!string.IsNullOrEmpty(id) && this.sourceCardRegistry.TryGetValue(id, out Card3DCtrl existing))
            {
                this.sourceCardRegistry.Remove(id);
                return existing;
            }
            return this.SpawnCardAt(prefab, spawnOrigin);
        }

        private IEnumerator SpawnBattleStatusRoutine(bool spawnSource)
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.WaitForDefinitions();
            if (spawnSource)
                yield return this.RunParallel(this.SpawnAlphaSourceRoutine(prefab), this.SpawnOmegaSourceRoutine(prefab));
            yield return this.RunParallel(
                this.SpawnAlphaLineRoutine(prefab, this.battleState.AlphaHand, this.deskPosition.AlphaHand, Location.in_hand),
                this.SpawnOmegaHandRoutine(prefab));
            yield return this.SpawnAlphaLineRoutine(prefab, this.battleState.AlphaFrontLine, this.deskPosition.AlphaFrontLine, Location.in_front);
            yield return this.SpawnAlphaLineRoutine(prefab, this.battleState.AlphaBackLine, this.deskPosition.AlphaBackLine, Location.in_back);
            this.spawnRoutine = null;
        }

        private IEnumerator RunParallel(IEnumerator a, IEnumerator b)
        {
            Coroutine ca = this.StartCoroutine(a);
            Coroutine cb = this.StartCoroutine(b);
            yield return ca;
            yield return cb;
        }

        private Card3DCtrl ResolvePrefab()
        {
            if (this.cardPool == null) return null;
            return this.cardPool.PoolPrefabs.GetByName(this.prefabName);
        }

        private IEnumerator SpawnAlphaSourceRoutine(Card3DCtrl prefab)
        {
            int count = this.battleState.AlphaTheSourceCount;
            int spawnedThisFrame = 0;
            for (int i = 0; i < count; i++)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card == null) continue;
                card.SetOwner(Owner.alpha);
                card.MoveAndRotate(this.deskPosition.AlphaTheSource, Location.in_source);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnOmegaSourceRoutine(Card3DCtrl prefab)
        {
            int count = this.battleState.OmegaTheSourceCount;
            int spawnedThisFrame = 0;
            for (int i = 0; i < count; i++)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card == null) continue;
                card.SetOwner(Owner.omega);
                card.MoveAndRotate(this.deskPosition.OmegaTheSource, Location.in_source);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnOmegaHandRoutine(Card3DCtrl prefab)
        {
            int count = this.battleState.OmegaHandCount;
            int spawnedThisFrame = 0;
            for (int i = 0; i < count; i++)
            {
                Transform target = this.deskPosition.GetOmegaHand(i);
                if (target == null) target = this.deskPosition.OmegaSpawnPoint;
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card == null) continue;
                card.SetOwner(Owner.omega);
                card.MoveAndRotate(target, Location.in_hand);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnAlphaLineRoutine(Card3DCtrl prefab, BattleCardSlot[] slots, Transform[] targets, Location location)
        {
            if (slots == null) yield break;
            int spawnedThisFrame = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                BattleCardSlot slot = slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.item_definition_code_name)) continue;
                CardHolderCtrl holder = this.FindAlphaHolderByLocation(location, i);
                Transform target = holder != null ? holder.transform : (i < targets.Length ? targets[i] : this.deskPosition.AlphaSpawnPoint);
                if (this.IsSlotOccupied(target)) continue;
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card == null) continue;
                card.SetOwner(Owner.alpha);
                string code = slot.item_definition_code_name;
                card.SetCodeName(code);
                card.SetInventoryItemId(slot.inventory_item_id);
                card.SetFallbackName(slot.item_definition_name);
                card.LoadCardByCodeName(code);
                card.SetDefinition(this.battleCardDefinitions?.GetDefinitionByCode(code));
                card.MoveAndRotate(target, location);
                this.slotOccupancy[target] = card;
                if (holder != null) holder.SetCard(card);
                this.ApplyFaceState(card, slot, location);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private void ApplyFaceState(Card3DCtrl card, BattleCardSlot slot, Location location)
        {
            if (location != Location.in_front && location != Location.in_back) return;
            if (slot.face_up) card.FaceUpUnknown();
            else card.FaceDownUnknown();
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

        private void SpawnSlots(BattleCardSlot[] slots, Transform[] positions)
        {
            if (this.cardPool == null) return;
            if (slots == null) return;
            Card3DCtrl prefab = this.cardPool.PoolPrefabs.GetByName(this.prefabName);
            if (prefab == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                Transform targetPos = i < positions.Length ? positions[i] : this.deskPosition.AlphaSpawnPoint;
                Card3DCtrl card = this.cardPool.Spawn(prefab, targetPos.position);
                card.transform.rotation = targetPos.rotation;
                card.SetOwner(Owner.alpha);
                BattleCardSlot deploySlot = slots[i];
                if (deploySlot != null)
                {
                    card.SetFallbackName(deploySlot.item_definition_name);
                    card.LoadCardByCodeName(deploySlot.item_definition_code_name);
                }
                card.gameObject.SetActive(true);
            }
        }
    }
}
