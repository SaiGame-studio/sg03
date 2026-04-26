using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using SaiGame.Services;
using SG03.Quest;

namespace SG03.UI
{
    // Binds DailyQuestContent.uxml to live data from QuestDailyManager.
    // Displays a 7-day week grid starting from today.
    // If QuestList has no data, calls Refresh() first then renders.
    public class DailyQuestContentUI
    {
        private readonly VisualElement weekGrid;
        private readonly VisualElement emptyState;

        // Vietnamese weekday names (Monday=2 … Saturday=7, Sunday=CN)
        private static readonly string[] DayNames = { "CN", "2", "3", "4", "5", "6", "7" };

        public DailyQuestContentUI(VisualElement root)
        {
            this.weekGrid   = root.Q("WeekGrid");
            this.emptyState = root.Q("EmptyState");

            QuestDailyManager manager = UnityEngine.Object.FindFirstObjectByType<QuestDailyManager>(UnityEngine.FindObjectsInactive.Include);
            if (manager == null) { this.ShowEmpty(); return; }

            QuestList[] lists = manager.QuestLists;
            if (lists == null || lists.Length == 0) { this.ShowEmpty(); return; }

            int pendingCount = 0;
            foreach (QuestList list in lists)
            {
                if (!list.HasData) pendingCount++;
            }

            if (pendingCount == 0) { this.Render(lists); return; }

            int completedCount = 0;
            foreach (QuestList list in lists)
            {
                if (list.HasData) continue;

                QuestList captured = list;
                Action onUpdated = null;
                onUpdated = () =>
                {
                    captured.OnDataUpdated -= onUpdated;
                    completedCount++;
                    if (completedCount >= pendingCount)
                        this.Render(lists);
                };
                captured.OnDataUpdated += onUpdated;
                captured.Refresh();
            }
        }

        private void ShowEmpty()
        {
            if (this.emptyState != null)
                this.emptyState.style.display = DisplayStyle.Flex;
            if (this.weekGrid != null)
                this.weekGrid.style.display = DisplayStyle.None;
        }

        private void Render(QuestList[] lists)
        {
            if (this.weekGrid == null) return;

            // Collect all entries from all lists into a date → entries map.
            Dictionary<string, List<DailyQuestEntryData>> byDate =
                new Dictionary<string, List<DailyQuestEntryData>>();

            int totalLoaded = 0;
            foreach (QuestList list in lists)
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

            if (isToday)
            {
                Label todayBadge = new Label("HÔM NAY");
                todayBadge.AddToClassList("dq-day-card__today-badge");
                header.Add(todayBadge);
            }

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

            Label statusLabel = new Label(this.StatusLabel(entry.status));
            statusLabel.AddToClassList("dq-quest-item__status");
            statusLabel.AddToClassList(this.StatusClass(entry.status));
            item.Add(statusLabel);

            return item;
        }

        private string StatusLabel(string status)
        {
            switch (status)
            {
                case "in_progress": return "▶ Đang làm";
                case "completed":   return "✓ Hoàn thành";
                case "claimed":     return "★ Đã nhận";
                default:            return "Mới";
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
    }
}
