using System;
using UnityEngine;
using SaiGame.Services;

namespace SG03.Quest
{
    // Data cache for one quest collection.
    //
    // Scene hierarchy pattern:
    //   QuestManager
    //   ├─ "abc-pool-id"   ← QuestList  (gameObject.name = pool_id from server)
    //   └─ "def-pool-id"   ← QuestList
    //
    // QuestManager matches server data to the child whose name equals the
    // incoming pool id, then calls SetEntries(). UI reads Entries[].
    public class QuestList : SaiBehaviour
    {
        [Header("Identity")]
        [Tooltip("Pool key from the server. Auto-loaded from GameObject name — rename the GameObject to match.")]
        [SerializeField] private string poolKey;
        [SerializeField] private QuestPoolManager manager;

        [Header("Refresh Settings")]
        [SerializeField] private QuestRefreshMode refreshMode = QuestRefreshMode.AssignAhead;
        [Tooltip("Number of days to assign ahead. Only used when Refresh Mode is AssignAhead.")]
        [SerializeField] private int daysAhead = 6;

        [Header("Quest Entries Cache")]
        [SerializeField] private DailyQuestEntryData[] entries;

        // Fired whenever SetEntries / Clear is called.
        public event Action OnDataUpdated;

        // ── LoadComponents ────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.poolKey = this.gameObject.name;
            this.LoadManager();
        }

        private void LoadManager()
        {
            if (this.manager != null) return;
            this.manager = this.GetComponentInParent<QuestPoolManager>();
            Debug.LogWarning(transform.name + ": LoadManager", gameObject);
        }

        // ── Properties ────────────────────────────────────────────────────

        public string            PoolKey     => this.poolKey;
        public QuestPoolManager  Manager     => this.manager;
        public QuestRefreshMode  RefreshMode => this.refreshMode;
        public int               DaysAhead   => this.daysAhead;
        public bool                  HasData  => this.entries != null && this.entries.Length > 0;
        public DailyQuestEntryData[] Entries  => this.entries;

        // ── Cache setter (called by QuestManager) ─────────────────────────

        public void SetEntries(DailyQuestEntryData[] data)
        {
            this.entries = data;
            this.OnDataUpdated?.Invoke();
        }

        public void Clear()
        {
            this.entries = null;
            this.OnDataUpdated?.Invoke();
        }

        // Calls GetPools on the manager, finds this list's pool, then loads
        // today's quest entries and stores them in Entries.
        public void Refresh(Action<DailyQuestEntryData[]> onSuccess = null)
        {
            if (this.manager == null)
            {
                Debug.LogWarning(transform.name + ": Refresh — no manager assigned.", gameObject);
                return;
            }

            this.manager.RefreshList(
                this.poolKey,
                this.refreshMode,
                this.daysAhead,
                onSuccess: entries =>
                {
                    this.SetEntries(entries);
                    onSuccess?.Invoke(entries);
                },
                onError:   err     => Debug.LogWarning(transform.name + ": Refresh failed — " + err, gameObject)
            );
        }

        // ── Helpers ───────────────────────────────────────────────────────

        public bool Matches(string key)
            => !string.IsNullOrEmpty(key)
            && this.poolKey == key;
    }
}
