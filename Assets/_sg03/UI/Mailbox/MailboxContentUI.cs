using System;
using SG03.UI.Components;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Binds MailboxContent.uxml to live data loaded by MailboxList.
    // Re-renders whenever MailboxList fires OnDataUpdated.
    public class MailboxContentUI
    {
        private readonly ScrollView mailList;
        private readonly VisualElement emptyState;
        private readonly VisualElement loadingState;
        private readonly Button refreshBtn;
        private readonly Button claimAllBtn;
        private readonly Button deleteAllClaimedBtn;
        private readonly MailboxList list;
        private bool isBulkActionRunning;

        public MailboxContentUI(VisualElement root)
        {
            this.mailList     = root.Q<ScrollView>("MailList");
            this.emptyState   = root.Q("EmptyState");
            this.loadingState = root.Q("LoadingState");
            this.refreshBtn   = root.Q<Button>("RefreshBtn");
            this.claimAllBtn  = root.Q<Button>("ClaimAllBtn");
            this.deleteAllClaimedBtn = root.Q<Button>("DeleteAllClaimedBtn");

            this.list = new MailboxList();
            this.list.OnDataUpdated += this.Render;

            if (this.refreshBtn != null)
                this.refreshBtn.RegisterCallback<ClickEvent>(_ => this.DoRefresh());

            if (this.claimAllBtn != null)
                this.claimAllBtn.RegisterCallback<ClickEvent>(_ => this.ClaimAll());

            if (this.deleteAllClaimedBtn != null)
                this.deleteAllClaimedBtn.RegisterCallback<ClickEvent>(_ => this.DeleteAllClaimed());

            this.ShowLoading();
            this.DoRefresh();
        }

        private void DoRefresh()
        {
            this.ShowLoading();
            this.list.Refresh();
        }

        private void ClaimAll()
        {
            if (this.isBulkActionRunning) return;

            this.isBulkActionRunning = true;
            this.UpdateHeaderActions();
            this.list.ClaimAllMessages(
                onSuccess: _ =>
                {
                    this.isBulkActionRunning = false;
                    this.DoRefresh();
                },
                onError: error =>
                {
                    this.isBulkActionRunning = false;
                    this.UpdateHeaderActions();
                    Debug.LogWarning($"[MailboxContentUI] ClaimAllMessages failed: {error}");
                }
            );
        }

        private void DeleteAllClaimed()
        {
            if (this.isBulkActionRunning) return;

            this.isBulkActionRunning = true;
            this.UpdateHeaderActions();
            this.list.DeleteAllClaimedMessages((deletedCount, lastError) =>
            {
                this.isBulkActionRunning = false;
                if (!string.IsNullOrEmpty(lastError))
                    Debug.LogWarning($"[MailboxContentUI] Deleted {deletedCount} claimed messages. Last error: {lastError}");
                this.DoRefresh();
            });
        }

        private void ShowLoading()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.Flex;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.None;
            if (this.mailList     != null) this.mailList.style.display      = DisplayStyle.None;
        }

        private void ShowEmpty()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.Flex;
            if (this.mailList     != null) this.mailList.style.display      = DisplayStyle.None;
        }

        private void ShowList()
        {
            if (this.loadingState != null) this.loadingState.style.display = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display   = DisplayStyle.None;
            if (this.mailList     != null) this.mailList.style.display      = DisplayStyle.Flex;
        }

        private void Render()
        {
            if (this.mailList == null) return;

            MailboxMessage[] messages = this.list.Messages;
            if (messages == null || messages.Length == 0)
            {
                this.ShowEmpty();
                this.UpdateHeaderActions();
                return;
            }

            this.mailList.Clear();
            foreach (MailboxMessage msg in messages)
                this.mailList.Add(this.BuildMessageRow(msg));

            this.ShowList();
            this.UpdateHeaderActions();
        }

        private void UpdateHeaderActions()
        {
            MailboxMessage[] messages = this.list.Messages;
            bool canClaimAll = false;
            bool canDeleteAllClaimed = false;

            if (messages != null)
            {
                foreach (MailboxMessage message in messages)
                {
                    bool hasAttachments = message.attachments != null && message.attachments.Length > 0;
                    if (hasAttachments && string.IsNullOrEmpty(message.claimed_at)) canClaimAll = true;
                    if (!string.IsNullOrEmpty(message.claimed_at)) canDeleteAllClaimed = true;
                }
            }

            if (this.claimAllBtn != null)
                this.claimAllBtn.SetEnabled(!this.isBulkActionRunning && canClaimAll);
            if (this.deleteAllClaimedBtn != null)
                this.deleteAllClaimedBtn.SetEnabled(!this.isBulkActionRunning && canDeleteAllClaimed);
        }

        private VisualElement BuildMessageRow(MailboxMessage msg)
        {
            bool isUnread       = string.IsNullOrEmpty(msg.read_at);
            bool isClaimed      = !string.IsNullOrEmpty(msg.claimed_at);
            bool hasAttachments = msg.attachments != null && msg.attachments.Length > 0;
            bool canClaim       = hasAttachments && !isClaimed;
            // Only allow delete when there are no unclaimed attachments, to prevent loss of rewards.
            bool canDelete      = !hasAttachments || isClaimed;

            VisualElement row = new VisualElement();
            row.AddToClassList("mb-msg-row");
            if (isUnread) row.AddToClassList("mb-msg-row--unread");

            // Unread dot (hidden via Visibility so it still takes up space for alignment)
            VisualElement dot = new VisualElement();
            dot.AddToClassList("mb-msg-row__dot");
            dot.style.visibility = isUnread ? Visibility.Visible : Visibility.Hidden;
            row.Add(dot);

            // Text content
            VisualElement content = new VisualElement();
            content.AddToClassList("mb-msg-row__content");

            Label subject = new Label(string.IsNullOrEmpty(msg.subject) ? "(No subject)" : msg.subject);
            subject.AddToClassList("mb-msg-row__subject");
            content.Add(subject);

            if (!string.IsNullOrEmpty(msg.body))
            {
                string bodyText = msg.body.Length > 80 ? msg.body.Substring(0, 80) + "…" : msg.body;
                Label body = new Label(bodyText);
                body.AddToClassList("mb-msg-row__body");
                content.Add(body);
            }

            if (hasAttachments)
                content.Add(this.BuildAttachmentRow(msg.attachments));

            Label date = new Label(this.FormatDate(msg.created_at));
            date.AddToClassList("mb-msg-row__date");
            content.Add(date);

            row.Add(content);

            // Right side: claim button or claimed badge, then delete button
            VisualElement right = new VisualElement();
            right.AddToClassList("mb-msg-row__right");

            if (canClaim)
            {
                string msgId = msg.id;
                Button claimBtn = new Button();
                claimBtn.text = "Claim";
                claimBtn.AddToClassList("mb-claim-btn");
                claimBtn.clicked += () =>
                {
                    claimBtn.SetEnabled(false);
                    this.list.ClaimMessage(
                        msgId,
                        onSuccess: _ => this.list.Refresh(),
                        onError: err =>
                        {
                            claimBtn.SetEnabled(true);
                            ToastMessage.ShowError(QuestActionErrorFormatter.Format(err), claimBtn);
                            Debug.LogWarning($"[MailboxContentUI] ClaimMessage failed ({msgId}): {err}");
                        }
                    );
                };
                right.Add(claimBtn);
            }
            else if (isClaimed)
            {
                Label badge = new Label("✓ Claimed");
                badge.AddToClassList("mb-status-badge");
                badge.AddToClassList("mb-status-badge--claimed");
                right.Add(badge);
            }

            // Delete button — only shown when there are no unclaimed attachments
            if (canDelete)
            {
                string deleteMsgId = msg.id;
                Button deleteBtn = new Button();
                deleteBtn.text = "🗑";
                deleteBtn.AddToClassList("mb-delete-btn");
                deleteBtn.clicked += () =>
                {
                    deleteBtn.SetEnabled(false);
                    this.list.DeleteMessage(
                        deleteMsgId,
                        onSuccess: _ => this.list.Refresh(),
                        onError: err =>
                        {
                            deleteBtn.SetEnabled(true);
                            Debug.LogWarning($"[MailboxContentUI] DeleteMessage failed ({deleteMsgId}): {err}");
                        }
                    );
                };
                right.Add(deleteBtn);
            }

            row.Add(right);
            return row;
        }

        private VisualElement BuildAttachmentRow(MailBoxAttachment[] attachments)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("mb-attachments");

            foreach (MailBoxAttachment att in attachments)
                row.Add(this.BuildAttachmentChip(att));

            return row;
        }

        private VisualElement BuildAttachmentChip(MailBoxAttachment att)
        {
            ItemDefinitionData def = att.item_definition;

            string displayName = def?.name
                              ?? att.definition_id
                              ?? att.type
                              ?? "?";

            string rarity   = def?.rarity   ?? string.Empty;
            string category = def?.category ?? string.Empty;

            VisualElement chip = new VisualElement();
            chip.AddToClassList("mb-attachment-chip");
            if (!string.IsNullOrEmpty(rarity))
                chip.AddToClassList($"mb-attachment-chip--{rarity.ToLower()}");

            Label nameLabel = new Label($"🎁 {displayName} x{att.quantity}");
            nameLabel.AddToClassList("mb-attachment-chip__name");
            chip.Add(nameLabel);

            if (!string.IsNullOrEmpty(rarity) || !string.IsNullOrEmpty(category))
            {
                VisualElement meta = new VisualElement();
                meta.AddToClassList("mb-attachment-chip__meta");

                if (!string.IsNullOrEmpty(rarity))
                {
                    Label rarityLabel = new Label(rarity);
                    rarityLabel.AddToClassList("mb-attachment-chip__rarity");
                    rarityLabel.AddToClassList($"mb-attachment-chip__rarity--{rarity.ToLower()}");
                    meta.Add(rarityLabel);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    Label catLabel = new Label(category);
                    catLabel.AddToClassList("mb-attachment-chip__category");
                    meta.Add(catLabel);
                }

                chip.Add(meta);
            }

            return chip;
        }

        private string FormatDate(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate)) return string.Empty;
            if (DateTime.TryParse(isoDate, out DateTime dt))
                return dt.ToString("dd/MM/yyyy HH:mm");
            return isoDate.Length >= 10 ? isoDate.Substring(0, 10) : isoDate;
        }
    }
}
