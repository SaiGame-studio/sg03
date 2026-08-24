using System;
using System.Collections;
using System.Collections.Generic;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/Battle/Client Actions")]
    public partial class ClientActions : SaiBehaviour
    {
        [SerializeField] private BattleState battleState;
        [SerializeField] private BattleScripts battleScripts;
        [SerializeField] private CardSpawning cardSpawning;
        [SerializeField] private BattleCardDefinitions battleCardDefinitions;
        [SerializeField] private LampOfSoulCtrl lampOfSoul;
        [SerializeField] private CardSelection cardSelection;
        [SerializeField] private DeskPositionCtrl deskPosition;
        [SerializeField] private BattleStateCtrl battleStateCtrl;
        [SerializeField] private float actionInterval = 0.1f;
        [SerializeField] private float omegaFrontLinePostDelay = 0.5f;

        [Header("Action Log")]
        [SerializeField] private bool logActions = false;

        private Coroutine dispatchRoutine;
        [SerializeField] private bool hasPendingActions;

        /// <summary>True while client actions are still being dispatched.</summary>
        public bool IsDispatching => this.dispatchRoutine != null;

        /// <summary>True while there are client actions that have not finished yet.</summary>
        public bool HasPendingActions => this.hasPendingActions;

        [SerializeField] private bool isResuming;
        /// <summary>True only while the explicitly requested battle-resume sequence is being dispatched.</summary>
        public bool IsResuming => this.isResuming;

        /// <summary>The side whose character HP bars are hidden for the current turn.</summary>
        // Battles begin on alpha's turn. Later turn-end actions are the sole source
        // that changes this value; do not derive it from BattleState.NextMove.
        public Owner? HpBarHiddenOwner { get; private set; } = Owner.alpha;

        public event Action<string> OnBattleCompleted;
        public event Action<string> OnCardTakeDamageExecuted;

        /// <summary>Marks the next received action sequence as an explicit battle resume.</summary>
        public void BeginResume()
        {
            this.isResuming = true;
        }

        /// <summary>Cancels resume mode when the battle-status request fails or is abandoned.</summary>
        public void CancelResume()
        {
            this.isResuming = false;
        }

        [SerializeField] private List<ClientActionLog> actionLog = new List<ClientActionLog>();

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleState();
            this.LoadBattleScripts();
            this.LoadCardSpawning();
            this.LoadBattleCardDefinitions();
            this.LoadLampOfSoul();
            this.LoadCardSelection();
            this.LoadDeskPosition();
            this.LoadBattleStateCtrl();
        }

        protected virtual void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = this.GetComponent<BattleStateCtrl>();
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleState = ctrl.BattleState;
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        protected virtual void LoadBattleScripts()
        {
            if (this.battleScripts != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleScripts = ctrl.BattleScripts;
            Debug.LogWarning(this.transform.name + ": LoadBattleScripts", this.gameObject);
        }

        protected virtual void LoadCardSpawning()
        {
            if (this.cardSpawning != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.cardSpawning = ctrl.CardSpawning;
            Debug.LogWarning(this.transform.name + ": LoadCardSpawning", this.gameObject);
        }

        protected virtual void LoadBattleCardDefinitions()
        {
            if (this.battleCardDefinitions != null) return;
            BattleStateCtrl ctrl = this.GetComponent<BattleStateCtrl>();
            if (ctrl == null) return;
            this.battleCardDefinitions = ctrl.BattleCardDefinitions;
            Debug.LogWarning(this.transform.name + ": LoadBattleCardDefinitions", this.gameObject);
        }

        protected virtual void LoadLampOfSoul()
        {
            if (this.lampOfSoul != null) return;
            this.lampOfSoul = UnityEngine.Object.FindFirstObjectByType<LampOfSoulCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadLampOfSoul", this.gameObject);
        }

        protected virtual void LoadCardSelection()
        {
            if (this.cardSelection != null) return;
            this.cardSelection = UnityEngine.Object.FindFirstObjectByType<CardSelection>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadCardSelection", this.gameObject);
        }

        protected virtual void LoadDeskPosition()
        {
            if (this.deskPosition != null) return;
            this.deskPosition = UnityEngine.Object.FindFirstObjectByType<DeskPositionCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadDeskPosition", this.gameObject);
        }

        private void OnEnable() => this.SubscribeEvents();
        private void OnDisable() => this.UnsubscribeEvents();

        private void SubscribeEvents()
        {
            if (this.battleState == null) return;
            this.battleState.OnClientActionsChanged += this.HandleClientActions;
        }

        private void UnsubscribeEvents()
        {
            BattleCardDefinitions.OnDefinitionsLoaded -= this.OnDefinitionsLoaded;
            if (this.battleState == null) return;
            this.battleState.OnClientActionsChanged -= this.HandleClientActions;
        }

        private void HandleClientActions(string[] actions)
        {
            if (actions == null) return;
            this.BuildActionLogs(actions);
            this.TryStartDispatchWhenDefinitionsLoaded();
        }

        private void TryStartDispatchWhenDefinitionsLoaded()
        {
            this.hasPendingActions = this.HasUnexecutedActions();
            if (this.battleCardDefinitions != null && this.battleCardDefinitions.IsLoaded)
            {
                this.StartDispatch();
                return;
            }
            if (this.logActions) Debug.Log("<color=#88FFFF>[ClientActions]</color> <color=#FFD700>Waiting for BattleCardDefinitions to load before dispatching actions...</color>", this.gameObject);
            BattleCardDefinitions.OnDefinitionsLoaded -= this.OnDefinitionsLoaded;
            BattleCardDefinitions.OnDefinitionsLoaded += this.OnDefinitionsLoaded;
        }

        private void OnDefinitionsLoaded()
        {
            BattleCardDefinitions.OnDefinitionsLoaded -= this.OnDefinitionsLoaded;
            this.StartDispatch();
        }

        private void BuildActionLogs(string[] actions)
        {
            // Debug.Log($"<color=#88FFFF>[ClientActions]</color> <color=#FFD700><b>Received {actions.Length} action(s)</b></color>", this.gameObject);
            foreach (string entry in actions)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                this.ParseAndAddAction(entry);
            }
        }

        private void ParseAndAddAction(string entry)
        {
            int firstColon = entry.IndexOf(':');
            string id = firstColon >= 0 ? entry.Substring(0, firstColon) : entry;
            string rest = firstColon >= 0 ? entry.Substring(firstColon + 1) : string.Empty;
            int secondColon = rest.IndexOf(':');
            string name = secondColon >= 0 ? rest.Substring(0, secondColon) : rest;
            string @params = secondColon >= 0 ? rest.Substring(secondColon + 1) : string.Empty;
            if (this.IsDuplicateAction(id)) return;
            this.actionLog.Add(new ClientActionLog(id, name, @params));
        }

        private bool IsDuplicateAction(string actionId)
        {
            foreach (ClientActionLog existing in this.actionLog)
            {
                if (existing.ActionId == actionId) return true;
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
            yield return this.StartCoroutine(this.DispatchSourceSpawnActions());
            int i = 0;
            while (i < this.actionLog.Count)
            {
                ClientActionLog log = this.actionLog[i];
                if (log.Executed) { i++; continue; }
                if (this.TryGetParallelGroupMatcher(log.ActionName, out Func<string, bool> matcher))
                {
                    int groupEnd = this.FindConsecutiveParallelGroupEnd(i, matcher);
                    yield return this.StartCoroutine(this.DispatchParallelGroup(i, groupEnd));
                    float postDelay = this.GetPostGroupDelay(log.ActionName);
                    if (postDelay > 0f) yield return new WaitForSeconds(postDelay);
                    i = groupEnd;
                }
                else
                {
                    Coroutine actionRoutine = this.ExecuteAction(log);
                    if (actionRoutine != null) yield return actionRoutine;
                    yield return new WaitForSeconds(this.actionInterval);
                    i++;
                }
            }
            this.dispatchRoutine = null;
            this.hasPendingActions = this.HasUnexecutedActions();
            this.FinishResumeWhenActionsComplete();
        }

        private void FinishResumeWhenActionsComplete()
        {
            if (this.hasPendingActions || !this.isResuming) return;

            this.isResuming = false;
            this.cardSpawning?.SpawnHpBarsAfterResume();
        }

        private IEnumerator DispatchSourceSpawnActions()
        {
            int launched = 0;
            int done = 0;
            foreach (ClientActionLog log in this.actionLog)
            {
                if (log.Executed) continue;
                if (!this.IsSourceSpawnAction(log.ActionName)) continue;
                Coroutine c = this.ExecuteAction(log);
                launched++;
                if (c != null)
                    this.StartCoroutine(this.WaitThenSignal(c, () => done++));
                else
                    done++;
            }
            yield return new WaitUntil(() => done >= launched);
        }

        private bool IsSourceSpawnAction(string actionName)
            => actionName == "alpha_source_spawn_card" || actionName == "omega_source_spawn_card";

        private bool TryGetParallelGroupMatcher(string actionName, out Func<string, bool> matcher)
        {
            if (actionName == "alpha_source_spawn_card" || actionName == "omega_source_spawn_card")
            {
                matcher = static n => n == "alpha_source_spawn_card" || n == "omega_source_spawn_card";
                return true;
            }
            if (actionName == "alpha_source_to_hand")
            {
                matcher = static n => n == "alpha_source_to_hand";
                return true;
            }
            // omega_source_to_hand runs in parallel; slot collision is handled in
            // MoveOmegaSourceToHand by falling back to the next available slot.
            if (actionName == "omega_source_to_hand")
            {
                matcher = static n => n == "omega_source_to_hand";
                return true;
            }
            if (actionName == "omega_hand_to_front_line")
            {
                matcher = static n => n == "omega_hand_to_front_line";
                return true;
            }
            matcher = null;
            return false;
        }

        private float GetPostGroupDelay(string actionName)
        {
            if (actionName == "omega_hand_to_front_line") return this.omegaFrontLinePostDelay;
            return 0f;
        }

        private int FindConsecutiveParallelGroupEnd(int from, Func<string, bool> matcher)
        {
            int end = from;
            while (end < this.actionLog.Count && !this.actionLog[end].Executed && matcher(this.actionLog[end].ActionName))
                end++;
            return end;
        }

        private IEnumerator DispatchParallelGroup(int from, int to)
        {
            int launched = 0;
            int done = 0;
            for (int i = from; i < to; i++)
            {
                ClientActionLog log = this.actionLog[i];
                if (log.Executed) continue;
                Coroutine actionRoutine = this.ExecuteAction(log);
                launched++;
                if (actionRoutine != null)
                    this.StartCoroutine(this.WaitThenSignal(actionRoutine, () => done++));
                else
                    done++;
            }
            yield return new WaitUntil(() => done >= launched);
        }

        private IEnumerator WaitThenSignal(Coroutine routine, System.Action onDone)
        {
            yield return routine;
            onDone();
        }

        private void LogAction(ClientActionLog log)
        {
            if (!this.logActions) return;
            string paramsText = string.IsNullOrEmpty(log.Parameters) ? "(no params)" : log.Parameters;
            Debug.Log($"<color=#88FFFF>[ClientActions]</color> Executing: <b>{log.ActionName}</b> | {paramsText}", this.gameObject);
        }

        private Coroutine ExecuteAction(ClientActionLog log)
        {
            this.SyncActionMoveDuration();
            this.LogAction(log);
            string[] parameters = string.IsNullOrEmpty(log.Parameters)
                ? System.Array.Empty<string>()
                : log.Parameters.Split(',');
            Coroutine result = null;
            switch (log.ActionName)
            {
                case "next_move": result = this.ExecuteNextMove(log, parameters); break;
                case "alpha_source_spawn_card": result = this.ExecuteAlphaSourceSpawnCard(parameters); break;
                case "omega_source_spawn_card": result = this.ExecuteOmegaSourceSpawnCard(parameters); break;
                case "alpha_source_to_hand": result = this.ExecuteAlphaSourceToHand(parameters); break;
                case "omega_source_to_hand": result = this.ExecuteOmegaSourceToHand(parameters); break;
                case "alpha_hand_to_front_line": result = this.ExecuteAlphaHandToFrontLine(parameters); break;
                case "alpha_hand_to_back_line": result = this.ExecuteAlphaHandToBackLine(parameters); break;
                case "omega_hand_to_front_line": result = this.ExecuteOmegaHandToFrontLine(parameters); break;
                case "omega_hand_to_back_line": result = this.ExecuteOmegaHandToBackLine(parameters); break;
                case "alpha_void_to_front_line": result = this.ExecuteAlphaVoidToFrontLine(parameters); break;
                case "omega_void_to_front_line": result = this.ExecuteOmegaVoidToFrontLine(parameters); break;
                case "alpha_card_take_damage": result = this.ExecuteCardTakeDamage(parameters); break;
                case "omega_card_take_damage": result = this.ExecuteCardTakeDamage(parameters); break;
                case "alpha_card_expose": result = this.ExecuteCardExpose(parameters); break;
                case "omega_card_expose": result = this.ExecuteOmegaCardExpose(parameters); break;
                case "alpha_card_sent_to_void": result = this.ExecuteAlphaCardSentToVoid(parameters); break;
                case "omega_card_sent_to_void": result = this.ExecuteOmegaCardSentToVoid(parameters); break;
                case "alpha_attack": result = this.ExecuteAlphaAttack(parameters); break;
                case "alpha_attack_omega_hp": result = this.ExecuteAlphaAttackOmegaHp(parameters); break;
                case "alpha_card_ability": result = this.ExecuteCardAbility(parameters); break;
                case "omega_card_ability": result = this.ExecuteCardAbility(parameters); break;
                case "alpha_card_guarded": result = this.ExecuteCardGuarded(parameters); break;
                case "omega_card_guarded": result = this.ExecuteCardGuarded(parameters); break;
                case "alpha_card_swapped": result = this.ExecuteCardSwapped(parameters); break;
                case "omega_card_swapped": result = this.ExecuteCardSwapped(parameters); break;
                case "omega_attack": result = this.ExecuteOmegaAttack(parameters); break;
                case "omega_attack_alpha_hp": result = this.ExecuteOmegaAttackAlphaHp(parameters); break;
                case "omega_card_move_back_to_holder": result = this.ExecuteOmegaCardMoveBackToHolder(parameters); break;
                case "omega_planing_character_attack": result = this.ExecuteOmegaPlaningCharacterAttack(parameters); break;
                case "alpha_take_lamp": result = this.ExecuteLampMoveToAlpha(); break;
                case "omega_take_lamp": result = this.ExecuteLampMoveToOmega(); break;
                case "alpha_turn_end": result = this.ExecuteAlphaEndTurn(); break;
                case "omega_turn_end": result = this.ExecuteOmegaEndTurn(); break;
                case "battle_completed":
                    string winner = parameters.Length > 0 ? parameters[0].Trim() : string.Empty;
                    this.OnBattleCompleted?.Invoke(winner);
                    break;
                default:
                    Debug.LogWarning($"<color=#88FFFF>[ClientActions]</color> Unknown action: {log.ActionName}", this.gameObject);
                    break;
            }
            log.MarkExecuted();
            return result;
        }



        private void SyncActionMoveDuration()
        {
            if (this.cardSpawning == null) return;
            this.cardSpawning.ActionMoveDuration = this.battleStateCtrl != null ? this.battleStateCtrl.CardMoveDuration : 1f;
            this.cardSpawning.ActionRotateDuration = this.battleStateCtrl != null ? this.battleStateCtrl.CardRotateDuration : 0.4f;
        }

        private bool IsLocalPlayerDeploy(string inventoryItemId, Link link, int slotIndex)
        {
            return this.cardSelection != null && this.cardSelection.TryConsumePlayerDeploy(inventoryItemId, link, slotIndex);
        }

        private IEnumerator WaitForCard(Card3DCtrl card)
        {
            yield return new WaitUntil(() => !card.IsAnimating);
        }

        private IEnumerator WaitForLamp()
        {
            yield return new WaitUntil(() => this.lampOfSoul == null || !this.lampOfSoul.IsAnimating);
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

        private bool HasUnexecutedActions()
        {
            foreach (ClientActionLog log in this.actionLog)
            {
                if (!log.Executed) return true;
            }
            return false;
        }
    }
}
