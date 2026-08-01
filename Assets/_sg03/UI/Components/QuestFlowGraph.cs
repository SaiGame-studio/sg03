using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    /// <summary>
    /// A reusable node editor-style graph for UI Toolkit. Consumers provide positioned nodes
    /// and directed edges; the component owns rendering, pan, zoom and fit-view controls.
    /// </summary>
    public class QuestFlowGraph
    {
        public const float DefaultNodeWidth = 220f;
        public const float DefaultNodeHeight = 92f;
        private const float MinZoom = .45f;
        private const float MaxZoom = 1.75f;

        private readonly VisualElement viewport;
        private readonly VisualElement canvas;
        private readonly VisualElement edgeLayer;
        private readonly VisualElement nodeLayer;
        private readonly Dictionary<string, QuestFlowNode> nodesById = new Dictionary<string, QuestFlowNode>();
        private readonly Dictionary<string, Button> nodeElementsById = new Dictionary<string, Button>();
        private Vector2 pan;
        private float zoom = 1f;
        private bool isPanning;
        private int panPointerId;
        private Vector2 panStartPointer;
        private Vector2 panStartPosition;

        public event Action<QuestFlowNode> NodeClicked;

        public QuestFlowGraph(VisualElement host)
        {
            this.viewport = new VisualElement { name = "QuestFlowViewport" };
            this.viewport.AddToClassList("quest-flow-viewport");
            this.canvas = new VisualElement { name = "QuestFlowCanvas" };
            this.canvas.AddToClassList("quest-flow-canvas");
            this.edgeLayer = new VisualElement { name = "QuestFlowEdges" };
            this.edgeLayer.AddToClassList("quest-flow-edge-layer");
            this.nodeLayer = new VisualElement { name = "QuestFlowNodes" };
            this.nodeLayer.AddToClassList("quest-flow-node-layer");
            this.canvas.Add(this.edgeLayer);
            this.canvas.Add(this.nodeLayer);
            this.viewport.Add(this.canvas);
            host.Add(this.viewport);
            this.AddControls(host);

            this.viewport.RegisterCallback<PointerDownEvent>(this.OnPointerDown);
            this.viewport.RegisterCallback<PointerMoveEvent>(this.OnPointerMove);
            this.viewport.RegisterCallback<PointerUpEvent>(this.OnPointerUp);
            this.viewport.RegisterCallback<PointerCaptureOutEvent>(_ => this.isPanning = false);
            this.viewport.RegisterCallback<WheelEvent>(this.OnWheel);
            this.viewport.RegisterCallback<GeometryChangedEvent>(_ => this.ApplyTransform());
        }

        public bool HasNodes => this.nodesById.Count > 0;

        public void SetGraph(IList<QuestFlowNode> nodes, IList<QuestFlowEdge> edges, bool fitView = true)
        {
            this.nodesById.Clear();
            this.nodeElementsById.Clear();
            this.edgeLayer.Clear();
            this.nodeLayer.Clear();
            if (nodes == null) return;

            foreach (QuestFlowNode node in nodes)
                if (node != null && !string.IsNullOrEmpty(node.id)) this.nodesById[node.id] = node;

            if (edges != null)
                foreach (QuestFlowEdge edge in edges) this.AddEdge(edge);
            foreach (QuestFlowNode node in this.nodesById.Values) this.AddNode(node);
            Rect bounds = this.GetBounds();
            this.canvas.style.width = bounds.xMax + 96f;
            this.canvas.style.height = bounds.yMax + 96f;
            if (!fitView) return;

            this.FitView();
            this.viewport.schedule.Execute(this.FitView);
        }

        public bool UpdateNodeStatuses(IList<QuestFlowNode> nodes)
        {
            if (nodes == null || nodes.Count != this.nodesById.Count) return false;

            foreach (QuestFlowNode node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id) || !this.nodesById.ContainsKey(node.id)) return false;
            }

            foreach (QuestFlowNode node in nodes)
            {
                QuestFlowNode currentNode = this.nodesById[node.id];
                currentNode.status = node.status;

                Button element = this.nodeElementsById[node.id];
                element.RemoveFromClassList("quest-flow-node--pending");
                element.RemoveFromClassList("quest-flow-node--active");
                element.RemoveFromClassList("quest-flow-node--completed");
                element.RemoveFromClassList("quest-flow-node--locked");
                element.AddToClassList($"quest-flow-node--{GetStatusClass(node.status)}");

                Label status = element.Q<Label>($"QuestFlowNodeStatus_{node.id}");
                if (status != null) status.text = string.IsNullOrEmpty(node.status) ? "unknown" : node.status;
            }

            return true;
        }

        public void FitView()
        {
            if (this.nodesById.Count == 0 || this.viewport.layout.width <= 0 || this.viewport.layout.height <= 0)
            {
                this.zoom = 1f;
                this.pan = Vector2.zero;
                this.ApplyTransform();
                return;
            }

            Rect bounds = this.GetBounds();
            float padding = 72f;
            this.zoom = Mathf.Clamp(Mathf.Min(
                (this.viewport.layout.width - padding * 2f) / bounds.width,
                (this.viewport.layout.height - padding * 2f) / bounds.height), MinZoom, MaxZoom);
            this.pan = new Vector2(
                (this.viewport.layout.width - bounds.width * this.zoom) * .5f - bounds.xMin * this.zoom,
                (this.viewport.layout.height - bounds.height * this.zoom) * .5f - bounds.yMin * this.zoom);
            this.ApplyTransform();
        }

        private void AddControls(VisualElement host)
        {
            VisualElement controls = new VisualElement();
            controls.AddToClassList("quest-flow-controls");
            controls.Add(this.CreateControl("+", () => this.SetZoom(this.zoom * 1.2f)));
            controls.Add(this.CreateControl("−", () => this.SetZoom(this.zoom / 1.2f)));
            controls.Add(this.CreateControl("Fit", this.FitView));
            host.Add(controls);
        }

        private Button CreateControl(string label, Action onClick)
        {
            Button button = new Button(onClick) { text = label };
            button.AddToClassList("quest-flow-controls__button");
            return button;
        }

        private void AddNode(QuestFlowNode node)
        {
            Button element = new Button();
            element.name = $"QuestFlowNode_{node.id}";
            element.AddToClassList("quest-flow-node");
            element.AddToClassList($"quest-flow-node--{GetStatusClass(node.status)}");
            element.style.left = node.position.x;
            element.style.top = node.position.y;
            element.style.width = node.width > 0 ? node.width : DefaultNodeWidth;
            element.style.height = node.height > 0 ? node.height : DefaultNodeHeight;

            Label title = new Label(node.title ?? "Unnamed quest");
            title.AddToClassList("quest-flow-node__title");
            element.Add(title);
            if (!string.IsNullOrEmpty(node.subtitle))
            {
                Label subtitle = new Label(node.subtitle);
                subtitle.AddToClassList("quest-flow-node__subtitle");
                element.Add(subtitle);
            }
            Label status = new Label(string.IsNullOrEmpty(node.status) ? "unknown" : node.status);
            status.name = $"QuestFlowNodeStatus_{node.id}";
            status.AddToClassList("quest-flow-node__status");
            element.Add(status);
            element.clicked += () => this.NodeClicked?.Invoke(node);
            this.nodeLayer.Add(element);
            this.nodeElementsById[node.id] = element;
        }

        private void AddEdge(QuestFlowEdge edge)
        {
            if (edge == null || !this.nodesById.TryGetValue(edge.sourceId, out QuestFlowNode source)
                || !this.nodesById.TryGetValue(edge.targetId, out QuestFlowNode target)) return;

            float sourceWidth = source.width > 0 ? source.width : DefaultNodeWidth;
            float sourceHeight = source.height > 0 ? source.height : DefaultNodeHeight;
            float targetHeight = target.height > 0 ? target.height : DefaultNodeHeight;
            float fromX = source.position.x + sourceWidth;
            float fromY = source.position.y + sourceHeight * .5f;
            float toX = target.position.x;
            float toY = target.position.y + targetHeight * .5f;
            float middleX = (fromX + toX) * .5f;
            this.AddLine(fromX, fromY, middleX - fromX, 2f);
            this.AddLine(middleX, Mathf.Min(fromY, toY), 2f, Mathf.Abs(toY - fromY));
            this.AddLine(middleX, toY, toX - middleX, 2f);

            VisualElement arrow = new VisualElement();
            arrow.AddToClassList("quest-flow-edge__arrow");
            arrow.style.left = toX - 9f;
            arrow.style.top = toY - 5f;
            this.edgeLayer.Add(arrow);
        }

        private void AddLine(float left, float top, float width, float height)
        {
            VisualElement line = new VisualElement();
            line.AddToClassList("quest-flow-edge");
            line.style.left = left;
            line.style.top = top;
            line.style.width = width;
            line.style.height = Mathf.Max(2f, height);
            this.edgeLayer.Add(line);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            this.isPanning = true;
            this.panPointerId = evt.pointerId;
            this.panStartPointer = new Vector2(evt.position.x, evt.position.y);
            this.panStartPosition = this.pan;
            this.viewport.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.isPanning || evt.pointerId != this.panPointerId) return;
            Vector2 pointerPosition = new Vector2(evt.position.x, evt.position.y);
            this.pan = this.panStartPosition + (pointerPosition - this.panStartPointer);
            this.ApplyTransform();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.isPanning || evt.pointerId != this.panPointerId) return;
            this.isPanning = false;
            this.viewport.ReleasePointer(evt.pointerId);
        }

        private void OnWheel(WheelEvent evt)
        {
            this.SetZoom(this.zoom * (evt.delta.y > 0 ? .9f : 1.1f));
            evt.StopPropagation();
        }

        private void SetZoom(float value)
        {
            this.zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            this.ApplyTransform();
        }

        private void ApplyTransform()
        {
            this.canvas.style.left = this.pan.x;
            this.canvas.style.top = this.pan.y;
            this.canvas.style.scale = new Scale(new Vector3(this.zoom, this.zoom, 1f));
        }

        private Rect GetBounds()
        {
            bool initialized = false;
            Rect bounds = new Rect();
            foreach (QuestFlowNode node in this.nodesById.Values)
            {
                Rect nodeRect = new Rect(node.position.x, node.position.y,
                    node.width > 0 ? node.width : DefaultNodeWidth,
                    node.height > 0 ? node.height : DefaultNodeHeight);
                if (!initialized) { bounds = nodeRect; initialized = true; }
                else bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, nodeRect.xMin), Mathf.Min(bounds.yMin, nodeRect.yMin), Mathf.Max(bounds.xMax, nodeRect.xMax), Mathf.Max(bounds.yMax, nodeRect.yMax));
            }
            return bounds;
        }

        private static string GetStatusClass(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "completed": case "claimed": return "completed";
                case "active": case "in_progress": return "active";
                case "locked": return "locked";
                default: return "pending";
            }
        }
    }

    public class QuestFlowNode
    {
        public string id;
        public string title;
        public string subtitle;
        public string status;
        public Vector2 position;
        public float width;
        public float height;
    }

    public class QuestFlowEdge
    {
        public string sourceId;
        public string targetId;
    }
}
