using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using SaiGame.Services;
using SG03.UI.Components;

namespace SG03.UI
{
    // Lobby panel — top menu tabs + bottom navigation bar.
    // Access all SaiServer services via the Server property.
    public class LobbyPanelUI : SaiBehaviour
    {
        public string PanelId => "Lobby";

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelAsset;

        [Header("References")]
        [SerializeField] private SaiServer saiServer;
        [SerializeField] private UIDocument uiDocument;

        [Header("Quest Panel Assets")]
        [SerializeField] private VisualTreeAsset questPanelAsset;
        [SerializeField] private VisualTreeAsset dailyQuestContentAsset;
        [SerializeField] private VisualTreeAsset mainQuestContentAsset;

        [Header("Mailbox Panel Assets")]
        [SerializeField] private VisualTreeAsset mailboxContentAsset;

        [Header("Inventory Panel Assets")]
        [SerializeField] private VisualTreeAsset inventoryContentAsset;

        [Header("Desk Content")]
        [SerializeField] private DeskContent deskContentBehaviour;

        [Header("Scene Navigation")]
        [SerializeField] private string gameSceneName = "2-game";

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
            this.LoadMailboxContentAsset();
            this.LoadInventoryContentAsset();
            this.LoadDeskContentBehaviour();
        }

        private void LoadSaiServer()
        {
            if (this.saiServer != null) return;
            this.saiServer = SaiServer.Instance;
            Debug.LogWarning(this.transform.name + ": LoadSaiServer", this.gameObject);
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        private void LoadPanelSettings()
        {
            if (this.uiDocument == null) return;
            if (this.uiDocument.panelSettings != null) return;
#if UNITY_EDITOR
            PanelSettings ps = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_sg03/UI/LobbyPanelSettings.asset");
            if (ps != null) this.uiDocument.panelSettings = ps;
            Debug.LogWarning(this.transform.name + ": LoadPanelSettings", this.gameObject);
#endif
        }

        private void LoadPanelAsset()
        {
#if UNITY_EDITOR
            if (this.panelAsset == null)
            {
                this.panelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/_sg03/UI/Lobby/LobbyPanel.uxml");
                Debug.LogWarning(this.transform.name + ": LoadPanelAsset", this.gameObject);
            }

            if (this.uiDocument != null && this.uiDocument.visualTreeAsset == null && this.panelAsset != null)
            {
                this.uiDocument.visualTreeAsset = this.panelAsset;
                Debug.LogWarning(this.transform.name + ": LoadPanelAsset → UIDocument.visualTreeAsset", this.gameObject);
            }
#endif
        }

        private void LoadQuestPanelAsset()
        {
            if (this.questPanelAsset != null) return;
#if UNITY_EDITOR
            this.questPanelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Quest/QuestPanel.uxml");
            Debug.LogWarning(this.transform.name + ": LoadQuestPanelAsset", this.gameObject);
#endif
        }

        private void LoadDailyQuestContentAsset()
        {
            if (this.dailyQuestContentAsset != null) return;
#if UNITY_EDITOR
            this.dailyQuestContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Quest/DailyQuest/DailyQuestContent.uxml");
            Debug.LogWarning(this.transform.name + ": LoadDailyQuestContentAsset", this.gameObject);
#endif
        }

        private void LoadMainQuestContentAsset()
        {
            if (this.mainQuestContentAsset != null) return;
#if UNITY_EDITOR
            this.mainQuestContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Quest/MainQuest/MainQuestContent.uxml");
            Debug.LogWarning(this.transform.name + ": LoadMainQuestContentAsset", this.gameObject);
#endif
        }

        private void LoadMailboxContentAsset()
        {
            if (this.mailboxContentAsset != null) return;
#if UNITY_EDITOR
            this.mailboxContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Mailbox/MailboxContent.uxml");
            Debug.LogWarning(this.transform.name + ": LoadMailboxContentAsset", this.gameObject);
#endif
        }

        private void LoadInventoryContentAsset()
        {
            if (this.inventoryContentAsset != null) return;
#if UNITY_EDITOR
            this.inventoryContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Inventory/InventoryContent.uxml");
            Debug.LogWarning(this.transform.name + ": LoadInventoryContentAsset", this.gameObject);
#endif
        }

        private void LoadDeskContentBehaviour()
        {
            if (this.deskContentBehaviour != null) return;
            this.deskContentBehaviour = FindFirstObjectByType<DeskContent>(FindObjectsInactive.Include);
            if (this.deskContentBehaviour == null) return;
            this.deskContentBehaviour.OnCardViewerShown  += this.EnterImmersiveMode;
            this.deskContentBehaviour.OnCardViewerHidden += this.ExitImmersiveMode;
            Debug.LogWarning(this.transform.name + ": LoadDeskContentBehaviour", this.gameObject);
        }

        // Top tabs
        private Button homeTab;
        private Button shopTab;
        private Button questTab;

        // Quest popup menu
        private PopupMenu questMenu;
        private QuestType? activeQuestType;

        // Bottom buttons
        private Button btnPlay;
        private Button btnProfile;
        private Button btnDesk;
        private Button btnInventory;
        private Button btnMailbox;

        // Player name label (top-right of TopMenu)
        private Label playerNameLabel;

        // Lobby background elements toggled during immersive mode
        private VisualElement lobbyRoot;
        private VisualElement lobbyViewport;

        // Content area — populate at runtime with sub-views
        protected VisualElement contentArea;
        private VisualElement root;

        protected override void Start()
        {
            base.Start();
            this.InitializeStandalonePanel();
        }

        private void InitializeStandalonePanel()
        {
            if (this.root != null) return;
            if (this.uiDocument == null) return;
            this.BindPanelRoot(this.uiDocument.rootVisualElement);
        }

        private void BindPanelRoot(VisualElement panelRoot)
        {
            if (panelRoot == null) return;
            this.root = panelRoot;
            this.BindFromRoot(this.root);
        }

        private void BindFromRoot(VisualElement root)
        {
            // Top tabs
            this.homeTab    = root.Q<Button>("HomeTab");
            this.shopTab    = root.Q<Button>("ShopTab");
            this.questTab   = root.Q<Button>("QuestTab");
            this.homeTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.homeTab));
            this.shopTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.shopTab));

            // Quest tab — PopupMenu handles all hover/click/close logic
            this.questMenu = new PopupMenu(root);

            if (this.questTab != null)
            {
                this.questTab.RegisterCallback<MouseEnterEvent>(_ =>
                    this.questMenu.Show(this.questTab, this.GetQuestMenuItems()));
                this.questTab.RegisterCallback<ClickEvent>(_ =>
                    this.questMenu.Show(this.questTab, this.GetQuestMenuItems()));
            }

            // Bottom buttons
            this.btnPlay      = root.Q<Button>("BtnPlay");
            this.btnProfile   = root.Q<Button>("BtnProfile");
            this.btnDesk      = root.Q<Button>("BtnDesk");
            this.btnInventory = root.Q<Button>("BtnInventory");
            this.btnMailbox   = root.Q<Button>("BtnMailbox");

            this.btnPlay?.RegisterCallback<ClickEvent>(_ => this.OnPlayClicked());
            this.btnProfile?.RegisterCallback<ClickEvent>(_ => this.OnProfileClicked());
            this.btnDesk?.RegisterCallback<ClickEvent>(_ => this.OnDeskClicked());
            this.btnInventory?.RegisterCallback<ClickEvent>(_ => this.OnInventoryClicked());
            this.btnMailbox?.RegisterCallback<ClickEvent>(_ => this.OnMailboxClicked());

            // Player name (top-right)
            this.playerNameLabel = root.Q<Label>("PlayerNameLabel");
            this.RefreshPlayerName();

            // Subscribe so name updates if lobby is loaded before login completes
            if (this.saiServer?.SaiAuth != null)
                this.saiServer.SaiAuth.OnLoginSuccess += this.OnLoginSuccess;

            // Content area
            this.contentArea = root.Q("ContentArea");

            // Enforce 16:9 aspect ratio with letterbox/pillarbox
            this.lobbyRoot     = root.Q("LobbyRoot");
            this.lobbyViewport = root.Q("LobbyViewport");
            if (this.lobbyRoot != null && this.lobbyViewport != null)
                new LobbyAspectRatioKeeper(this.lobbyRoot, this.lobbyViewport);
        }

        private void OnLoginSuccess(LoginResponse _) => this.RefreshPlayerName();

        private void RefreshPlayerName()
        {
            if (this.playerNameLabel == null) return;
            UserData user = this.saiServer != null ? this.saiServer.CurrentUser : null;
            string name = user?.display_name;
            if (string.IsNullOrEmpty(name)) name = user?.username;
            this.playerNameLabel.text = string.IsNullOrEmpty(name) ? "👤 Guest" : $"👤 {name}";
        }

        // ------------------------------------------------------------------
        //  Top tab selection
        // ------------------------------------------------------------------
        private void OnTopTabClicked(Button selected)
        {
            foreach (Button tab in new[] { this.homeTab, this.shopTab, this.questTab })
            {
                if (tab == null) continue;
                tab.RemoveFromClassList("lobby-tab--active");
            }

            selected.AddToClassList("lobby-tab--active");
        }

        // ------------------------------------------------------------------
        //  Quest popup menu items
        // ------------------------------------------------------------------

        /// <summary>
        /// Build the quest menu item list with up-to-date IsActive states.
        /// Called every time Show/Toggle is invoked so the active highlight
        /// always reflects the currently-loaded quest type.
        /// </summary>
        private PopupMenuItem[] GetQuestMenuItems() => new[]
        {
            new PopupMenuItem
            {
                Label    = "Daily Quest",
                IsActive = this.activeQuestType == QuestType.Daily,
                OnClick  = () => this.OnQuestMenuItemClicked(QuestType.Daily),
            },
            new PopupMenuItem
            {
                Label    = "Main Quest",
                IsActive = this.activeQuestType == QuestType.Main,
                OnClick  = () => this.OnQuestMenuItemClicked(QuestType.Main),
            },
        };

        private void OnQuestMenuItemClicked(QuestType type)
        {
            this.activeQuestType = type;
            this.OnTopTabClicked(this.questTab);
            this.LoadQuestPanel(type);
        }

        private void LoadQuestPanel(QuestType type)
        {
            if (this.contentArea == null || this.questPanelAsset == null) return;

            this.contentArea.Clear();
            TemplateContainer panelRoot = this.questPanelAsset.Instantiate();

            // TemplateContainer must stretch to fill ContentArea entirely.
            panelRoot.style.flexGrow   = 1;
            panelRoot.style.flexShrink = 1;
            panelRoot.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.height     = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.alignSelf  = Align.Stretch;

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
        protected virtual void OnPlayClicked()
        {
            if (string.IsNullOrWhiteSpace(this.gameSceneName)) return;
            SceneManager.LoadScene(this.gameSceneName);
        }
        protected virtual void OnInventoryClicked()
        {
            if (this.contentArea == null || this.inventoryContentAsset == null) return;

            this.contentArea.Clear();
            TemplateContainer content = this.inventoryContentAsset.Instantiate();
            content.style.flexGrow   = 1;
            content.style.flexShrink = 1;
            content.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.height     = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.alignSelf  = Align.Stretch;
            this.contentArea.Add(content);

            new InventoryContentUI(content);
        }
        protected virtual void OnProfileClicked()   { }
        protected virtual void OnDeskClicked()
        {
            if (this.contentArea == null || this.deskContentBehaviour == null) return;

            this.contentArea.Clear();
            this.deskContentBehaviour.Show(this.contentArea);
        }

        protected virtual void OnMailboxClicked()
        {
            if (this.contentArea == null || this.mailboxContentAsset == null) return;

            this.contentArea.Clear();
            TemplateContainer content = this.mailboxContentAsset.Instantiate();
            content.style.flexGrow   = 1;
            content.style.flexShrink = 1;
            content.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.height     = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.alignSelf  = Align.Stretch;
            this.contentArea.Add(content);

            new MailboxContentUI(content);
        }

        protected virtual void OnDestroy()
        {
            this.UnsubscribeFromLoginSuccess();
        }

        private void UnsubscribeFromLoginSuccess()
        {
            if (this.saiServer?.SaiAuth == null) return;
            this.saiServer.SaiAuth.OnLoginSuccess -= this.OnLoginSuccess;
        }

        // ------------------------------------------------------------------
        //  Immersive mode — hides desktop background to reveal 3D scene
        // ------------------------------------------------------------------
        private void EnterImmersiveMode()
        {
            this.lobbyRoot?.AddToClassList("lobby-root--immersive");
            this.lobbyViewport?.AddToClassList("lobby-viewport--immersive");
        }

        private void ExitImmersiveMode()
        {
            this.lobbyRoot?.RemoveFromClassList("lobby-root--immersive");
            this.lobbyViewport?.RemoveFromClassList("lobby-viewport--immersive");
        }
    }
}
