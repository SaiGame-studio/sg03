using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Manages the shared QuestPanel layout.
    // Instantiated by LobbyPanelUI; receives the panel root and the two
    // content VisualTreeAssets (DailyQuestContent / MainQuestContent).
    public class QuestPanelUI
    {
        private readonly Button dailyNavBtn;
        private readonly Button mainNavBtn;
        private readonly Button battlePassNavBtn;
        private readonly VisualElement contentArea;

        private readonly VisualTreeAsset dailyQuestAsset;
        private readonly VisualTreeAsset mainQuestAsset;
        private readonly VisualTreeAsset battlePassAsset;
        private readonly VisualTreeAsset thisWeekAsset;
        private readonly VisualTreeAsset thisMonthAsset;
        private readonly VisualTreeAsset next7DaysAsset;
        private readonly VisualTreeAsset next30DaysAsset;
        private readonly Sprite refreshIcon;
        private DailyQuestContentUI dailyQuestContent;
        private MainQuestContentUI mainQuestContent;
        private BattlePassContentUI battlePassContent;

        public QuestPanelUI(
            VisualElement panelRoot,
            VisualTreeAsset dailyAsset,
            VisualTreeAsset mainAsset,
            VisualTreeAsset battlePassAsset,
            VisualTreeAsset thisWeekAsset,
            VisualTreeAsset thisMonthAsset,
            VisualTreeAsset next7DaysAsset,
            VisualTreeAsset next30DaysAsset,
            Sprite refreshIcon)
        {
            this.dailyQuestAsset = dailyAsset;
            this.mainQuestAsset  = mainAsset;
            this.battlePassAsset = battlePassAsset;
            this.thisWeekAsset = thisWeekAsset;
            this.thisMonthAsset = thisMonthAsset;
            this.next7DaysAsset = next7DaysAsset;
            this.next30DaysAsset = next30DaysAsset;
            this.refreshIcon = refreshIcon;

            this.dailyNavBtn = panelRoot.Q<Button>("DailyQuestNavBtn");
            this.mainNavBtn  = panelRoot.Q<Button>("MainQuestNavBtn");
            this.battlePassNavBtn = panelRoot.Q<Button>("BattlePassNavBtn");
            this.contentArea = panelRoot.Q("QuestContentArea");

            this.dailyNavBtn?.RegisterCallback<ClickEvent>(_ => this.ShowQuest(QuestType.Daily));
            this.mainNavBtn?.RegisterCallback<ClickEvent>(_ => this.ShowQuest(QuestType.Main));
            this.battlePassNavBtn?.RegisterCallback<ClickEvent>(_ => this.ShowQuest(QuestType.BattlePass));
        }

        // Load and display the content for the requested quest type.
        public void ShowQuest(QuestType type)
        {
            // Update sidebar active state
            this.dailyNavBtn?.RemoveFromClassList("quest-nav-btn--active");
            this.mainNavBtn?.RemoveFromClassList("quest-nav-btn--active");
            this.battlePassNavBtn?.RemoveFromClassList("quest-nav-btn--active");

            if (type == QuestType.Daily)
                this.dailyNavBtn?.AddToClassList("quest-nav-btn--active");
            else if (type == QuestType.Main)
                this.mainNavBtn?.AddToClassList("quest-nav-btn--active");
            else
                this.battlePassNavBtn?.AddToClassList("quest-nav-btn--active");

            // Swap content
            if (this.contentArea == null) return;
            this.contentArea.Clear();

            VisualTreeAsset asset = type == QuestType.Daily
                ? this.dailyQuestAsset
                : type == QuestType.Main
                    ? this.mainQuestAsset
                    : this.battlePassAsset;

            if (asset == null) return;

            TemplateContainer content = asset.Instantiate();
            content.style.flexGrow   = 1;
            content.style.flexShrink = 1;
            content.style.alignSelf  = Align.Stretch;
            content.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            this.contentArea.Add(content);

            this.dailyQuestContent = null;
            this.mainQuestContent?.Dispose();
            this.mainQuestContent = null;
            this.battlePassContent?.Dispose();
            this.battlePassContent = null;
            if (type == QuestType.Daily)
                this.dailyQuestContent = new DailyQuestContentUI(
                    content,
                    this.thisWeekAsset,
                    this.thisMonthAsset,
                    this.next7DaysAsset,
                    this.next30DaysAsset,
                    this.refreshIcon);
            else if (type == QuestType.Main)
                this.mainQuestContent = new MainQuestContentUI(content);
            else
                this.battlePassContent = new BattlePassContentUI(content);
        }

        public bool CloseQuestDetailOnEscape()
            => this.dailyQuestContent?.CloseQuestDetailOnEscape()
               ?? this.mainQuestContent?.CloseQuestDetailOnEscape()
               ?? this.battlePassContent?.CloseQuestDetailOnEscape()
               ?? false;
    }

    /// <summary>Converts quest API error payloads into messages suitable for the quest detail UI.</summary>
    internal static class QuestActionErrorFormatter
    {
        [Serializable]
        private class QuestApiError
        {
            public string error;
            public string message;
            public RequiredQuest[] required_quests;
        }

        [Serializable]
        private class RequiredQuest
        {
            public string name;
        }

        public static string Format(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return "The quest action failed.";

            string json = ExtractRawResponse(error);
            try
            {
                QuestApiError response = JsonUtility.FromJson<QuestApiError>(json);
                if (response == null || (string.IsNullOrWhiteSpace(response.error) && string.IsNullOrWhiteSpace(response.message)))
                    return error;

                StringBuilder message = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(response.error)) message.Append(response.error);
                if (!string.IsNullOrWhiteSpace(response.message))
                {
                    if (message.Length > 0) message.AppendLine();
                    message.Append(response.message);
                }

                if (response.required_quests != null)
                {
                    foreach (RequiredQuest quest in response.required_quests)
                    {
                        if (string.IsNullOrWhiteSpace(quest?.name)) continue;
                        if (message.Length > 0) message.AppendLine();
                        message.Append("Required quest: ").Append(quest.name);
                    }
                }

                return message.Length > 0 ? message.ToString() : error;
            }
            catch (Exception)
            {
                return error;
            }
        }

        private static string ExtractRawResponse(string error)
        {
            const string marker = "Raw Data:";
            int markerIndex = error.IndexOf(marker, StringComparison.Ordinal);
            return markerIndex < 0 ? error : error.Substring(markerIndex + marker.Length).Trim();
        }
    }
}
