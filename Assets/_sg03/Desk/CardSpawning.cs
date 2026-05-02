using System.Collections;
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
        [SerializeField] private string prefabName = "Card3D";
        [SerializeField] private int spawnPerFrame = 1;
        [SerializeField] private float spawnInterval = 0.05f;

        private Coroutine spawnRoutine;

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SpawnGameStart()
        {
            Debug.Log("<color=#FFD700><b>[CardSpawning] SpawnGameStart — alpha: " + BattleState.Instance.AlphaTheSourceCount + ", omega: " + BattleState.Instance.OmegaTheSourceCount + "</b></color>");
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.SpawnGameStartRoutine());
        }

        public void SpawnGameResume()
        {
            Debug.Log("<color=#00CFFF><b>[CardSpawning] SpawnGameResume — alpha: " + BattleState.Instance.AlphaTheSourceCount + ", omega: " + BattleState.Instance.OmegaTheSourceCount + "</b></color>");
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

        private void SubscribeEvents()
        {
            BattleState.OnGameStart += this.SpawnGameStart;
            BattleState.OnGameResume += this.SpawnGameResume;
        }

        private void UnsubscribeEvents()
        {
            BattleState.OnGameStart -= this.SpawnGameStart;
            BattleState.OnGameResume -= this.SpawnGameResume;
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
            int count = BattleState.Instance.AlphaTheSourceCount;
            int spawnedThisFrame = 0;
            for (int i = 0; i < count; i++)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card != null) card.MoveTo(this.deskPosition.AlphaTheSource, Location.in_source);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnOmegaSourceRoutine(Card3DCtrl prefab)
        {
            int count = BattleState.Instance.OmegaTheSourceCount;
            int spawnedThisFrame = 0;
            for (int i = 0; i < count; i++)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card != null) card.MoveTo(this.deskPosition.OmegaTheSource, Location.in_source);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnOmegaHandRoutine(Card3DCtrl prefab)
        {
            int count = BattleState.Instance.OmegaHandCount;
            int spawnedThisFrame = 0;
            for (int i = 0; i < count; i++)
            {
                Transform target = this.deskPosition.GetOmegaHand(i);
                if (target == null) target = this.deskPosition.OmegaSpawnPoint;
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card != null) card.MoveTo(target, Location.in_hand);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return this.WaitAfterSpawn();
            }
        }

        private IEnumerator SpawnAlphaHandResumeRoutine(Card3DCtrl prefab)
        {
            BattleCardSlot[] handSlots = BattleState.Instance.AlphaHand;
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
                    card.MoveTo(target, Location.in_hand);
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
