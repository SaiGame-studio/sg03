using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SG03.UI
{
    public class GamePanelUI : SaiBehaviour
    {
        private const string BattleStartScriptName = "battle_start";
        private const string BattleModeNormal = "normal";

        public string PanelId => "Game";

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelAsset;

        [Header("References")]
        [SerializeField] private SaiServer saiServer;
        [SerializeField] private ItemPreset itemPreset;
        [SerializeField] private BattleScript battleScript;
        [SerializeField] private UIDocument uiDocument;

        [Header("Selection")]
        [SerializeField] private PresetData selectedPreset;

        [Header("Scene Navigation")]
        [SerializeField] private string lobbySceneName = "1-lobby";

        private Button btnLoadAddDesk;
        private VisualElement deskTabs;
        private readonly List<Button> deskButtons = new List<Button>();
        private Button btnBackToLobby;
        private Button btnEndTurn;
        private Button btnDrawCard;
        private Button btnAttack;
        private Button btnStartBattle;
        private TextField enemyCodeNameInput;
        private Label cardCountLabel;
        private Label playerNameLabel;
        private VisualElement gameRoot;
        private VisualElement gameViewport;
        private VisualElement root;
        private bool authEventsSubscribed;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadSaiServer();
            this.LoadItemPreset();
            this.LoadBattleScript();
            this.LoadUIDocument();
            this.LoadPanelSettings();
            this.LoadPanelAsset();
        }

        private void LoadSaiServer()
        {
            SaiServer instance = SaiServer.Instance;
            if (instance == null) return;
            if (this.saiServer == instance) return;
            this.UnsubscribeFromAuthEvents();
            this.saiServer = instance;
            this.itemPreset = null;
            this.battleScript = null;
            Debug.LogWarning(this.transform.name + ": LoadSaiServer", this.gameObject);
        }

        private void LoadItemPreset()
        {
            if (this.saiServer == null) return;
            if (this.HasValidItemPresetReference()) return;
            this.itemPreset = this.saiServer.ItemPreset;
            if (this.itemPreset == null) this.itemPreset = this.saiServer.GetComponentInChildren<ItemPreset>(true);
            if (this.itemPreset == null) return;
            Debug.LogWarning(this.transform.name + ": LoadItemPreset", this.gameObject);
        }

        private void LoadBattleScript()
        {
            if (this.saiServer == null) return;
            BattleScript serverBattleScript = this.saiServer.BattleScript;
            if (serverBattleScript == null) return;
            if (this.battleScript == serverBattleScript) return;
            this.battleScript = serverBattleScript;
            Debug.LogWarning(this.transform.name + ": LoadBattleScript", this.gameObject);
        }

        private bool HasValidItemPresetReference()
        {
            if (this.itemPreset == null) return false;
            if (this.saiServer == null) return false;
            return this.itemPreset.transform.IsChildOf(this.saiServer.transform);
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
            PanelSettings panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                "Assets/_sg03/UI/LobbyPanelSettings.asset");
            if (panelSettings == null) return;
            this.uiDocument.panelSettings = panelSettings;
            Debug.LogWarning(this.transform.name + ": LoadPanelSettings", this.gameObject);
#endif
        }

        private void LoadPanelAsset()
        {
#if UNITY_EDITOR
            this.LoadPanelAssetReference();
            this.AssignPanelAssetToDocument();
#endif
        }

#if UNITY_EDITOR
        private void LoadPanelAssetReference()
        {
            if (this.panelAsset != null) return;
            this.panelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Game/GamePanel.uxml");
            Debug.LogWarning(this.transform.name + ": LoadPanelAsset", this.gameObject);
        }

        private void AssignPanelAssetToDocument()
        {
            if (this.uiDocument == null) return;
            if (this.uiDocument.visualTreeAsset != null) return;
            if (this.panelAsset == null) return;
            this.uiDocument.visualTreeAsset = this.panelAsset;
            Debug.LogWarning(this.transform.name + ": LoadPanelAssetToUIDocument", this.gameObject);
        }
#endif

        protected override void Start()
        {
            base.Start();
            this.EnsureServiceReferences();
            this.InitializeStandalonePanel();
        }

        private void EnsureServiceReferences()
        {
            this.EnsureSaiServerReference();
            this.EnsureItemPresetReference();
            this.EnsureBattleScriptReference();
            this.SubscribeToAuthEvents();
        }

        private void EnsureSaiServerReference()
        {
            SaiServer instance = SaiServer.Instance;
            if (instance == null) return;
            if (this.saiServer == instance) return;
            this.UnsubscribeFromAuthEvents();
            this.saiServer = instance;
            this.itemPreset = null;
            this.battleScript = null;
        }

        private void EnsureItemPresetReference()
        {
            if (this.saiServer == null) return;
            ItemPreset serverItemPreset = this.saiServer.ItemPreset;
            if (serverItemPreset == null) return;
            if (this.itemPreset == serverItemPreset) return;
            this.itemPreset = serverItemPreset;
        }

        private void EnsureBattleScriptReference()
        {
            if (this.saiServer == null) return;
            BattleScript serverBattleScript = this.saiServer.BattleScript;
            if (serverBattleScript == null) return;
            if (this.battleScript == serverBattleScript) return;
            this.battleScript = serverBattleScript;
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

        private void BindFromRoot(VisualElement panelRoot)
        {
            this.BindTopMenu(panelRoot);
            this.BindBottomMenu(panelRoot);
            this.BindPlayerName(panelRoot);
            this.BindViewport(panelRoot);
            this.SubscribeToAuthEvents();
        }

        private void BindTopMenu(VisualElement panelRoot)
        {
            this.deskTabs = panelRoot.Q("DeskTabs");
            this.btnLoadAddDesk = panelRoot.Q<Button>("BtnLoadAddDesk");
            this.cardCountLabel = panelRoot.Q<Label>("CardCountLabel");
            this.btnLoadAddDesk?.RegisterCallback<ClickEvent>(_ => this.OnLoadAddDeskClicked());
        }

        private void BindBottomMenu(VisualElement panelRoot)
        {
            this.btnBackToLobby = panelRoot.Q<Button>("BtnBackToLobby");
            this.btnEndTurn = panelRoot.Q<Button>("BtnEndTurn");
            this.btnDrawCard = panelRoot.Q<Button>("BtnDrawCard");
            this.btnAttack = panelRoot.Q<Button>("BtnAttack");
            this.enemyCodeNameInput = panelRoot.Q<TextField>("EnemyCodeNameInput");
            this.btnStartBattle = panelRoot.Q<Button>("BtnStartBattle");
            this.btnBackToLobby?.RegisterCallback<ClickEvent>(_ => this.OnBackToLobbyClicked());
            this.btnEndTurn?.RegisterCallback<ClickEvent>(_ => this.OnEndTurnClicked());
            this.btnDrawCard?.RegisterCallback<ClickEvent>(_ => this.OnDrawCardClicked());
            this.btnAttack?.RegisterCallback<ClickEvent>(_ => this.OnAttackClicked());
            this.enemyCodeNameInput?.RegisterValueChangedCallback(_ => this.ResetStartBattleButtonText());
            this.btnStartBattle?.RegisterCallback<ClickEvent>(_ => this.OnStartBattleClicked());
        }

        private void BindPlayerName(VisualElement panelRoot)
        {
            this.playerNameLabel = panelRoot.Q<Label>("PlayerNameLabel");
            this.RefreshPlayerName();
        }

        private void BindViewport(VisualElement panelRoot)
        {
            this.gameRoot = panelRoot.Q("GameRoot");
            this.gameViewport = panelRoot.Q("GameViewport");
            if (this.gameRoot == null) return;
            if (this.gameViewport == null) return;
            _ = new LobbyAspectRatioKeeper(this.gameRoot, this.gameViewport);
        }

        private void OnDeskTabClicked(Button selected)
        {
            if (selected == null) return;
            this.ClearDeskSelection();
            selected.AddToClassList("game-tab--active");
        }

        private void OnPresetDeskTabClicked(PresetData preset, Button selected)
        {
            this.EnsureServiceReferences();
            this.OnDeskTabClicked(selected);
            this.selectedPreset = preset;
            this.ResetStartBattleButtonText();
            if (preset == null) return;
            if (this.itemPreset == null) return;
            if (string.IsNullOrWhiteSpace(preset.id)) return;
            this.SetCardCountLoading();
            this.itemPreset.GetPreset(preset.id, this.OnPresetSlotsLoaded, this.OnPresetSlotsLoadFailed);
        }

        private void ClearDeskSelection()
        {
            foreach (Button deskButton in this.deskButtons)
            {
                deskButton.RemoveFromClassList("game-tab--active");
            }
        }

        private void OnLoadAddDeskClicked()
        {
            this.EnsureServiceReferences();
            if (this.itemPreset == null) return;
            this.SetLoadAddDeskLoading(true);
            this.itemPreset.GetPresets(this.OnPresetsLoaded, this.OnPresetsLoadFailed);
        }

        private void OnPresetsLoaded(PresetResponse response)
        {
            this.SetLoadAddDeskLoading(false);
            this.RenderDeskTabs(response?.containers);
        }

        private void OnPresetsLoadFailed(string error)
        {
            this.SetLoadAddDeskLoading(false);
            this.RenderDeskTabs(null);
        }

        private void SetLoadAddDeskLoading(bool isLoading)
        {
            if (this.btnLoadAddDesk == null) return;
            this.btnLoadAddDesk.SetEnabled(!isLoading);
            this.btnLoadAddDesk.text = isLoading ? "Loading..." : "Load Add Desk";
        }

        private void RenderDeskTabs(PresetData[] presets)
        {
            if (this.deskTabs == null) return;
            this.ClearDeskTabs();
            if (presets == null) return;

            for (int index = 0; index < presets.Length; index++)
            {
                PresetData preset = presets[index];
                if (preset == null) continue;
                this.AddDeskTab(preset, index);
            }
        }

        private void ClearDeskTabs()
        {
            foreach (Button deskButton in this.deskButtons)
            {
                deskButton.RemoveFromHierarchy();
            }

            this.deskButtons.Clear();
        }

        private void AddDeskTab(PresetData preset, int index)
        {
            Button deskButton = new Button();
            deskButton.name = $"preset-desk-tab-{index + 1}";
            deskButton.text = this.GetPresetDisplayName(preset, index);
            deskButton.AddToClassList("game-tab");
            PresetData capturedPreset = preset;
            deskButton.RegisterCallback<ClickEvent>(_ => this.OnPresetDeskTabClicked(capturedPreset, deskButton));
            this.deskButtons.Add(deskButton);
            this.deskTabs.Add(deskButton);
        }

        private string GetPresetDisplayName(PresetData preset, int index)
        {
            if (preset == null) return $"Desk {index + 1}";
            if (!string.IsNullOrWhiteSpace(preset.name)) return preset.name;
            if (preset.definition != null && !string.IsNullOrWhiteSpace(preset.definition.name)) return preset.definition.name;
            return $"Desk {index + 1}";
        }

        private void OnPresetSlotsLoaded(PresetData preset)
        {
            this.selectedPreset = preset;
            this.UpdatePresetInspectorData(preset);
            this.SetCardCount(this.GetFilledSlotCount(preset));
        }

        private void UpdatePresetInspectorData(PresetData updatedPreset)
        {
            if (updatedPreset == null) return;
            if (this.itemPreset == null) return;
            PresetResponse currentPresets = this.itemPreset.CurrentPresets;
            if (currentPresets == null) return;
            if (currentPresets.containers == null) return;

            for (int index = 0; index < currentPresets.containers.Length; index++)
            {
                PresetData preset = currentPresets.containers[index];
                if (preset == null) continue;
                if (preset.id != updatedPreset.id) continue;
                currentPresets.containers[index] = updatedPreset;
                this.MarkItemPresetDirty();
                return;
            }
        }

        private void MarkItemPresetDirty()
        {
#if UNITY_EDITOR
            if (this.itemPreset == null) return;
            UnityEditor.EditorUtility.SetDirty(this.itemPreset);
#endif
        }

        private void OnPresetSlotsLoadFailed(string error)
        {
            this.SetCardCount(0);
        }

        private int GetFilledSlotCount(PresetData preset)
        {
            if (preset == null) return 0;
            if (preset.slots == null) return 0;

            int count = 0;
            foreach (PresetSlotData slot in preset.slots)
            {
                if (slot == null) continue;
                if (string.IsNullOrWhiteSpace(slot.inventory_item_id)) continue;
                count++;
            }

            return count;
        }

        private void SetCardCountLoading()
        {
            if (this.cardCountLabel == null) return;
            this.cardCountLabel.text = "Card Count: ...";
        }

        private void SetCardCount(int count)
        {
            if (this.cardCountLabel == null) return;
            this.cardCountLabel.text = $"Card Count: {count}";
        }

        private void SubscribeToAuthEvents()
        {
            if (this.authEventsSubscribed) return;
            if (this.saiServer?.SaiAuth == null) return;
            this.saiServer.SaiAuth.OnLoginSuccess += this.OnLoginSuccess;
            this.authEventsSubscribed = true;
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (!this.authEventsSubscribed) return;
            if (this.saiServer?.SaiAuth == null) return;
            this.saiServer.SaiAuth.OnLoginSuccess -= this.OnLoginSuccess;
            this.authEventsSubscribed = false;
        }

        private void OnLoginSuccess(LoginResponse response)
        {
            this.RefreshPlayerName();
        }

        private void RefreshPlayerName()
        {
            this.EnsureServiceReferences();
            if (this.playerNameLabel == null) return;
            UserData user = this.saiServer != null ? this.saiServer.CurrentUser : null;
            string displayName = user?.display_name;
            if (string.IsNullOrEmpty(displayName)) displayName = user?.username;
            this.playerNameLabel.text = string.IsNullOrEmpty(displayName) ? "Guest" : displayName;
        }

        protected virtual void OnEndTurnClicked()
        {
        }

        protected virtual void OnDrawCardClicked()
        {
        }

        protected virtual void OnAttackClicked()
        {
        }

        protected virtual void OnStartBattleClicked()
        {
            this.StartBattle();
        }

        private void StartBattle()
        {
            this.EnsureServiceReferences();
            if (!this.CanStartBattle()) return;
            string enemyCodeName = this.GetEnemyCodeName();
            string presetInstanceId = this.selectedPreset.id;
            string requestBody = this.BuildBattleStartRequestBody(enemyCodeName, presetInstanceId);
            this.SetStartBattleLoading(true);
            this.battleScript.RunScript(
                BattleStartScriptName,
                requestBody,
                this.OnBattleStartSucceeded,
                this.OnBattleStartFailed);
        }

        private bool CanStartBattle()
        {
            if (this.selectedPreset == null)
            {
                this.SetStartBattleButtonText("Select Desk");
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.selectedPreset.id))
            {
                this.SetStartBattleButtonText("Select Desk");
                return false;
            }

            if (string.IsNullOrWhiteSpace(this.GetEnemyCodeName()))
            {
                this.SetStartBattleButtonText("Enter Enemy");
                return false;
            }

            if (this.battleScript != null) return true;
            this.SetStartBattleButtonText("No Script");
            return false;
        }

        private string GetEnemyCodeName()
        {
            if (this.enemyCodeNameInput == null) return string.Empty;
            if (this.enemyCodeNameInput.value == null) return string.Empty;
            return this.enemyCodeNameInput.value.Trim();
        }

        private string BuildBattleStartRequestBody(string enemyCodeName, string presetInstanceId)
        {
            BattleStartScriptRequest request = new BattleStartScriptRequest();
            request.payload = new BattleStartPayload();
            request.payload.battle_mode = BattleModeNormal;
            request.payload.enemy_entity_key = enemyCodeName;
            request.payload.preset_instance_id = presetInstanceId;
            return JsonUtility.ToJson(request);
        }

        private void SetStartBattleLoading(bool isLoading)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.SetEnabled(!isLoading);
            this.btnStartBattle.text = isLoading ? "Starting..." : "Start Battle";
        }

        private void SetStartBattleButtonText(string text)
        {
            if (this.btnStartBattle == null) return;
            this.btnStartBattle.text = text;
        }

        private void ResetStartBattleButtonText()
        {
            this.SetStartBattleButtonText("Start Battle");
        }

        private void OnBattleStartSucceeded(string response)
        {
            this.SetStartBattleLoading(false);
            this.SetStartBattleButtonText("Battle Started");
        }

        private void OnBattleStartFailed(string error)
        {
            this.SetStartBattleLoading(false);
            this.SetStartBattleButtonText("Battle Failed");
            Debug.LogWarning(this.transform.name + ": Battle start failed: " + error, this.gameObject);
        }

        protected virtual void OnBackToLobbyClicked()
        {
            if (string.IsNullOrWhiteSpace(this.lobbySceneName)) return;
            SceneManager.LoadScene(this.lobbySceneName);
        }

        protected virtual void OnDestroy()
        {
            this.UnsubscribeFromAuthEvents();
            this.ClearDeskTabs();
        }
    }
}