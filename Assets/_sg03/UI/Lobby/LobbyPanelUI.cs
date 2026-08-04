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
        private const string SoulGeneratorItemCode = "soul_generaror";
        private const string SoulItemCode = "soul";

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
        [SerializeField] private VisualTreeAsset thisWeekContentAsset;
        [SerializeField] private VisualTreeAsset thisMonthContentAsset;
        [SerializeField] private VisualTreeAsset next7DaysContentAsset;
        [SerializeField] private VisualTreeAsset next30DaysContentAsset;

        [Header("Mailbox Panel Assets")]
        [SerializeField] private VisualTreeAsset mailboxContentAsset;

        [Header("Inventory Panel Assets")]
        [SerializeField] private VisualTreeAsset inventoryContentAsset;

        [Header("Desk Content")]
        [SerializeField] private DeskContent deskContentBehaviour;

        [Header("Scene Navigation")]
        [SerializeField] private string gameSceneName = "2-game";
        private QuestPanelUI questPanel;

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

        // Soul Energy indicator (shown only when the player owns soul_generaror).
        private VisualElement soulEnergy;
        private Label soulEnergyValue;
        private VisualElement soulEnergyPopup;
        private VisualElement soulEnergyClaimRow;
        private Label soulEnergyClaimValue;
        private Label soulEnergyFullLabel;
        private Label soulEnergyFullValue;
        private ItemGenerator subscribedItemGenerator;
        private InventoryItemData[] soulCurrencyItems = System.Array.Empty<InventoryItemData>();

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
            this.OnQuestTabClicked();
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

            this.btnPlay?.RegisterCallback<ClickEvent>(_ => this.OnPlayClicked());
            this.btnDesk?.RegisterCallback<ClickEvent>(_ => this.OnDeskClicked());
            this.btnInventory?.RegisterCallback<ClickEvent>(_ => this.OnInventoryClicked());
            this.btnMailbox?.RegisterCallback<ClickEvent>(_ => this.OnMailboxClicked());

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

            this.soulEnergy = root.Q("SoulEnergy");
            this.soulEnergyValue = root.Q<Label>("SoulEnergyValue");
            this.soulEnergyPopup = root.Q("SoulEnergyPopup");
            this.soulEnergyClaimRow = root.Q("SoulEnergyClaimRow");
            this.soulEnergyClaimValue = root.Q<Label>("SoulEnergyClaimValue");
            this.soulEnergyFullLabel = root.Q<Label>("SoulEnergyFullLabel");
            this.soulEnergyFullValue = root.Q<Label>("SoulEnergyFullValue");
            this.soulEnergy?.RegisterCallback<PointerEnterEvent>(_ => this.ShowSoulEnergyPopup());
            this.soulEnergy?.RegisterCallback<PointerLeaveEvent>(_ => this.HideSoulEnergyPopup());
            this.soulEnergy?.RegisterCallback<ClickEvent>(_ => this.ClaimSoul());
            this.SubscribeSoulEnergyData();
            this.LoadSoulEnergy();

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
            this.LoadSoulEnergy();
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

        private void SubscribeSoulEnergyData()
        {
            SaiServer activeServer = SaiServer.Instance;
            ItemGenerator itemGenerator = activeServer?.ItemGenerator;

            if (itemGenerator != null)
            {
                itemGenerator.OnGetGeneratorsSuccess += this.OnSoulEnergyGeneratorsUpdated;
                this.subscribedItemGenerator = itemGenerator;
            }
        }

        /// <summary>
        /// Loads the currency and generator data used by the Soul Energy badge.
        /// This is called again after login so the lobby also works when opened directly.
        /// </summary>
        private void LoadSoulEnergy()
        {
            this.RefreshSoulEnergy();

            SaiServer activeServer = SaiServer.Instance;
            if (activeServer == null || !activeServer.IsAuthenticated) return;

            this.LoadSoulCurrencies(activeServer);
            activeServer.ItemGenerator?.GetGenerators();
        }

        /// <summary>
        /// Loads currency items into a private cache so other inventory refreshes cannot
        /// overwrite the Soul Energy count.
        /// </summary>
        private void LoadSoulCurrencies(SaiServer activeServer)
        {
            string endpoint = $"/api/v1/games/{activeServer.GameId}/inventory?limit=1000&offset=0&include_metadata=true&category=currency";
            this.StartCoroutine(activeServer.GetRequest(
                endpoint,
                response =>
                {
                    string sanitized = InventoryJsonHelper.StringifyObjectFields(response);
                    InventoryResponse currencies = JsonUtility.FromJson<InventoryResponse>(sanitized);
                    this.soulCurrencyItems = currencies?.items ?? System.Array.Empty<InventoryItemData>();
                    this.RefreshSoulEnergy();
                },
                _ => { }));
        }

        private void OnSoulEnergyGeneratorsUpdated(GeneratorsResponse _)
        {
            this.RefreshSoulEnergy();
        }

        private void RefreshSoulEnergy()
        {
            if (this.soulEnergy == null) return;

            InventoryItemData[] items = this.soulCurrencyItems;
            GeneratorData[] generators = SaiServer.Instance?.ItemGenerator?.CurrentGenerators?.generators;
            GeneratorData soulGenerator = this.FindSoulGenerator(generators, items);
            if (soulGenerator == null)
            {
                this.soulEnergy.style.display = DisplayStyle.None;
                return;
            }

            int current = GetItemQuantity(items, SoulItemCode);
            int collectCap = GetSoulCollectCap(soulGenerator, items);
            this.soulEnergyValue.text = $"{current} / {collectCap}";
            this.soulEnergy.style.display = DisplayStyle.Flex;
        }

        private void ShowSoulEnergyPopup()
        {
            InventoryItemData[] items = this.soulCurrencyItems;
            ItemGenerator itemGenerator = SaiServer.Instance?.ItemGenerator;
            GeneratorData soulGenerator = this.FindSoulGenerator(itemGenerator?.CurrentGenerators?.generators, items);
            if (this.soulEnergyPopup == null || soulGenerator == null) return;

            GeneratorExpectedOutput expectedSoul = GetSoulExpectedOutput(itemGenerator, soulGenerator, items);
            this.soulEnergyClaimValue.text = FormatExpectedSoulAmount(expectedSoul);
            int currentSoul = GetItemQuantity(items, SoulItemCode);
            int collectCap = GetSoulCollectCap(soulGenerator, items);
            bool isFull = SoulEnergyUtility.IsFull(currentSoul, collectCap);
            this.soulEnergyClaimRow.style.display = isFull ? DisplayStyle.None : DisplayStyle.Flex;
            this.soulEnergyFullLabel.text = isFull ? "Already full" : "Full in";
            this.soulEnergyFullValue.text = isFull
                ? string.Empty
                : itemGenerator.GetGeneratorTimeUntilFull(soulGenerator.inventory_item_id);
            this.PositionSoulEnergyPopup();
            this.soulEnergyPopup.style.display = DisplayStyle.Flex;
        }

        private void PositionSoulEnergyPopup()
        {
            if (this.soulEnergyPopup == null || this.soulEnergy == null || this.lobbyRoot == null) return;

            Rect soulBounds = this.soulEnergy.worldBound;
            Rect rootBounds = this.lobbyRoot.worldBound;
            this.soulEnergyPopup.style.left = soulBounds.xMax - rootBounds.xMin - 164f;
            this.soulEnergyPopup.style.top = soulBounds.yMax - rootBounds.yMin + 6f;
        }

        private void HideSoulEnergyPopup()
        {
            if (this.soulEnergyPopup != null)
                this.soulEnergyPopup.style.display = DisplayStyle.None;
        }

        private void ClaimSoul()
        {
            ItemGenerator itemGenerator = SaiServer.Instance?.ItemGenerator;
            InventoryItemData[] items = this.soulCurrencyItems;
            GeneratorData soulGenerator = this.FindSoulGenerator(itemGenerator?.CurrentGenerators?.generators, items);
            if (itemGenerator == null || soulGenerator == null || soulGenerator.GetCurrentPendingUnits() <= 0) return;

            this.soulEnergy?.SetEnabled(false);
            itemGenerator.CollectGenerator(
                soulGenerator.inventory_item_id,
                onSuccess: _ =>
                {
                    this.soulEnergy?.SetEnabled(true);
                    this.LoadSoulEnergy();
                },
                onError: _ => this.soulEnergy?.SetEnabled(true));
        }

        private GeneratorData FindSoulGenerator(GeneratorData[] generators, InventoryItemData[] items)
        {
            if (generators == null) return null;

            foreach (GeneratorData generator in generators)
            {
                if (generator == null) continue;
                if (string.Equals(generator.definition?.item_code, SoulGeneratorItemCode, System.StringComparison.OrdinalIgnoreCase))
                    return generator;

                foreach (InventoryItemData item in items ?? System.Array.Empty<InventoryItemData>())
                {
                    if (item?.id != generator.inventory_item_id) continue;
                    if (string.Equals(item.definition?.item_code, SoulGeneratorItemCode, System.StringComparison.OrdinalIgnoreCase))
                        return generator;
                }
            }

            return null;
        }

        private static int GetItemQuantity(InventoryItemData[] items, string itemCode)
        {
            int quantity = 0;
            foreach (InventoryItemData item in items ?? System.Array.Empty<InventoryItemData>())
            {
                if (string.Equals(item?.definition?.item_code, itemCode, System.StringComparison.OrdinalIgnoreCase))
                    quantity += item.quantity;
            }

            return quantity;
        }

        private static int GetSoulCollectCap(GeneratorData generator, InventoryItemData[] items)
        {
            string soulDefinitionId = null;
            foreach (InventoryItemData item in items ?? System.Array.Empty<InventoryItemData>())
            {
                if (string.Equals(item?.definition?.item_code, SoulItemCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    soulDefinitionId = item.item_definition_id;
                    break;
                }
            }

            foreach (GeneratorOutputPool output in generator.output_pool ?? System.Array.Empty<GeneratorOutputPool>())
            {
                if (output == null) continue;
                if (string.IsNullOrEmpty(soulDefinitionId) || output.item_definition_id == soulDefinitionId)
                    return output.collect_cap;
            }

            return 0;
        }

        private static GeneratorExpectedOutput GetSoulExpectedOutput(
            ItemGenerator itemGenerator,
            GeneratorData generator,
            InventoryItemData[] items)
        {
            if (itemGenerator == null || generator == null) return null;

            string soulDefinitionId = null;
            foreach (InventoryItemData item in items ?? System.Array.Empty<InventoryItemData>())
            {
                if (string.Equals(item?.definition?.item_code, SoulItemCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    soulDefinitionId = item.item_definition_id;
                    break;
                }
            }

            foreach (GeneratorExpectedOutput output in itemGenerator.GetGeneratorExpectedOutput(generator.inventory_item_id) ?? System.Array.Empty<GeneratorExpectedOutput>())
            {
                if (string.IsNullOrEmpty(soulDefinitionId) || output.item_definition_id == soulDefinitionId)
                    return output;
            }

            return null;
        }

        private static string FormatExpectedSoulAmount(GeneratorExpectedOutput expectedSoul)
        {
            if (expectedSoul == null) return "0";
            return expectedSoul.expected_min == expectedSoul.expected_max
                ? expectedSoul.expected_min.ToString()
                : $"{expectedSoul.expected_min}–{expectedSoul.expected_max}";
        }

        private void OnLogoutClicked()
        {
            SaiAuth auth = this.GetSaiAuth();
            if (auth == null) return;

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
            foreach (Button tab in new[] { this.homeTab, this.shopTab, this.questTab })
            {
                if (tab == null) continue;
                tab.RemoveFromClassList("lobby-tab--active");
            }

            selected.AddToClassList("lobby-tab--active");
        }

        private void OnQuestTabClicked()
        {
            this.OnTopTabClicked(this.questTab);
            this.LoadQuestPanel(QuestType.Main);
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
            this.UnsubscribeFromLogout();
            this.UnsubscribeFromSoulEnergyData();
        }

        private void UnsubscribeFromSoulEnergyData()
        {
            if (this.subscribedItemGenerator != null)
            {
                this.subscribedItemGenerator.OnGetGeneratorsSuccess -= this.OnSoulEnergyGeneratorsUpdated;
                this.subscribedItemGenerator = null;
            }
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

    /// <summary>
    /// Shared capacity rules for Soul Energy displays and interactions.
    /// </summary>
    public static class SoulEnergyUtility
    {
        /// <summary>
        /// Returns true when the current Soul quantity has reached its collect cap.
        /// A cap of zero means unlimited, so it can never be full.
        /// </summary>
        public static bool IsFull(int currentCount, int collectCap)
        {
            return collectCap > 0 && currentCount >= collectCap;
        }
    }
}
