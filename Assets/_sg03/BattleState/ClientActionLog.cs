using System;
using UnityEngine;

namespace SG03
{
    [Serializable]
    public class ClientActionLog
    {
        [SerializeField] private string actionName;
        [SerializeField] private string actionId;
        [SerializeField] private string parameters;
        [SerializeField] private bool   executed;

        public string ActionId    => this.actionId;
        public string ActionName  => this.actionName;
        public string Parameters  => this.parameters;
        public bool   Executed    => this.executed;

        public ClientActionLog(string actionId, string actionName, string parameters)
        {
            this.actionId   = actionId;
            this.actionName = actionName;
            this.parameters = parameters;
            this.executed   = false;
        }

        public void MarkExecuted() => this.executed = true;
    }
}
