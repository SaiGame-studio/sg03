using System;
using System.Collections;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    public partial class ClientActions
    {
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

        private Coroutine ExecuteAlphaHandToBackLine(ClientActionLog log, string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex))
            {
                this.LogAlphaHandToBackLine(log?.ActionId, "PARSE_FAILED", null, null, -1);
                return null;
            }
            return this.StartCoroutine(this.AlphaHandToBackLineRoutine(log.ActionId, inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaHandToBackLineRoutine(string actionId, string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.FindCardById(inventoryItemId);
            this.LogAlphaHandToBackLine(actionId, "START", card, inventoryItemId, slotIndex);
            if (card == null)
            {
                this.LogAlphaHandToBackLine(actionId, "CARD_NOT_FOUND", null, inventoryItemId, slotIndex);
                yield break;
            }
            if (this.IsLocalPlayerDeploy(inventoryItemId, Link.back, slotIndex))
            {
                this.LogAlphaHandToBackLine(actionId, "LOCAL_DEPLOY_CONSUMED", card, inventoryItemId, slotIndex);
                yield return this.StartCoroutine(this.WaitForAlphaBackLineCard(
                    actionId, "local-deploy", card, inventoryItemId, slotIndex));
                this.LogAlphaHandToBackLine(actionId, "COMPLETE_LOCAL_DEPLOY", card, inventoryItemId, slotIndex);
                yield break;
            }

            this.LogAlphaHandToBackLine(actionId, "FACE_DOWN_BEGIN", card, inventoryItemId, slotIndex);
            card.FaceDownUnknown();
            yield return this.StartCoroutine(this.WaitForAlphaBackLineCard(
                actionId, "face-down", card, inventoryItemId, slotIndex));
            this.LogAlphaHandToBackLine(actionId, "FACE_DOWN_END", card, inventoryItemId, slotIndex);

            this.LogAlphaHandToBackLine(actionId, "MOVE_ABOVE_LINE_BEGIN", card, inventoryItemId, slotIndex);
            card = this.cardSpawning?.MoveAlphaHandToBackLine(inventoryItemId, slotIndex);
            if (card == null)
            {
                this.LogAlphaHandToBackLine(actionId, "MOVE_ABOVE_LINE_REJECTED", null, inventoryItemId, slotIndex);
                yield break;
            }
            yield return this.StartCoroutine(this.WaitForAlphaBackLineCard(
                actionId, "move-above-line", card, inventoryItemId, slotIndex));
            this.LogAlphaHandToBackLine(actionId, "MOVE_ABOVE_LINE_END", card, inventoryItemId, slotIndex);

            this.LogAlphaHandToBackLine(actionId, "SETTLE_BEGIN", card, inventoryItemId, slotIndex);
            this.cardSpawning?.SettleAlphaHandInBackLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForAlphaBackLineCard(
                actionId, "settle", card, inventoryItemId, slotIndex));
            this.LogAlphaHandToBackLine(actionId, "COMPLETE", card, inventoryItemId, slotIndex);
        }

        private IEnumerator WaitForAlphaBackLineCard(
            string actionId,
            string phase,
            Card3DCtrl card,
            string inventoryItemId,
            int slotIndex)
        {
            this.LogAlphaHandToBackLine(actionId, phase + ":WAIT_BEGIN", card, inventoryItemId, slotIndex);
            float nextLogTime = Time.realtimeSinceStartup + this.alphaBackLineLogInterval;
            while (card != null && card.IsAnimating)
            {
                if (Time.realtimeSinceStartup >= nextLogTime)
                {
                    this.LogAlphaHandToBackLine(actionId, phase + ":WAITING", card, inventoryItemId, slotIndex);
                    nextLogTime = Time.realtimeSinceStartup + this.alphaBackLineLogInterval;
                }
                yield return null;
            }
            this.LogAlphaHandToBackLine(actionId, phase + ":WAIT_END", card, inventoryItemId, slotIndex);
        }

        private void LogAlphaHandToBackLine(
            string actionId,
            string phase,
            Card3DCtrl card,
            string inventoryItemId,
            int slotIndex)
        {
            if (!this.logAlphaHandToBackLine) return;

            CardHolderCtrl targetHolder = this.deskPosition?.GetAlphaBackLine(slotIndex);
            string cardState = card == null
                ? "card=NULL"
                : $"card={card.name}, location={card.Location}, face={card.FaceState}, "
                    + $"position={card.transform.position:F3}, holder={card.CardHolder?.name ?? "NULL"}, "
                    + $"isAnimating={card.IsAnimating}, animations=[{card.AnimationDebugState}]";
            string targetState = targetHolder == null
                ? "targetHolder=NULL"
                : $"targetHolder={targetHolder.name}, targetPosition={targetHolder.transform.position:F3}, "
                    + $"distance={GetDistanceToHolder(card, targetHolder):F3}, "
                    + $"heldCard={targetHolder.HeldCard?.name ?? "NULL"}, "
                    + $"heldCardId={targetHolder.HeldCard?.InventoryItemId ?? "NULL"}";

            Debug.Log(
                $"<color=#FFB347>[AlphaHandToBackLine]</color> action={actionId ?? "NULL"}, "
                + $"phase={phase}, id={inventoryItemId ?? "NULL"}, slot={slotIndex} | "
                + cardState + " | " + targetState,
                this.gameObject);
        }

        private static float GetDistanceToHolder(Card3DCtrl card, CardHolderCtrl holder)
        {
            if (card == null || holder == null) return -1f;
            return Vector3.Distance(card.transform.position, holder.transform.position);
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

        private Coroutine ExecuteAlphaVoidToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.AlphaVoidToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator AlphaVoidToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.MoveAlphaVoidToFrontLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleAlphaVoidInFrontLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }

        private Coroutine ExecuteOmegaVoidToFrontLine(string[] parameters)
        {
            if (!this.TryParseSourceToHand(parameters, out string inventoryItemId, out int slotIndex)) return null;
            return this.StartCoroutine(this.OmegaVoidToFrontLineRoutine(inventoryItemId, slotIndex));
        }

        private IEnumerator OmegaVoidToFrontLineRoutine(string inventoryItemId, int slotIndex)
        {
            Card3DCtrl card = this.cardSpawning?.MoveOmegaVoidToFrontLine(inventoryItemId, slotIndex);
            if (card == null) yield break;
            yield return this.StartCoroutine(this.WaitForCard(card));
            this.cardSpawning?.SettleOmegaVoidInFrontLine(card, inventoryItemId, slotIndex);
            yield return this.StartCoroutine(this.WaitForCard(card));
        }
    }
}
