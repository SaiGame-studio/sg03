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

        [Header("Action Log")]
        [SerializeField] private List<ClientActionLog> actionLog = new List<ClientActionLog>();

        private Coroutine dispatchRoutine;

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
            this.StartDispatch();
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
                Debug.Log($"[ClientActions] Parsed action: <b>{name}</b> | params: {(@params.Length > 0 ? @params : "(none)")}", this.gameObject);
                if (this.IsDuplicateAction(name, @params))
                {
                    Debug.Log($"[ClientActions] Skipped duplicate: <b>{name}</b> | {(@params.Length > 0 ? @params : "(none)")}", this.gameObject);
                    continue;
                }
                if (!name.StartsWith("alpha_") && !name.StartsWith("omega_"))
                {
                    Debug.LogWarning($"[ClientActions] Cannot categorize action: {name}", this.gameObject);
                    continue;
                }
                this.actionLog.Add(new ClientActionLog(name, @params));
            }
        }

        private bool IsDuplicateAction(string actionName, string parameters)
        {
            foreach (ClientActionLog existing in this.actionLog)
            {
                if (existing.ActionName == actionName && existing.Parameters == parameters) return true;
            }
            return false;
        }

        private void StartDispatch()
        {
            if (this.dispatchRoutine != null) this.StopCoroutine(this.dispatchRoutine);
            this.dispatchRoutine = this.StartCoroutine(this.DispatchRoutine());
        }

        private IEnumerator DispatchRoutine()
        {
            yield return this.StartCoroutine(this.DispatchParallelFirstTwo());
            yield return this.StartCoroutine(this.DispatchSequentialRemaining());
            this.dispatchRoutine = null;
        }

        private IEnumerator DispatchParallelFirstTwo()
        {
            int launched = 0;
            int done = 0;
            foreach (ClientActionLog log in this.actionLog)
            {
                if (log.Executed) continue;
                if (launched >= 2) break;
                Coroutine actionRoutine = this.ExecuteAction(log);
                launched++;
                if (actionRoutine != null)
                    this.StartCoroutine(this.WaitThenSignal(actionRoutine, () => done++));
                else
                    done++;
            }
            yield return new UnityEngine.WaitUntil(() => done >= launched);
        }

        private IEnumerator WaitThenSignal(Coroutine routine, System.Action onDone)
        {
            yield return routine;
            onDone();
        }

        private IEnumerator DispatchSequentialRemaining()
        {
            foreach (ClientActionLog log in this.actionLog)
            {
                if (log.Executed) continue;
                Coroutine actionRoutine = this.ExecuteAction(log);
                if (actionRoutine != null) yield return actionRoutine;
                yield return new WaitForSeconds(this.actionInterval);
            }
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
                case "alpha_card_damaged":         this.ExecuteCardDamaged(parameters);                   break;
                case "omega_card_damaged":         this.ExecuteCardDamaged(parameters);                   break;
                case "alpha_card_sent_to_void":    this.ExecuteAlphaCardSentToVoid(parameters);           break;
                case "omega_card_sent_to_void":    this.ExecuteOmegaCardSentToVoid(parameters);           break;
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

        private void ExecuteCardDamaged(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return;
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            card?.RunUp();
        }

        private void ExecuteAlphaCardSentToVoid(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return;
            this.cardSpawning?.MoveAlphaCardToVoid(inventoryItemId);
        }

        private void ExecuteOmegaCardSentToVoid(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return;
            this.cardSpawning?.MoveOmegaCardToVoid(inventoryItemId);
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
