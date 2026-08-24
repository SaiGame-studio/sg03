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

            // A battle-status response contains the resolved state, so an Omega
            // card can already be marked FaceUp before this queued action runs.
            // Load its revealed data first; otherwise the early return below
            // leaves its face/back art hidden until it is moved to the Void.
            beforeExpose?.Invoke(inventoryItemId);
            card.SetExpose(true);
            if (card.FaceState == FaceState.FaceUp) return null;
            return this.StartCoroutine(this.CardExposeRoutine(card));
        }

        private IEnumerator CardExposeRoutine(Card3DCtrl card)
        {
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
            return this.StartCoroutine(this.MoveLampToAlphaRoutine());
        }

        private Coroutine ExecuteLampMoveToOmega()
        {
            return this.StartCoroutine(this.MoveLampToOmegaRoutine());
        }

        private IEnumerator MoveLampToAlphaRoutine()
        {
            yield return this.StartCoroutine(this.ReturnFullDetailCardBeforeLampMove());
            if (this.lampOfSoul == null) yield break;

            this.lampOfSoul.MoveToAlpha();
            yield return this.StartCoroutine(this.WaitForLamp());
        }

        private IEnumerator MoveLampToOmegaRoutine()
        {
            yield return this.StartCoroutine(this.ReturnFullDetailCardBeforeLampMove());
            if (this.lampOfSoul == null) yield break;

            this.lampOfSoul.MoveToOmega();
            yield return this.StartCoroutine(this.WaitForLamp());
        }

        private IEnumerator ReturnFullDetailCardBeforeLampMove()
        {
            Card3DCtrl fullDetailCard = this.cardSelection?.ReturnFullDetailCard();
            if (fullDetailCard != null)
                yield return this.StartCoroutine(this.WaitForCard(fullDetailCard));
        }

        private Coroutine ExecuteOmegaEndTurn()
        {
            this.cardSelection?.ResetCharDeployCount();
            this.cardSpawning?.RefreshHpBarsAfterTurnEnd();
            this.SetHpBarVisibilityForNextTurn(Owner.alpha);
            return null;
        }

        private Coroutine ExecuteAlphaEndTurn()
        {
            this.cardSpawning?.RefreshHpBarsAfterTurnEnd();
            this.SetHpBarVisibilityForNextTurn(Owner.omega);
            return null;
        }

        private void SetHpBarVisibilityForNextTurn(Owner sideToHide)
        {
            this.HpBarHiddenOwner = sideToHide;
            this.cardSpawning?.RefreshHpBarsForTurnChange();
        }
    }
}
