using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using SaiGame.Services;

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
        [SerializeField] private CurrencyWallet currencyWallet;

        [Header("Quest Panel Assets")]
        [SerializeField] private VisualTreeAsset questPanelAsset;
        [SerializeField] private VisualTreeAsset dailyQuestContentAsset;
        [SerializeField] private VisualTreeAsset mainQuestContentAsset;
        [SerializeField] private VisualTreeAsset thisWeekContentAsset;
        [SerializeField] private VisualTreeAsset thisMonthContentAsset;
        [SerializeField] private VisualTreeAsset next7DaysContentAsset;
        [SerializeField] private VisualTreeAsset next30DaysContentAsset;

        [Header("Shop Panel Assets")]
        [SerializeField] private VisualTreeAsset shopPanelAsset;

        [Header("Mailbox Panel Assets")]
        [SerializeField] private VisualTreeAsset mailboxContentAsset;

        [Header("Inventory Panel Assets")]
        [SerializeField] private VisualTreeAsset inventoryContentAsset;

        [Header("Desk Content")]
        [SerializeField] private DeskContent deskContentBehaviour;

        [Header("Scene Navigation")]
        [SerializeField] private string gameSceneName = "2-game";
        private QuestPanelUI questPanel;
        private ShopPanelUI shopPanel;

        // Provides access to every SaiServer service (Auth, GamerProgress, Shop, …).
        protected SaiServer Server => this.saiServer;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadSaiServer();
            this.LoadUIDocument();
            this.LoadCurrencyWallet();
            this.LoadPanelSettings();
            this.LoadPanelAsset();
            this.LoadQuestPanelAsset();
            this.LoadShopPanelAsset();
            this.LoadDailyQuestContentAsset();
            this.LoadMainQuestContentAsset();
            this.LoadDailyQuestTabContentAssets();
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

        // Bottom buttons
        private Button btnPlay;
        private Button btnDesk;
        private Button btnInventory;
        private Button btnMailbox;

        // Player name label (top-right of TopMenu)
        private Label playerNameLabel;
        private Button btnLogout;
        private Button btnQuitGame;
        private Button btnCancelQuit;
        private Button btnConfirmQuit;
        private VisualElement quitConfirmOverlay;
        private SaiAuth subscribedAuth;
        private SaiAuth logoutAuth;

        private SoulEnergyUI soulEnergyUI;

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
            this.currencyWallet?.Refresh();
            this.OnQuestTabClicked();
        }

        private void LoadCurrencyWallet()
        {
            if (this.currencyWallet != null) return;
            this.currencyWallet = this.GetComponent<CurrencyWallet>();
            if (this.currencyWallet == null)
                this.currencyWallet = this.gameObject.AddComponent<CurrencyWallet>();
        }

        private void LoadShopPanelAsset()
        {
            if (this.shopPanelAsset != null) return;
#if UNITY_EDITOR
            this.shopPanelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Shop/ShopPanel.uxml");
            Debug.LogWarning(this.transform.name + ": LoadShopPanelAsset", this.gameObject);
#endif
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
            this.shopTab?.RegisterCallback<ClickEvent>(_ => this.OnShopTabClicked());

            // Quest tab opens the panel with Main Quest selected.
            if (this.questTab != null)
            {
                this.questTab.RegisterCallback<ClickEvent>(_ => this.OnQuestTabClicked());
            }

            // Bottom buttons
            this.btnPlay      = root.Q<Button>("BtnPlay");
            this.btnDesk      = root.Q<Button>("BtnDesk");
            this.btnInventory = root.Q<Button>("BtnInventory");
            this.btnMailbox   = root.Q<Button>("BtnMailbox");

            this.btnPlay?.RegisterCallback<ClickEvent>(_ => this.OnBottomButtonClicked(this.btnPlay, this.OnPlayClicked));
            this.btnDesk?.RegisterCallback<ClickEvent>(_ => this.OnBottomButtonClicked(this.btnDesk, this.OnDeskClicked));
            this.btnInventory?.RegisterCallback<ClickEvent>(_ => this.OnBottomButtonClicked(this.btnInventory, this.OnInventoryClicked));
            this.btnMailbox?.RegisterCallback<ClickEvent>(_ => this.OnBottomButtonClicked(this.btnMailbox, this.OnMailboxClicked));

            // Player name (top-right)
            this.playerNameLabel = root.Q<Label>("PlayerNameLabel");
            this.btnLogout = root.Q<Button>("BtnLogout");
            this.btnQuitGame = root.Q<Button>("BtnQuitGame");
            this.btnCancelQuit = root.Q<Button>("BtnCancelQuit");
            this.btnConfirmQuit = root.Q<Button>("BtnConfirmQuit");
            this.quitConfirmOverlay = root.Q("QuitConfirmOverlay");
            this.btnLogout?.RegisterCallback<ClickEvent>(_ => this.OnLogoutClicked());
            this.btnQuitGame?.RegisterCallback<ClickEvent>(_ => this.ShowQuitConfirmation());
            this.btnCancelQuit?.RegisterCallback<ClickEvent>(_ => this.HideQuitConfirmation());
            this.btnConfirmQuit?.RegisterCallback<ClickEvent>(_ => this.QuitGame());
            this.RefreshPlayerName();

            this.soulEnergyUI?.Dispose();
            this.soulEnergyUI = new SoulEnergyUI(this, root, this.currencyWallet);
            this.soulEnergyUI.Initialize();

            // Subscribe so name updates if lobby is loaded before login completes
            SaiAuth auth = this.GetSaiAuth();
            if (auth != null)
            {
                auth.OnLoginSuccess += this.OnLoginSuccess;
                this.subscribedAuth = auth;
            }

            // Content area
            this.contentArea = root.Q("ContentArea");

            // Enforce 16:9 aspect ratio with letterbox/pillarbox
            this.lobbyRoot     = root.Q("LobbyRoot");
            this.lobbyViewport = root.Q("LobbyViewport");
            if (this.lobbyRoot != null && this.lobbyViewport != null)
                new LobbyAspectRatioKeeper(this.lobbyRoot, this.lobbyViewport);
        }

        private void OnLoginSuccess(LoginResponse response)
        {
            this.SetPlayerName(response?.user);
            this.soulEnergyUI?.Load();
            this.currencyWallet?.Refresh();
        }

        private void RefreshPlayerName()
        {
            UserData user = this.GetSaiAuth()?.CurrentUser;
            this.SetPlayerName(user);
        }

        private SaiAuth GetSaiAuth()
        {
            // SaiServer is persistent. The serialized scene reference can point to a
            // duplicate that Unity destroys when moving from login to lobby.
            SaiServer activeServer = SaiServer.Instance;
            return activeServer != null ? activeServer.SaiAuth : this.saiServer?.SaiAuth;
        }

        private void SetPlayerName(UserData user)
        {
            if (this.playerNameLabel == null) return;
            string name = user?.display_name;
            if (string.IsNullOrEmpty(name)) name = user?.username;
            this.playerNameLabel.text = string.IsNullOrEmpty(name) ? "👤 Guest" : $"👤 {name}";
        }


        private void OnLogoutClicked()
        {
            SaiAuth auth = this.GetSaiAuth();
            if (auth == null) return;

            this.currencyWallet?.Deactivate();
            this.btnLogout?.SetEnabled(false);
            this.logoutAuth = auth;
            auth.OnLogoutSuccess += this.OnLogoutFinished;
            auth.OnLogoutFailure += this.OnLogoutFailed;
            auth.Logout();
        }

        private void OnLogoutFinished()
        {
            this.UnsubscribeFromLogout();
            SceneManager.LoadScene("0-login");
        }

        private void OnLogoutFailed(string _)
        {
            // SaiAuth clears local credentials even when the server request fails.
            this.OnLogoutFinished();
        }

        private void ShowQuitConfirmation()
        {
            if (this.quitConfirmOverlay != null)
                this.quitConfirmOverlay.style.display = DisplayStyle.Flex;
        }

        private void HideQuitConfirmation()
        {
            if (this.quitConfirmOverlay != null)
                this.quitConfirmOverlay.style.display = DisplayStyle.None;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------
        //  Top tab selection
        // ------------------------------------------------------------------
        private void OnTopTabClicked(Button selected)
        {
            this.ClearBottomButtonSelection();
            foreach (Button tab in new[] { this.homeTab, this.shopTab, this.questTab })
            {
                if (tab == null) continue;
                tab.RemoveFromClassList("lobby-tab--active");
            }

            selected.AddToClassList("lobby-tab--active");
        }

        private void OnBottomButtonClicked(Button selected, System.Action action)
        {
            foreach (Button button in new[] { this.btnPlay, this.btnDesk, this.btnInventory, this.btnMailbox })
                button?.RemoveFromClassList("lobby-bottom-btn--active");

            selected?.AddToClassList("lobby-bottom-btn--active");
            foreach (Button tab in new[] { this.homeTab, this.shopTab, this.questTab })
                tab?.RemoveFromClassList("lobby-tab--active");

            action?.Invoke();
        }

        private void ClearBottomButtonSelection()
        {
            foreach (Button button in new[] { this.btnPlay, this.btnDesk, this.btnInventory, this.btnMailbox })
                button?.RemoveFromClassList("lobby-bottom-btn--active");
        }

        private void OnQuestTabClicked()
        {
            this.OnTopTabClicked(this.questTab);
            this.LoadQuestPanel(QuestType.Main);
        }

        private void OnShopTabClicked()
        {
            this.OnTopTabClicked(this.shopTab);
            this.LoadShopPanel();
        }

        private void LoadDailyQuestTabContentAssets()
        {
#if UNITY_EDITOR
            if (this.thisWeekContentAsset == null)
                this.thisWeekContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/_sg03/UI/Quest/DailyQuest/ThisWeekContent.uxml");
            if (this.thisMonthContentAsset == null)
                this.thisMonthContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/_sg03/UI/Quest/DailyQuest/ThisMonthContent.uxml");
            if (this.next7DaysContentAsset == null)
                this.next7DaysContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/_sg03/UI/Quest/DailyQuest/Next7DaysContent.uxml");
            if (this.next30DaysContentAsset == null)
                this.next30DaysContentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/_sg03/UI/Quest/DailyQuest/Next30DaysContent.uxml");
#endif
        }

        private void LoadQuestPanel(QuestType type)
        {
            if (this.contentArea == null || this.questPanelAsset == null) return;

            this.DisposeShopPanel();
            this.contentArea.Clear();
            TemplateContainer panelRoot = this.questPanelAsset.Instantiate();

            // TemplateContainer must stretch to fill ContentArea entirely.
            panelRoot.style.flexGrow   = 1;
            panelRoot.style.flexShrink = 1;
            panelRoot.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.height     = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.alignSelf  = Align.Stretch;

            this.contentArea.Add(panelRoot);

            this.questPanel = new QuestPanelUI(
                panelRoot,
                this.dailyQuestContentAsset,
                this.mainQuestContentAsset,
                this.thisWeekContentAsset,
                this.thisMonthContentAsset,
                this.next7DaysContentAsset,
                this.next30DaysContentAsset);

            this.questPanel.ShowQuest(type);
        }

        private void LoadShopPanel()
        {
            if (this.contentArea == null || this.shopPanelAsset == null) return;

            this.DisposeShopPanel();
            this.contentArea.Clear();
            TemplateContainer panelRoot = this.shopPanelAsset.Instantiate();
            panelRoot.style.flexGrow = 1;
            panelRoot.style.flexShrink = 1;
            panelRoot.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.alignSelf = Align.Stretch;
            this.contentArea.Add(panelRoot);

            this.currencyWallet?.Refresh();
            this.shopPanel = new ShopPanelUI(panelRoot, SaiServer.Instance?.Shop, this.currencyWallet);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                this.questPanel?.CloseQuestDetailOnEscape();
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

            this.DisposeShopPanel();
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
        protected virtual void OnDeskClicked()
        {
            if (this.contentArea == null || this.deskContentBehaviour == null) return;

            this.DisposeShopPanel();
            this.contentArea.Clear();
            this.deskContentBehaviour.Show(this.contentArea);
        }

        protected virtual void OnMailboxClicked()
        {
            if (this.contentArea == null || this.mailboxContentAsset == null) return;

            this.DisposeShopPanel();
            this.contentArea.Clear();
            TemplateContainer content = this.mailboxContentAsset.Instantiate();
            content.style.flexGrow   = 1;
            content.style.flexShrink = 1;
            content.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.height     = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.alignSelf  = Align.Stretch;
            this.contentArea.Add(content);

            new MailboxContentUI(content, () => this.currencyWallet?.Refresh());
        }

        protected virtual void OnDestroy()
        {
            this.soulEnergyUI?.Dispose();
            this.soulEnergyUI = null;
            this.DisposeShopPanel();
            this.UnsubscribeFromLoginSuccess();
            this.UnsubscribeFromLogout();
        }

        private void DisposeShopPanel()
        {
            this.shopPanel?.Dispose();
            this.shopPanel = null;
        }

        private void UnsubscribeFromLoginSuccess()
        {
            if (this.subscribedAuth == null) return;
            this.subscribedAuth.OnLoginSuccess -= this.OnLoginSuccess;
            this.subscribedAuth = null;
        }

        private void UnsubscribeFromLogout()
        {
            if (this.logoutAuth == null) return;
            this.logoutAuth.OnLogoutSuccess -= this.OnLogoutFinished;
            this.logoutAuth.OnLogoutFailure -= this.OnLogoutFailed;
            this.logoutAuth = null;
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
