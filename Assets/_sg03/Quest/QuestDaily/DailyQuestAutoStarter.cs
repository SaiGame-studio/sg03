using System;
using UnityEngine;
using SaiGame.Services;

namespace SG03.Quest
{
    // Automatically calls StartQuest for every today's quest whose status
    // is "not_started" whenever the sibling QuestList receives new data.
    //
    // Place this component on the SAME GameObject as a QuestList.
    public class DailyQuestAutoStarter : SaiBehaviour
    {
        [SerializeField] private QuestList questList;

        // ── LoadComponents ────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadQuestList();
        }

        private void LoadQuestList()
        {
            if (this.questList != null) return;
            this.questList = this.GetComponent<QuestList>();
            Debug.LogWarning(transform.name + ": LoadQuestList", gameObject);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────

        protected override void Start()
        {
            base.Start();
            if (this.questList == null) return;
            this.questList.OnDataUpdated += this.OnDataUpdated;

            // If the list already has data when we start, process it immediately.
            if (this.questList.HasData)
                this.OnDataUpdated();
        }

        private void OnDestroy()
        {
            if (this.questList != null)
                this.questList.OnDataUpdated -= this.OnDataUpdated;
        }

        // ── Auto-start logic ──────────────────────────────────────────────

        private void OnDataUpdated()
        {
            if (this.questList?.Entries == null) return;

            string today = DateTime.Today.ToString("yyyy-MM-dd");

            foreach (DailyQuestEntryData entry in this.questList.Entries)
            {
                if (!this.IsTodayEntry(entry, today)) continue;
                if (entry.status != "not_started") continue;
                if (entry.quest == null || string.IsNullOrEmpty(entry.quest.id)) continue;

                this.AutoStartQuest(entry.quest.id);
            }
        }

        private bool IsTodayEntry(DailyQuestEntryData entry, string today)
        {
            string raw = entry.assignment?.assigned_date;
            if (string.IsNullOrEmpty(raw)) return false;
            // Normalize full ISO datetime "2026-04-26T00:00:00Z" → "2026-04-26"
            string date = raw.Length >= 10 ? raw.Substring(0, 10) : raw;
            return date == today;
        }

        private void AutoStartQuest(string questDefinitionId)
        {
            QuestProgressor progressor = SaiServer.Instance?.QuestProgressor;
            if (progressor == null)
            {
                Debug.LogWarning(transform.name + ": QuestProgressor not found — cannot auto-start: " + questDefinitionId, gameObject);
                return;
            }

            progressor.StartQuest(
                questDefinitionId: questDefinitionId,
                onSuccess: r =>
                {
                    Debug.Log(transform.name + ": Auto-started quest " + questDefinitionId + " → status=" + r.status, gameObject);
                    // Refresh so QuestList (and the UI listening to it) gets the updated status.
                    this.questList.Refresh();
                },
                onError: err => Debug.LogWarning(transform.name + ": Auto-start failed for " + questDefinitionId + ": " + err, gameObject)
            );
        }
    }
}
