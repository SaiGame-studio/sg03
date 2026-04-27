using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;
using SaiGame.Services;
using SG03.Quest;

namespace SG03.UI
{
    // Binds DailyQuestContent.uxml to live data from QuestDailyManager.
    // Displays a 7-day week grid starting from today.
    // Re-renders automatically whenever any QuestList fires OnDataUpdated
    // (e.g. after auto-start + refresh).
    public class DailyQuestContentUI
    {
        private readonly VisualElement weekGrid;
        private readonly VisualElement emptyState;
        private QuestList[] lists;
        private bool hasCheckedOnOpen;

        // Vietnamese weekday names (Monday=2 … Saturday=7, Sunday=CN)
        private static readonly string[] DayNames = { "CN", "2", "3", "4", "5", "6", "7" };

        public DailyQuestContentUI(VisualElement root)
        {
            this.weekGrid   = root.Q("WeekGrid");
            this.emptyState = root.Q("EmptyState");

            QuestDailyManager manager = UnityEngine.Object.FindFirstObjectByType<QuestDailyManager>(UnityEngine.FindObjectsInactive.Include);
            if (manager == null) { this.ShowEmpty(); return; }

            this.lists = manager.QuestLists;
            if (this.lists == null || this.lists.Length == 0) { this.ShowEmpty(); return; }

            // Subscribe permanently — re-render on every future data update.
            foreach (QuestList list in this.lists)
            {
                if (list == null) continue;
                list.OnDataUpdated += this.OnAnyListUpdated;
            }

            // If some lists have no data yet, refresh them; otherwise render now.
            int pendingCount = 0;
            foreach (QuestList list in this.lists)
            {
                if (!list.HasData) pendingCount++;
            }

            if (pendingCount == 0) { this.Render(); return; }

            foreach (QuestList list in this.lists)
            {
                if (!list.HasData) list.Refresh();
            }
        }

        private void OnAnyListUpdated() => this.Render();

        private void ShowEmpty()
        {
            if (this.emptyState != null)
                this.emptyState.style.display = DisplayStyle.Flex;
            if (this.weekGrid != null)
                this.weekGrid.style.display = DisplayStyle.None;
        }

        private void Render()
        {
            if (this.weekGrid == null) return;

            // Collect all entries from all lists into a date → entries map.
            Dictionary<string, List<DailyQuestEntryData>> byDate =
                new Dictionary<string, List<DailyQuestEntryData>>();

            int totalLoaded = 0;
            foreach (QuestList list in this.lists)
            {
                if (list?.Entries == null) continue;
                foreach (DailyQuestEntryData entry in list.Entries)
                {
                    totalLoaded++;
                    string raw = entry.assignment?.assigned_date;
                    if (string.IsNullOrEmpty(raw)) continue;
                    // Normalize: take only the date part (yyyy-MM-dd) in case server
                    // returns a full ISO datetime string.
                    string date = raw.Length >= 10 ? raw.Substring(0, 10) : raw;
                    if (!byDate.ContainsKey(date))
                        byDate[date] = new List<DailyQuestEntryData>();
                    byDate[date].Add(entry);
                }
            }

            // No data loaded at all → show empty state.
            if (totalLoaded == 0)
            {
                this.ShowEmpty();
                return;
            }

            // Data exists → always show grid; individual day cards may be empty.
            if (this.emptyState != null)
                this.emptyState.style.display = DisplayStyle.None;

            this.weekGrid.Clear();
            this.weekGrid.style.display = DisplayStyle.Flex;

            DateTime today = DateTime.Today;
            for (int i = 0; i < 7; i++)
            {
                DateTime day = today.AddDays(i);
                string key = day.ToString("yyyy-MM-dd");
                byDate.TryGetValue(key, out List<DailyQuestEntryData> entries);
                this.weekGrid.Add(this.BuildDayCard(day, i == 0, entries));
            }

            if (!this.hasCheckedOnOpen)
            {
                this.hasCheckedOnOpen = true;
                this.CheckInProgressTodayQuests();
            }
        }

        // Calls CheckQuest for every today in_progress quest once per UI open.
        // On success, refreshes the owning QuestList so status is up to date.
        private void CheckInProgressTodayQuests()
        {
            if (SaiServer.Instance?.QuestProgressor == null) return;

            string todayKey = DateTime.Today.ToString("yyyy-MM-dd");

            foreach (QuestList list in this.lists)
            {
                if (list?.Entries == null) continue;

                foreach (DailyQuestEntryData entry in list.Entries)
                {
                    if (entry.status != "in_progress") continue;

                    string raw = entry.assignment?.assigned_date;
                    if (string.IsNullOrEmpty(raw)) continue;

                    string date = raw.Length >= 10 ? raw.Substring(0, 10) : raw;
                    if (date != todayKey) continue;

                    string questId = entry.quest?.id;
                    if (string.IsNullOrEmpty(questId)) continue;

                    QuestList ownerList = list;
                    SaiServer.Instance.QuestProgressor.CheckQuest(
                        questId,
                        onSuccess: _ => ownerList.Refresh(),
                        onError: err => UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] CheckQuest failed ({questId}): {err}")
                    );
                }
            }
        }

        private VisualElement BuildDayCard(DateTime day, bool isToday, List<DailyQuestEntryData> entries)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("dq-day-card");
            if (isToday) card.AddToClassList("dq-day-card--today");

            // Header
            VisualElement header = new VisualElement();
            header.AddToClassList("dq-day-card__header");

            Label dayName = new Label(DayNames[(int)day.DayOfWeek]);
            dayName.AddToClassList("dq-day-card__day-name");
            header.Add(dayName);

            Label dateLabel = new Label(day.ToString("dd/MM"));
            dateLabel.AddToClassList("dq-day-card__date");
            header.Add(dateLabel);

            card.Add(header);

            // Quest list
            VisualElement questsArea = new VisualElement();
            questsArea.AddToClassList("dq-day-card__quests");

            if (entries == null || entries.Count == 0)
            {
                VisualElement noQuest = new VisualElement();
                noQuest.AddToClassList("dq-day-card__no-quest");
                Label noQuestIcon = new Label("—");
                noQuestIcon.AddToClassList("dq-day-card__no-quest-icon");
                noQuest.Add(noQuestIcon);
                questsArea.Add(noQuest);
            }
            else
            {
                foreach (DailyQuestEntryData entry in entries)
                    questsArea.Add(this.BuildQuestItem(entry));
            }

            card.Add(questsArea);
            return card;
        }

        private VisualElement BuildQuestItem(DailyQuestEntryData entry)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("dq-quest-item");

            Label nameLabel = new Label(entry.quest?.name ?? "—");
            nameLabel.AddToClassList("dq-quest-item__name");
            item.Add(nameLabel);

            if (entry.rewards != null && entry.rewards.Length > 0)
                item.Add(this.BuildRewardRow(entry.rewards));

            // Spacer pushes bottom content to the bottom of the card.
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList("dq-quest-item__spacer");
            item.Add(spacer);

            // Bottom section: claim button (if completed) then status label.
            VisualElement bottom = new VisualElement();
            bottom.AddToClassList("dq-quest-item__bottom");

            if (entry.status == "completed")
            {
                string questId = entry.quest?.id;
                QuestList ownerList = this.FindOwnerList(questId);

                Button claimBtn = new Button();
                claimBtn.text = "Claim";
                claimBtn.AddToClassList("dq-quest-item__claim-btn");
                claimBtn.clicked += () =>
                {
                    if (string.IsNullOrEmpty(questId)) return;
                    if (SaiServer.Instance?.QuestProgressor == null) return;
                    claimBtn.SetEnabled(false);
                    SaiServer.Instance.QuestProgressor.ClaimQuest(
                        questId,
                        onSuccess: _ => ownerList?.Refresh(),
                        onError: err =>
                        {
                            claimBtn.SetEnabled(true);
                            UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] ClaimQuest failed ({questId}): {err}");
                        }
                    );
                };
                bottom.Add(claimBtn);
            }

            string statusText = this.StatusLabel(entry.status);
            if (!string.IsNullOrEmpty(statusText))
            {
                Label statusLabel = new Label(statusText);
                statusLabel.AddToClassList("dq-quest-item__status");
                statusLabel.AddToClassList(this.StatusClass(entry.status));
                bottom.Add(statusLabel);
            }

            string assignedDate = entry.assignment?.assigned_date;
            string expiresAt    = entry.assignment?.expires_at;

            bool assignedInFuture = IsInFuture(assignedDate);
            if (assignedInFuture)
            {
                // Quest hasn't started yet — show when it will begin.
                Label startsLabel = new Label($"Starts {TimeIn(assignedDate)}");
                startsLabel.AddToClassList("dq-quest-item__expires");
                bottom.Add(startsLabel);
            }
            else if (!string.IsNullOrEmpty(expiresAt))
            {
                Label expiresLabel = new Label(TimeAgo(expiresAt));
                expiresLabel.AddToClassList("dq-quest-item__expires");
                bottom.Add(expiresLabel);
            }

            item.Add(bottom);
            return item;
        }

        private QuestList FindOwnerList(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            foreach (QuestList list in this.lists)
            {
                if (list?.Entries == null) continue;
                foreach (DailyQuestEntryData entry in list.Entries)
                {
                    if (entry.quest?.id == questId) return list;
                }
            }
            return null;
        }

        private VisualElement BuildRewardRow(DailyRewardData[] rewards)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dq-quest-item__rewards");

            foreach (DailyRewardData reward in rewards)
            {
                string itemName = reward.item_definition?.name
                               ?? reward.item_definition?.item_code
                               ?? reward.item_definition_id ?? "?";

                string qty = reward.quantity_min == reward.quantity_max
                    ? reward.quantity_min.ToString()
                    : $"{reward.quantity_min}–{reward.quantity_max}";

                Label chip = new Label($"{itemName}: {qty}");
                chip.AddToClassList("dq-reward-chip");
                row.Add(chip);
            }

            return row;
        }

        private string StatusLabel(string status)
        {
            switch (status)
            {
                case "in_progress": return "▶ In Progress";
                case "completed":   return "✓ Completed";
                case "claimed":     return "★ Claimed";
                default:            return string.Empty;
            }
        }

        private string StatusClass(string status)
        {
            switch (status)
            {
                case "in_progress": return "dq-quest-item__status--in-progress";
                case "completed":   return "dq-quest-item__status--completed";
                case "claimed":     return "dq-quest-item__status--claimed";
                default:            return "dq-quest-item__status--not-started";
            }
        }

        private static string TimeIn(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return string.Empty;

            if (!DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind, out DateTime target))
                return isoTimestamp;

            TimeSpan diff = target.ToUniversalTime() - DateTime.UtcNow;

            if (diff.TotalSeconds <= 0) return "now";
            if (diff.TotalSeconds < 60)  return $"in {(int)diff.TotalSeconds}s";
            if (diff.TotalMinutes < 60)  return $"in {(int)diff.TotalMinutes}m";
            if (diff.TotalHours < 24)    return $"in {(int)diff.TotalHours}h";
            return $"in {(int)diff.TotalDays}d";
        }

        private static bool IsInFuture(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return false;
            // assigned_date may be a date-only string "yyyy-MM-dd"; treat it as start of that UTC day.
            if (!DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind, out DateTime target))
                return false;
            return target.ToUniversalTime() > DateTime.UtcNow;
        }

        private static string TimeAgo(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return string.Empty;

            if (!DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind, out DateTime target))
                return isoTimestamp;

            TimeSpan diff = target.ToUniversalTime() - DateTime.UtcNow;

            if (diff.TotalSeconds <= 0) return "Expired";
            if (diff.TotalSeconds < 60)  return $"{(int)diff.TotalSeconds}s left";
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}m left";
            if (diff.TotalHours < 24)    return $"{(int)diff.TotalHours}h left";
            return $"{(int)diff.TotalDays}d left";
        }
    }
}
