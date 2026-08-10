using System.Collections.Generic;
using SaiGame.Services;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>Converts a ChainQuest tree into positioned data for <see cref="QuestFlowGraph"/>.</summary>
    public static class QuestChainFlowRenderer
    {
        public static float Append(
            QuestTreeNode[] treeNodes,
            List<QuestFlowNode> nodes,
            List<QuestFlowEdge> edges,
            Vector2 origin,
            float cardWidth,
            float cardHeight,
            float columnGap,
            float rowGap,
            string nodeIdPrefix = "",
            string subtitle = null)
        {
            if (treeNodes == null || nodes == null || edges == null) return 0f;

            List<LayoutNode> roots = new List<LayoutNode>();
            foreach (QuestTreeNode node in treeNodes)
                if (node != null) roots.Add(CreateLayoutNode(node, 0));

            int nextLeafRow = 0;
            foreach (LayoutNode root in roots) AssignRows(root, ref nextLeafRow);
            foreach (LayoutNode root in roots)
                AppendGraphData(root, nodes, edges, origin, cardWidth, cardHeight, columnGap, rowGap, nodeIdPrefix, subtitle);
            return nextLeafRow * rowGap;
        }

        private static LayoutNode CreateLayoutNode(QuestTreeNode node, int depth)
        {
            LayoutNode layoutNode = new LayoutNode { node = node, depth = depth };
            if (node.children != null)
                foreach (QuestTreeNode child in node.children)
                    if (child != null) layoutNode.children.Add(CreateLayoutNode(child, depth + 1));
            return layoutNode;
        }

        private static void AssignRows(LayoutNode node, ref int nextLeafRow)
        {
            if (node.children.Count == 0) { node.row = nextLeafRow++; return; }
            foreach (LayoutNode child in node.children) AssignRows(child, ref nextLeafRow);
            node.row = (node.children[0].row + node.children[node.children.Count - 1].row) / 2f;
        }

        private static void AppendGraphData(
            LayoutNode layoutNode,
            List<QuestFlowNode> nodes,
            List<QuestFlowEdge> edges,
            Vector2 origin,
            float cardWidth,
            float cardHeight,
            float columnGap,
            float rowGap,
            string nodeIdPrefix,
            string subtitle)
        {
            string nodeId = nodeIdPrefix + layoutNode.node.quest_id;
            nodes.Add(new QuestFlowNode
            {
                id = nodeId,
                title = layoutNode.node.quest_name,
                subtitle = subtitle,
                status = layoutNode.node.status,
                position = new Vector2(
                    origin.x + layoutNode.depth * (cardWidth + columnGap),
                    origin.y + layoutNode.row * rowGap),
                width = cardWidth,
                height = cardHeight
            });
            foreach (LayoutNode child in layoutNode.children)
            {
                edges.Add(new QuestFlowEdge { sourceId = nodeId, targetId = nodeIdPrefix + child.node.quest_id });
                AppendGraphData(child, nodes, edges, origin, cardWidth, cardHeight, columnGap, rowGap, nodeIdPrefix, subtitle);
            }
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
