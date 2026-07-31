using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    /// <summary>Renders the active quest-chain tree with progression flowing from left to right.</summary>
    public class MainQuestContentUI : IDisposable
    {
        private const float CardWidth = 220f;
        private const float CardHeight = 92f;
        private const float ColumnGap = 120f;
        private const float RowGap = 132f;
        private const float CanvasPadding = 48f;

        private readonly Label chainName;
        private readonly Label state;
        private readonly QuestFlowGraph graph;
        private readonly ChainQuest chainQuest;
        private readonly VisualElement detailPanel;
        private readonly VisualElement detailContent;
        private readonly Button detailStartButton;
        private readonly Button detailCheckButton;
        private readonly Button detailClaimButton;
        private readonly Label detailExpiredMessage;
        private readonly Label detailClaimedMessage;
        private readonly Label detailUnavailableMessage;
        private bool waitingForLogin;
        private int detailRequestVersion;
        private QuestFlowNode selectedDetailNode;
        private QuestDefinitionStatusResponse currentDetailResponse;
        private readonly Dictionary<string, ItemDefinitionData> rewardDefinitionCache = new Dictionary<string, ItemDefinitionData>();
        private readonly HashSet<string> loadingRewardDefinitions = new HashSet<string>();

        public MainQuestContentUI(VisualElement root)
        {
            this.chainName = root.Q<Label>("MainQuestChainName");
            this.state = root.Q<Label>("MainQuestState");
            this.graph = new QuestFlowGraph(root.Q<VisualElement>("MainQuestGraphHost"));
            this.detailPanel = root.Q<VisualElement>("MainQuestDetailPanel");
            this.detailContent = root.Q<VisualElement>("MainQuestDetailContent");
            this.detailStartButton = root.Q<Button>("MainQuestDetailStartButton");
            this.detailCheckButton = root.Q<Button>("MainQuestDetailCheckButton");
            this.detailClaimButton = root.Q<Button>("MainQuestDetailClaimButton");
            this.detailExpiredMessage = root.Q<Label>("MainQuestDetailExpiredMessage");
            this.detailClaimedMessage = root.Q<Label>("MainQuestDetailClaimedMessage");
            this.detailUnavailableMessage = root.Q<Label>("MainQuestDetailUnavailableMessage");
            root.Q<Button>("MainQuestRefreshButton")?.RegisterCallback<ClickEvent>(_ => this.LoadTree());
            root.Q<Button>("CloseMainQuestDetailButton")?.RegisterCallback<ClickEvent>(_ => this.HideQuestDetail());
            this.detailStartButton?.RegisterCallback<ClickEvent>(_ => this.StartSelectedQuest());
            this.detailCheckButton?.RegisterCallback<ClickEvent>(_ => this.CheckSelectedQuest());
            this.detailClaimButton?.RegisterCallback<ClickEvent>(_ => this.ClaimSelectedQuest());
            this.graph.NodeClicked += this.ShowQuestDetail;
            this.chainQuest = SaiServer.Instance?.ChainQuest;
            this.LoadTree();
        }

        public void Dispose()
        {
            this.detailRequestVersion++;
            this.graph.NodeClicked -= this.ShowQuestDetail;
            if (!this.waitingForLogin || SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.waitingForLogin = false;
        }

        public bool CloseQuestDetailOnEscape()
        {
            if (this.detailPanel == null || !this.detailPanel.ClassListContains("main-quest-detail-panel--open")) return false;
            this.HideQuestDetail();
            return true;
        }

        private void LoadTree()
        {
            if (this.chainQuest == null)
            {
                this.ShowState("Quest service is unavailable.");
                return;
            }

            if (SaiServer.Instance == null || !SaiServer.Instance.IsAuthenticated)
            {
                this.ShowState("Waiting for login before loading main quest...");
                this.WaitForLogin();
                return;
            }

            this.StopWaitingForLogin();

            this.ShowState("Loading main quest...");
            ChainQuestData activeChain = this.GetActiveChain();
            if (activeChain != null)
            {
                this.LoadTree(activeChain);
                return;
            }

            this.chainQuest.GetChains(
                onSuccess: _ =>
                {
                    ChainQuestData loadedChain = this.GetActiveChain();
                    if (loadedChain == null) this.ShowState("No active quest chain is available.");
                    else this.LoadTree(loadedChain);
                },
                onError: error => this.ShowState($"Could not load quest chains: {error}"));
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
            this.LoadTree();
        }

        private void StopWaitingForLogin()
        {
            if (!this.waitingForLogin || SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.waitingForLogin = false;
        }

        private ChainQuestData GetActiveChain()
        {
            ChainQuestData[] chains = this.chainQuest.GetActiveChains();
            return chains != null && chains.Length > 0 ? chains[0] : null;
        }

        private void LoadTree(ChainQuestData chain)
        {
            this.chainName.text = string.IsNullOrEmpty(chain.display_name) ? "Main Quest" : chain.display_name;
            this.chainQuest.GetChainTree(chain.id, this.RenderTree,
                error => this.ShowState($"Could not load quest tree: {error}"));
        }

        private void RenderTree(ChainQuestTreeResponse response)
        {
            if (response?.nodes == null || response.nodes.Length == 0)
            {
                this.ShowState("This quest chain has no quests yet.");
                return;
            }

            if (!string.IsNullOrEmpty(response.chain_name)) this.chainName.text = response.chain_name;
            this.state.style.display = DisplayStyle.None;

            List<LayoutNode> roots = new List<LayoutNode>();
            foreach (QuestTreeNode node in response.nodes)
                if (node != null) roots.Add(this.CreateLayoutNode(node, 0));

            int nextLeafRow = 0;
            foreach (LayoutNode root in roots)
                this.AssignRows(root, ref nextLeafRow);

            List<QuestFlowNode> nodes = new List<QuestFlowNode>();
            List<QuestFlowEdge> edges = new List<QuestFlowEdge>();
            foreach (LayoutNode root in roots) this.AppendGraphData(root, nodes, edges);
            this.graph.SetGraph(nodes, edges);
        }

        private LayoutNode CreateLayoutNode(QuestTreeNode node, int depth)
        {
            LayoutNode layoutNode = new LayoutNode { node = node, depth = depth };
            if (node.children != null)
                foreach (QuestTreeNode child in node.children)
                    if (child != null) layoutNode.children.Add(this.CreateLayoutNode(child, depth + 1));
            return layoutNode;
        }

        private void AssignRows(LayoutNode node, ref int nextLeafRow)
        {
            if (node.children.Count == 0) { node.row = nextLeafRow++; return; }
            foreach (LayoutNode child in node.children) this.AssignRows(child, ref nextLeafRow);
            node.row = (node.children[0].row + node.children[node.children.Count - 1].row) / 2f;
        }

        private void AppendGraphData(LayoutNode layoutNode, List<QuestFlowNode> nodes, List<QuestFlowEdge> edges)
        {
            nodes.Add(new QuestFlowNode
            {
                id = layoutNode.node.quest_id,
                title = layoutNode.node.quest_name,
                status = layoutNode.node.status,
                position = new Vector2(this.GetX(layoutNode), this.GetY(layoutNode)),
                width = CardWidth,
                height = CardHeight
            });
            foreach (LayoutNode child in layoutNode.children)
            {
                edges.Add(new QuestFlowEdge { sourceId = layoutNode.node.quest_id, targetId = child.node.quest_id });
                this.AppendGraphData(child, nodes, edges);
            }
        }

        private float GetX(LayoutNode node) => CanvasPadding + node.depth * (CardWidth + ColumnGap);
        private float GetY(LayoutNode node) => CanvasPadding + node.row * RowGap;

        private void ShowState(string message)
        {
            this.graph?.SetGraph(new List<QuestFlowNode>(), new List<QuestFlowEdge>());
            if (this.state == null) return;
            this.state.text = message;
            this.state.style.display = DisplayStyle.Flex;
        }

        private void ShowQuestDetail(QuestFlowNode node)
        {
            if (node == null || this.detailPanel == null || this.detailContent == null || string.IsNullOrEmpty(node.id)) return;
            QuestHistory questHistory = SaiServer.Instance?.QuestHistory;
            if (questHistory == null) return;

            int requestVersion = ++this.detailRequestVersion;
            questHistory.GetQuestStatus(
                node.id,
                response =>
                {
                    if (requestVersion != this.detailRequestVersion) return;
                    this.RenderQuestDetail(node, response);
                },
                error =>
                {
                    if (requestVersion != this.detailRequestVersion) return;
                    this.RenderQuestDetailError(error);
                });
        }

        private void RenderQuestDetail(QuestFlowNode node, QuestDefinitionStatusResponse response)
        {
            QuestDefinitionData definition = response?.quest_definition;
            this.selectedDetailNode = node;
            this.currentDetailResponse = response;
            this.detailContent.Clear();

            Label title = new Label(definition?.name ?? node.title ?? "Unnamed quest");
            title.AddToClassList("main-quest-detail__name");
            this.detailContent.Add(title);
            if (!string.IsNullOrEmpty(definition?.description))
            {
                Label description = new Label(definition.description);
                description.AddToClassList("main-quest-detail__description");
                this.detailContent.Add(description);
            }

            this.AddDetailSection("Status");
            this.AddDetailRow("Quest ID", node.id);
            this.AddDetailRow("Status", response?.progress?.status ?? response?.status ?? node.status ?? "unknown");
            if (definition != null)
            {
                this.AddDetailRow("Code", definition.code_name);
                this.AddDetailRow("Type", definition.quest_type);
                if (definition.conditions?.clauses != null && definition.conditions.clauses.Length > 0)
                    this.AddDetailSection("Conditions");
                this.AddConditionDetails(definition.conditions);
            }

            this.AddDetailSection("Chain");
            this.AddDetailRow("Chain", this.chainName?.text);
            this.AddDetailSection("Progress");
            this.AddDetailRow("Progress status", response?.progress?.status);
            this.AddDetailRow("Completed", response?.progress?.completed_at);
            this.AddDetailRow("Claimed", response?.progress?.claimed_at);
            this.AddDetailSection("Expected rewards");
            this.AddRewardDetails(definition?.rewards);
            this.ConfigureQuestDetailActions(response?.progress?.status ?? response?.status ?? node.status);

            this.detailPanel.RemoveFromClassList("main-quest-detail-panel--hidden");
            this.detailPanel.AddToClassList("main-quest-detail-panel--open");
        }

        private void RenderQuestDetailError(string error)
        {
            this.detailContent.Clear();
            Label message = new Label($"Could not load quest definition: {error}");
            message.AddToClassList("main-quest-detail__error");
            this.detailContent.Add(message);
            this.detailPanel.RemoveFromClassList("main-quest-detail-panel--hidden");
            this.detailPanel.AddToClassList("main-quest-detail-panel--open");
        }

        private void AddDetailSection(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("main-quest-detail__section");
            this.detailContent.Add(label);
        }

        private void AddConditionDetails(QuestConditions conditions)
        {
            if (conditions?.clauses == null || conditions.clauses.Length == 0) return;
            string operation = string.IsNullOrEmpty(conditions.operator_type) ? "AND" : conditions.operator_type.ToUpperInvariant();
            foreach (QuestClause clause in conditions.clauses)
            {
                if (clause == null) continue;
                VisualElement card = new VisualElement();
                card.AddToClassList("main-quest-detail__condition");
                string clauseType = string.IsNullOrEmpty(clause.type) ? "Requirement" : clause.type;
                Label type = new Label($"{operation} · {clauseType}");
                type.AddToClassList("main-quest-detail__condition-type");
                card.Add(type);
                if (!string.IsNullOrEmpty(clause.clause_id))
                    card.Add(this.CreateDetailLabel($"Rule: {clause.clause_id}", "main-quest-detail__condition-rule"));
                if (clause.items != null)
                    foreach (QuestClauseItem item in clause.items)
                    {
                        if (item == null) continue;
                        this.rewardDefinitionCache.TryGetValue(item.item_definition_id ?? string.Empty, out ItemDefinitionData definition);
                        string itemName = definition?.name ?? definition?.item_code ?? "Item";
                        card.Add(this.CreateDetailLabel($"Item: {itemName} × {item.quantity}", "main-quest-detail__condition-rule"));
                        if (!string.IsNullOrEmpty(definition?.rarity))
                            card.Add(this.CreateDetailLabel($"Rarity: {definition.rarity}", "main-quest-detail__condition-rule"));
                        if (definition == null && !string.IsNullOrEmpty(item.item_definition_id))
                        {
                            card.Add(this.CreateDetailLabel("Loading item definition...", "main-quest-detail__condition-rule"));
                            this.LoadItemDefinition(item.item_definition_id);
                        }
                    }
                if (clause.packs != null && !string.IsNullOrEmpty(clause.packs.gacha_pack_id))
                    card.Add(this.CreateDetailLabel($"Gacha pack: {clause.packs.gacha_pack_id} × {clause.packs.quantity}", "main-quest-detail__condition-rule"));
                this.detailContent.Add(card);
            }
        }

        private void AddRewardDetails(QuestReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0) { this.AddDetailRow("Rewards", "No reward data"); return; }
            foreach (QuestReward reward in rewards)
            {
                if (reward == null) continue;
                int min = reward.quantity_min > 0 ? reward.quantity_min : reward.amount;
                int max = reward.quantity_max > 0 ? reward.quantity_max : min;
                string quantity = min == max ? min.ToString() : $"{min}–{max}";
                this.AddRewardDetail(reward, quantity);
            }
        }

        private void AddRewardDetail(QuestReward reward, string quantity)
        {
            this.rewardDefinitionCache.TryGetValue(reward.item_definition_id ?? string.Empty, out ItemDefinitionData definition);
            VisualElement card = new VisualElement();
            card.AddToClassList("main-quest-detail__reward");
            string itemName = definition?.name ?? definition?.item_code ?? reward.reward_type ?? "Reward";
            card.Add(this.CreateDetailLabel($"{itemName} × {quantity}", "main-quest-detail__reward-name"));
            if (!string.IsNullOrEmpty(definition?.item_code)) card.Add(this.CreateDetailLabel($"Code: {definition.item_code}", "main-quest-detail__reward-info"));
            if (!string.IsNullOrEmpty(definition?.rarity))
                card.Add(this.CreateDetailLabel($"Rarity: {definition.rarity}", "main-quest-detail__reward-info"));
            if (!string.IsNullOrEmpty(definition?.category))
                card.Add(this.CreateDetailLabel($"Category: {definition.category}", "main-quest-detail__reward-info"));
            if (!string.IsNullOrEmpty(definition?.ParsedMetadata?.description)) card.Add(this.CreateDetailLabel(definition.ParsedMetadata.description, "main-quest-detail__reward-info"));
            if (!string.IsNullOrEmpty(reward.reward_type)) card.Add(this.CreateDetailLabel(reward.reward_type, "main-quest-detail__reward-info"));
            if (definition == null && !string.IsNullOrEmpty(reward.item_definition_id))
            {
                card.Add(this.CreateDetailLabel("Loading item definition...", "main-quest-detail__reward-info"));
                this.LoadItemDefinition(reward.item_definition_id);
            }
            this.detailContent.Add(card);
        }

        private void LoadItemDefinition(string itemDefinitionId)
        {
            if (this.loadingRewardDefinitions.Contains(itemDefinitionId)) return;
            PlayerItem playerItem = SaiServer.Instance?.PlayerItem;
            if (playerItem == null) return;
            this.loadingRewardDefinitions.Add(itemDefinitionId);
            playerItem.GetItemDefinition(itemDefinitionId,
                definition =>
                {
                    this.loadingRewardDefinitions.Remove(itemDefinitionId);
                    if (definition != null) this.rewardDefinitionCache[itemDefinitionId] = definition;
                    if (this.selectedDetailNode != null && this.currentDetailResponse != null)
                        this.RenderQuestDetail(this.selectedDetailNode, this.currentDetailResponse);
                },
                _ => this.loadingRewardDefinitions.Remove(itemDefinitionId));
        }

        private void AddDetailRow(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("main-quest-detail__row");
            Label labelElement = new Label(label);
            labelElement.AddToClassList("main-quest-detail__key");
            Label valueElement = new Label(value ?? "—");
            valueElement.AddToClassList("main-quest-detail__value");
            row.Add(labelElement);
            row.Add(valueElement);
            this.detailContent.Add(row);
        }

        private Label CreateDetailLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private void ConfigureQuestDetailActions(string status)
        {
            string normalized = (status ?? string.Empty).ToLowerInvariant();
            bool isClaimed = normalized == "claimed";
            bool isExpired = normalized == "expired";
            bool isUnavailable = normalized == "locked";
            bool hideActions = isClaimed || isExpired || isUnavailable;
            SetActionDisplay(this.detailStartButton, !hideActions);
            SetActionDisplay(this.detailCheckButton, !hideActions);
            SetActionDisplay(this.detailClaimButton, !hideActions);
            SetMessageDisplay(this.detailExpiredMessage, isExpired);
            SetMessageDisplay(this.detailClaimedMessage, isClaimed);
            SetMessageDisplay(this.detailUnavailableMessage, isUnavailable);

            if (hideActions) return;
            ConfigureAction(this.detailStartButton, "Start", normalized == "not_started");
            ConfigureAction(this.detailCheckButton, "Check", normalized == "in_progress");
            ConfigureAction(this.detailClaimButton, "Claim", normalized == "completed");
        }

        private static void SetActionDisplay(Button button, bool visible)
        {
            if (button != null) button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetMessageDisplay(Label message, bool visible)
        {
            if (message != null) message.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void ConfigureAction(Button button, string action, bool enabled)
        {
            if (button == null) return;
            button.SetEnabled(enabled);
            button.tooltip = enabled ? action : $"Quest must be {action.ToLowerInvariant()}able first.";
        }

        private void StartSelectedQuest()
        {
            SaiServer.Instance?.QuestProgressor?.StartQuest(this.selectedDetailNode?.id, _ => this.ShowQuestDetail(this.selectedDetailNode), this.RenderQuestDetailError);
        }

        private void CheckSelectedQuest()
        {
            SaiServer.Instance?.QuestProgressor?.CheckQuest(this.selectedDetailNode?.id, _ => this.ShowQuestDetail(this.selectedDetailNode), this.RenderQuestDetailError);
        }

        private void ClaimSelectedQuest()
        {
            SaiServer.Instance?.QuestProgressor?.ClaimQuest(this.selectedDetailNode?.id, _ => this.ShowQuestDetail(this.selectedDetailNode), this.RenderQuestDetailError);
        }

        private void HideQuestDetail()
        {
            if (this.detailPanel == null) return;
            this.detailRequestVersion++;
            this.detailPanel.RemoveFromClassList("main-quest-detail-panel--open");
            this.detailPanel.AddToClassList("main-quest-detail-panel--hidden");
        }

        private class LayoutNode
        {
            public QuestTreeNode node;
            public int depth;
            public float row;
            public readonly List<LayoutNode> children = new List<LayoutNode>();
        }
    }
}
