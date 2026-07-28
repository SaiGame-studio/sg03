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
        private VisualElement weekGrid;
        private VisualElement daySelector;
        private VisualElement emptyState;
        private readonly VisualElement tabContent;
        private readonly Button thisWeekTab;
        private readonly Button next7DaysTab;
        private readonly Button next30DaysTab;
        private readonly Button thisMonthTab;
        private readonly DropdownField poolDropdown;
        private readonly Button refreshButton;
        private readonly VisualTreeAsset thisWeekAsset;
        private readonly VisualTreeAsset thisMonthAsset;
        private readonly VisualTreeAsset next7DaysAsset;
        private readonly VisualTreeAsset next30DaysAsset;
        private QuestList[] lists;
        private readonly List<QuestList> poolLists = new List<QuestList>();
        private QuestList selectedPoolList;
        private bool hasCheckedOnOpen;
        private DateRange selectedRange = DateRange.Next7Days;
        private DateTime selectedDay = DateTime.Today;

        // Vietnamese weekday names (Monday=2 … Saturday=7, Sunday=CN)
        private static readonly string[] DayNames = { "CN", "2", "3", "4", "5", "6", "7" };

        private enum DateRange
        {
            ThisWeek,
            Next7Days,
            Next30Days,
            ThisMonth,
        }

        public DailyQuestContentUI(
            VisualElement root,
            VisualTreeAsset thisWeekAsset,
            VisualTreeAsset thisMonthAsset,
            VisualTreeAsset next7DaysAsset,
            VisualTreeAsset next30DaysAsset)
        {
            this.tabContent = root.Q("DailyQuestTabContent");
            this.thisWeekAsset = thisWeekAsset;
            this.thisMonthAsset = thisMonthAsset;
            this.next7DaysAsset = next7DaysAsset;
            this.next30DaysAsset = next30DaysAsset;
            this.thisWeekTab    = root.Q<Button>("ThisWeekTab");
            this.next7DaysTab   = root.Q<Button>("Next7DaysTab");
            this.next30DaysTab  = root.Q<Button>("Next30DaysTab");
            this.thisMonthTab   = root.Q<Button>("ThisMonthTab");
            this.poolDropdown   = root.Q<DropdownField>("PoolDropdown");
            this.refreshButton  = root.Q<Button>("RefreshButton");

            this.thisWeekTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.ThisWeek));
            this.next7DaysTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.Next7Days));
            this.next30DaysTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.Next30Days));
            this.thisMonthTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.ThisMonth));
            this.poolDropdown?.RegisterValueChangedCallback(this.OnPoolChanged);
            this.refreshButton?.RegisterCallback<ClickEvent>(_ => this.RefreshSelectedPool());

            QuestDailyManager manager = UnityEngine.Object.FindFirstObjectByType<QuestDailyManager>(UnityEngine.FindObjectsInactive.Include);
            if (manager == null) { this.ShowSelectedTab(); return; }

            this.lists = manager.QuestLists;
            if (this.lists == null || this.lists.Length == 0) { this.ShowSelectedTab(); return; }

            this.LoadPoolChoices();

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

            if (pendingCount == 0) { this.ShowSelectedTab(); return; }

            foreach (QuestList list in this.lists)
            {
                if (!list.HasData) list.Refresh();
            }

            this.ShowSelectedTab();
        }

        private void OnAnyListUpdated() => this.Render();

        private void LoadPoolChoices()
        {
            if (SaiServer.Instance?.DailyQuest == null)
            {
                this.SetPoolChoices(null);
                return;
            }

            SaiServer.Instance.DailyQuest.GetPools(
                onSuccess: response => this.SetPoolChoices(response?.pools),
                onError: error =>
                {
                    UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Load pools failed: {error}");
                    this.SetPoolChoices(null);
                }
            );
        }

        private void SetPoolChoices(DailyQuestPoolData[] pools)
        {
            if (this.poolDropdown == null || this.lists == null) return;

            QuestList previousSelection = this.selectedPoolList;
            this.poolLists.Clear();
            List<string> choices = new List<string>();

            foreach (QuestList list in this.lists)
            {
                if (list == null) continue;

                DailyQuestPoolData pool = null;
                if (pools != null)
                {
                    foreach (DailyQuestPoolData candidate in pools)
                    {
                        if (candidate != null && (candidate.pool_key == list.PoolKey || candidate.id == list.PoolKey))
                        {
                            pool = candidate;
                            break;
                        }
                    }
                }

                this.poolLists.Add(list);
                choices.Add(pool == null
                    ? list.PoolKey
                    : $"{pool.display_name} ({pool.pool_key})");
            }

            this.poolDropdown.choices = choices;
            int selectedIndex = this.poolLists.IndexOf(previousSelection);
            if (selectedIndex < 0 && this.poolLists.Count > 0) selectedIndex = 0;

            this.selectedPoolList = selectedIndex >= 0 ? this.poolLists[selectedIndex] : null;
            this.poolDropdown.index = selectedIndex;
            this.Render();
        }

        private void OnPoolChanged(ChangeEvent<string> evt)
        {
            int index = this.poolDropdown?.index ?? -1;
            if (index < 0 || index >= this.poolLists.Count) return;

            this.selectedPoolList = this.poolLists[index];
            this.RefreshSelectedPool();
        }

        private void RefreshSelectedPool()
        {
            this.selectedPoolList?.Refresh();
            this.Render();
        }

        private void SelectRange(DateRange range)
        {
            this.selectedRange = range;
            this.UpdateRangeTabStates();
            this.ShowSelectedTab();
        }

        private void ShowSelectedTab()
        {
            if (this.tabContent == null) return;

            this.tabContent.Clear();
            this.weekGrid = null;
            this.daySelector = null;
            this.emptyState = null;

            VisualTreeAsset asset = this.GetSelectedTabAsset();
            if (asset == null) return;

            TemplateContainer content = asset.Instantiate();
            content.style.flexGrow = 1;
            content.style.flexShrink = 1;
            content.style.alignSelf = Align.Stretch;
            this.tabContent.Add(content);

            if (this.selectedRange != DateRange.Next7Days || this.lists == null) return;

            this.weekGrid = content.Q("WeekGrid");
            this.daySelector = content.Q("DaySelector");
            this.emptyState = content.Q("EmptyState");
            this.Render();
        }

        private VisualTreeAsset GetSelectedTabAsset()
        {
            switch (this.selectedRange)
            {
                case DateRange.ThisWeek: return this.thisWeekAsset;
                case DateRange.ThisMonth: return this.thisMonthAsset;
                case DateRange.Next30Days: return this.next30DaysAsset;
                default: return this.next7DaysAsset;
            }
        }

        private void UpdateRangeTabStates()
        {
            this.SetRangeTabState(this.thisWeekTab, this.selectedRange == DateRange.ThisWeek);
            this.SetRangeTabState(this.next7DaysTab, this.selectedRange == DateRange.Next7Days);
            this.SetRangeTabState(this.next30DaysTab, this.selectedRange == DateRange.Next30Days);
            this.SetRangeTabState(this.thisMonthTab, this.selectedRange == DateRange.ThisMonth);
        }

        private void SetRangeTabState(Button tab, bool isActive)
        {
            if (tab == null) return;
            if (isActive) tab.AddToClassList("dq-range-tab--active");
            else tab.RemoveFromClassList("dq-range-tab--active");
        }

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
            foreach (QuestList list in this.GetDisplayedLists())
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

            this.BuildDaySelector();

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

            string selectedDayKey = this.selectedDay.ToString("yyyy-MM-dd");
            byDate.TryGetValue(selectedDayKey, out List<DailyQuestEntryData> selectedEntries);
            if (selectedEntries == null || selectedEntries.Count == 0)
                this.weekGrid.Add(this.BuildNoQuestPlaceholder());
            else
                foreach (DailyQuestEntryData entry in selectedEntries)
                    this.weekGrid.Add(this.BuildQuestItem(entry));

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

                    string assignmentId = entry.assignment?.id;
                    if (string.IsNullOrEmpty(assignmentId)) continue;

                    QuestList ownerList = list;
                    SaiServer.Instance.QuestProgressor.CheckDailyQuestAssignment(
                        assignmentId: assignmentId,
                        onSuccess: _ => ownerList.Refresh(),
                        onError: err => UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Check daily quest failed ({assignmentId}): {err}")
                    );
                }
            }
        }

        private void BuildDaySelector()
        {
            if (this.daySelector == null) return;

            this.daySelector.Clear();
            DateTime today = DateTime.Today;
            if (this.selectedDay < today || this.selectedDay > today.AddDays(6))
                this.selectedDay = today;

            for (int i = 0; i < 7; i++)
            {
                DateTime day = today.AddDays(i);
                Button dayButton = new Button { text = $"{DayNames[(int)day.DayOfWeek]}\n{day:dd/MM}" };
                dayButton.AddToClassList("dq-day-selector__button");
                if (day == today) dayButton.AddToClassList("dq-day-selector__button--today");
                if (day == this.selectedDay) dayButton.AddToClassList("dq-day-selector__button--active");
                dayButton.clicked += () =>
                {
                    this.selectedDay = day;
                    this.Render();
                };
                this.daySelector.Add(dayButton);
            }
        }

        private VisualElement BuildNoQuestPlaceholder()
        {
            VisualElement noQuest = new VisualElement();
            noQuest.AddToClassList("dq-quest-grid__empty");
            noQuest.Add(new Label("No quests for this day.") { name = "NoQuestLabel" });
            return noQuest;
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

            string questId = entry.quest?.id ?? entry.assignment?.quest_definition_id;
            string assignmentId = entry.assignment?.id;
            QuestList ownerList = this.FindOwnerList(questId);
            Button actionButton = this.BuildQuestActionButton(entry.status, assignmentId, questId, ownerList);

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

            // The action is always the last element so it stays below status and timing details.
            if (actionButton != null) bottom.Add(actionButton);

            item.Add(bottom);
            return item;
        }

        private Button BuildQuestActionButton(string status, string assignmentId, string questId, QuestList ownerList)
        {
            if (string.IsNullOrEmpty(assignmentId)) return null;

            string action;
            switch (status)
            {
                case "not_started": action = "Start"; break;
                case "in_progress": action = "Check"; break;
                case "completed": action = "Claim"; break;
                default: return null;
            }

            Button button = new Button { text = action };
            button.AddToClassList("dq-quest-item__action-btn");
            button.AddToClassList($"dq-quest-item__action-btn--{status}");
            button.clicked += () =>
            {
                if (SaiServer.Instance?.QuestProgressor == null) return;

                button.SetEnabled(false);
                if (status == "not_started")
                {
                    SaiServer.Instance.QuestProgressor.StartDailyQuestAssignment(
                        assignmentId: assignmentId,
                        onSuccess: _ => ownerList?.Refresh(),
                        onError: err => this.OnQuestActionFailed(button, action, questId, err)
                    );
                }
                else if (status == "in_progress")
                {
                    SaiServer.Instance.QuestProgressor.CheckDailyQuestAssignment(
                        assignmentId: assignmentId,
                        onSuccess: _ => ownerList?.Refresh(),
                        onError: err => this.OnQuestActionFailed(button, action, questId, err)
                    );
                }
                else
                {
                    SaiServer.Instance.QuestProgressor.ClaimDailyQuestAssignment(
                        assignmentId: assignmentId,
                        onSuccess: _ => ownerList?.Refresh(),
                        onError: err => this.OnQuestActionFailed(button, action, questId, err)
                    );
                }
            };

            return button;
        }

        private void OnQuestActionFailed(Button button, string action, string questId, string error)
        {
            button.SetEnabled(true);
            UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] {action} quest failed ({questId}): {error}");
        }

        private QuestList FindOwnerList(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            if (this.selectedPoolList != null && this.ListContainsQuest(this.selectedPoolList, questId))
                return this.selectedPoolList;
            foreach (QuestList list in this.lists)
            {
                if (this.ListContainsQuest(list, questId)) return list;
            }
            return null;
        }

        private IEnumerable<QuestList> GetDisplayedLists()
        {
            if (this.selectedPoolList != null) yield return this.selectedPoolList;
            else if (this.lists != null)
                foreach (QuestList list in this.lists) yield return list;
        }

        private bool ListContainsQuest(QuestList list, string questId)
        {
            if (list?.Entries == null) return false;
            foreach (DailyQuestEntryData entry in list.Entries)
            {
                string entryQuestId = entry.quest?.id ?? entry.assignment?.quest_definition_id;
                if (entryQuestId == questId) return true;
            }
            return false;
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
