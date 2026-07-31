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
        private bool waitingForLogin;

        public MainQuestContentUI(VisualElement root)
        {
            this.chainName = root.Q<Label>("MainQuestChainName");
            this.state = root.Q<Label>("MainQuestState");
            this.graph = new QuestFlowGraph(root.Q<VisualElement>("MainQuestGraphHost"));
            root.Q<Button>("MainQuestRefreshButton")?.RegisterCallback<ClickEvent>(_ => this.LoadTree());
            this.chainQuest = SaiServer.Instance?.ChainQuest;
            this.LoadTree();
        }

        public void Dispose()
        {
            if (!this.waitingForLogin || SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess -= this.HandleLoginSuccess;
            this.waitingForLogin = false;
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
                subtitle = string.IsNullOrEmpty(layoutNode.node.quest_id) ? null : $"#{layoutNode.node.quest_id}",
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

        private class LayoutNode
        {
            public QuestTreeNode node;
            public int depth;
            public float row;
            public readonly List<LayoutNode> children = new List<LayoutNode>();
        }
    }
}
