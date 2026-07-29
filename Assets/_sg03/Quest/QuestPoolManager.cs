using UnityEngine;
using SaiGame.Services;
using System.Collections.Generic;

namespace SG03.Quest
{
    // Abstract base for all quest pool managers (Daily, Main, etc.).
    //
    // Owns the QuestList children.
    // Concrete subclasses implement Subscribe/Unsubscribe and GetPools.
    // Access SaiServer via SaiServer.Instance.
    //
    // Scene hierarchy pattern:
    //   QuestDailyManager  (extends QuestPoolManager)
    //   ├─ "abc-pool-id"   (QuestList)
    //   └─ "def-pool-id"   (QuestList)
    public abstract class QuestPoolManager : SaiBehaviour
    {
        [Header("── QuestPoolManager ──────────────────")]
        [SerializeField] protected DailyQuestPoolData[] pools;

        [Space(4)]
        [SerializeField] protected QuestList[] questLists;

        // Read-only access for UI binding.
        public QuestList[] QuestLists => this.questLists;

        // ── LoadComponents ────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadQuestLists();
        }

        private void LoadQuestLists()
        {
            if (this.questLists != null && this.questLists.Length > 0) return;
            this.questLists = this.GetComponentsInChildren<QuestList>(includeInactive: true);
            Debug.LogWarning(transform.name + ": LoadQuestLists — found " + this.questLists.Length, gameObject);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────

        protected override void Start()
        {
            base.Start();
            this.Subscribe();
        }

        protected virtual void OnDestroy()
        {
            this.Unsubscribe();
        }

        // ── Abstract contract ─────────────────────────────────────────────

        protected abstract void Subscribe();
        protected abstract void Unsubscribe();

        public abstract void GetPools();

        // Fetches pools, finds the pool matching poolKey, then loads today's quests
        // for that pool and returns the entries via onSuccess.
        public abstract void RefreshList(
            string poolKey,
            QuestRefreshMode mode,
            int daysAhead,
            System.Action<DailyQuestEntryData[]> onSuccess,
            System.Action<string> onError = null);

        // ── Shared public API ─────────────────────────────────────────────

        // Returns the QuestList whose poolKey matches the given key, or null.
        public QuestList GetList(string key)
        {
            foreach (QuestList list in this.questLists)
            {
                if (list != null && list.Matches(key)) return list;
            }
            return null;
        }

        // Keeps the UI-facing lists in step with pools supplied by the API.
        // Prefabs may provide initial lists, but a game can add/remove pools
        // without requiring a prefab change.
        protected void SynchronizeQuestLists(DailyQuestPoolData[] loadedPools)
        {
            if (loadedPools == null) return;

            Dictionary<string, QuestList> listsByKey = new Dictionary<string, QuestList>();
            foreach (QuestList list in this.questLists)
            {
                if (list != null && !string.IsNullOrEmpty(list.PoolKey))
                    listsByKey[list.PoolKey] = list;
            }

            List<QuestList> synchronizedLists = new List<QuestList>();
            foreach (DailyQuestPoolData pool in loadedPools)
            {
                if (pool == null || string.IsNullOrEmpty(pool.pool_key)) continue;

                if (!listsByKey.TryGetValue(pool.pool_key, out QuestList list))
                {
                    GameObject listObject = new GameObject(pool.pool_key);
                    listObject.transform.SetParent(this.transform, false);
                    list = listObject.AddComponent<QuestList>();
                    list.Initialize(pool.pool_key, this);
                    listsByKey.Add(pool.pool_key, list);
                }

                synchronizedLists.Add(list);
            }

            this.questLists = synchronizedLists.ToArray();
        }

        // Clears all cached quest data (e.g., on logout).
        public void ClearAll()
        {
            foreach (QuestList list in this.questLists)
                list?.Clear();
        }

        // Pushes entries into the matching QuestList child.
        protected void DistributeEntries(string poolId, DailyQuestEntryData[] entries)
        {
            if (string.IsNullOrEmpty(poolId)) return;

            foreach (QuestList list in this.questLists)
            {
                if (list == null) continue;
                if (!list.Matches(poolId)) continue;
                list.SetEntries(entries);
                return;
            }

            Debug.LogWarning(transform.name
                + ": No QuestList found for pool id [" + poolId + "]", gameObject);
        }
    }
}
