using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;
using SaiGame.Services;
using SG03.Quest;
using SG03.UI.Components;

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
        private readonly Button assignAheadButton;
        private readonly Button refreshButton;
        private readonly QuestDetailPanelUI commonQuestDetailPanel;
        private readonly VisualElement questDetailPanel;
        private readonly VisualElement questDetailContent;
        private readonly Button closeQuestDetailButton;
        private readonly Button questDetailStartButton;
        private readonly Button questDetailCheckButton;
        private readonly Button questDetailClaimButton;
        private readonly Label questDetailExpiredMessage;
        private readonly Label questDetailClaimedMessage;
        private readonly Label questDetailUnavailableMessage;
        private readonly VisualElement serverTimeLabel;
        private readonly ServerTimeLabelComponent serverTime;
        private readonly VisualTreeAsset thisWeekAsset;
        private readonly VisualTreeAsset thisMonthAsset;
        private readonly VisualTreeAsset next7DaysAsset;
        private readonly VisualTreeAsset next30DaysAsset;
        private QuestList[] lists;
        private readonly List<QuestList> poolLists = new List<QuestList>();
        private readonly HashSet<QuestList> subscribedLists = new HashSet<QuestList>();
        private readonly Dictionary<QuestList, DailyQuestPoolData> poolDataByList = new Dictionary<QuestList, DailyQuestPoolData>();
        private readonly Dictionary<QuestList, DailyQuestEntryData[]> next7DaysCache = new Dictionary<QuestList, DailyQuestEntryData[]>();
        private readonly Dictionary<QuestList, DailyQuestEntryData[]> next30DaysCache = new Dictionary<QuestList, DailyQuestEntryData[]>();
        private QuestList selectedPoolList;
        private DailyTimeframe thisWeekTimeframe;
        private DailyTimeframeResponse thisWeekResponse;
        private DailyTimeframeResponse thisMonthResponse;
        private VisualElement selectedQuestItem;
        private int questDetailRequestVersion;
        private bool hasCheckedOnOpen;
        private DateRange selectedRange = DateRange.Next7Days;
        private DateTime selectedDay;

        private static readonly string[] DayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

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
            this.assignAheadButton = root.Q<Button>("AssignAheadButton");
            this.refreshButton  = root.Q<Button>("RefreshButton");
            this.commonQuestDetailPanel = new QuestDetailPanelUI(root, this.RefreshSelectedPoolData);
            this.questDetailPanel = root.Q("QuestDetailPanel");
            this.questDetailContent = root.Q("QuestDetailContent");
            this.closeQuestDetailButton = root.Q<Button>("CloseQuestDetailButton");
            this.questDetailStartButton = root.Q<Button>("QuestDetailStartButton");
            this.questDetailCheckButton = root.Q<Button>("QuestDetailCheckButton");
            this.questDetailClaimButton = root.Q<Button>("QuestDetailClaimButton");
            this.questDetailExpiredMessage = root.Q<Label>("QuestDetailExpiredMessage");
            this.questDetailClaimedMessage = root.Q<Label>("QuestDetailClaimedMessage");
            this.questDetailUnavailableMessage = root.Q<Label>("QuestDetailUnavailableMessage");
            this.serverTimeLabel = root.Q("ServerTimeLabel");
            if (this.serverTimeLabel != null)
                this.serverTime = new ServerTimeLabelComponent(this.serverTimeLabel);

            this.thisWeekTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.ThisWeek));
            this.next7DaysTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.Next7Days));
            this.next30DaysTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.Next30Days));
            this.thisMonthTab?.RegisterCallback<ClickEvent>(_ => this.SelectRange(DateRange.ThisMonth));
            this.poolDropdown?.RegisterValueChangedCallback(this.OnPoolChanged);
            this.assignAheadButton?.RegisterCallback<ClickEvent>(_ => this.AssignAhead());
            this.refreshButton?.RegisterCallback<ClickEvent>(_ => this.RefreshSelectedPool());
            this.closeQuestDetailButton?.RegisterCallback<ClickEvent>(_ => this.HideQuestDetail());
            this.UpdateAssignAheadButtonVisibility();
            this.UpdateHeaderAlignment();

            QuestDailyManager manager = UnityEngine.Object.FindFirstObjectByType<QuestDailyManager>(UnityEngine.FindObjectsInactive.Include);
            if (manager == null) { this.ShowSelectedTab(); return; }

            this.lists = manager.QuestLists;
            if (this.lists == null || this.lists.Length == 0) { this.ShowSelectedTab(); return; }

            this.LoadPoolChoices();

            // Subscribe permanently — re-render on every future data update.
            this.SubscribeToLists();

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

        private void LoadPoolChoices(Action onComplete = null)
        {
            if (SaiServer.Instance?.DailyQuest == null)
            {
                this.SetPoolChoices(null);
                onComplete?.Invoke();
                return;
            }

            SaiServer.Instance.DailyQuest.GetPools(
                onSuccess: response =>
                {
                    this.SetPoolChoices(response?.pools);
                    onComplete?.Invoke();
                },
                onError: error =>
                {
                    UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Load pools failed: {error}");
                    this.SetPoolChoices(null);
                    onComplete?.Invoke();
                }
            );
        }

        private void SetPoolChoices(DailyQuestPoolData[] pools)
        {
            if (this.poolDropdown == null) return;

            QuestDailyManager manager = UnityEngine.Object.FindFirstObjectByType<QuestDailyManager>(UnityEngine.FindObjectsInactive.Include);
            if (manager != null) this.lists = manager.QuestLists;
            if (this.lists == null) return;
            this.SubscribeToLists();

            QuestList previousSelection = this.selectedPoolList;
            this.poolLists.Clear();
            this.poolDataByList.Clear();
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
                if (pool != null) this.poolDataByList[list] = pool;
                choices.Add(pool == null
                    ? list.PoolKey
                    : $"{pool.display_name} ({pool.pool_key})");
            }

            this.poolDropdown.choices = choices;
            int selectedIndex = this.poolLists.IndexOf(previousSelection);
            if (selectedIndex < 0 && this.poolLists.Count > 0) selectedIndex = 0;

            this.selectedPoolList = selectedIndex >= 0 ? this.poolLists[selectedIndex] : null;
            this.poolDropdown.index = selectedIndex;
            this.UpdateAssignAheadButtonVisibility();
            this.Render();
        }

        private void SubscribeToLists()
        {
            if (this.lists == null) return;

            foreach (QuestList list in this.lists)
            {
                if (list == null || !this.subscribedLists.Add(list)) continue;
                list.OnDataUpdated += this.OnAnyListUpdated;
            }
        }

        private void OnPoolChanged(ChangeEvent<string> evt)
        {
            int index = this.poolDropdown?.index ?? -1;
            if (index < 0 || index >= this.poolLists.Count) return;

            this.selectedPoolList = this.poolLists[index];
            if (this.selectedRange == DateRange.Next7Days || this.selectedRange == DateRange.Next30Days)
                this.LoadSelectedRange(null, forceRefresh: false);
            else if (this.selectedRange == DateRange.ThisWeek)
                this.LoadThisWeekTimeframe();
            else if (this.selectedRange == DateRange.ThisMonth)
                this.LoadThisMonthTimeframe();
        }

        private void AssignAhead()
        {
            this.LoadSelectedRange(this.assignAheadButton, forceRefresh: true);
        }

        private void LoadSelectedRange(Button triggerButton, bool forceRefresh)
        {
            if (this.selectedRange != DateRange.Next7Days && this.selectedRange != DateRange.Next30Days) return;
            if (this.selectedPoolList == null || SaiServer.Instance?.DailyQuest == null) return;
            if (!this.poolDataByList.TryGetValue(this.selectedPoolList, out DailyQuestPoolData pool)) return;

            QuestList targetList = this.selectedPoolList;
            Dictionary<QuestList, DailyQuestEntryData[]> cache = this.selectedRange == DateRange.Next30Days
                ? this.next30DaysCache
                : this.next7DaysCache;

            if (!forceRefresh && cache.TryGetValue(targetList, out DailyQuestEntryData[] cachedEntries))
            {
                targetList.SetEntries(cachedEntries);
                return;
            }

            int daysAhead = this.selectedRange == DateRange.Next30Days ? 30 : 7;
            triggerButton?.SetEnabled(false);
            SaiServer.Instance.DailyQuest.AssignAhead(
                dqPoolId: pool.id,
                daysAhead: daysAhead,
                onSuccess: response =>
                {
                    triggerButton?.SetEnabled(true);
                    DailyQuestEntryData[] entries = this.FlattenAssignedDays(response?.days);
                    cache[targetList] = entries;
                    targetList.SetEntries(entries);
                },
                onError: error =>
                {
                    triggerButton?.SetEnabled(true);
                    UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Assign ahead failed ({pool.pool_key}): {error}");
                }
            );
        }

        private DailyQuestEntryData[] FlattenAssignedDays(DailyDayData[] days)
        {
            if (days == null) return Array.Empty<DailyQuestEntryData>();

            List<DailyQuestEntryData> entries = new List<DailyQuestEntryData>();
            foreach (DailyDayData day in days)
            {
                if (day?.quests == null) continue;
                entries.AddRange(day.quests);
            }
            return entries.ToArray();
        }

        private void RefreshSelectedPool()
        {
            this.refreshButton?.SetEnabled(false);
            this.LoadPoolChoices(() =>
            {
                this.refreshButton?.SetEnabled(true);
                this.RefreshSelectedPoolData();
            });
        }

        private void RefreshSelectedPoolData()
        {
            if (this.selectedRange == DateRange.ThisWeek)
            {
                this.LoadThisWeekTimeframe();
                return;
            }

            if (this.selectedRange == DateRange.ThisMonth)
            {
                this.LoadThisMonthTimeframe();
                return;
            }

            if (this.selectedRange == DateRange.Next7Days || this.selectedRange == DateRange.Next30Days)
            {
                this.LoadSelectedRange(this.refreshButton, forceRefresh: true);
                return;
            }

            this.selectedPoolList?.Refresh();
            this.Render();
        }

        private void LoadThisWeekTimeframe()
        {
            if (this.selectedPoolList == null) return;

            this.thisWeekTimeframe ??= UnityEngine.Object.FindFirstObjectByType<DailyTimeframe>(UnityEngine.FindObjectsInactive.Include);
            if (this.thisWeekTimeframe == null)
            {
                UnityEngine.Debug.LogWarning("[DailyQuestContentUI] DailyTimeframe was not found.");
                return;
            }

            string poolKey = this.selectedPoolList.PoolKey;
            if (this.poolDataByList.TryGetValue(this.selectedPoolList, out DailyQuestPoolData pool))
                poolKey = pool.pool_key;
            if (string.IsNullOrEmpty(poolKey)) return;

            if (!this.TryGetThisWeekStart(out DateTime start)) return;
            this.thisWeekTimeframe.GetTimeframe(
                requestedPoolKey: poolKey,
                requestedStartDate: start.ToString("yyyy-MM-dd"),
                requestedEndDate: start.AddDays(6).ToString("yyyy-MM-dd"),
                onSuccess: response =>
                {
                    this.thisWeekResponse = response;
                    this.Render();
                },
                onError: error => UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Load this week failed: {error}")
            );
        }

        private bool TryGetThisWeekStart(out DateTime start)
        {
            if (!TryGetServerDate(out DateTime today))
            {
                start = default;
                return false;
            }

            start = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            return true;
        }

        private bool TryGetThisMonthStart(out DateTime start)
        {
            if (!TryGetServerDate(out DateTime today))
            {
                start = default;
                return false;
            }

            start = new DateTime(today.Year, today.Month, 1);
            return true;
        }

        private static bool TryGetServerDate(out DateTime date)
        {
            if (TryGetServerTime(out DateTime serverTime))
            {
                date = serverTime.Date;
                return true;
            }

            date = default;
            return false;
        }

        private static bool TryGetServerTime(out DateTime serverTime)
        {
            SaiServer server = SaiServer.Instance;
            if (server != null && server.HasServerTime)
            {
                serverTime = server.CurrentServerTime;
                return true;
            }

            serverTime = default;
            return false;
        }

        private void LoadThisMonthTimeframe()
        {
            if (this.selectedPoolList == null) return;

            this.thisWeekTimeframe ??= UnityEngine.Object.FindFirstObjectByType<DailyTimeframe>(UnityEngine.FindObjectsInactive.Include);
            if (this.thisWeekTimeframe == null)
            {
                UnityEngine.Debug.LogWarning("[DailyQuestContentUI] DailyTimeframe was not found.");
                return;
            }

            string poolKey = this.selectedPoolList.PoolKey;
            if (this.poolDataByList.TryGetValue(this.selectedPoolList, out DailyQuestPoolData pool))
                poolKey = pool.pool_key;
            if (string.IsNullOrEmpty(poolKey)) return;

            if (!this.TryGetThisMonthStart(out DateTime start)) return;
            DateTime end = start.AddMonths(1).AddDays(-1);
            this.thisWeekTimeframe.GetTimeframe(
                requestedPoolKey: poolKey,
                requestedStartDate: start.ToString("yyyy-MM-dd"),
                requestedEndDate: end.ToString("yyyy-MM-dd"),
                onSuccess: response =>
                {
                    this.thisMonthResponse = response;
                    this.Render();
                },
                onError: error => UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Load this month failed: {error}")
            );
        }

        private void SelectRange(DateRange range)
        {
            this.selectedRange = range;
            this.UpdateRangeTabStates();
            this.UpdateAssignAheadButtonVisibility();
            this.UpdateHeaderAlignment();
            this.ShowSelectedTab();

            if (range == DateRange.ThisWeek)
                this.LoadThisWeekTimeframe();
            if (range == DateRange.ThisMonth)
                this.LoadThisMonthTimeframe();
            if (range == DateRange.Next7Days || range == DateRange.Next30Days)
                this.LoadSelectedRange(null, forceRefresh: false);
        }

        private void UpdateAssignAheadButtonVisibility()
        {
            if (this.assignAheadButton == null) return;
            bool isVisible = this.selectedRange == DateRange.Next7Days || this.selectedRange == DateRange.Next30Days;
            this.assignAheadButton.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            this.assignAheadButton.SetEnabled(isVisible && this.selectedPoolList != null && this.poolDataByList.ContainsKey(this.selectedPoolList));
        }

        private void UpdateHeaderAlignment()
        {
            if (this.poolDropdown == null) return;
            bool alignRight = this.selectedRange == DateRange.ThisWeek || this.selectedRange == DateRange.ThisMonth;
            if (alignRight) this.poolDropdown.AddToClassList("dq-pool-dropdown--right");
            else this.poolDropdown.RemoveFromClassList("dq-pool-dropdown--right");
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

            if (this.lists == null) return;

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
            if (this.selectedRange == DateRange.ThisWeek)
            {
                this.RenderThisWeek();
                return;
            }
            if (this.selectedRange == DateRange.ThisMonth)
            {
                this.RenderThisMonth();
                return;
            }

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
                    string raw = entry.assignment?.available_at;
                    if (string.IsNullOrEmpty(raw)) continue;
                    // Normalize: take only the date part (yyyy-MM-dd) in case server
                    // returns a full ISO datetime string.
                    string date = raw.Length >= 10 ? raw.Substring(0, 10) : raw;
                    if (!byDate.ContainsKey(date))
                        byDate[date] = new List<DailyQuestEntryData>();
                    byDate[date].Add(entry);
                }
            }

            if (!TryGetServerDate(out DateTime today))
            {
                this.ShowEmpty();
                return;
            }

            int displayedDayCount = this.selectedRange == DateRange.Next30Days ? 30 : 7;
            this.RenderDateMap(byDate, totalLoaded, displayedDayCount, today, today);
        }

        private void RenderThisWeek()
        {
            Dictionary<string, List<DailyQuestEntryData>> byDate = new Dictionary<string, List<DailyQuestEntryData>>();
            int totalLoaded = 0;

            if (this.thisWeekResponse?.days != null)
            {
                foreach (DailyDayData day in this.thisWeekResponse.days)
                {
                    if (day?.quests == null) continue;
                    string date = day.date;
                    if (string.IsNullOrEmpty(date)) continue;
                    if (!byDate.ContainsKey(date)) byDate[date] = new List<DailyQuestEntryData>();
                    byDate[date].AddRange(day.quests);
                    totalLoaded += day.quests.Length;
                }
            }

            if (!this.TryGetThisWeekStart(out DateTime start) || !TryGetServerDate(out DateTime today))
            {
                this.ShowEmpty();
                return;
            }

            this.RenderDateMap(byDate, totalLoaded, 7, start, today);
        }

        private void RenderThisMonth()
        {
            Dictionary<string, List<DailyQuestEntryData>> byDate = new Dictionary<string, List<DailyQuestEntryData>>();
            int totalLoaded = 0;

            if (this.thisMonthResponse?.days != null)
            {
                foreach (DailyDayData day in this.thisMonthResponse.days)
                {
                    if (day?.quests == null || string.IsNullOrEmpty(day.date)) continue;
                    if (!byDate.ContainsKey(day.date)) byDate[day.date] = new List<DailyQuestEntryData>();
                    byDate[day.date].AddRange(day.quests);
                    totalLoaded += day.quests.Length;
                }
            }

            if (!this.TryGetThisMonthStart(out DateTime start) || !TryGetServerDate(out DateTime today))
            {
                this.ShowEmpty();
                return;
            }

            this.RenderDateMap(byDate, totalLoaded, DateTime.DaysInMonth(start.Year, start.Month), start, today);
        }

        private void RenderDateMap(
            Dictionary<string, List<DailyQuestEntryData>> byDate,
            int totalLoaded,
            int displayedDayCount,
            DateTime startDay,
            DateTime today)
        {
            this.BuildDaySelector(byDate, displayedDayCount, startDay, today);

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

            if (!TryGetServerDate(out DateTime today)) return;
            string todayKey = today.ToString("yyyy-MM-dd");

            foreach (QuestList list in this.lists)
            {
                if (list?.Entries == null) continue;

                foreach (DailyQuestEntryData entry in list.Entries)
                {
                    if (entry.status != "in_progress") continue;

                    string raw = entry.assignment?.available_at;
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

        private void BuildDaySelector(
            Dictionary<string, List<DailyQuestEntryData>> questsByDate,
            int dayCount,
            DateTime startDay,
            DateTime today)
        {
            if (this.daySelector == null) return;

            this.daySelector.Clear();
            if (this.selectedDay < startDay || this.selectedDay > startDay.AddDays(dayCount - 1))
                this.selectedDay = startDay;

            for (int i = 0; i < dayCount; i++)
            {
                DateTime day = startDay.AddDays(i);
                string dayKey = day.ToString("yyyy-MM-dd");
                questsByDate.TryGetValue(dayKey, out List<DailyQuestEntryData> quests);
                int questCount = quests?.Count ?? 0;
                string questLabel = questCount == 1 ? "1 quest" : $"{questCount} quests";
                Button dayButton = new Button();
                dayButton.AddToClassList("dq-day-selector__button");
                if (i == dayCount - 1) dayButton.AddToClassList("dq-day-selector__button--last");

                VisualElement dayHeader = new VisualElement();
                dayHeader.AddToClassList("dq-day-selector__header");
                Label dayName = new Label(DayNames[(int)day.DayOfWeek]);
                dayName.AddToClassList("dq-day-selector__weekday");
                Label date = new Label(day.ToString("dd/MM"));
                date.AddToClassList("dq-day-selector__date");
                dayHeader.Add(dayName);
                dayHeader.Add(date);
                dayButton.Add(dayHeader);

                Label questCountLabel = new Label(questLabel);
                questCountLabel.AddToClassList("dq-day-selector__quest-count");
                if (questCount == 0) questCountLabel.AddToClassList("dq-day-selector__quest-count--empty");
                dayButton.Add(questCountLabel);

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
            item.RegisterCallback<ClickEvent>(_ => this.ShowQuestDetail(entry, item));

            VisualElement selectionIndicator = new VisualElement();
            selectionIndicator.AddToClassList("dq-quest-item__selection-indicator");
            item.Add(selectionIndicator);

            Label nameLabel = new Label(entry.quest?.name ?? "—");
            nameLabel.AddToClassList("dq-quest-item__name");
            item.Add(nameLabel);

            if (entry.rewards != null && entry.rewards.Length > 0)
                item.Add(this.BuildRewardRow(entry.rewards));

            // Spacer pushes the status and timing details to the bottom of the card.
            VisualElement spacer = new VisualElement();
            spacer.AddToClassList("dq-quest-item__spacer");
            item.Add(spacer);

            // Actions are kept in the Quest Detail panel footer.
            VisualElement bottom = new VisualElement();
            bottom.AddToClassList("dq-quest-item__bottom");

            string availableAt = entry.assignment?.available_at;
            bool availableInFuture = IsInFuture(availableAt);

            string statusText = this.StatusLabel(entry.status);
            if (!string.IsNullOrEmpty(statusText))
            {
                Label statusLabel = new Label(statusText);
                statusLabel.AddToClassList("dq-quest-item__status");
                statusLabel.AddToClassList(this.StatusClass(entry.status));
                bottom.Add(statusLabel);
            }

            string expiresAt    = entry.assignment?.expires_at;
            if (!string.IsNullOrEmpty(availableAt))
            {
                Label startDateLabel = new Label($"Available: {ShortDate(availableAt)}");
                startDateLabel.AddToClassList("dq-quest-item__start-date");
                bottom.Add(startDateLabel);
            }

            if (availableInFuture)
            {
                // Quest hasn't started yet — show when it will begin.
                Label startsLabel = new Label($"Available {TimeIn(availableAt)}");
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

        private void ShowQuestDetail(DailyQuestEntryData entry, VisualElement questItem)
        {
            if (entry?.quest != null)
            {
                this.SetSelectedQuestItem(questItem);
                this.commonQuestDetailPanel.Show(new QuestFlowNode { id = entry.quest.id ?? entry.assignment?.quest_definition_id, title = entry.quest.name, status = entry.status });
                return;
            }
            if (entry == null || this.questDetailPanel == null || this.questDetailContent == null) return;

            int requestVersion = ++this.questDetailRequestVersion;
            if (this.questDetailPanel.ClassListContains("dq-quest-detail-panel--open"))
            {
                this.HideQuestDetail(invalidatePendingRequests: false);
                this.questDetailPanel.schedule.Execute(() =>
                {
                    if (requestVersion == this.questDetailRequestVersion)
                    {
                        this.SetSelectedQuestItem(questItem);
                        this.OpenQuestDetailAndLoadClaim(entry, requestVersion);
                    }
                }).StartingIn(260);
                return;
            }

            this.SetSelectedQuestItem(questItem);
            this.OpenQuestDetailAndLoadClaim(entry, requestVersion);
        }

        private void SetSelectedQuestItem(VisualElement questItem)
        {
            this.selectedQuestItem?.RemoveFromClassList("dq-quest-item--selected");
            this.selectedQuestItem = questItem;
            this.selectedQuestItem?.AddToClassList("dq-quest-item--selected");
        }

        private void OpenQuestDetailAndLoadClaim(DailyQuestEntryData entry, int requestVersion)
        {
            this.OpenQuestDetail(entry);
            this.LoadQuestClaim(entry, requestVersion);
        }

        private void OpenQuestDetail(DailyQuestEntryData entry)
        {
            if (entry == null || this.questDetailPanel == null || this.questDetailContent == null) return;

            this.ConfigureQuestDetailActions(entry);

            this.questDetailContent.Clear();
            this.questDetailContent.Add(this.CreateDetailLabel(entry.quest?.name ?? "Quest", "dq-quest-detail__name"));
            if (!string.IsNullOrEmpty(entry.quest?.description))
                this.questDetailContent.Add(this.CreateDetailLabel(entry.quest.description, "dq-quest-detail__description"));

            this.AddDetailSection("Status");
            string statusLabel = this.StatusLabel(entry.status);
            this.AddDetailRow("Status", string.IsNullOrEmpty(statusLabel) ? entry.status : statusLabel);
            this.AddDetailRow("Quest type", entry.quest?.quest_type);
            this.AddDetailRow("Code", entry.quest?.code_name);
            this.AddDetailRow("Quest ID", entry.quest?.id ?? entry.assignment?.quest_definition_id);
            this.AddConditionDetails(entry.quest?.conditions);

            this.AddDetailSection("Assignment");
            this.AddDetailRow("Assignment ID", entry.assignment?.id);
            this.AddDetailRow("Pool ID", entry.assignment?.pool_id);
            this.AddDetailRow("Available", entry.assignment?.available_at);
            this.AddDetailRow("Expires", entry.assignment?.expires_at);
            this.AddDetailRow("Created", entry.assignment?.created_at);

            this.AddDetailSection("Progress");
            this.AddDetailRow("Progress status", entry.progress?.status);
            this.AddDetailRow("Completed", entry.progress?.completed_at);
            this.AddDetailRow("Claimed", entry.progress?.claimed_at);
            this.AddDetailRow("Reset", entry.progress?.reset_at);
            this.AddDetailRow("Progress data", entry.progress?.progress_data_json);

            this.AddDetailSection("Expected rewards");
            if (entry.rewards == null || entry.rewards.Length == 0)
                this.AddDetailRow("Rewards", "No reward data");
            else
            {
                foreach (DailyRewardData reward in entry.rewards)
                {
                    string quantity = reward.quantity_min == reward.quantity_max
                        ? reward.quantity_min.ToString()
                        : $"{reward.quantity_min}–{reward.quantity_max}";
                    this.AddRewardDetail(
                        reward.reward_type,
                        reward.item_definition_id,
                        quantity,
                        reward.item_definition);
                }
            }

            this.AddClaimedRewardDetails(entry);

            this.questDetailPanel.RemoveFromClassList("dq-quest-detail-panel--hidden");
            this.questDetailPanel.AddToClassList("dq-quest-detail-panel--open");
        }

        private void LoadQuestClaim(DailyQuestEntryData entry, int requestVersion)
        {
            string progressId = entry.progress?.id;
            QuestHistory history = SaiServer.Instance?.QuestHistory;
            if (string.IsNullOrEmpty(progressId) || history == null) return;

            history.GetClaims(
                limit: 50,
                progressId: progressId,
                onSuccess: response =>
                {
                    if (requestVersion != this.questDetailRequestVersion) return;

                    QuestClaimRecord claim = null;
                    if (response?.claims != null)
                    {
                        foreach (QuestClaimRecord candidate in response.claims)
                        {
                            if (candidate?.progress_id == progressId)
                            {
                                claim = candidate;
                                break;
                            }
                        }
                    }

                    if (claim != null) this.OpenQuestClaimDetail(claim, entry);
                },
                onError: error => UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] Load quest claim failed ({progressId}): {error}")
            );
        }

        private void OpenQuestClaimDetail(QuestClaimRecord claim, DailyQuestEntryData entry)
        {
            if (claim == null || this.questDetailContent == null) return;

            QuestDefinitionData quest = claim.quest_definition;
            this.questDetailContent.Clear();
            this.questDetailContent.Add(this.CreateDetailLabel(quest?.name ?? "Quest claim", "dq-quest-detail__name"));
            if (!string.IsNullOrEmpty(quest?.description))
                this.questDetailContent.Add(this.CreateDetailLabel(quest.description, "dq-quest-detail__description"));

            this.AddDetailSection("Claim");
            this.AddDetailRow("Status", "Claimed");
            this.AddDetailRow("Claim ID", claim.id);
            this.AddDetailRow("Progress ID", claim.progress_id);
            this.AddDetailRow("Claimed at", claim.claimed_at);
            this.AddDetailRow("Idempotency key", claim.idempotency_key);

            this.AddDetailSection("Quest");
            this.AddDetailRow("Quest type", quest?.quest_type);
            this.AddDetailRow("Code", quest?.code_name);
            this.AddDetailRow("Quest ID", claim.quest_definition_id);
            this.AddConditionDetails(quest?.conditions);

            this.AddDetailSection("Expected rewards");
            if (quest?.rewards == null || quest.rewards.Length == 0)
                this.AddDetailRow("Rewards", "No reward data");
            else
            {
                foreach (QuestReward reward in quest.rewards)
                {
                    if (reward == null) continue;
                    int min = reward.quantity_min > 0
                        ? reward.quantity_min
                        : reward.amount;
                    int max = reward.quantity_max > 0 ? reward.quantity_max : min;
                    string quantity = min == max ? min.ToString() : $"{min}–{max}";
                    this.AddRewardDetail(
                        reward.reward_type,
                        reward.item_definition_id,
                        quantity,
                        this.FindItemDefinition(entry, reward.item_definition_id));
                }
            }

            this.AddDetailSection("Received rewards");
            if (claim.rewards_granted == null || claim.rewards_granted.Length == 0)
                this.AddDetailRow("Rewards", "No granted reward data");
            else
            {
                foreach (ClaimQuestGrantedReward reward in claim.rewards_granted)
                {
                    if (reward == null) continue;
                    int quantity = reward.quantity > 0 ? reward.quantity : reward.amount;
                    this.AddRewardDetail(
                        reward.reward_type,
                        reward.item_definition_id,
                        quantity.ToString(),
                        this.FindItemDefinition(entry, reward.item_definition_id));
                }
            }
        }

        /// <summary>
        /// Daily assignments include resolved item definitions. Match by ID so the compact
        /// reward objects returned by quest-claims can show readable item information.
        /// </summary>
        private ItemDefinitionData FindItemDefinition(DailyQuestEntryData entry, string itemDefinitionId)
        {
            if (string.IsNullOrEmpty(itemDefinitionId)) return null;

            if (entry?.rewards != null)
            {
                foreach (DailyRewardData reward in entry.rewards)
                {
                    if (reward?.item_definition_id == itemDefinitionId && reward.item_definition != null)
                        return reward.item_definition;
                }
            }

            InventoryItemData[] items = SaiServer.Instance?.PlayerItem?.CurrentInventory?.items;
            if (items == null) return null;

            foreach (InventoryItemData item in items)
            {
                if (item?.item_definition_id == itemDefinitionId && item.definition != null)
                    return item.definition;
            }

            return null;
        }

        private void AddRewardDetail(
            string rewardType,
            string itemDefinitionId,
            string quantity,
            ItemDefinitionData definition)
        {
            if (definition == null)
            {
                this.AddDetailRow(itemDefinitionId ?? rewardType ?? "Reward", quantity);
                return;
            }

            VisualElement card = new VisualElement();
            card.AddToClassList("dq-quest-detail__reward");

            string itemName = !string.IsNullOrEmpty(definition.name)
                ? definition.name
                : (!string.IsNullOrEmpty(definition.item_code) ? definition.item_code : "Item");
            card.Add(this.CreateDetailLabel($"{itemName} x {quantity}", "dq-quest-detail__reward-name"));

            if (!string.IsNullOrEmpty(definition.item_code))
                card.Add(this.CreateDetailLabel($"Code: {definition.item_code}", "dq-quest-detail__reward-info"));

            string classification = "";
            if (!string.IsNullOrEmpty(definition.category)) classification = definition.category;
            if (!string.IsNullOrEmpty(definition.rarity))
                classification = string.IsNullOrEmpty(classification)
                    ? definition.rarity
                    : $"{classification} / {definition.rarity}";
            if (!string.IsNullOrEmpty(classification))
                card.Add(this.CreateDetailLabel(classification, "dq-quest-detail__reward-info"));

            string description = definition.ParsedMetadata?.description;
            if (string.IsNullOrEmpty(description)) description = definition.ParsedMetadata?.flavor_text;
            if (!string.IsNullOrEmpty(description))
                card.Add(this.CreateDetailLabel(description, "dq-quest-detail__reward-info"));

            card.Add(this.CreateDetailLabel($"Quantity: {quantity}", "dq-quest-detail__reward-info"));
            this.questDetailContent.Add(card);
        }

        private void HideQuestDetail()
        {
            this.HideQuestDetail(invalidatePendingRequests: true);
        }

        private void HideQuestDetail(bool invalidatePendingRequests)
        {
            if (this.questDetailPanel == null) return;
            if (invalidatePendingRequests) this.questDetailRequestVersion++;
            this.ClearQuestDetailActions();
            this.SetSelectedQuestItem(null);
            this.questDetailPanel.RemoveFromClassList("dq-quest-detail-panel--open");
            this.questDetailPanel.AddToClassList("dq-quest-detail-panel--hidden");
        }

        public bool CloseQuestDetailOnEscape()
        {
            if (this.commonQuestDetailPanel.CloseOnEscape()) { this.SetSelectedQuestItem(null); return true; }
            if (this.questDetailPanel == null
                || !this.questDetailPanel.ClassListContains("dq-quest-detail-panel--open")) return false;

            this.HideQuestDetail();
            return true;
        }

        private void AddDetailSection(string text)
        {
            this.questDetailContent.Add(this.CreateDetailLabel(text, "dq-quest-detail__section"));
        }

        private void AddConditionDetails(QuestConditions conditions)
        {
            if (conditions?.clauses == null || conditions.clauses.Length == 0) return;

            string operation = string.IsNullOrEmpty(conditions.operator_type)
                ? "AND"
                : conditions.operator_type.ToUpperInvariant();
            this.AddDetailSection($"Conditions · {operation}");

            foreach (QuestClause clause in conditions.clauses)
            {
                if (clause == null) continue;

                VisualElement card = new VisualElement();
                card.AddToClassList("dq-quest-detail__condition");
                string clauseType = string.IsNullOrEmpty(clause.type) ? "Requirement" : clause.type;
                card.Add(this.CreateDetailLabel(clauseType, "dq-quest-detail__condition-type"));
                if (clause.items != null)
                {
                    foreach (QuestClauseItem item in clause.items)
                    {
                        if (item == null) continue;
                        card.Add(this.CreateDetailLabel(
                            $"Item: {item.item_definition_id} × {item.quantity}",
                            "dq-quest-detail__condition-rule"));
                    }
                }

                if (clause.packs != null && !string.IsNullOrEmpty(clause.packs.gacha_pack_id))
                {
                    card.Add(this.CreateDetailLabel(
                        $"Gacha pack: {clause.packs.gacha_pack_id} × {clause.packs.quantity}",
                        "dq-quest-detail__condition-rule"));
                }

                this.questDetailContent.Add(card);
            }
        }

        private void AddClaimedRewardDetails(DailyQuestEntryData entry)
        {
            if (entry.status != "claimed") return;

            ClaimQuestResponse claim = SaiServer.Instance?.QuestProgressor?.LastClaimedQuest;
            string questId = entry.quest?.id ?? entry.assignment?.quest_definition_id;
            if (claim == null || claim.quest_definition_id != questId || claim.rewards_granted == null)
            {
                this.AddDetailSection("Received rewards");
                this.AddDetailRow("Status", "Claimed — actual reward details are unavailable in this session.");
                return;
            }

            this.AddDetailSection("Received rewards");
            this.AddDetailRow("Claimed at", claim.claimed_at);
            foreach (ClaimQuestGrantedReward reward in claim.rewards_granted)
            {
                if (reward == null) continue;
                int quantity = reward.quantity > 0 ? reward.quantity : reward.amount;
                this.AddRewardDetail(
                    reward.reward_type,
                    reward.item_definition_id,
                    quantity.ToString(),
                    this.FindItemDefinition(entry, reward.item_definition_id));
            }
        }

        private void AddDetailRow(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            VisualElement row = new VisualElement();
            row.AddToClassList("dq-quest-detail__row");
            row.Add(this.CreateDetailLabel(key, "dq-quest-detail__key"));
            row.Add(this.CreateDetailLabel(value, "dq-quest-detail__value"));
            this.questDetailContent.Add(row);
        }

        private Label CreateDetailLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private void ConfigureQuestDetailActions(DailyQuestEntryData entry)
        {
            bool isClaimed = entry?.status == "claimed";
            bool isExpired = IsQuestExpired(entry);
            bool isUnavailable = !isClaimed && !isExpired && IsInFuture(entry?.assignment?.available_at);
            this.SetQuestDetailActionState(isExpired, isClaimed, isUnavailable);
            if (isExpired || isClaimed || isUnavailable)
            {
                this.ClearQuestDetailActions();
                return;
            }

            string assignmentId = entry.assignment?.id;
            string questId = entry.quest?.id ?? entry.assignment?.quest_definition_id;
            QuestList ownerList = this.FindOwnerList(questId);
            bool canStart = entry.status == "not_started" && !IsInFuture(entry.assignment?.available_at);
            bool canCheck = entry.status == "in_progress";
            bool canClaim = entry.status == "completed";

            this.ConfigureQuestDetailAction(this.questDetailStartButton, "Start", canStart, entry, assignmentId, questId, ownerList);
            this.ConfigureQuestDetailAction(this.questDetailCheckButton, "Check", canCheck, entry, assignmentId, questId, ownerList);
            this.ConfigureQuestDetailAction(this.questDetailClaimButton, "Claim", canClaim, entry, assignmentId, questId, ownerList);
        }

        private void ConfigureQuestDetailAction(
            Button button,
            string action,
            bool enabled,
            DailyQuestEntryData entry,
            string assignmentId,
            string questId,
            QuestList ownerList)
        {
            if (button == null) return;

            button.clicked -= button.userData as Action;
            Action onClick = () => this.RunQuestDetailAction(button, action, assignmentId, questId, ownerList);
            button.userData = onClick;
            button.clicked += onClick;
            button.SetEnabled(enabled && !string.IsNullOrEmpty(assignmentId));
            button.tooltip = enabled ? action : this.GetQuestActionUnavailableReason(action, entry);
        }

        private string GetQuestActionUnavailableReason(string action, DailyQuestEntryData entry)
        {
            if (action == "Start" && IsInFuture(entry.assignment?.available_at))
                return "Not available yet";
            return $"Quest must be {action.ToLowerInvariant()}able first.";
        }

        private void ClearQuestDetailActions()
        {
            this.ClearQuestDetailAction(this.questDetailStartButton);
            this.ClearQuestDetailAction(this.questDetailCheckButton);
            this.ClearQuestDetailAction(this.questDetailClaimButton);
        }

        private void SetQuestDetailActionState(bool isExpired, bool isClaimed, bool isUnavailable)
        {
            bool hideActions = isExpired || isClaimed || isUnavailable;
            if (this.questDetailStartButton != null)
                this.questDetailStartButton.style.display = hideActions ? DisplayStyle.None : DisplayStyle.Flex;
            if (this.questDetailCheckButton != null)
                this.questDetailCheckButton.style.display = hideActions ? DisplayStyle.None : DisplayStyle.Flex;
            if (this.questDetailClaimButton != null)
                this.questDetailClaimButton.style.display = hideActions ? DisplayStyle.None : DisplayStyle.Flex;
            if (this.questDetailExpiredMessage != null)
                this.questDetailExpiredMessage.style.display = isExpired ? DisplayStyle.Flex : DisplayStyle.None;
            if (this.questDetailClaimedMessage != null)
                this.questDetailClaimedMessage.style.display = isClaimed ? DisplayStyle.Flex : DisplayStyle.None;
            if (this.questDetailUnavailableMessage != null)
                this.questDetailUnavailableMessage.style.display = isUnavailable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static bool IsQuestExpired(DailyQuestEntryData entry)
        {
            // A completed claim remains claimed even after the assignment's expiry time.
            if (entry?.status == "claimed") return false;
            if (entry?.status == "expired") return true;

            string expiresAt = entry?.assignment?.expires_at;
            if (string.IsNullOrEmpty(expiresAt)
                || !DateTime.TryParse(expiresAt, null, DateTimeStyles.RoundtripKind, out DateTime expiration))
                return false;

            return TryGetServerTime(out DateTime serverTime) && expiration <= serverTime;
        }

        private void ClearQuestDetailAction(Button button)
        {
            if (button == null) return;
            button.clicked -= button.userData as Action;
            button.userData = null;
            button.SetEnabled(false);
        }

        private void RunQuestDetailAction(
            Button button,
            string action,
            string assignmentId,
            string questId,
            QuestList ownerList)
        {
            if (SaiServer.Instance == null || string.IsNullOrEmpty(assignmentId)) return;

            this.InvalidateFutureRangeCaches();
            button.SetEnabled(false);
            if (action == "Start")
            {
                QuestActionRequest.RunDailyAssignmentAction(
                    assignmentId, "start",
                    () => this.ReloadOpenQuestDetail(ownerList, assignmentId),
                    err => this.OnQuestActionFailed(button, action, questId, err));
            }
            else if (action == "Check")
            {
                QuestActionRequest.RunDailyAssignmentAction(
                    assignmentId, "check",
                    () => this.ReloadOpenQuestDetail(ownerList, assignmentId),
                    err => this.OnQuestActionFailed(button, action, questId, err));
            }
            else
            {
                QuestActionRequest.RunDailyAssignmentAction(
                    assignmentId, "claim",
                    () => this.ReloadOpenQuestDetail(ownerList, assignmentId),
                    err => this.OnQuestActionFailed(button, action, questId, err));
            }
        }

        private void InvalidateFutureRangeCaches()
        {
            this.next7DaysCache.Clear();
            this.next30DaysCache.Clear();
        }

        private void ReloadOpenQuestDetail(QuestList ownerList, string assignmentId)
        {
            if (ownerList == null || string.IsNullOrEmpty(assignmentId)) return;

            int requestVersion = ++this.questDetailRequestVersion;
            ownerList.Refresh(entries =>
            {
                if (requestVersion != this.questDetailRequestVersion) return;

                DailyQuestEntryData refreshedEntry = this.FindEntryByAssignmentId(entries, assignmentId);
                if (refreshedEntry != null)
                    this.OpenQuestDetailAndLoadClaim(refreshedEntry, requestVersion);
            });
        }

        private DailyQuestEntryData FindEntryByAssignmentId(DailyQuestEntryData[] entries, string assignmentId)
        {
            if (entries == null) return null;

            foreach (DailyQuestEntryData entry in entries)
            {
                if (entry?.assignment?.id == assignmentId) return entry;
            }

            return null;
        }

        private void OnQuestActionFailed(Button button, string action, string questId, string error)
        {
            button.SetEnabled(true);
            this.ShowQuestActionError(error);
            UnityEngine.Debug.LogWarning($"[DailyQuestContentUI] {action} quest failed ({questId}): {error}");
        }

        private void ShowQuestActionError(string error)
        {
            ToastMessage.ShowError(QuestActionErrorFormatter.Format(error), this.questDetailPanel);
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
                case "in_progress": return "In Progress";
                case "completed":   return "Completed";
                case "claimed":     return "Claimed";
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

            if (!TryGetServerTime(out DateTime serverTime)) return string.Empty;
            TimeSpan diff = target - serverTime;

            if (diff.TotalSeconds <= 0) return "now";
            if (diff.TotalSeconds < 60)  return $"in {(int)diff.TotalSeconds}s";
            if (diff.TotalMinutes < 60)  return $"in {(int)diff.TotalMinutes}m";
            if (diff.TotalHours < 24)    return $"in {(int)diff.TotalHours}h";
            return $"in {(int)diff.TotalDays}d";
        }

        private static string ShortDate(string isoTimestamp)
        {
            if (DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind, out DateTime date))
                return date.ToString("dd/MM");

            return isoTimestamp != null && isoTimestamp.Length >= 10
                ? $"{isoTimestamp.Substring(8, 2)}/{isoTimestamp.Substring(5, 2)}"
                : isoTimestamp;
        }

        private static bool IsInFuture(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return false;
            if (!DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind, out DateTime target))
                return false;
            return TryGetServerTime(out DateTime serverTime) && target > serverTime;
        }

        private static string TimeAgo(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return string.Empty;

            if (!DateTime.TryParse(isoTimestamp, null, DateTimeStyles.RoundtripKind, out DateTime target))
                return isoTimestamp;

            if (!TryGetServerTime(out DateTime serverTime)) return string.Empty;
            TimeSpan diff = target - serverTime;

            if (diff.TotalSeconds <= 0) return "Expired";
            if (diff.TotalSeconds < 60)  return $"{(int)diff.TotalSeconds}s left";
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}m left";
            if (diff.TotalHours < 24)    return $"{(int)diff.TotalHours}h left";
            return $"{(int)diff.TotalDays}d left";
        }
    }
}
