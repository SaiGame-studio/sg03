using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/Battle/Client Actions")]
    public class ClientActions : SaiBehaviour
    {
        [SerializeField] private BattleState  battleState;
        [SerializeField] private CardSpawning cardSpawning;

        [Header("Action Log")]
        [SerializeField] private List<ClientActionLog> actionLog = new List<ClientActionLog>();

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleState();
            this.LoadCardSpawning();
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleState = ctrl.BattleState;
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        protected virtual void LoadCardSpawning()
        {
            if (this.cardSpawning != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.cardSpawning = ctrl.CardSpawning;
            Debug.LogWarning(this.transform.name + ": LoadCardSpawning", this.gameObject);
        }

        private void OnEnable()  => this.SubscribeEvents();
        private void OnDisable() => this.UnsubscribeEvents();

        private void SubscribeEvents()
        {
            if (this.battleState == null) return;
            this.battleState.OnClientActionsChanged += this.HandleClientActions;
        }

        private void UnsubscribeEvents()
        {
            if (this.battleState == null) return;
            this.battleState.OnClientActionsChanged -= this.HandleClientActions;
        }

        private void HandleClientActions(string[] actions)
        {
            if (actions == null) return;
            this.BuildActionLog(actions);
            this.ExecutePendingActions();
        }

        private void BuildActionLog(string[] actions)
        {
            this.actionLog.Clear();
            foreach (string entry in actions)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                int colonIndex = entry.IndexOf(':');
                string name   = colonIndex >= 0 ? entry.Substring(0, colonIndex) : entry;
                string @params = colonIndex >= 0 ? entry.Substring(colonIndex + 1) : string.Empty;
                this.actionLog.Add(new ClientActionLog(name, @params));
            }
        }

        private void ExecutePendingActions()
        {
            foreach (ClientActionLog log in this.actionLog)
            {
                if (log.Executed) continue;
                this.DispatchAction(log);
            }
        }

        private void DispatchAction(ClientActionLog log)
        {
            string[] parameters = string.IsNullOrEmpty(log.Parameters)
                ? System.Array.Empty<string>()
                : log.Parameters.Split(',');
            switch (log.ActionName)
            {
                case "alpha_source_spawn_card": this.ExecuteAlphaSourceSpawnCard(parameters); break;
                case "omega_source_spawn_card": this.ExecuteOmegaSourceSpawnCard(parameters); break;
                default: Debug.LogWarning($"[ClientActions] Unknown action: {log.ActionName}", this.gameObject); break;
            }
            log.MarkExecuted();
        }

        private void ExecuteAlphaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return;
            this.cardSpawning?.SpawnAlphaSourceCards(count);
        }

        private void ExecuteOmegaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return;
            this.cardSpawning?.SpawnOmegaSourceCards(count);
        }

        private bool TryParseCount(string[] parameters, out int count)
        {
            count = 0;
            if (parameters == null || parameters.Length == 0) return false;
            return int.TryParse(parameters[0].Trim(), out count);
        }
    }
}
