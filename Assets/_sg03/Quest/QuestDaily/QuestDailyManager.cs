using UnityEngine;
using SaiGame.Services;

namespace SG03.Quest
{
    // Handles Daily Quest pool fetching and distributes today's quest entries
    // to matching QuestList children.
    public class QuestDailyManager : QuestPoolManager
    {
        // ── Public data access ────────────────────────────────────────────

        // Today's quest entries from the server response.
        public DailyQuestEntryData[] DailyQuests =>
            SaiServer.Instance?.DailyQuest?.CurrentTodayQuestResponse?.entries;

        // ── Abstract implementations ──────────────────────────────────────

        protected override void Subscribe()
        {
            if (SaiServer.Instance == null) return;
            if (SaiServer.Instance.DailyQuest == null) return;

            SaiServer.Instance.DailyQuest.OnGetPoolsSuccess       += this.OnPoolsReceived;
            SaiServer.Instance.DailyQuest.OnGetTodayQuestsSuccess += this.OnTodayQuestsReceived;
        }

        protected override void Unsubscribe()
        {
            if (SaiServer.Instance == null) return;
            if (SaiServer.Instance.DailyQuest == null) return;

            SaiServer.Instance.DailyQuest.OnGetPoolsSuccess       -= this.OnPoolsReceived;
            SaiServer.Instance.DailyQuest.OnGetTodayQuestsSuccess -= this.OnTodayQuestsReceived;
        }

        public override void GetPools()
        {
            if (SaiServer.Instance?.DailyQuest == null)
            {
                Debug.LogWarning(transform.name + ": DailyQuest not found on SaiServer.", gameObject);
                return;
            }
            SaiServer.Instance.DailyQuest.GetPools();
        }

        public override void RefreshList(
            string poolKey,
            QuestRefreshMode mode,
            int daysAhead,
            System.Action<DailyQuestEntryData[]> onSuccess,
            System.Action<string> onError = null)
        {
            if (SaiServer.Instance?.DailyQuest == null)
            {
                onError?.Invoke(transform.name + ": DailyQuest not found on SaiServer.");
                return;
            }

            SaiServer.Instance.DailyQuest.GetPools(
                onSuccess: poolsResponse =>
                {
                    if (poolsResponse?.pools == null)
                    {
                        onError?.Invoke(transform.name + ": No pools returned.");
                        return;
                    }

                    DailyQuestPoolData found = null;
                    foreach (DailyQuestPoolData pool in poolsResponse.pools)
                    {
                        if (pool.pool_key == poolKey) { found = pool; break; }
                    }

                    if (found == null)
                    {
                        onError?.Invoke(transform.name + ": Pool '" + poolKey + "' not found.");
                        return;
                    }

                    if (mode == QuestRefreshMode.TodayOnly)
                    {
                        SaiServer.Instance.DailyQuest.GetTodayQuests(
                            dqPoolId:  found.id,
                            onSuccess: r => onSuccess?.Invoke(r?.entries),
                            onError:   onError
                        );
                    }
                    else
                    {
                        SaiServer.Instance.DailyQuest.AssignAhead(
                            dqPoolId:  found.id,
                            daysAhead: daysAhead,
                            onSuccess: r =>
                            {
                                if (r?.days == null) { onSuccess?.Invoke(null); return; }

                                int total = 0;
                                foreach (DailyDayData day in r.days)
                                    if (day.quests != null) total += day.quests.Length;

                                DailyQuestEntryData[] allEntries = new DailyQuestEntryData[total];
                                int idx = 0;
                                foreach (DailyDayData day in r.days)
                                {
                                    if (day.quests == null) continue;
                                    foreach (DailyQuestEntryData e in day.quests)
                                        allEntries[idx++] = e;
                                }
                                onSuccess?.Invoke(allEntries);
                            },
                            onError: onError
                        );
                    }
                },
                onError: onError
            );
        }

        // ── Data handlers ─────────────────────────────────────────────────

        private void OnPoolsReceived(DailyQuestPoolsResponse response)
        {
            if (response == null) return;
            this.pools = response.pools;
            this.SynchronizeQuestLists(this.pools);
        }

        private void OnTodayQuestsReceived(TodayQuestResponse response)
        {
            if (response == null) return;
            this.DistributeEntries(response.pool?.id, response.entries);
        }
    }
}
