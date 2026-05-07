using System.Collections;
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
        [SerializeField] private float actionInterval = 0.1f;

        [Header("Alpha Action Log")]
        [SerializeField] private List<ClientActionLog> alphaActionLog = new List<ClientActionLog>();

        [Header("Omega Action Log")]
        [SerializeField] private List<ClientActionLog> omegaActionLog = new List<ClientActionLog>();

        private Coroutine alphaDispatchRoutine;
        private Coroutine omegaDispatchRoutine;

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
            this.BuildActionLogs(actions);
            this.StartAlphaDispatch();
            this.StartOmegaDispatch();
        }

        private void BuildActionLogs(string[] actions)
        {
            Debug.Log($"<color=#FFD700><b>[ClientActions] Received {actions.Length} action(s)</b></color>", this.gameObject);
            foreach (string entry in actions)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                int colonIndex = entry.IndexOf(':');
                string name    = colonIndex >= 0 ? entry.Substring(0, colonIndex) : entry;
                string @params = colonIndex >= 0 ? entry.Substring(colonIndex + 1) : string.Empty;
                if (this.IsDuplicateAction(name, @params)) continue;
                ClientActionLog log = new ClientActionLog(name, @params);
                if (name.StartsWith("alpha_")) this.alphaActionLog.Add(log);
                else if (name.StartsWith("omega_")) this.omegaActionLog.Add(log);
                else Debug.LogWarning($"[ClientActions] Cannot categorize action: {name}", this.gameObject);
            }
        }

        private bool IsDuplicateAction(string actionName, string parameters)
        {
            List<ClientActionLog> list = actionName.StartsWith("alpha_") ? this.alphaActionLog : this.omegaActionLog;
            foreach (ClientActionLog existing in list)
            {
                if (existing.ActionName == actionName && existing.Parameters == parameters) return true;
            }
            return false;
        }

        private void StartAlphaDispatch()
        {
            if (this.alphaDispatchRoutine != null) this.StopCoroutine(this.alphaDispatchRoutine);
            this.alphaDispatchRoutine = this.StartCoroutine(this.AlphaDispatchRoutine());
        }

        private void StartOmegaDispatch()
        {
            if (this.omegaDispatchRoutine != null) this.StopCoroutine(this.omegaDispatchRoutine);
            this.omegaDispatchRoutine = this.StartCoroutine(this.OmegaDispatchRoutine());
        }

        private IEnumerator AlphaDispatchRoutine()
        {
            foreach (ClientActionLog log in this.alphaActionLog)
            {
                if (log.Executed) continue;
                Coroutine actionRoutine = this.ExecuteAction(log);
                if (actionRoutine != null) yield return actionRoutine;
                yield return new WaitForSeconds(this.actionInterval);
            }
            this.alphaDispatchRoutine = null;
        }

        private IEnumerator OmegaDispatchRoutine()
        {
            foreach (ClientActionLog log in this.omegaActionLog)
            {
                if (log.Executed) continue;
                Coroutine actionRoutine = this.ExecuteAction(log);
                if (actionRoutine != null) yield return actionRoutine;
                yield return new WaitForSeconds(this.actionInterval);
            }
            this.omegaDispatchRoutine = null;
        }

        private Coroutine ExecuteAction(ClientActionLog log)
        {
            Debug.Log($"[ClientActions] <color=#88FFFF>Executing:</color> <b>{log.ActionName}</b> | {(string.IsNullOrEmpty(log.Parameters) ? "(no params)" : log.Parameters)} | executed={log.Executed}", this.gameObject);
            string[] parameters = string.IsNullOrEmpty(log.Parameters)
                ? System.Array.Empty<string>()
                : log.Parameters.Split(',');
            Coroutine result = null;
            bool handled = true;
            switch (log.ActionName)
            {
                case "alpha_source_spawn_card": result = this.ExecuteAlphaSourceSpawnCard(parameters); break;
                case "omega_source_spawn_card": result = this.ExecuteOmegaSourceSpawnCard(parameters); break;
                case "alpha_source_to_hand":       result = this.ExecuteAlphaSourceToHand(parameters);    break;
                case "omega_source_to_hand":       this.ExecuteOmegaSourceToHand(parameters);             break;
                case "alpha_hand_to_front_line":   this.ExecuteAlphaHandToFrontLine(parameters);          break;
                case "alpha_hand_to_back_line":    this.ExecuteAlphaHandToBackLine(parameters);           break;
                case "omega_hand_to_front_line":   this.ExecuteOmegaHandToFrontLine(parameters);          break;
                case "omega_hand_to_back_line":    this.ExecuteOmegaHandToBackLine(parameters);           break;
                default:
                    Debug.LogWarning($"[ClientActions] Unknown action: {log.ActionName}", this.gameObject);
                    handled = false;
                    break;
            }
            if (handled) log.MarkExecuted();
            return result;
        }

        private Coroutine ExecuteAlphaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return null;
            return this.cardSpawning?.SpawnAlphaSourceCards(count);
        }

        private Coroutine ExecuteOmegaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return null;
            return this.cardSpawning?.SpawnOmegaSourceCards(count);
        }

        private Coroutine ExecuteAlphaSourceToHand(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaSourceToHandRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaSourceToHandRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.SetAlphaSourceCardData(inventoryItemId);
            yield return new WaitForSeconds(this.actionInterval);
            this.cardSpawning?.CommitAlphaSourceToHand(card, inventoryItemId, slotIndex);
        }

        private void ExecuteOmegaSourceToHand(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return;
            this.cardSpawning?.MoveOmegaSourceToHand(inventoryItemId, slotIndex);
        }

        private void ExecuteAlphaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return;
            this.cardSpawning?.MoveAlphaHandToFrontLine(inventoryItemId, slotIndex);
        }

        private void ExecuteAlphaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return;
            this.cardSpawning?.MoveAlphaHandToBackLine(inventoryItemId, slotIndex);
        }

        private void ExecuteOmegaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return;
            this.cardSpawning?.MoveOmegaHandToFrontLine(inventoryItemId, slotIndex);
        }

        private void ExecuteOmegaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return;
            this.cardSpawning?.MoveOmegaHandToBackLine(inventoryItemId, slotIndex);
        }

        private bool TryParseSourceToHand(string[] parameters, out string inventoryItemId, out int slotIndex)
        {
            inventoryItemId = null;
            slotIndex = 0;
            if (parameters == null || parameters.Length < 2) return false;
            inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return false;
            return int.TryParse(parameters[1].Trim(), out slotIndex);
        }

        private bool TryParseCount(string[] parameters, out int count)
        {
            count = 0;
            if (parameters == null || parameters.Length == 0) return false;
            return int.TryParse(parameters[0].Trim(), out count);
        }
    }
}
