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

        private Coroutine spawnRoutine;

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SpawnGameStart()
        {
            Debug.Log("<color=#FFD700><b>[CardSpawning] SpawnGameStart — alpha: " + BattleState.Instance.AlphaTheSourceCount + ", omega: " + BattleState.Instance.OmegaTheSourceCount + "</b></color>");
            if (this.spawnRoutine != null) this.StopCoroutine(this.spawnRoutine);
            this.spawnRoutine = this.StartCoroutine(this.SpawnGameStartRoutine());
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
        }

        private void UnsubscribeEvents()
        {
            BattleState.OnGameStart -= this.SpawnGameStart;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private IEnumerator SpawnGameStartRoutine()
        {
            if (this.cardPool == null) yield break;
            Card3DCtrl prefab = this.cardPool.PoolPrefabs.GetByName(this.prefabName);
            if (prefab == null) yield break;

            int alphaCount = BattleState.Instance.AlphaTheSourceCount;
            int omegaCount = BattleState.Instance.OmegaTheSourceCount;
            int spawnedThisFrame = 0;

            for (int i = 0; i < alphaCount; i++)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.AlphaSpawnPoint);
                if (card != null) card.MoveTo(this.deskPosition.AlphaTheSource.position);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return null;
            }

            for (int i = 0; i < omegaCount; i++)
            {
                Card3DCtrl card = this.SpawnCardAt(prefab, this.deskPosition.OmegaSpawnPoint);
                if (card != null) card.MoveTo(this.deskPosition.OmegaTheSource.position);
                spawnedThisFrame++;
                if (spawnedThisFrame < this.spawnPerFrame) continue;
                spawnedThisFrame = 0;
                yield return null;
            }

            this.spawnRoutine = null;
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
                card.SetFallbackName(slots[i].item_definition_name);
                card.LoadCardByCodeName(slots[i].item_definition_code_name);
                card.gameObject.SetActive(true);
            }
        }
    }
}
