using System;
using SaiGame.Services;
using UnityEngine.UIElements;
using SG03.UI.Components;

namespace SG03.UI
{
    /// <summary>Shared quest-definition drawer used by every quest flow.</summary>
    public class QuestDetailPanelUI
    {
        private readonly VisualElement panel;
        private readonly VisualElement content;
        private readonly Button startButton;
        private readonly Button checkButton;
        private readonly Button claimButton;
        private readonly Label expiredMessage;
        private readonly Label claimedMessage;
        private readonly Label unavailableMessage;
        private readonly Func<QuestFlowNode, string> questIdResolver;
        private readonly Action refresh;
        private QuestFlowNode selectedNode;
        private string selectedQuestId;
        private int requestVersion;

        public QuestDetailPanelUI(VisualElement root, Action refresh, Func<QuestFlowNode, string> questIdResolver = null)
        {
            this.panel = root.Q<VisualElement>("MainQuestDetailPanel");
            this.content = root.Q<VisualElement>("MainQuestDetailContent");
            this.startButton = root.Q<Button>("MainQuestDetailStartButton");
            this.checkButton = root.Q<Button>("MainQuestDetailCheckButton");
            this.claimButton = root.Q<Button>("MainQuestDetailClaimButton");
            this.expiredMessage = root.Q<Label>("MainQuestDetailExpiredMessage");
            this.claimedMessage = root.Q<Label>("MainQuestDetailClaimedMessage");
            this.unavailableMessage = root.Q<Label>("MainQuestDetailUnavailableMessage");
            this.refresh = refresh;
            this.questIdResolver = questIdResolver ?? (node => node?.id);
            root.Q<Button>("CloseMainQuestDetailButton")?.RegisterCallback<ClickEvent>(_ => this.Hide());
            this.startButton?.RegisterCallback<ClickEvent>(_ => this.RunAction("start"));
            this.checkButton?.RegisterCallback<ClickEvent>(_ => this.RunAction("check"));
            this.claimButton?.RegisterCallback<ClickEvent>(_ => this.RunAction("claim"));
        }

        public bool CloseOnEscape()
        {
            if (this.panel == null || !this.panel.ClassListContains("main-quest-detail-panel--open")) return false;
            this.Hide();
            return true;
        }

        public void Show(QuestFlowNode node)
        {
            string questId = this.questIdResolver(node);
            if (node == null || string.IsNullOrEmpty(questId) || this.panel == null || this.content == null) return;
            QuestHistory history = SaiServer.Instance?.QuestHistory;
            if (history == null) return;
            this.selectedNode = node;
            this.selectedQuestId = questId;
            int version = ++this.requestVersion;
            history.GetQuestStatus(questId,
                response => { if (version == this.requestVersion) this.Render(node, questId, response); },
                error => { if (version == this.requestVersion) this.RenderError(error); });
        }

        private void Render(QuestFlowNode node, string questId, QuestDefinitionStatusResponse response)
        {
            QuestDefinitionData definition = response?.quest_definition;
            this.content.Clear();
            this.AddLabel(definition?.name ?? node.title ?? "Unnamed quest", "main-quest-detail__name");
            if (!string.IsNullOrEmpty(definition?.description)) this.AddLabel(definition.description, "main-quest-detail__description");
            this.AddLabel("Status", "main-quest-detail__section");
            this.AddRow("Quest ID", questId);
            string status = response?.progress?.status ?? response?.status ?? node.status;
            this.AddRow("Status", status);
            if (definition != null) { this.AddRow("Code", definition.code_name); this.AddRow("Type", definition.quest_type); }
            this.AddLabel("Progress", "main-quest-detail__section");
            this.AddRow("Completed", response?.progress?.completed_at);
            this.AddRow("Claimed", response?.progress?.claimed_at);
            this.ConfigureActions(status);
            this.panel.RemoveFromClassList("main-quest-detail-panel--hidden");
            this.panel.AddToClassList("main-quest-detail-panel--open");
        }

        private void RenderError(string error)
        {
            this.content.Clear();
            this.AddLabel($"Could not load quest definition: {error}", "main-quest-detail__error");
            this.panel.RemoveFromClassList("main-quest-detail-panel--hidden");
            this.panel.AddToClassList("main-quest-detail-panel--open");
        }

        private void ConfigureActions(string status)
        {
            string value = (status ?? string.Empty).ToLowerInvariant();
            bool hide = value == "claimed" || value == "expired" || value == "locked";
            SetDisplay(this.startButton, !hide); SetDisplay(this.checkButton, !hide); SetDisplay(this.claimButton, !hide);
            SetDisplay(this.expiredMessage, value == "expired"); SetDisplay(this.claimedMessage, value == "claimed"); SetDisplay(this.unavailableMessage, value == "locked");
            if (hide) return;
            SetAction(this.startButton, "Start", value == "not_started");
            SetAction(this.checkButton, "Check", value == "in_progress");
            SetAction(this.claimButton, "Claim", value == "completed");
        }

        private void RunAction(string action)
        {
            QuestActionRequest.RunDefinitionAction(this.selectedQuestId, action,
                () => { this.refresh?.Invoke(); if (this.selectedNode != null) this.Show(this.selectedNode); },
                error => ToastMessage.ShowError(QuestActionErrorFormatter.Format(error), this.panel));
        }

        private void Hide()
        {
            if (this.panel == null) return;
            this.requestVersion++;
            this.panel.RemoveFromClassList("main-quest-detail-panel--open");
            this.panel.AddToClassList("main-quest-detail-panel--hidden");
        }

        private void AddLabel(string text, string className) { Label label = new Label(text); label.AddToClassList(className); this.content.Add(label); }
        private void AddRow(string key, string value)
        {
            VisualElement row = new VisualElement(); row.AddToClassList("main-quest-detail__row");
            Label keyLabel = new Label(key); keyLabel.AddToClassList("main-quest-detail__key");
            Label valueLabel = new Label(value ?? "—"); valueLabel.AddToClassList("main-quest-detail__value");
            row.Add(keyLabel); row.Add(valueLabel); this.content.Add(row);
        }
        private static void SetDisplay(VisualElement element, bool show) { if (element != null) element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None; }
        private static void SetAction(Button button, string action, bool enabled) { if (button == null) return; button.SetEnabled(enabled); button.tooltip = enabled ? action : $"Quest must be {action.ToLowerInvariant()}able first."; }
    }
}
