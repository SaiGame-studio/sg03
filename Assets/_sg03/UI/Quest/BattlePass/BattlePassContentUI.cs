using System;
using System.Collections.Generic;
using System.Globalization;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;
using SG03.UI.Components;

namespace SG03.UI
{
    /// <summary>Displays the ordered quest chains assigned to a selected battle pass.</summary>
    public class BattlePassContentUI : IDisposable
    {
        private const float CardWidth = 220f;
        private const float CardHeight = 92f;
        private const float CardGap = 120f;
        private const float RowGap = 132f;
        private const float ChainGap = 88f;
        private const float CanvasPadding = 48f;

        private readonly DropdownField battlePassDropdown;
        private readonly VisualElement sessionSchedule;
        private readonly Label scheduleType;
        private readonly Label scheduleDetails;
        private readonly Label scheduleState;
        private readonly ServerTimeLabelComponent serverTime;
        private readonly Label state;
        private readonly QuestFlowGraph graph;
        private readonly BattlePass battlePass;
        private readonly ChainQuest chainQuest;
        private readonly QuestDetailPanelUI questDetailPanel;
        private readonly VisualElement detailPanel;
        private readonly VisualElement detailContent;
        private readonly Dictionary<string, BattlePassData> battlePassesByLabel = new Dictionary<string, BattlePassData>();
        private readonly Dictionary<string, string> questIdsByGraphNodeId = new Dictionary<string, string>();
        private bool waitingForLogin;
        private int requestVersion;
        private int detailRequestVersion;
        private BattlePassData selectedBattlePass;

        public BattlePassContentUI(VisualElement root)
        {
            this.battlePassDropdown = root.Q<DropdownField>("BattlePassDropdown");
            this.sessionSchedule = root.Q<VisualElement>("BattlePassSessionSchedule");
            this.scheduleType = root.Q<Label>("BattlePassScheduleType");
            this.scheduleDetails = root.Q<Label>("BattlePassScheduleDetails");
            this.scheduleState = root.Q<Label>("BattlePassScheduleState");
            VisualElement serverTimeLabel = root.Q<VisualElement>("BattlePassServerTimeLabel");
            if (serverTimeLabel != null) this.serverTime = new ServerTimeLabelComponent(serverTimeLabel);
            this.state = root.Q<Label>("BattlePassState");
            this.graph = new QuestFlowGraph(root.Q<VisualElement>("BattlePassGraphHost"));
            this.battlePass = SaiServer.Instance?.BattlePass;
            this.chainQuest = SaiServer.Instance?.ChainQuest;
            this.detailPanel = root.Q<VisualElement>("MainQuestDetailPanel");
            this.detailContent = root.Q<VisualElement>("MainQuestDetailContent");

            this.battlePassDropdown?.RegisterValueChangedCallback(this.HandleBattlePassSelectionChanged);
            Button refreshButton = root.Q<Button>("BattlePassRefreshButton");
            new RefreshButtonComponent(refreshButton, null);
            refreshButton?.RegisterCallback<ClickEvent>(_ => this.LoadBattlePasses());
            this.questDetailPanel = new QuestDetailPanelUI(root, this.LoadBattlePasses,
                node => node != null && this.questIdsByGraphNodeId.TryGetValue(node.id, out string questId) ? questId : null,
                () => this.selectedBattlePass?.type_config?.session);
            this.graph.NodeClicked += this.questDetailPanel.Show;
            this.LoadBattlePasses();
        }

        public void Dispose()
        {
            this.serverTime?.Dispose();
            this.requestVersion++;
            this.detailRequestVersion++;
            this.graph.NodeClicked -= this.questDetailPanel.Show;
            if (!this.waitingForLogin || SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.waitingForLogin = false;
        }

        private void LoadBattlePasses()
        {
            if (this.battlePass == null || this.chainQuest == null)
            {
                this.ShowState("Battle Pass or quest-chain service is unavailable.");
                return;
            }

            if (SaiServer.Instance == null || !SaiServer.Instance.IsAuthenticated)
            {
                this.ShowState("Waiting for login before loading battle passes...");
                this.WaitForLogin();
                return;
            }

            this.StopWaitingForLogin();
            int version = ++this.requestVersion;
            this.battlePassDropdown?.SetEnabled(false);
            this.ShowState("Loading battle passes...", clearGraph: false);
            this.battlePass.GetBattlePasses(
                onSuccess: response =>
                {
                    if (version != this.requestVersion) return;
                    this.PopulateBattlePassDropdown(response?.pools);
                },
                onError: error =>
                {
                    if (version != this.requestVersion) return;
                    this.battlePassDropdown?.SetEnabled(true);
                    this.ShowState($"Could not load battle passes: {error}");
                });
        }

        private void PopulateBattlePassDropdown(BattlePassData[] battlePasses)
        {
            this.battlePassesByLabel.Clear();
            List<string> labels = new List<string>();
            if (battlePasses != null)
            {
                foreach (BattlePassData battlePassData in battlePasses)
                {
                    if (battlePassData == null || string.IsNullOrEmpty(battlePassData.id)) continue;
                    string label = this.CreateBattlePassLabel(battlePassData, labels.Count);
                    this.battlePassesByLabel[label] = battlePassData;
                    labels.Add(label);
                }
            }

            this.battlePassDropdown?.SetEnabled(labels.Count > 0);
            if (this.battlePassDropdown != null) this.battlePassDropdown.choices = labels;
            if (labels.Count == 0)
            {
                this.ShowState("No battle passes are available.");
                return;
            }

            this.battlePassDropdown?.SetValueWithoutNotify(labels[0]);
            BattlePassData selectedBattlePass = this.battlePassesByLabel[labels[0]];
            this.selectedBattlePass = selectedBattlePass;
            this.RenderSessionSchedule(selectedBattlePass);
            this.LoadBattlePassChains(selectedBattlePass);
        }

        private string CreateBattlePassLabel(BattlePassData battlePassData, int index)
        {
            string label = !string.IsNullOrEmpty(battlePassData.display_name)
                ? battlePassData.display_name
                : !string.IsNullOrEmpty(battlePassData.pool_key)
                    ? battlePassData.pool_key
                    : $"Battle Pass {index + 1}";
            if (!this.battlePassesByLabel.ContainsKey(label)) return label;
            return $"{label} ({battlePassData.id})";
        }

        private void HandleBattlePassSelectionChanged(ChangeEvent<string> evt)
        {
            if (this.battlePassesByLabel.TryGetValue(evt.newValue, out BattlePassData battlePassData))
            {
                this.selectedBattlePass = battlePassData;
                this.RenderSessionSchedule(battlePassData);
                this.LoadBattlePassChains(battlePassData);
            }
        }

        private void RenderSessionSchedule(BattlePassData battlePassData)
        {
            BattlePassSessionData session = battlePassData?.type_config?.session;
            if (this.sessionSchedule == null) return;
            this.sessionSchedule.style.display = session == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (session == null) return;
            string scheduleMode = session.schedule_mode?.ToLowerInvariant();
            bool isInterval = scheduleMode == "interval";
            bool isAnnual = scheduleMode == "annual";
            this.scheduleType.text = isInterval ? "INTERVAL" : isAnnual ? "ANNUAL" : "FIXED";
            this.scheduleDetails.text = isInterval
                ? $"From {this.FormatUtcTime(session.cycle_start_at)} | Every {session.repeat_amount} {this.FormatRepeatType(session.repeat_type)}"
                : $"{this.FormatUtcTime(session.session_start_at)} - {this.FormatUtcTime(session.session_end_at)}";
            string sessionState = this.GetSessionState(session);
            this.scheduleState.text = sessionState;
            this.sessionSchedule.EnableInClassList("battle-pass-session-schedule--upcoming", sessionState == "Upcoming");
            this.sessionSchedule.EnableInClassList("battle-pass-session-schedule--active", sessionState == "Active");
            this.sessionSchedule.EnableInClassList("battle-pass-session-schedule--expired", sessionState == "Expired");
        }

        private string GetSessionState(BattlePassSessionData session)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string startValue = string.Equals(session.schedule_mode, "interval", StringComparison.OrdinalIgnoreCase)
                ? session.cycle_start_at
                : session.session_start_at;
            if (DateTimeOffset.TryParse(startValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset start) && now < start)
                return "Upcoming";
            if (!string.Equals(session.schedule_mode, "interval", StringComparison.OrdinalIgnoreCase)
                && DateTimeOffset.TryParse(session.session_end_at, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset end)
                && now > end)
                return "Expired";
            return "Active";
        }

        private string FormatRepeatType(string repeatType)
        {
            if (string.IsNullOrEmpty(repeatType)) return "periods";
            return repeatType.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? repeatType : $"{repeatType}s";
        }

        private string FormatUtcTime(string value)
        {
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTime))
                return string.IsNullOrEmpty(value) ? "Not set" : value;
            return dateTime.ToUniversalTime().ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        }

        private void LoadBattlePassChains(BattlePassData battlePassData)
        {
            if (battlePassData == null || string.IsNullOrEmpty(battlePassData.id)) return;

            int version = ++this.requestVersion;
            this.ShowState("Loading battle pass chains...", clearGraph: false);
            this.battlePass.GetBattlePassChains(
                battlePassData.id,
                response =>
                {
                    if (version != this.requestVersion) return;
                    this.LoadChainFlows(response?.chains, version);
                },
                error =>
                {
                    if (version != this.requestVersion) return;
                    this.ShowState($"Could not load battle pass chains: {error}");
                });
        }

        private void LoadChainFlows(BattlePassChainData[] chainData, int version)
        {
            if (chainData == null || chainData.Length == 0)
            {
                this.ShowState("This battle pass has no quest chains.");
                return;
            }

            List<ChainQuestData> chains = new List<ChainQuestData>();
            foreach (BattlePassChainData data in chainData)
            {
                ChainQuestData chain = data?.chain;
                if (chain == null || string.IsNullOrEmpty(chain.id)) continue;
                chains.Add(chain);
            }

            if (chains.Count == 0)
            {
                this.ShowState("This battle pass has no valid quest chains.");
                return;
            }

            Dictionary<string, ChainQuestTreeResponse> treesByChainId = new Dictionary<string, ChainQuestTreeResponse>();
            int completed = 0;
            foreach (ChainQuestData chain in chains)
            {
                ChainQuestData requestedChain = chain;
                this.chainQuest.GetChainTree(
                    requestedChain.id,
                    response =>
                    {
                        if (version != this.requestVersion) return;
                        treesByChainId[requestedChain.id] = response;
                        this.CompleteChainFlowLoad(chains, treesByChainId, ref completed, version);
                    },
                    _ =>
                    {
                        if (version != this.requestVersion) return;
                        this.CompleteChainFlowLoad(chains, treesByChainId, ref completed, version);
                    });
            }
        }

        private void CompleteChainFlowLoad(
            List<ChainQuestData> chains,
            Dictionary<string, ChainQuestTreeResponse> treesByChainId,
            ref int completed,
            int version)
        {
            completed++;
            if (completed != chains.Count || version != this.requestVersion) return;
            this.RenderChainFlows(chains, treesByChainId);
        }

        private void RenderChainFlows(
            List<ChainQuestData> chains,
            Dictionary<string, ChainQuestTreeResponse> treesByChainId)
        {
            List<QuestFlowNode> nodes = new List<QuestFlowNode>();
            List<QuestFlowEdge> edges = new List<QuestFlowEdge>();
            this.questIdsByGraphNodeId.Clear();
            float nextChainY = CanvasPadding;

            // Preserve the ordering supplied by BattlePass while rendering each chain's own tree.
            foreach (ChainQuestData chain in chains)
            {
                if (!treesByChainId.TryGetValue(chain.id, out ChainQuestTreeResponse response)
                    || response?.nodes == null || response.nodes.Length == 0) continue;

                this.RegisterQuestNodeIds(chain.id, response.nodes);
                float chainHeight = QuestChainFlowRenderer.Append(
                    response.nodes, nodes, edges, new Vector2(CanvasPadding, nextChainY),
                    CardWidth, CardHeight, CardGap, RowGap,
                    nodeIdPrefix: $"{chain.id}:");
                nextChainY += Mathf.Max(RowGap, chainHeight) + ChainGap;
            }

            if (nodes.Count == 0)
            {
                this.ShowState("Could not load quest flows for this battle pass.");
                return;
            }

            this.state.style.display = DisplayStyle.None;
            this.graph.SetGraph(nodes, edges);
        }

        private void RegisterQuestNodeIds(string chainId, QuestTreeNode[] treeNodes)
        {
            if (treeNodes == null) return;
            foreach (QuestTreeNode node in treeNodes)
            {
                if (node == null || string.IsNullOrEmpty(node.quest_id)) continue;
                this.questIdsByGraphNodeId[$"{chainId}:{node.quest_id}"] = node.quest_id;
                this.RegisterQuestNodeIds(chainId, node.children);
            }
        }

        public bool CloseQuestDetailOnEscape()
        {
            return this.questDetailPanel.CloseOnEscape();
        }

        private void ShowQuestDetail(QuestFlowNode node)
        {
            if (node == null || this.detailPanel == null || this.detailContent == null
                || !this.questIdsByGraphNodeId.TryGetValue(node.id, out string questId)) return;
            QuestHistory questHistory = SaiServer.Instance?.QuestHistory;
            if (questHistory == null) return;

            int version = ++this.detailRequestVersion;
            questHistory.GetQuestStatus(
                questId,
                response =>
                {
                    if (version != this.detailRequestVersion) return;
                    this.RenderQuestDetail(node, questId, response);
                },
                error =>
                {
                    if (version != this.detailRequestVersion) return;
                    this.RenderQuestDetailError(error);
                });
        }

        private void RenderQuestDetail(QuestFlowNode node, string questId, QuestDefinitionStatusResponse response)
        {
            QuestDefinitionData definition = response?.quest_definition;
            this.detailContent.Clear();
            this.AddDetailLabel(definition?.name ?? node.title ?? "Unnamed quest", "main-quest-detail__name");
            if (!string.IsNullOrEmpty(definition?.description))
                this.AddDetailLabel(definition.description, "main-quest-detail__description");

            this.AddDetailSection("Status");
            this.AddDetailRow("Quest ID", questId);
            this.AddDetailRow("Status", response?.progress?.status ?? response?.status ?? node.status);
            if (definition != null)
            {
                this.AddDetailRow("Code", definition.code_name);
                this.AddDetailRow("Type", definition.quest_type);
            }
            this.AddDetailSection("Progress");
            this.AddDetailRow("Completed", response?.progress?.completed_at);
            this.AddDetailRow("Claimed", response?.progress?.claimed_at);
            this.ShowDetailPanel();
        }

        private void RenderQuestDetailError(string error)
        {
            this.detailContent.Clear();
            this.AddDetailLabel($"Could not load quest definition: {error}", "main-quest-detail__error");
            this.ShowDetailPanel();
        }

        private void AddDetailSection(string text) => this.AddDetailLabel(text, "main-quest-detail__section");

        private void AddDetailRow(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("main-quest-detail__row");
            Label key = new Label(label);
            key.AddToClassList("main-quest-detail__key");
            Label itemValue = new Label(value ?? "—");
            itemValue.AddToClassList("main-quest-detail__value");
            row.Add(key);
            row.Add(itemValue);
            this.detailContent.Add(row);
        }

        private void AddDetailLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            this.detailContent.Add(label);
        }

        private void ShowDetailPanel()
        {
            this.detailPanel.RemoveFromClassList("main-quest-detail-panel--hidden");
            this.detailPanel.AddToClassList("main-quest-detail-panel--open");
        }

        private void HideQuestDetail()
        {
            if (this.detailPanel == null) return;
            this.detailRequestVersion++;
            this.detailPanel.RemoveFromClassList("main-quest-detail-panel--open");
            this.detailPanel.AddToClassList("main-quest-detail-panel--hidden");
        }


        private void WaitForLogin()
        {
            if (this.waitingForLogin || SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess += this.HandleLoginSuccess;
            this.waitingForLogin = true;
        }

        private void HandleLoginSuccess(LoginResponse _)
        {
            this.StopWaitingForLogin();
            this.LoadBattlePasses();
        }

        private void StopWaitingForLogin()
        {
            if (!this.waitingForLogin || SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.waitingForLogin = false;
        }

        private void ShowState(string message, bool clearGraph = true)
        {
            if (clearGraph) this.graph.SetGraph(new List<QuestFlowNode>(), new List<QuestFlowEdge>());
            if (this.state == null) return;
            this.state.text = message;
            this.state.style.display = DisplayStyle.Flex;
        }

    }
}
