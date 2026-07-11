using System.Collections;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    public partial class ClientActions
    {
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
    }
}
