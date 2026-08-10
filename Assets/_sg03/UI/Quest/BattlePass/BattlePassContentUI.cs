using System;
using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

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
        private readonly Label state;
        private readonly QuestFlowGraph graph;
        private readonly BattlePass battlePass;
        private readonly ChainQuest chainQuest;
        private readonly Dictionary<string, BattlePassData> battlePassesByLabel = new Dictionary<string, BattlePassData>();
        private bool waitingForLogin;
        private int requestVersion;

        public BattlePassContentUI(VisualElement root)
        {
            this.battlePassDropdown = root.Q<DropdownField>("BattlePassDropdown");
            this.state = root.Q<Label>("BattlePassState");
            this.graph = new QuestFlowGraph(root.Q<VisualElement>("BattlePassGraphHost"));
            this.battlePass = SaiServer.Instance?.BattlePass;
            this.chainQuest = SaiServer.Instance?.ChainQuest;

            this.battlePassDropdown?.RegisterValueChangedCallback(this.HandleBattlePassSelectionChanged);
            root.Q<Button>("BattlePassRefreshButton")?.RegisterCallback<ClickEvent>(_ => this.LoadBattlePasses());
            this.LoadBattlePasses();
        }

        public void Dispose()
        {
            this.requestVersion++;
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
            this.LoadBattlePassChains(this.battlePassesByLabel[labels[0]]);
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
                this.LoadBattlePassChains(battlePassData);
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
            float nextChainY = CanvasPadding;

            // Preserve the ordering supplied by BattlePass while rendering each chain's own tree.
            foreach (ChainQuestData chain in chains)
            {
                if (!treesByChainId.TryGetValue(chain.id, out ChainQuestTreeResponse response)
                    || response?.nodes == null || response.nodes.Length == 0) continue;

                List<ChainLayoutNode> roots = new List<ChainLayoutNode>();
                foreach (QuestTreeNode node in response.nodes)
                    if (node != null) roots.Add(this.CreateLayoutNode(node, 0));

                int nextLeafRow = 0;
                foreach (ChainLayoutNode root in roots) this.AssignRows(root, ref nextLeafRow);
                foreach (ChainLayoutNode root in roots) this.AppendGraphData(chain, root, nextChainY, nodes, edges);
                nextChainY += Mathf.Max(1, nextLeafRow) * RowGap + ChainGap;
            }

            if (nodes.Count == 0)
            {
                this.ShowState("Could not load quest flows for this battle pass.");
                return;
            }

            this.state.style.display = DisplayStyle.None;
            this.graph.SetGraph(nodes, edges);
        }

        private ChainLayoutNode CreateLayoutNode(QuestTreeNode node, int depth)
        {
            ChainLayoutNode layoutNode = new ChainLayoutNode { node = node, depth = depth };
            if (node.children != null)
                foreach (QuestTreeNode child in node.children)
                    if (child != null) layoutNode.children.Add(this.CreateLayoutNode(child, depth + 1));
            return layoutNode;
        }

        private void AssignRows(ChainLayoutNode node, ref int nextLeafRow)
        {
            if (node.children.Count == 0) { node.row = nextLeafRow++; return; }
            foreach (ChainLayoutNode child in node.children) this.AssignRows(child, ref nextLeafRow);
            node.row = (node.children[0].row + node.children[node.children.Count - 1].row) / 2f;
        }

        private void AppendGraphData(
            ChainQuestData chain,
            ChainLayoutNode layoutNode,
            float chainY,
            List<QuestFlowNode> nodes,
            List<QuestFlowEdge> edges)
        {
            string nodeId = $"{chain.id}:{layoutNode.node.quest_id}";
            nodes.Add(new QuestFlowNode
            {
                id = nodeId,
                title = layoutNode.node.quest_name,
                subtitle = string.IsNullOrEmpty(chain.display_name) ? chain.chain_key : chain.display_name,
                status = layoutNode.node.status,
                position = new Vector2(CanvasPadding + layoutNode.depth * (CardWidth + CardGap), chainY + layoutNode.row * RowGap),
                width = CardWidth,
                height = CardHeight
            });
            foreach (ChainLayoutNode child in layoutNode.children)
            {
                edges.Add(new QuestFlowEdge
                {
                    sourceId = nodeId,
                    targetId = $"{chain.id}:{child.node.quest_id}"
                });
                this.AppendGraphData(chain, child, chainY, nodes, edges);
            }
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

        private class ChainLayoutNode
        {
            public QuestTreeNode node;
            public int depth;
            public float row;
            public readonly List<ChainLayoutNode> children = new List<ChainLayoutNode>();
        }
    }
}
