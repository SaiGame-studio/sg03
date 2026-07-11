using System;
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
        [SerializeField] private BattleState          battleState;
        [SerializeField] private BattleScripts        battleScripts;
        [SerializeField] private CardSpawning         cardSpawning;
        [SerializeField] private BattleCardDefinitions battleCardDefinitions;
        [SerializeField] private LampOfSoulCtrl       lampOfSoul;
        [SerializeField] private CardSelection        cardSelection;
        [SerializeField] private DeskPositionCtrl     deskPosition;
        [SerializeField] private BattleStateCtrl      battleStateCtrl;
        [SerializeField] private float actionInterval = 0.1f;
        [SerializeField] private float omegaFrontLinePostDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool logActions = false;

        [Header("Action Log")]
        [SerializeField] private List<ClientActionLog> actionLog = new List<ClientActionLog>();

        private Coroutine dispatchRoutine;
        [SerializeField] private bool hasPendingActions;

        /// <summary>True while client actions are still being dispatched.</summary>
        public bool IsDispatching => this.dispatchRoutine != null;

        /// <summary>True while there are client actions that have not finished yet.</summary>
        public bool HasPendingActions => this.hasPendingActions;

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

        private void OnEnable()  => this.SubscribeEvents();
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
            string id   = firstColon >= 0 ? entry.Substring(0, firstColon) : entry;
            string rest = firstColon >= 0 ? entry.Substring(firstColon + 1) : string.Empty;
            int secondColon = rest.IndexOf(':');
            string name   = secondColon >= 0 ? rest.Substring(0, secondColon) : rest;
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
                case "next_move":                  result = this.ExecuteNextMove(log, parameters);         break;
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
                case "omega_card_expose":          result = this.ExecuteOmegaCardExpose(parameters);      break;
                case "alpha_card_sent_to_void":    result = this.ExecuteAlphaCardSentToVoid(parameters);  break;
                case "omega_card_sent_to_void":    result = this.ExecuteOmegaCardSentToVoid(parameters);  break;
                case "alpha_attack":               result = this.ExecuteAlphaAttack(parameters);           break;
                case "alpha_attack_omega_hp":       result = this.ExecuteAlphaAttackOmegaHp(parameters);   break;
                case "alpha_card_ability":         result = this.ExecuteCardAbility(parameters);           break;
                case "omega_card_ability":         result = this.ExecuteCardAbility(parameters);           break;
                case "omega_attack":               result = this.ExecuteOmegaAttack(parameters);           break;
                case "omega_card_move_back_to_holder": result = this.ExecuteOmegaCardMoveBackToHolder(parameters); break;
                case "omega_planing_character_attack": result = this.ExecuteOmegaPlaningCharacterAttack(parameters); break;
                case "alpha_take_lamp":             result = this.ExecuteLampMoveToAlpha();                break;
                case "omega_take_lamp":             result = this.ExecuteLampMoveToOmega();                break;
                case "omega_turn_end":              result = this.ExecuteOmegaEndTurn();                    break;
                default:
                    Debug.LogWarning($"<color=#88FFFF>[ClientActions]</color> Unknown action: {log.ActionName}", this.gameObject);
                    break;
            }
            log.MarkExecuted();
            return result;
        }

        private Coroutine ExecuteNextMove(ClientActionLog log, string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string moveType = parameters[0].Trim();
            
            if (moveType == "init_cards")
            {
                bool isLastAction = this.actionLog.IndexOf(log) == this.actionLog.Count - 1;
                if (!isLastAction)
                {
                    Debug.Log("<color=#88FFFF>[ClientActions]</color> Skip init_cards script because it is not the last action in the queue.", this.gameObject);
                }
                else if (this.battleScripts == null)
                {
                    Debug.Log("<color=#88FFFF>[ClientActions]</color> Skip init_cards script because BattleScripts reference is null.", this.gameObject);
                }
                else
                {
                    this.battleScripts.RunInitCards(
                        response => 
                        {
                            if (!string.IsNullOrWhiteSpace(response))
                                this.battleState?.UpdateFromBattleStatus(response);
                        },
                        error => Debug.LogWarning("<color=#88FFFF>[ClientActions]</color> Init cards failed: " + error, this.gameObject)
                    );
                }
            }
            return null;
        }

        private Coroutine ExecuteAlphaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return null;
            return this.StartCoroutine(this.WaitDefinitionsThenSpawn(() => this.cardSpawning?.SpawnAlphaSourceCards(count)));
        }

        private Coroutine ExecuteOmegaSourceSpawnCard(string[] parameters)
        {
            if (!this.TryParseCount(parameters, out int count)) return null;
            return this.StartCoroutine(this.WaitDefinitionsThenSpawn(() => this.cardSpawning?.SpawnOmegaSourceCards(count)));
        }

        private IEnumerator WaitDefinitionsThenSpawn(Func<Coroutine> spawnAction)
        {
            yield return new WaitUntil(() => this.battleCardDefinitions != null && this.battleCardDefinitions.IsLoaded);
            yield return spawnAction();
        }

        private Coroutine ExecuteAlphaSourceToHand(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaSourceToHandRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaSourceToHandRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.SetAlphaSourceCardData(inventoryItemId);
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] card={(card != null ? card.name : "NULL")}, id={inventoryItemId}, slot={slotIndex}");
            yield return new WaitForSeconds(this.actionInterval);
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] before commit — IsAnimating={card?.IsAnimating}, Location={card?.Location}");
            this.cardSpawning?.CommitAlphaSourceToHand(card, inventoryItemId, slotIndex);
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] after commit — IsAnimating={card?.IsAnimating}, Location={card?.Location}");
            if (card != null) yield return this.StartCoroutine(this.WaitForCard(card));
            if (this.logActions) Debug.Log($"[AlphaSourceToHand] WaitForCard done — IsAnimating={card?.IsAnimating}");
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
            return this.StartCoroutine(this.AlphaHandToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaHandToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) yield break;
            if (this.IsLocalPlayerDeploy(inventoryItemId, Link.front, slotIndex))
            {
                yield return this.StartCoroutine(this.WaitForCard(card));
                yield break;
            }
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveAlphaHandToFrontLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleAlphaHandInFrontLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaHandToBackLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaHandToBackLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) yield break;
            if (this.IsLocalPlayerDeploy(inventoryItemId, Link.back, slotIndex))
            {
                yield return this.StartCoroutine(this.WaitForCard(card));
                yield break;
            }
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveAlphaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleAlphaHandInBackLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaHandToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaHandToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaHandToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.PeekOmegaHandCard();
            if (card == null) yield break;
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveOmegaHandToFrontLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleOmegaHandInFrontLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaHandToBackLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaHandToBackLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaHandToBackLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.PeekOmegaHandCard();
            if (card == null) yield break;
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForCard(card));
            card = this.cardSpawning?.MoveOmegaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleOmegaHandInBackLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteCardDamaged(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) return null;
            card.Damaged();
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaCardExpose(string[] parameters)
        {
            return this.ExecuteCardExposeInternal(
                parameters,
                beforeExpose: inventoryItemId => this.cardSpawning?.LoadOmegaCardData(inventoryItemId));
        }

        private Coroutine ExecuteCardExpose(string[] parameters)
        {
            return this.ExecuteCardExposeInternal(parameters);
        }

        private Coroutine ExecuteCardExposeInternal(string[] parameters, System.Action<string> beforeExpose = null)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) return null;
            if (card.FaceState == FaceState.FaceUp) return null;
            return this.StartCoroutine(this.CardExposeRoutine(card, inventoryItemId, beforeExpose));
        }

        private IEnumerator CardExposeRoutine(Card3DCtrl card, string inventoryItemId, System.Action<string> beforeExpose = null)
        {
            beforeExpose?.Invoke(inventoryItemId);
            yield return this.StartCoroutine(this.PlayFaceUpAnimation(card));
        }

        private IEnumerator PlayFaceUpAnimation(Card3DCtrl card)
        {
            if (card == null || card.FaceState == FaceState.FaceUp) yield break;
            card.FaceUp();
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteAlphaCardSentToVoid(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveAlphaCardToVoid(inventoryItemId);
            if (card == null) return null;
            return this.StartCoroutine(this.AlphaCardToVoidRoutine(card));
        }

        private IEnumerator AlphaCardToVoidRoutine(Card3DCtrl card)
        {
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.RotateAlphaCardAtVoidTransit(card);
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleAlphaCardInVoid(card);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaCardSentToVoid(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.MoveOmegaCardToVoid(inventoryItemId);
            if (card == null) return null;
            return this.StartCoroutine(this.OmegaCardToVoidRoutine(card));
        }

        private IEnumerator OmegaCardToVoidRoutine(Card3DCtrl card)
        {
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.RotateOmegaCardAtVoidTransit(card);
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleOmegaCardInVoid(card);
            yield return this.StartCoroutine(this.WaitForCard(card));
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
            
            return this.StartCoroutine(this.AlphaAttackRoutine(attacker, defender));
        }

        private IEnumerator AlphaAttackRoutine(Card3DCtrl attacker, Card3DCtrl defender)
        {
            if (attacker.IsCharacter()) attacker.AttackLunge(defender.transform.position);
            else attacker.AbilityActive();
            
            defender.Damaged();

            yield return this.StartCoroutine(this.WaitForCard(attacker));
            yield return this.StartCoroutine(this.WaitForCard(defender));
        }

        private Coroutine ExecuteAlphaAttackOmegaHp(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string attackerId = parameters[0].Trim();
            if (string.IsNullOrEmpty(attackerId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            if (attacker == null || this.deskPosition == null) return null;
            
            if (attacker.IsCharacter())
            {
                attacker.AttackLunge(this.deskPosition.OmegaTheSource.position);
            }
            else
            {
                attacker.AbilityActive();
            }

            return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Coroutine ExecuteOmegaAttack(string[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            string attackerId = parameters[0].Trim();
            string defenderId = parameters[1].Trim();
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(defenderId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            Card3DCtrl defender = this.cardSpawning?.FindCardById(defenderId);
            if (attacker == null || defender == null) return null;

            return this.StartCoroutine(this.OmegaAttackRoutine(attacker, defender));
        }

        private IEnumerator OmegaAttackRoutine(Card3DCtrl attacker, Card3DCtrl defender)
        {
            if (attacker.IsCharacter()) attacker.AttackBackstepLunge(defender.transform.position);
            else attacker.AbilityActive();

            defender.Damaged();

            yield return this.StartCoroutine(this.WaitForCard(attacker));
            yield return this.StartCoroutine(this.WaitForCard(defender));
        }

        private Coroutine ExecuteOmegaCardMoveBackToHolder(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            string inventoryItemId = parameters[0].Trim();
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            if (card == null) return null;
            card.MoveBackToHolder();
            return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteCardAbility(string[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            
            string sourceId = null;
            string abilityName = null;
            string targetId = null;
            string selectedId = null;

            foreach (string p in parameters)
            {
                string[] kv = p.Split('=');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLower();
                    string value = kv[1].Trim();
                    if (key == "source") sourceId = value;
                    else if (key == "ability") abilityName = value;
                    else if (key == "target") targetId = value;
                    else if (key == "selected") selectedId = value;
                }
            }

            // Fallback for old format just in case
            if (string.IsNullOrEmpty(sourceId) && parameters.Length > 0 && !parameters[0].Contains("="))
            {
                sourceId = parameters[0].Trim();
            }

            if (string.IsNullOrEmpty(sourceId)) return null;
            return this.StartCoroutine(this.CardAbilityRoutine(sourceId, targetId, selectedId));
        }

        private IEnumerator CardAbilityRoutine(string sourceId, string targetId, string selectedId)
        {
            Card3DCtrl sourceCard = !string.IsNullOrEmpty(sourceId) ? this.cardSpawning?.FindCardById(sourceId) : null;
            Card3DCtrl targetCard = !string.IsNullOrEmpty(targetId) ? this.cardSpawning?.FindCardById(targetId) : null;
            Card3DCtrl selectedCard = !string.IsNullOrEmpty(selectedId) ? this.cardSpawning?.FindCardById(selectedId) : null;

            if (sourceCard != null) sourceCard.RunUp();
            if (selectedCard != null && targetCard != null) selectedCard.AttackLunge(targetCard.transform.position);
            if (targetCard != null) targetCard.Damaged();

            if (sourceCard != null) yield return this.StartCoroutine(this.WaitForCard(sourceCard));
            if (selectedCard != null) yield return this.StartCoroutine(this.WaitForCard(selectedCard));
            if (targetCard != null) yield return this.StartCoroutine(this.WaitForCard(targetCard));
        }

        private Coroutine ExecuteOmegaPlaningCharacterAttack(string[] parameters)
        {
            if (parameters == null || parameters.Length < 2) return null;
            string attackerId = parameters[0].Trim();
            string defenderId = parameters[1].Trim();
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(defenderId)) return null;
            Card3DCtrl attacker = this.cardSpawning?.FindCardById(attackerId);
            Card3DCtrl defender = this.cardSpawning?.FindCardById(defenderId);
            if (attacker == null || defender == null) return null;
            return this.StartCoroutine(this.OmegaPlaningCharacterAttackRoutine(attacker, defender));
        }

        private IEnumerator OmegaPlaningCharacterAttackRoutine(Card3DCtrl attacker, Card3DCtrl defender)
        {
            yield return this.StartCoroutine(this.WaitForCard(attacker));
            // Planning attack: card advances next to the defender on the side it came from
            // (target.x - 1 when attacking from the left, target.x + 1 when from the right).
            // No return — waits for alpha's decision in the next server response.
            Vector3 destination = this.BuildPlanningAttackDestination(attacker.transform.position, defender.transform.position);
            attacker.PlanningLungeTo(destination);
            yield return this.StartCoroutine(this.WaitForCard(attacker));
        }

        private Vector3 BuildPlanningAttackDestination(Vector3 attackerPosition, Vector3 defenderPosition)
        {
            float offsetX = attackerPosition.x < defenderPosition.x ? -1f : 1f;
            float offsetZ = attackerPosition.z < defenderPosition.z ? -9f : 9f;
            return new Vector3(defenderPosition.x + offsetX, defenderPosition.y + 0.2f, defenderPosition.z + offsetZ);
        }

        private Coroutine ExecuteLampMoveToAlpha()
        {
            if (this.lampOfSoul == null) return null;
            this.lampOfSoul.MoveToAlpha();
            return this.StartCoroutine(this.WaitForLamp());
        }

        private Coroutine ExecuteLampMoveToOmega()
        {
            if (this.lampOfSoul == null) return null;
            this.lampOfSoul.MoveToOmega();
            return this.StartCoroutine(this.WaitForLamp());
        }

        private Coroutine ExecuteOmegaEndTurn()
        {
            this.cardSelection?.ResetCharDeployCount();
            return null;
        }

        private void SyncActionMoveDuration()
        {
            if (this.cardSpawning == null) return;
            this.cardSpawning.ActionMoveDuration   = this.battleStateCtrl != null ? this.battleStateCtrl.CardMoveDuration : 1f;
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
