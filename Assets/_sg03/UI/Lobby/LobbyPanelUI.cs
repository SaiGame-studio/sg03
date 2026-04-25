using UnityEngine;
using UnityEngine.UIElements;
using SaiGame.Services;
using SaiGame.UI;

namespace SG03.UI
{
    // Lobby panel — top menu tabs + bottom navigation bar.
    // Access all SaiServer services via the Server property.
    public class LobbyPanelUI : UIPanelBase
    {
        public override string PanelId => "Lobby";

        [Header("References")]
        [SerializeField] private SaiServer saiServer;
        [SerializeField] private UIDocument uiDocument;

        [Header("Quest Panel Assets")]
        [SerializeField] private VisualTreeAsset questPanelAsset;
        [SerializeField] private VisualTreeAsset dailyQuestContentAsset;
        [SerializeField] private VisualTreeAsset mainQuestContentAsset;

        // Provides access to every SaiServer service (Auth, GamerProgress, Shop, …).
        protected SaiServer Server => this.saiServer;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadSaiServer();
            this.LoadUIDocument();
            this.LoadPanelSettings();
            this.LoadPanelAsset();
            this.LoadQuestPanelAsset();
            this.LoadDailyQuestContentAsset();
            this.LoadMainQuestContentAsset();
        }

        private void LoadSaiServer()
        {
            if (this.saiServer != null) return;
            this.saiServer = SaiServer.Instance;
            Debug.LogWarning(transform.name + ": LoadSaiServer", gameObject);
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            Debug.LogWarning(transform.name + ": LoadUIDocument", gameObject);
        }

        private void LoadPanelSettings()
        {
            if (this.uiDocument == null) return;
            if (this.uiDocument.panelSettings != null) return;
#if UNITY_EDITOR
            PanelSettings ps = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_sg03/UI/LobbyPanelSettings.asset");
            if (ps != null) this.uiDocument.panelSettings = ps;
            Debug.LogWarning(transform.name + ": LoadPanelSettings", gameObject);
#endif
        }

        private void LoadPanelAsset()
        {
#if UNITY_EDITOR
            if (this.panelAsset == null)
            {
                this.panelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/_sg03/UI/Lobby/LobbyPanel.uxml");
                Debug.LogWarning(transform.name + ": LoadPanelAsset", gameObject);
            }

            if (this.uiDocument != null && this.uiDocument.visualTreeAsset == null && this.panelAsset != null)
            {
                this.uiDocument.visualTreeAsset = this.panelAsset;
                Debug.LogWarning(transform.name + ": LoadPanelAsset → UIDocument.visualTreeAsset", gameObject);
            }
#endif
        }

        private void LoadQuestPanelAsset()
        {
            if (this.questPanelAsset != null) return;
#if UNITY_EDITOR
            this.questPanelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Quest/QuestPanel.uxml");
            Debug.LogWarning(transform.name + ": LoadQuestPanelAsset", gameObject);
#endif
        }

        private void LoadDailyQuestContentAsset()
        {
            if (this.dailyQuestContentAsset != null) return;
#if UNITY_EDITOR
            this.dailyQuestContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Quest/DailyQuest/DailyQuestContent.uxml");
            Debug.LogWarning(transform.name + ": LoadDailyQuestContentAsset", gameObject);
#endif
        }

        private void LoadMainQuestContentAsset()
        {
            if (this.mainQuestContentAsset != null) return;
#if UNITY_EDITOR
            this.mainQuestContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Quest/MainQuest/MainQuestContent.uxml");
            Debug.LogWarning(transform.name + ": LoadMainQuestContentAsset", gameObject);
#endif
        }

        // Top tabs
        private Button homeTab;
        private Button shopTab;
        private Button questTab;
        private Button profileTab;

        // Quest submenu
        private VisualElement questTabWrapper;
        private VisualElement questSubmenu;
        private Button dailyQuestMenuBtn;
        private Button mainQuestMenuBtn;
        private IVisualElementScheduledItem hideSubmenuSchedule;

        // Bottom buttons
        private Button btnPlay;
        private Button btnInventory;
        private Button btnFriends;
        private Button btnSettings;

        // Player name label (top-right of TopMenu)
        private Label playerNameLabel;

        // Content area — populate at runtime with sub-views
        protected VisualElement contentArea;

        // Self-initialize when used standalone (UIDocument on same object, no UIRouter).
        protected override void Start()
        {
            base.Start();

            if (this.Root != null) return;

            UIDocument doc = this.GetComponent<UIDocument>();
            if (doc == null) return;

            this.BindFromRoot(doc.rootVisualElement);
        }

        protected override void OnBindElements(VisualElement root)
        {
            this.BindFromRoot(root);
        }

        private void BindFromRoot(VisualElement root)
        {
            // Top tabs
            this.homeTab    = root.Q<Button>("HomeTab");
            this.shopTab    = root.Q<Button>("ShopTab");
            this.questTab   = root.Q<Button>("QuestTab");
            this.profileTab = root.Q<Button>("ProfileTab");

            this.homeTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.homeTab));
            this.shopTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.shopTab));
            this.profileTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.profileTab));

            // Quest tab wrapper — hover shows submenu, click toggles it
            this.questTabWrapper   = root.Q("QuestTabWrapper");
            this.questSubmenu      = root.Q("QuestSubmenu");
            this.dailyQuestMenuBtn = root.Q<Button>("DailyQuestMenuBtn");
            this.mainQuestMenuBtn  = root.Q<Button>("MainQuestMenuBtn");

            if (this.questTab != null)
            {
                this.questTab.RegisterCallback<MouseEnterEvent>(_ => this.ShowQuestSubmenu());
                this.questTab.RegisterCallback<MouseLeaveEvent>(_ => this.ScheduleHideSubmenu());
                this.questTab.RegisterCallback<ClickEvent>(_ => this.ToggleQuestSubmenu());
            }

            if (this.questSubmenu != null)
            {
                this.questSubmenu.RegisterCallback<MouseEnterEvent>(_ => this.CancelScheduledHide());
                this.questSubmenu.RegisterCallback<MouseLeaveEvent>(_ => this.HideQuestSubmenu());
            }

            this.dailyQuestMenuBtn?.RegisterCallback<ClickEvent>(_ => this.OnQuestMenuItemClicked(QuestType.Daily));
            this.mainQuestMenuBtn?.RegisterCallback<ClickEvent>(_ =>  this.OnQuestMenuItemClicked(QuestType.Main));

            // Bottom buttons
            this.btnPlay      = root.Q<Button>("BtnPlay");
            this.btnInventory = root.Q<Button>("BtnInventory");
            this.btnFriends   = root.Q<Button>("BtnFriends");
            this.btnSettings  = root.Q<Button>("BtnSettings");

            this.btnPlay?.RegisterCallback<ClickEvent>(_ => this.OnPlayClicked());
            this.btnInventory?.RegisterCallback<ClickEvent>(_ => this.OnInventoryClicked());
            this.btnFriends?.RegisterCallback<ClickEvent>(_ => this.OnFriendsClicked());
            this.btnSettings?.RegisterCallback<ClickEvent>(_ => this.OnSettingsClicked());

            // Player name (top-right)
            this.playerNameLabel = root.Q<Label>("PlayerNameLabel");
            this.RefreshPlayerName();

            // Subscribe so name updates if lobby is loaded before login completes
            if (this.saiServer?.SaiAuth != null)
                this.saiServer.SaiAuth.OnLoginSuccess += this.OnLoginSuccess;

            // Content area
            this.contentArea = root.Q("ContentArea");
        }

        private void OnLoginSuccess(LoginResponse _) => this.RefreshPlayerName();

        private void RefreshPlayerName()
        {
            if (this.playerNameLabel == null) return;
            UserData user = this.saiServer != null ? this.saiServer.CurrentUser : null;
            string name = user?.display_name;
            if (string.IsNullOrEmpty(name)) name = user?.username;
            this.playerNameLabel.text = string.IsNullOrEmpty(name) ? string.Empty : $"👤 {name}";
        }

        // ------------------------------------------------------------------
        //  Top tab selection
        // ------------------------------------------------------------------
        private void OnTopTabClicked(Button selected)
        {
            foreach (Button tab in new[] { this.homeTab, this.shopTab, this.questTab, this.profileTab })
            {
                if (tab == null) continue;
                tab.RemoveFromClassList("lobby-tab--active");
            }

            selected.AddToClassList("lobby-tab--active");
        }

        // ------------------------------------------------------------------
        //  Quest submenu show / hide
        // ------------------------------------------------------------------
        private void ShowQuestSubmenu()
        {
            this.CancelScheduledHide();
            this.questSubmenu?.RemoveFromClassList("quest-submenu--hidden");
        }

        private void HideQuestSubmenu()
        {
            this.CancelScheduledHide();
            this.questSubmenu?.AddToClassList("quest-submenu--hidden");
        }

        private void ToggleQuestSubmenu()
        {
            if (this.questSubmenu == null) return;

            if (this.questSubmenu.ClassListContains("quest-submenu--hidden"))
                this.ShowQuestSubmenu();
            else
                this.HideQuestSubmenu();
        }

        // Delay hiding to allow mouse to travel from the tab button into the submenu.
        private void ScheduleHideSubmenu()
        {
            this.CancelScheduledHide();

            if (this.questSubmenu == null) return;

            this.hideSubmenuSchedule = this.questSubmenu.schedule
                .Execute(this.HideQuestSubmenu);
            this.hideSubmenuSchedule.ExecuteLater(80);
        }

        private void CancelScheduledHide()
        {
            this.hideSubmenuSchedule?.Pause();
            this.hideSubmenuSchedule = null;
        }

        // ------------------------------------------------------------------
        //  Quest submenu item clicked → load quest panel
        // ------------------------------------------------------------------
        private void OnQuestMenuItemClicked(QuestType type)
        {
            this.HideQuestSubmenu();
            this.OnTopTabClicked(this.questTab);

            // Update submenu item active highlight
            this.dailyQuestMenuBtn?.RemoveFromClassList("quest-submenu__item--active");
            this.mainQuestMenuBtn?.RemoveFromClassList("quest-submenu__item--active");

            if (type == QuestType.Daily)
                this.dailyQuestMenuBtn?.AddToClassList("quest-submenu__item--active");
            else
                this.mainQuestMenuBtn?.AddToClassList("quest-submenu__item--active");

            this.LoadQuestPanel(type);
        }

        private void LoadQuestPanel(QuestType type)
        {
            if (this.contentArea == null || this.questPanelAsset == null) return;

            this.contentArea.Clear();
            TemplateContainer panelRoot = this.questPanelAsset.Instantiate();
            this.contentArea.Add(panelRoot);

            var questPanel = new QuestPanelUI(
                panelRoot,
                this.dailyQuestContentAsset,
                this.mainQuestContentAsset);

            questPanel.ShowQuest(type);
        }

        // ------------------------------------------------------------------
        //  Bottom button handlers — override in subclass or extend here
        // ------------------------------------------------------------------
        protected virtual void OnPlayClicked()      { }
        protected virtual void OnInventoryClicked() { }
        protected virtual void OnFriendsClicked()   { }
        protected virtual void OnSettingsClicked()  { }

        protected virtual void OnDestroy()
        {
            if (this.saiServer?.SaiAuth != null)
                this.saiServer.SaiAuth.OnLoginSuccess -= this.OnLoginSuccess;
        }
    }
}
