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

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SpawnGameStart()
        {
            this.SpawnAtPoint(BattleState.Instance.AlphaCardsDrawn, this.deskPosition.AlphaSpawnPoint);
            this.SpawnAtPoint(BattleState.Instance.OmegaCardsDrawn, this.deskPosition.OmegaSpawnPoint);
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

        private void SpawnAtPoint(int count, Transform spawnPoint)
        {
            if (this.cardPool == null) return;
            if (spawnPoint == null) return;
            Card3DCtrl prefab = this.cardPool.PoolPrefabs.GetByName(this.prefabName);
            if (prefab == null) return;
            for (int i = 0; i < count; i++)
            {
                Card3DCtrl card = this.cardPool.Spawn(prefab, spawnPoint.position);
                card.gameObject.SetActive(true);
            }
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
