using System;
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
    }
}
