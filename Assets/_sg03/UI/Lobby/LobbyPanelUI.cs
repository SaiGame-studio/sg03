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

        // Provides access to every SaiServer service (Auth, GamerProgress, Shop, …).
        protected SaiServer Server => this.saiServer;

        protected override void LoadComponents()
        {
            base.LoadComponents();

            if (this.saiServer == null)
                this.saiServer = SaiServer.Instance;
        }

        // Top tabs
        private Button homeTab;
        private Button shopTab;
        private Button questTab;
        private Button profileTab;

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
            this.questTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.questTab));
            this.profileTab?.RegisterCallback<ClickEvent>(_ => this.OnTopTabClicked(this.profileTab));

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
