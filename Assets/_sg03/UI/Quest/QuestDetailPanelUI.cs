using System;
using System.Collections.Generic;
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
        private QuestDefinitionStatusResponse selectedResponse;
        private QuestClaimRecord selectedClaim;
        private int requestVersion;
        private readonly Dictionary<string, ItemDefinitionData> itemDefinitions = new Dictionary<string, ItemDefinitionData>();
        private readonly HashSet<string> loadingItemDefinitions = new HashSet<string>();

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
            (root.Q<Button>("CloseMainQuestDetailButton") ?? root.Q<Button>("CloseQuestDetailButton"))?.RegisterCallback<ClickEvent>(_ => this.Hide());
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
                response =>
                {
                    if (version != this.requestVersion) return;
                    this.selectedClaim = null;
                    this.Render(node, questId, response);
                    this.LoadReceivedRewards(questId, version);
                },
                error => { if (version == this.requestVersion) this.RenderError(error); });
        }

        private void Render(QuestFlowNode node, string questId, QuestDefinitionStatusResponse response)
        {
            QuestDefinitionData definition = response?.quest_definition;
            this.selectedNode = node;
            this.selectedQuestId = questId;
            this.selectedResponse = response;
            this.content.Clear();
            this.AddLabel(definition?.name ?? node.title ?? "Unnamed quest", "main-quest-detail__name");
            if (!string.IsNullOrEmpty(definition?.description)) this.AddLabel(definition.description, "main-quest-detail__description");
            this.AddLabel("Status", "main-quest-detail__section");
            this.AddRow("Quest ID", questId);
            string status = response?.progress?.status ?? response?.status ?? node.status;
            this.AddRow("Status", status);
            if (definition != null) { this.AddRow("Code", definition.code_name); this.AddRow("Type", definition.quest_type); }
            if (definition?.conditions?.clauses != null && definition.conditions.clauses.Length > 0)
            {
                this.AddLabel("Conditions", "main-quest-detail__section");
                this.AddConditionDetails(definition.conditions);
            }
            this.AddLabel("Progress", "main-quest-detail__section");
            this.AddRow("Completed", response?.progress?.completed_at);
            this.AddRow("Claimed", response?.progress?.claimed_at);
            this.AddLabel("Expected rewards", "main-quest-detail__section");
            this.AddRewardDetails(definition?.rewards);
            if (this.selectedClaim != null)
            {
                this.AddLabel("Received rewards", "main-quest-detail__section");
                this.AddReceivedRewardDetails(this.selectedClaim.rewards_granted);
            }
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

        private void AddConditionDetails(QuestConditions conditions)
        {
            if (conditions?.clauses == null) return;
            string operation = string.IsNullOrEmpty(conditions.operator_type) ? "AND" : conditions.operator_type.ToUpperInvariant();
            foreach (QuestClause clause in conditions.clauses)
            {
                if (clause == null) continue;
                VisualElement card = new VisualElement(); card.AddToClassList("main-quest-detail__condition");
                AddTo(card, $"{operation} · {(string.IsNullOrEmpty(clause.type) ? "Requirement" : clause.type)}", "main-quest-detail__condition-type");
                if (!string.IsNullOrEmpty(clause.clause_id)) AddTo(card, $"Rule: {clause.clause_id}", "main-quest-detail__condition-rule");
                if (clause.items != null) foreach (QuestClauseItem item in clause.items)
                {
                    if (item == null) continue;
                    this.itemDefinitions.TryGetValue(item.item_definition_id ?? string.Empty, out ItemDefinitionData definition);
                    AddTo(card, $"Item: {definition?.name ?? definition?.item_code ?? "Item"} × {item.quantity}", "main-quest-detail__condition-rule");
                    if (!string.IsNullOrEmpty(definition?.rarity)) AddTo(card, $"Rarity: {definition.rarity}", "main-quest-detail__condition-rule");
                    if (definition == null && !string.IsNullOrEmpty(item.item_definition_id)) this.LoadItemDefinition(item.item_definition_id);
                }
                if (clause.packs != null && !string.IsNullOrEmpty(clause.packs.gacha_pack_id))
                    AddTo(card, $"Gacha pack: {clause.packs.gacha_pack_id} × {clause.packs.quantity}", "main-quest-detail__condition-rule");
                this.content.Add(card);
            }
        }

        private void AddRewardDetails(QuestReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0) { this.AddRow("Rewards", "No reward data"); return; }
            foreach (QuestReward reward in rewards)
            {
                if (reward == null) continue;
                int min = reward.quantity_min > 0 ? reward.quantity_min : reward.amount;
                int max = reward.quantity_max > 0 ? reward.quantity_max : min;
                this.itemDefinitions.TryGetValue(reward.item_definition_id ?? string.Empty, out ItemDefinitionData definition);
                VisualElement card = new VisualElement(); card.AddToClassList("main-quest-detail__reward");
                AddTo(card, $"{definition?.name ?? definition?.item_code ?? reward.reward_type ?? "Reward"} × {(min == max ? min.ToString() : $"{min}–{max}")}", "main-quest-detail__reward-name");
                if (!string.IsNullOrEmpty(definition?.item_code)) AddTo(card, $"Code: {definition.item_code}", "main-quest-detail__reward-info");
                if (!string.IsNullOrEmpty(definition?.rarity)) AddTo(card, $"Rarity: {definition.rarity}", "main-quest-detail__reward-info");
                if (!string.IsNullOrEmpty(definition?.category)) AddTo(card, $"Category: {definition.category}", "main-quest-detail__reward-info");
                if (!string.IsNullOrEmpty(definition?.ParsedMetadata?.description)) AddTo(card, definition.ParsedMetadata.description, "main-quest-detail__reward-info");
                if (!string.IsNullOrEmpty(reward.reward_type)) AddTo(card, reward.reward_type, "main-quest-detail__reward-info");
                if (definition == null && !string.IsNullOrEmpty(reward.item_definition_id)) this.LoadItemDefinition(reward.item_definition_id);
                this.content.Add(card);
            }
        }

        private void LoadItemDefinition(string itemDefinitionId)
        {
            if (this.loadingItemDefinitions.Contains(itemDefinitionId)) return;
            PlayerItem playerItem = SaiServer.Instance?.PlayerItem;
            if (playerItem == null) return;
            this.loadingItemDefinitions.Add(itemDefinitionId);
            playerItem.GetItemDefinition(itemDefinitionId, definition =>
            {
                this.loadingItemDefinitions.Remove(itemDefinitionId);
                if (definition != null) this.itemDefinitions[itemDefinitionId] = definition;
                if (this.selectedNode != null && this.selectedResponse != null) this.Render(this.selectedNode, this.selectedQuestId, this.selectedResponse);
            }, _ => this.loadingItemDefinitions.Remove(itemDefinitionId));
        }

        private void LoadReceivedRewards(string questId, int version)
        {
            QuestHistory history = SaiServer.Instance?.QuestHistory;
            if (history == null) return;
            history.GetClaims(limit: 100, onSuccess: response =>
            {
                if (version != this.requestVersion || response?.claims == null) return;
                foreach (QuestClaimRecord claim in response.claims)
                    if (claim?.quest_definition_id == questId) { this.selectedClaim = claim; break; }
                if (this.selectedClaim != null && this.selectedNode != null)
                    this.Render(this.selectedNode, this.selectedQuestId, this.selectedResponse);
            });
        }

        private void AddReceivedRewardDetails(ClaimQuestGrantedReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0) { this.AddRow("Rewards", "No granted reward data"); return; }
            foreach (ClaimQuestGrantedReward reward in rewards)
            {
                if (reward == null) continue;
                int quantity = reward.quantity > 0 ? reward.quantity : reward.amount;
                this.itemDefinitions.TryGetValue(reward.item_definition_id ?? string.Empty, out ItemDefinitionData definition);
                VisualElement card = new VisualElement(); card.AddToClassList("main-quest-detail__reward");
                AddTo(card, $"{definition?.name ?? definition?.item_code ?? reward.reward_type ?? "Reward"} × {quantity}", "main-quest-detail__reward-name");
                if (!string.IsNullOrEmpty(reward.reward_type)) AddTo(card, reward.reward_type, "main-quest-detail__reward-info");
                if (definition == null && !string.IsNullOrEmpty(reward.item_definition_id)) this.LoadItemDefinition(reward.item_definition_id);
                this.content.Add(card);
            }
        }

        private void AddLabel(string text, string className) { Label label = new Label(text); label.AddToClassList(className); this.content.Add(label); }
        private void AddRow(string key, string value)
        {
            VisualElement row = new VisualElement(); row.AddToClassList("main-quest-detail__row");
            Label keyLabel = new Label(key); keyLabel.AddToClassList("main-quest-detail__key");
            Label valueLabel = new Label(value ?? "—"); valueLabel.AddToClassList("main-quest-detail__value");
            row.Add(keyLabel); row.Add(valueLabel); this.content.Add(row);
        }
        private static void AddTo(VisualElement parent, string text, string className) { Label label = new Label(text); label.AddToClassList(className); parent.Add(label); }
        private static void SetDisplay(VisualElement element, bool show) { if (element != null) element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None; }
        private static void SetAction(Button button, string action, bool enabled) { if (button == null) return; button.SetEnabled(enabled); button.tooltip = enabled ? action : $"Quest must be {action.ToLowerInvariant()}able first."; }
    }
}
