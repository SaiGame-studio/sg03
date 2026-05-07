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
        [SerializeField] private float actionMoveDuration   = 1f;
        [SerializeField] private float actionRotateDuration = 0.4f;

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
                if (this.IsDuplicateAction(name, @params)) continue;
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
            this.SyncActionMoveDuration();
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
                case "omega_source_to_hand":       result = this.ExecuteOmegaSourceToHand(parameters);    break;
                case "alpha_hand_to_front_line":   result = this.ExecuteAlphaHandToFrontLine(parameters); break;
                case "alpha_hand_to_back_line":    result = this.ExecuteAlphaHandToBackLine(parameters);  break;
                case "omega_hand_to_front_line":   result = this.ExecuteOmegaHandToFrontLine(parameters); break;
                case "omega_hand_to_back_line":    result = this.ExecuteOmegaHandToBackLine(parameters);  break;
                case "alpha_card_damaged":         result = this.ExecuteCardDamaged(parameters);           break;
                case "omega_card_damaged":         result = this.ExecuteCardDamaged(parameters);           break;
                case "alpha_card_expose":          result = this.ExecuteCardExpose(parameters);            break;
                case "omega_card_expose":          result = this.ExecuteCardExpose(parameters);            break;
                case "alpha_card_sent_to_void":    result = this.ExecuteAlphaCardSentToVoid(parameters);  break;
                case "omega_card_sent_to_void":    result = this.ExecuteOmegaCardSentToVoid(parameters);  break;
                case "alpha_attack":               result = this.ExecuteAlphaAttack(parameters);           break;
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

        private Coroutine ExecuteOmegaSourceToHand(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveOmegaSourceToHand(inventoryItemId, slotIndex);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveAlphaHandToFrontLine(inventoryItemId, slotIndex);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveAlphaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveOmegaHandToFrontLine(inventoryItemId, slotIndex);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveOmegaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteCardDamaged(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) return null;
            card.RunUp();
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteCardExpose(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) return null;
            if (card.FaceState == FaceState.FaceUp) return null;
            card.FaceUp();
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaCardSentToVoid(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveAlphaCardToVoid(inventoryItemId);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaCardSentToVoid(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveOmegaCardToVoid(inventoryItemId);
            if (card == null) return null;
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaAttack(string[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            string attackerId = parameters[0].Trim();
            string defenderId = parameters[1].Trim();
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(defenderId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            Card3DCtrl defender = this.cardSpawning?.FindCardById(defenderId);
            if (attacker == null || defender == null) return null;
            attacker.AttackLunge(defender.transform.position);
            return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private void SyncActionMoveDuration()
        {
            if (this.cardSpawning == null) return;
            this.cardSpawning.ActionMoveDuration   = this.actionMoveDuration;
            this.cardSpawning.ActionRotateDuration = this.actionRotateDuration;
        }

        private IEnumerator WaitForCard(Card3DCtrl card)
        {
            yield return new WaitUntil(() => !card.IsAnimating);
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
