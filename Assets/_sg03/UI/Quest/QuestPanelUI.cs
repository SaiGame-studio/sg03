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
        private readonly VisualElement contentArea;

        private readonly VisualTreeAsset dailyQuestAsset;
        private readonly VisualTreeAsset mainQuestAsset;
        private readonly VisualTreeAsset thisWeekAsset;
        private readonly VisualTreeAsset thisMonthAsset;
        private readonly VisualTreeAsset next7DaysAsset;
        private readonly VisualTreeAsset next30DaysAsset;
        private DailyQuestContentUI dailyQuestContent;
        private MainQuestContentUI mainQuestContent;

        public QuestPanelUI(
            VisualElement panelRoot,
            VisualTreeAsset dailyAsset,
            VisualTreeAsset mainAsset,
            VisualTreeAsset thisWeekAsset,
            VisualTreeAsset thisMonthAsset,
            VisualTreeAsset next7DaysAsset,
            VisualTreeAsset next30DaysAsset)
        {
            this.dailyQuestAsset = dailyAsset;
            this.mainQuestAsset  = mainAsset;
            this.thisWeekAsset = thisWeekAsset;
            this.thisMonthAsset = thisMonthAsset;
            this.next7DaysAsset = next7DaysAsset;
            this.next30DaysAsset = next30DaysAsset;

            this.dailyNavBtn = panelRoot.Q<Button>("DailyQuestNavBtn");
            this.mainNavBtn  = panelRoot.Q<Button>("MainQuestNavBtn");
            this.contentArea = panelRoot.Q("QuestContentArea");

            this.dailyNavBtn?.RegisterCallback<ClickEvent>(_ => this.ShowQuest(QuestType.Daily));
            this.mainNavBtn?.RegisterCallback<ClickEvent>(_ => this.ShowQuest(QuestType.Main));
        }

        // Load and display the content for the requested quest type.
        public void ShowQuest(QuestType type)
        {
            // Update sidebar active state
            this.dailyNavBtn?.RemoveFromClassList("quest-nav-btn--active");
            this.mainNavBtn?.RemoveFromClassList("quest-nav-btn--active");

            if (type == QuestType.Daily)
                this.dailyNavBtn?.AddToClassList("quest-nav-btn--active");
            else
                this.mainNavBtn?.AddToClassList("quest-nav-btn--active");

            // Swap content
            if (this.contentArea == null) return;
            this.contentArea.Clear();

            VisualTreeAsset asset = type == QuestType.Daily
                ? this.dailyQuestAsset
                : this.mainQuestAsset;

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
            if (type == QuestType.Daily)
                this.dailyQuestContent = new DailyQuestContentUI(
                    content,
                    this.thisWeekAsset,
                    this.thisMonthAsset,
                    this.next7DaysAsset,
                    this.next30DaysAsset);
            else
                this.mainQuestContent = new MainQuestContentUI(content);
        }

        public bool CloseQuestDetailOnEscape()
            => this.dailyQuestContent?.CloseQuestDetailOnEscape()
               ?? this.mainQuestContent?.CloseQuestDetailOnEscape()
               ?? false;
    }
}
