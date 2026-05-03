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
        [SerializeField] private CardPool cardPool;
        [SerializeField] private DeskPositionCtrl deskPosition;
        [SerializeField] private BattleState battleState;
        [SerializeField] private string prefabName = "Card3D";
        [SerializeField] private int spawnPerFrame = 1;
        [SerializeField] private float spawnInterval = 0.05f;

        private Coroutine spawnRoutine;

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SpawnGameStart()
        {
            Debug.Log("<color=#FFD700><b>[CardSpawning] SpawnGameStart — alpha: " + this.battleState.AlphaTheSourceCount + ", omega: " + this.battleState.OmegaTheSourceCount + "</b></color>");
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.SpawnGameStartRoutine());
        }

        public void SpawnGameResume()
        {
            Debug.Log("<color=#00CFFF><b>[CardSpawning] SpawnGameResume — alpha: " + this.battleState.AlphaTheSourceCount + ", omega: " + this.battleState.OmegaTheSourceCount + "</b></color>");
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.SpawnGameResumeRoutine());
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

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected void OnEnable()
        {
            this.SubscribeEvents();
        }

        protected void OnDisable()
        {
            this.UnsubscribeEvents();
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCardPool();
            this.LoadDeskPosition();
            this.LoadBattleState();
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
            this.battleState = GameObject.FindAnyObjectByType<BattleState>();
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        private void SubscribeEvents()
        {
            BattleState.OnGameStart += this.SpawnGameStart;
            BattleState.OnGameResume += this.SpawnGameResume;
            BattleState.OnInitCards += this.OnInitCardsReceived;
        }

        private void UnsubscribeEvents()
        {
            BattleState.OnGameStart -= this.SpawnGameStart;
            BattleState.OnGameResume -= this.SpawnGameResume;
            BattleState.OnInitCards -= this.OnInitCardsReceived;
        }

        private void OnInitCardsReceived(InitCardsResult result)
        {
            this.LogInitCardsResult(result);
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.InitCardsRoutine(result));
        }

        private void LogInitCardsResult(InitCardsResult result)
        {
            Debug.Log(
                "<color=#FFD700><b>[CardSpawning] InitCards complete</b></color>" +
                " | Alpha added to hand: " + result.AlphaCardsAddedToHand +
                " | Alpha removed from source: " + result.AlphaCardsRemovedFromSource +
                " | Omega added to hand: " + result.OmegaCardsAddedToHand +
                " | Omega removed from source: " + result.OmegaCardsRemovedFromSource);
        }

        private IEnumerator InitCardsRoutine(InitCardsResult result)
        {
            yield return this.RunParallel(
                this.MoveAlphaSourceToHandRoutine(result.AlphaCardsAddedToHand),
                this.MoveOmegaSourceToHandRoutine(result.OmegaCardsAddedToHand));
            this.spawnRoutine = null;
        }

        private IEnumerator MoveAlphaSourceToHandRoutine(int count)
        {
            List<Card3DCtrl> sourceCards = this.FindSourceCards(this.deskPosition.AlphaTheSource);
            BattleCardSlot[] hand = this.battleState.AlphaHand;
            int moveCount = Mathf.Min(count, sourceCards.Count);
            int spawnedThisFrame = 0;
            for (int i = 0; i < moveCount; i++)
            {
                Transform target = i < this.deskPosition.AlphaHand.Length
                    ? this.deskPosition.AlphaHand[i]
                    : this.deskPosition.AlphaSpawnPoint;
                Card3DCtrl card = sourceCards[i];
                if (hand != null && i < hand.Length)
                {
                    card.SetFallbackName(hand[i].item_definition_name);
                    card.LoadCardByCodeName(hand[i].item_definition_code_name);
                }
                card.MoveAndRotate(target, Location.in_hand);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator MoveOmegaSourceToHandRoutine(int count)
        {
            List<Card3DCtrl> sourceCards = this.FindSourceCards(this.deskPosition.OmegaTheSource);
            OmegaInitCardSlot[] hand = this.battleState.OmegaHand;
            int moveCount = Mathf.Min(count, sourceCards.Count);
            int spawnedThisFrame = 0;
            for (int i = 0; i < moveCount; i++)
            {
                Transform target = this.deskPosition.GetOmegaHand(i);
                if (target == null) target = this.deskPosition.OmegaSpawnPoint;
                Card3DCtrl card = sourceCards[i];
                if (hand != null && i < hand.Length)
                    card.LoadCardByCodeName(hand[i].item_code_name);
                card.MoveAndRotate(target, Location.in_hand);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private List<Card3DCtrl> FindSourceCards(Transform nearestTo)
        {
            Card3DCtrl[] all = Object.FindObjectsByType<Card3DCtrl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<Card3DCtrl> result = new List<Card3DCtrl>();
            foreach (Card3DCtrl card in all)
            {
                if (card.Location != Location.in_source) continue;
                result.Add(card);
            }
            result.Sort((a, b) =>
            {
                float da = Vector3.Distance(a.transform.position, nearestTo.position);
                float db = Vector3.Distance(b.transform.position, nearestTo.position);
                return da.CompareTo(db);
            });
            return result;
        }

        private IEnumerator SpawnGameResumeRoutine()
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.RunParallel(this.SpawnAlphaSourceRoutine(prefab), this.SpawnOmegaSourceRoutine(prefab));
            yield return this.RunParallel(this.SpawnAlphaHandResumeRoutine(prefab), this.SpawnOmegaHandRoutine(prefab));
            this.spawnRoutine = null;
        }

        private IEnumerator SpawnGameStartRoutine()
        {
            Card3DCtrl prefab = this.ResolvePrefab();
            if (prefab == null) yield break;
            yield return this.RunParallel(this.SpawnAlphaSourceRoutine(prefab), this.SpawnOmegaSourceRoutine(prefab));
            yield return this.SpawnOmegaHandRoutine(prefab);
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
                if (card != null) card.MoveAndRotate(this.deskPosition.AlphaTheSource, Location.in_source);
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
                if (card != null) card.MoveAndRotate(this.deskPosition.OmegaTheSource, Location.in_source);
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
                if (card != null) card.MoveAndRotate(target, Location.in_hand);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnAlphaHandResumeRoutine(Card3DCtrl prefab)
        {
            BattleCardSlot[] handSlots = this.battleState.AlphaHand;
            if (handSlots == null) yield break;
            int spawnedThisFrame = 0;
            for (int i = 0; i < handSlots.Length; i++)
            {
                Transform target = i < this.deskPosition.AlphaHand.Length
                    ? this.deskPosition.AlphaHand[i]
                    : this.deskPosition.AlphaSpawnPoint;
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card != null)
                {
                    card.SetFallbackName(handSlots[i].item_definition_name);
                    card.LoadCardByCodeName(handSlots[i].item_definition_code_name);
                    card.MoveAndRotate(target, Location.in_hand);
                }
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
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
                card.SetFallbackName(slots[i].item_definition_name);
                card.LoadCardByCodeName(slots[i].item_definition_code_name);
                card.gameObject.SetActive(true);
            }
        }
    }
}
