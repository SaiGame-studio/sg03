using System;
using System.Collections.Generic;
using SaiGame.Services;

namespace SG03.UI
{
    // Intermediary between MailboxContentUI and the SaiServer Mailbox component.
    // Fetches the message list on demand and fires OnDataUpdated when the data changes.
    public class MailboxList
    {
        public event Action OnDataUpdated;

        public MailboxMessage[] Messages { get; private set; }
        public bool HasData => this.Messages != null;
        public bool IsLoading { get; private set; }

        private readonly Mailbox mailbox;

        public MailboxList()
        {
            this.mailbox = UnityEngine.Object.FindFirstObjectByType<Mailbox>(UnityEngine.FindObjectsInactive.Include);
        }

        public void Refresh()
        {
            if (this.mailbox == null)
            {
                this.Messages = Array.Empty<MailboxMessage>();
                this.OnDataUpdated?.Invoke();
                return;
            }

            if (this.IsLoading) return;

            this.IsLoading = true;
            this.mailbox.GetMessages(
                onSuccess: response =>
                {
                    this.IsLoading = false;
                    this.Messages  = response?.messages ?? Array.Empty<MailboxMessage>();
                    this.OnDataUpdated?.Invoke();
                },
                onError: _ =>
                {
                    this.IsLoading = false;
                    this.Messages  = Array.Empty<MailboxMessage>();
                    this.OnDataUpdated?.Invoke();
                }
            );
        }

        public void ClaimMessage(string messageId, Action<MailboxMessage> onSuccess, Action<string> onError)
        {
            if (this.mailbox == null)
            {
                onError?.Invoke("Mailbox service not available.");
                return;
            }

            this.mailbox.ClaimMessage(messageId, onSuccess, onError);
        }

        public void DeleteMessage(string messageId, Action<string> onSuccess, Action<string> onError)
        {
            if (this.mailbox == null)
            {
                onError?.Invoke("Mailbox service not available.");
                return;
            }

            this.mailbox.DeleteMessage(messageId, onSuccess, onError);
        }

        public void ClaimAllMessages(Action<MailboxMessage[]> onSuccess, Action<string> onError)
        {
            if (this.mailbox == null)
            {
                onError?.Invoke("Mailbox service not available.");
                return;
            }

            var claimableMessages = new List<MailboxMessage>();
            foreach (MailboxMessage message in this.Messages ?? Array.Empty<MailboxMessage>())
            {
                bool hasAttachments = message.attachments != null && message.attachments.Length > 0;
                if (hasAttachments && string.IsNullOrEmpty(message.claimed_at) && !string.IsNullOrEmpty(message.id))
                    claimableMessages.Add(message);
            }

            if (claimableMessages.Count == 0)
            {
                onError?.Invoke("No unclaimed messages found.");
                return;
            }

            this.ClaimMessages(claimableMessages, 0, new List<MailboxMessage>(), null, onSuccess, onError);
        }

        // Claim one message at a time. A failure is recorded but must not prevent
        // the remaining mailbox rewards from being claimed.
        private void ClaimMessages(
            List<MailboxMessage> messages,
            int index,
            List<MailboxMessage> claimed,
            string lastError,
            Action<MailboxMessage[]> onSuccess,
            Action<string> onError)
        {
            if (index >= messages.Count)
            {
                if (claimed.Count > 0)
                    onSuccess?.Invoke(claimed.ToArray());
                else
                    onError?.Invoke(lastError ?? "Failed to claim any messages.");
                return;
            }

            MailboxMessage message = messages[index];
            this.ClaimMessage(
                message.id,
                claimedMessage =>
                {
                    claimed.Add(claimedMessage ?? message);
                    this.ClaimMessages(messages, index + 1, claimed, lastError, onSuccess, onError);
                },
                error => this.ClaimMessages(messages, index + 1, claimed, error, onSuccess, onError)
            );
        }

        // The mailbox API only exposes deletion for one message at a time, so delete
        // claimed messages sequentially to avoid racing its local message cache.
        public void DeleteAllClaimedMessages(Action<int, string> onComplete)
        {
            var messageIds = new List<string>();
            foreach (MailboxMessage message in this.Messages ?? Array.Empty<MailboxMessage>())
            {
                if (!string.IsNullOrEmpty(message.id) && !string.IsNullOrEmpty(message.claimed_at))
                    messageIds.Add(message.id);
            }

            this.DeleteClaimedMessages(messageIds, 0, 0, null, onComplete);
        }

        private void DeleteClaimedMessages(List<string> messageIds, int index, int deletedCount, string lastError, Action<int, string> onComplete)
        {
            if (index >= messageIds.Count)
            {
                onComplete?.Invoke(deletedCount, lastError);
                return;
            }

            this.DeleteMessage(
                messageIds[index],
                _ => this.DeleteClaimedMessages(messageIds, index + 1, deletedCount + 1, lastError, onComplete),
                error => this.DeleteClaimedMessages(messageIds, index + 1, deletedCount, error, onComplete)
            );
        }
    }
}
