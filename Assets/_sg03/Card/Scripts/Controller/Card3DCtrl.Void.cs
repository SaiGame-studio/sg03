using UnityEngine;

namespace SG03
{
    public partial class Card3DCtrl
    {
        /// <summary>
        /// Moves the card to <paramref name="holder"/>'s position and ensures face-up state.
        /// Intended for void to line transitions.
        /// </summary>
        public void MoveToLineFaceUp(CardHolderCtrl holder, bool isAlpha, System.Action onReady = null)
        {
            if (this.movement == null) return;
            this.movement.MoveToLineFaceUp(holder, () =>
            {
                if (isAlpha)
                {
                    this.RotateZ180(() =>
                    {
                        this.FaceUp();
                        onReady?.Invoke();
                        this.SpawnHpBarAfterCardSettles(holder);
                    });
                }
                else
                {
                    this.FaceUp();
                    onReady?.Invoke();
                    this.SpawnHpBarAfterCardSettles(holder);
                }
            });
        }

        /// <summary>
        /// Moves the card from The Void to the target line <paramref name="holder"/>.
        /// Delegates movement and face-state animation directly to <see cref="CardMovement.MoveVoidToLine"/>.
        /// </summary>
        public void MoveVoidToLine(CardHolderCtrl holder, bool isAlpha, bool isFaceUp = true, System.Action onReady = null)
        {
            if (this.movement == null) return;
            this.movement.MoveVoidToLine(holder, isAlpha, isFaceUp, () =>
            {
                onReady?.Invoke();
                this.SpawnHpBarAfterCardSettles(holder);
            });
        }
    }
}

